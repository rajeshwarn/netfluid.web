using Newtonsoft.Json.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
namespace Newtonsoft.Json
{
	internal class JsonTextReader : JsonReader, IJsonLineInfo
	{
		private const char UnicodeReplacementChar = '�';
		private const int MaximumJavascriptIntegerCharacterLength = 380;
		private readonly TextReader _reader;
		private char[] _chars;
		private int _charsUsed;
		private int _charPos;
		private int _lineStartPos;
		private int _lineNumber;
		private bool _isEndOfFile;
		private StringBuffer _buffer;
		private StringReference _stringReference;
		internal PropertyNameTable NameTable;
		internal int LineNumber
		{
			get
			{
				if (base.CurrentState == JsonReader.State.Start && this.LinePosition == 0)
				{
					return 0;
				}
				return this._lineNumber;
			}
		}
		internal int LinePosition
		{
			get
			{
				return this._charPos - this._lineStartPos;
			}
		}
		internal JsonTextReader(TextReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException("reader");
			}
			this._reader = reader;
			this._lineNumber = 1;
			this._chars = new char[1025];
		}
		private StringBuffer GetBuffer()
		{
			if (this._buffer == null)
			{
				this._buffer = new StringBuffer(1025);
			}
			else
			{
				this._buffer.Position = 0;
			}
			return this._buffer;
		}
		private void OnNewLine(int pos)
		{
			this._lineNumber++;
			this._lineStartPos = pos - 1;
		}
		private void ParseString(char quote)
		{
			this._charPos++;
			this.ShiftBufferIfNeeded();
			this.ReadStringIntoBuffer(quote);
			base.SetPostValueState(true);
			if (this._readType == ReadType.ReadAsBytes)
			{
				byte[] data;
				if (this._stringReference.Length == 0)
				{
					data = new byte[0];
				}
				else
				{
					data = Convert.FromBase64CharArray(this._stringReference.Chars, this._stringReference.StartIndex, this._stringReference.Length);
				}
				base.SetToken(JsonToken.Bytes, data, false);
				return;
			}
			if (this._readType == ReadType.ReadAsString)
			{
				string text = this._stringReference.ToString();
				base.SetToken(JsonToken.String, text, false);
				this._quoteChar = quote;
				return;
			}
			string text2 = this._stringReference.ToString();
			if (this._dateParseHandling != DateParseHandling.None)
			{
				DateParseHandling dateParseHandling;
				if (this._readType == ReadType.ReadAsDateTime)
				{
					dateParseHandling = DateParseHandling.DateTime;
				}
				else
				{
					if (this._readType == ReadType.ReadAsDateTimeOffset)
					{
						dateParseHandling = DateParseHandling.DateTimeOffset;
					}
					else
					{
						dateParseHandling = this._dateParseHandling;
					}
				}
				object dt;
				if (DateTimeUtils.TryParseDateTime(text2, dateParseHandling, base.DateTimeZoneHandling, base.DateFormatString, base.Culture, out dt))
				{
					base.SetToken(JsonToken.Date, dt, false);
					return;
				}
			}
			base.SetToken(JsonToken.String, text2, false);
			this._quoteChar = quote;
		}
		private static void BlockCopyChars(char[] src, int srcOffset, char[] dst, int dstOffset, int count)
		{
			Buffer.BlockCopy(src, srcOffset * 2, dst, dstOffset * 2, count * 2);
		}
		private void ShiftBufferIfNeeded()
		{
			int length = this._chars.Length;
			if ((double)(length - this._charPos) <= (double)length * 0.1)
			{
				int count = this._charsUsed - this._charPos;
				if (count > 0)
				{
					JsonTextReader.BlockCopyChars(this._chars, this._charPos, this._chars, 0, count);
				}
				this._lineStartPos -= this._charPos;
				this._charPos = 0;
				this._charsUsed = count;
				this._chars[this._charsUsed] = '\0';
			}
		}
		private int ReadData(bool append)
		{
			return this.ReadData(append, 0);
		}
		private int ReadData(bool append, int charsRequired)
		{
			if (this._isEndOfFile)
			{
				return 0;
			}
			if (this._charsUsed + charsRequired >= this._chars.Length - 1)
			{
				if (append)
				{
					int newArrayLength = Math.Max(this._chars.Length * 2, this._charsUsed + charsRequired + 1);
					char[] dst = new char[newArrayLength];
					JsonTextReader.BlockCopyChars(this._chars, 0, dst, 0, this._chars.Length);
					this._chars = dst;
				}
				else
				{
					int remainingCharCount = this._charsUsed - this._charPos;
					if (remainingCharCount + charsRequired + 1 >= this._chars.Length)
					{
						char[] dst2 = new char[remainingCharCount + charsRequired + 1];
						if (remainingCharCount > 0)
						{
							JsonTextReader.BlockCopyChars(this._chars, this._charPos, dst2, 0, remainingCharCount);
						}
						this._chars = dst2;
					}
					else
					{
						if (remainingCharCount > 0)
						{
							JsonTextReader.BlockCopyChars(this._chars, this._charPos, this._chars, 0, remainingCharCount);
						}
					}
					this._lineStartPos -= this._charPos;
					this._charPos = 0;
					this._charsUsed = remainingCharCount;
				}
			}
			int attemptCharReadCount = this._chars.Length - this._charsUsed - 1;
			int charsRead = this._reader.Read(this._chars, this._charsUsed, attemptCharReadCount);
			this._charsUsed += charsRead;
			if (charsRead == 0)
			{
				this._isEndOfFile = true;
			}
			this._chars[this._charsUsed] = '\0';
			return charsRead;
		}
		private bool EnsureChars(int relativePosition, bool append)
		{
			return this._charPos + relativePosition < this._charsUsed || this.ReadChars(relativePosition, append);
		}
		private bool ReadChars(int relativePosition, bool append)
		{
			if (this._isEndOfFile)
			{
				return false;
			}
			int charsRequired = this._charPos + relativePosition - this._charsUsed + 1;
			int totalCharsRead = 0;
			do
			{
				int charsRead = this.ReadData(append, charsRequired - totalCharsRead);
				if (charsRead == 0)
				{
					break;
				}
				totalCharsRead += charsRead;
			}
			while (totalCharsRead < charsRequired);
			return totalCharsRead >= charsRequired;
		}
		[DebuggerStepThrough]
		internal override bool Read()
		{
			this._readType = ReadType.Read;
			if (!this.ReadInternal())
			{
				base.SetToken(JsonToken.None);
				return false;
			}
			return true;
		}
		internal override byte[] ReadAsBytes()
		{
			return base.ReadAsBytesInternal();
		}
		internal override decimal? ReadAsDecimal()
		{
			return base.ReadAsDecimalInternal();
		}
		internal override int? ReadAsInt32()
		{
			return base.ReadAsInt32Internal();
		}
		internal override string ReadAsString()
		{
			return base.ReadAsStringInternal();
		}
		internal override DateTime? ReadAsDateTime()
		{
			return base.ReadAsDateTimeInternal();
		}
		internal override DateTimeOffset? ReadAsDateTimeOffset()
		{
			return base.ReadAsDateTimeOffsetInternal();
		}
		internal override bool ReadInternal()
		{
			while (true)
			{
				switch (this._currentState)
				{
				case JsonReader.State.Start:
				case JsonReader.State.Property:
				case JsonReader.State.ArrayStart:
				case JsonReader.State.Array:
				case JsonReader.State.ConstructorStart:
				case JsonReader.State.Constructor:
					goto IL_43;
				case JsonReader.State.ObjectStart:
				case JsonReader.State.Object:
					goto IL_4A;
				case JsonReader.State.PostValue:
					if (this.ParsePostValue())
					{
						return true;
					}
					continue;
				case JsonReader.State.Finished:
					goto IL_5B;
				}
				break;
			}
			goto IL_BA;
			IL_43:
			return this.ParseValue();
			IL_4A:
			return this.ParseObject();
			IL_5B:
			if (!this.EnsureChars(0, false))
			{
				return false;
			}
			this.EatWhitespace(false);
			if (this._isEndOfFile)
			{
				return false;
			}
			if (this._chars[this._charPos] == '/')
			{
				this.ParseComment();
				return true;
			}
			throw JsonReaderException.Create(this, "Additional text encountered after finished reading JSON content: {0}.".FormatWith(CultureInfo.InvariantCulture, this._chars[this._charPos]));
			IL_BA:
			throw JsonReaderException.Create(this, "Unexpected state: {0}.".FormatWith(CultureInfo.InvariantCulture, base.CurrentState));
		}
		private void ReadStringIntoBuffer(char quote)
		{
			int charPos = this._charPos;
			int initialPosition = this._charPos;
			int lastWritePosition = this._charPos;
			StringBuffer buffer = null;
			char currentChar;
			while (true)
			{
				char c = this._chars[charPos++];
				if (c <= '\r')
				{
					if (c != '\0')
					{
						if (c != '\n')
						{
							if (c == '\r')
							{
								this._charPos = charPos - 1;
								this.ProcessCarriageReturn(true);
								charPos = this._charPos;
							}
						}
						else
						{
							this._charPos = charPos - 1;
							this.ProcessLineFeed();
							charPos = this._charPos;
						}
					}
					else
					{
						if (this._charsUsed == charPos - 1)
						{
							charPos--;
							if (this.ReadData(true) == 0)
							{
								break;
							}
						}
					}
				}
				else
				{
					if (c != '"' && c != '\'')
					{
						if (c == '\\')
						{
							this._charPos = charPos;
							if (!this.EnsureChars(0, true))
							{
								goto Block_10;
							}
							int escapeStartPos = charPos - 1;
							currentChar = this._chars[charPos];
							char c2 = currentChar;
							char writeChar;
							if (c2 <= '\\')
							{
								if (c2 <= '\'')
								{
									if (c2 != '"' && c2 != '\'')
									{
										goto Block_14;
									}
								}
								else
								{
									if (c2 != '/')
									{
										if (c2 != '\\')
										{
											goto Block_16;
										}
										charPos++;
										writeChar = '\\';
										goto IL_2BF;
									}
								}
								writeChar = currentChar;
								charPos++;
							}
							else
							{
								if (c2 <= 'f')
								{
									if (c2 != 'b')
									{
										if (c2 != 'f')
										{
											goto Block_19;
										}
										charPos++;
										writeChar = '\f';
									}
									else
									{
										charPos++;
										writeChar = '\b';
									}
								}
								else
								{
									if (c2 != 'n')
									{
										switch (c2)
										{
										case 'r':
											charPos++;
											writeChar = '\r';
											goto IL_2BF;
										case 't':
											charPos++;
											writeChar = '\t';
											goto IL_2BF;
										case 'u':
											charPos++;
											this._charPos = charPos;
											writeChar = this.ParseUnicode();
											if (StringUtils.IsLowSurrogate(writeChar))
											{
												writeChar = '�';
											}
											else
											{
												if (StringUtils.IsHighSurrogate(writeChar))
												{
													bool anotherHighSurrogate;
													do
													{
														anotherHighSurrogate = false;
														if (this.EnsureChars(2, true) && this._chars[this._charPos] == '\\' && this._chars[this._charPos + 1] == 'u')
														{
															char highSurrogate = writeChar;
															this._charPos += 2;
															writeChar = this.ParseUnicode();
															if (!StringUtils.IsLowSurrogate(writeChar))
															{
																if (StringUtils.IsHighSurrogate(writeChar))
																{
																	highSurrogate = '�';
																	anotherHighSurrogate = true;
																}
																else
																{
																	highSurrogate = '�';
																}
															}
															if (buffer == null)
															{
																buffer = this.GetBuffer();
															}
															this.WriteCharToBuffer(buffer, highSurrogate, lastWritePosition, escapeStartPos);
															lastWritePosition = this._charPos;
														}
														else
														{
															writeChar = '�';
														}
													}
													while (anotherHighSurrogate);
												}
											}
											charPos = this._charPos;
											goto IL_2BF;
										}
										goto Block_21;
									}
									charPos++;
									writeChar = '\n';
								}
							}
							IL_2BF:
							if (buffer == null)
							{
								buffer = this.GetBuffer();
							}
							this.WriteCharToBuffer(buffer, writeChar, lastWritePosition, escapeStartPos);
							lastWritePosition = charPos;
						}
					}
					else
					{
						if (this._chars[charPos - 1] == quote)
						{
							goto Block_30;
						}
					}
				}
			}
			this._charPos = charPos;
			throw JsonReaderException.Create(this, "Unterminated string. Expected delimiter: {0}.".FormatWith(CultureInfo.InvariantCulture, quote));
			Block_10:
			this._charPos = charPos;
			throw JsonReaderException.Create(this, "Unterminated string. Expected delimiter: {0}.".FormatWith(CultureInfo.InvariantCulture, quote));
			Block_14:
			Block_16:
			Block_19:
			Block_21:
			charPos++;
			this._charPos = charPos;
			throw JsonReaderException.Create(this, "Bad JSON escape sequence: {0}.".FormatWith(CultureInfo.InvariantCulture, "\\" + currentChar));
			Block_30:
			charPos--;
			if (initialPosition == lastWritePosition)
			{
				this._stringReference = new StringReference(this._chars, initialPosition, charPos - initialPosition);
			}
			else
			{
				if (buffer == null)
				{
					buffer = this.GetBuffer();
				}
				if (charPos > lastWritePosition)
				{
					buffer.Append(this._chars, lastWritePosition, charPos - lastWritePosition);
				}
				this._stringReference = new StringReference(buffer.GetInternalBuffer(), 0, buffer.Position);
			}
			charPos++;
			this._charPos = charPos;
		}
		private void WriteCharToBuffer(StringBuffer buffer, char writeChar, int lastWritePosition, int writeToPosition)
		{
			if (writeToPosition > lastWritePosition)
			{
				buffer.Append(this._chars, lastWritePosition, writeToPosition - lastWritePosition);
			}
			buffer.Append(writeChar);
		}
		private char ParseUnicode()
		{
			if (this.EnsureChars(4, true))
			{
				string hexValues = new string(this._chars, this._charPos, 4);
				char hexChar = Convert.ToChar(int.Parse(hexValues, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo));
				char writeChar = hexChar;
				this._charPos += 4;
				return writeChar;
			}
			throw JsonReaderException.Create(this, "Unexpected end while parsing unicode character.");
		}
		private void ReadNumberIntoBuffer()
		{
			int charPos = this._charPos;
			while (true)
			{
				char c = this._chars[charPos];
				if (c <= 'F')
				{
					if (c != '\0')
					{
						switch (c)
						{
						case '+':
						case '-':
						case '.':
						case '0':
						case '1':
						case '2':
						case '3':
						case '4':
						case '5':
						case '6':
						case '7':
						case '8':
						case '9':
						case 'A':
						case 'B':
						case 'C':
						case 'D':
						case 'E':
						case 'F':
							goto IL_E4;
						}
						break;
					}
					this._charPos = charPos;
					if (this._charsUsed != charPos || this.ReadData(true) == 0)
					{
						return;
					}
					continue;
				}
				else
				{
					if (c != 'X')
					{
						switch (c)
						{
						case 'a':
						case 'b':
						case 'c':
						case 'd':
						case 'e':
						case 'f':
							break;
						default:
							if (c != 'x')
							{
								goto Block_6;
							}
							break;
						}
					}
				}
				IL_E4:
				charPos++;
			}
			Block_6:
			this._charPos = charPos;
			char currentChar = this._chars[this._charPos];
			if (char.IsWhiteSpace(currentChar) || currentChar == ',' || currentChar == '}' || currentChar == ']' || currentChar == ')' || currentChar == '/')
			{
				return;
			}
			throw JsonReaderException.Create(this, "Unexpected character encountered while parsing number: {0}.".FormatWith(CultureInfo.InvariantCulture, currentChar));
		}
		private void ClearRecentString()
		{
			if (this._buffer != null)
			{
				this._buffer.Position = 0;
			}
			this._stringReference = default(StringReference);
		}
		private bool ParsePostValue()
		{
			char currentChar;
			while (true)
			{
				currentChar = this._chars[this._charPos];
				char c = currentChar;
				if (c <= ')')
				{
					if (c <= '\r')
					{
						if (c != '\0')
						{
							switch (c)
							{
							case '\t':
								break;
							case '\n':
								this.ProcessLineFeed();
								continue;
							case '\v':
							case '\f':
								goto IL_145;
							case '\r':
								this.ProcessCarriageReturn(false);
								continue;
							default:
								goto IL_145;
							}
						}
						else
						{
							if (this._charsUsed != this._charPos)
							{
								this._charPos++;
								continue;
							}
							if (this.ReadData(false) == 0)
							{
								break;
							}
							continue;
						}
					}
					else
					{
						if (c != ' ')
						{
							if (c != ')')
							{
								goto IL_145;
							}
							goto IL_E5;
						}
					}
					this._charPos++;
					continue;
				}
				if (c <= '/')
				{
					if (c == ',')
					{
						goto IL_105;
					}
					if (c == '/')
					{
						goto IL_FD;
					}
				}
				else
				{
					if (c == ']')
					{
						goto IL_CD;
					}
					if (c == '}')
					{
						goto IL_B5;
					}
				}
				IL_145:
				if (!char.IsWhiteSpace(currentChar))
				{
					goto IL_160;
				}
				this._charPos++;
			}
			this._currentState = JsonReader.State.Finished;
			return false;
			IL_B5:
			this._charPos++;
			base.SetToken(JsonToken.EndObject);
			return true;
			IL_CD:
			this._charPos++;
			base.SetToken(JsonToken.EndArray);
			return true;
			IL_E5:
			this._charPos++;
			base.SetToken(JsonToken.EndConstructor);
			return true;
			IL_FD:
			this.ParseComment();
			return true;
			IL_105:
			this._charPos++;
			base.SetStateBasedOnCurrent();
			return false;
			IL_160:
			throw JsonReaderException.Create(this, "After parsing a value an unexpected character was encountered: {0}.".FormatWith(CultureInfo.InvariantCulture, currentChar));
		}
		private bool ParseObject()
		{
			while (true)
			{
				char currentChar = this._chars[this._charPos];
				char c = currentChar;
				if (c <= '\r')
				{
					if (c != '\0')
					{
						switch (c)
						{
						case '\t':
							break;
						case '\n':
							this.ProcessLineFeed();
							continue;
						case '\v':
						case '\f':
							goto IL_BF;
						case '\r':
							this.ProcessCarriageReturn(false);
							continue;
						default:
							goto IL_BF;
						}
					}
					else
					{
						if (this._charsUsed != this._charPos)
						{
							this._charPos++;
							continue;
						}
						if (this.ReadData(false) == 0)
						{
							break;
						}
						continue;
					}
				}
				else
				{
					if (c != ' ')
					{
						if (c == '/')
						{
							goto IL_8D;
						}
						if (c != '}')
						{
							goto IL_BF;
						}
						goto IL_75;
					}
				}
				this._charPos++;
				continue;
				IL_BF:
				if (!char.IsWhiteSpace(currentChar))
				{
					goto IL_DA;
				}
				this._charPos++;
			}
			return false;
			IL_75:
			base.SetToken(JsonToken.EndObject);
			this._charPos++;
			return true;
			IL_8D:
			this.ParseComment();
			return true;
			IL_DA:
			return this.ParseProperty();
		}
		private bool ParseProperty()
		{
			char firstChar = this._chars[this._charPos];
			char quoteChar;
			if (firstChar == '"' || firstChar == '\'')
			{
				this._charPos++;
				quoteChar = firstChar;
				this.ShiftBufferIfNeeded();
				this.ReadStringIntoBuffer(quoteChar);
			}
			else
			{
				if (!this.ValidIdentifierChar(firstChar))
				{
					throw JsonReaderException.Create(this, "Invalid property identifier character: {0}.".FormatWith(CultureInfo.InvariantCulture, this._chars[this._charPos]));
				}
				quoteChar = '\0';
				this.ShiftBufferIfNeeded();
				this.ParseUnquotedProperty();
			}
			string propertyName;
			if (this.NameTable != null)
			{
				propertyName = this.NameTable.Get(this._stringReference.Chars, this._stringReference.StartIndex, this._stringReference.Length);
				if (propertyName == null)
				{
					propertyName = this._stringReference.ToString();
				}
			}
			else
			{
				propertyName = this._stringReference.ToString();
			}
			this.EatWhitespace(false);
			if (this._chars[this._charPos] != ':')
			{
				throw JsonReaderException.Create(this, "Invalid character after parsing property name. Expected ':' but got: {0}.".FormatWith(CultureInfo.InvariantCulture, this._chars[this._charPos]));
			}
			this._charPos++;
			base.SetToken(JsonToken.PropertyName, propertyName);
			this._quoteChar = quoteChar;
			this.ClearRecentString();
			return true;
		}
		private bool ValidIdentifierChar(char value)
		{
			return char.IsLetterOrDigit(value) || value == '_' || value == '$';
		}
		private void ParseUnquotedProperty()
		{
			int initialPosition = this._charPos;
			char currentChar;
			while (true)
			{
				char c = this._chars[this._charPos];
				if (c == '\0')
				{
					if (this._charsUsed != this._charPos)
					{
						goto IL_3C;
					}
					if (this.ReadData(true) == 0)
					{
						break;
					}
				}
				else
				{
					currentChar = this._chars[this._charPos];
					if (!this.ValidIdentifierChar(currentChar))
					{
						goto IL_7E;
					}
					this._charPos++;
				}
			}
			throw JsonReaderException.Create(this, "Unexpected end while parsing unquoted property name.");
			IL_3C:
			this._stringReference = new StringReference(this._chars, initialPosition, this._charPos - initialPosition);
			return;
			IL_7E:
			if (char.IsWhiteSpace(currentChar) || currentChar == ':')
			{
				this._stringReference = new StringReference(this._chars, initialPosition, this._charPos - initialPosition);
				return;
			}
			throw JsonReaderException.Create(this, "Invalid JavaScript property identifier character: {0}.".FormatWith(CultureInfo.InvariantCulture, currentChar));
		}
		private bool ParseValue()
		{
			char currentChar;
			while (true)
			{
				currentChar = this._chars[this._charPos];
				char c = currentChar;
				if (c <= 'I')
				{
					if (c <= '\r')
					{
						if (c != '\0')
						{
							switch (c)
							{
							case '\t':
								break;
							case '\n':
								this.ProcessLineFeed();
								continue;
							case '\v':
							case '\f':
								goto IL_272;
							case '\r':
								this.ProcessCarriageReturn(false);
								continue;
							default:
								goto IL_272;
							}
						}
						else
						{
							if (this._charsUsed != this._charPos)
							{
								this._charPos++;
								continue;
							}
							if (this.ReadData(false) == 0)
							{
								break;
							}
							continue;
						}
					}
					else
					{
						switch (c)
						{
						case ' ':
							break;
						case '!':
							goto IL_272;
						case '"':
							goto IL_110;
						default:
							switch (c)
							{
							case '\'':
								goto IL_110;
							case '(':
							case '*':
							case '+':
							case '.':
								goto IL_272;
							case ')':
								goto IL_230;
							case ',':
								goto IL_226;
							case '-':
								goto IL_1A3;
							case '/':
								goto IL_1D0;
							default:
								if (c != 'I')
								{
									goto IL_272;
								}
								goto IL_19B;
							}
							break;
						}
					}
					this._charPos++;
					continue;
				}
				if (c <= 'f')
				{
					if (c == 'N')
					{
						goto IL_193;
					}
					switch (c)
					{
					case '[':
						goto IL_1F7;
					case '\\':
						break;
					case ']':
						goto IL_20E;
					default:
						if (c == 'f')
						{
							goto IL_121;
						}
						break;
					}
				}
				else
				{
					if (c == 'n')
					{
						goto IL_129;
					}
					switch (c)
					{
					case 't':
						goto IL_119;
					case 'u':
						goto IL_1D8;
					default:
						if (c == '{')
						{
							goto IL_1E0;
						}
						break;
					}
				}
				IL_272:
				if (!char.IsWhiteSpace(currentChar))
				{
					goto IL_28D;
				}
				this._charPos++;
			}
			return false;
			IL_110:
			this.ParseString(currentChar);
			return true;
			IL_119:
			this.ParseTrue();
			return true;
			IL_121:
			this.ParseFalse();
			return true;
			IL_129:
			if (this.EnsureChars(1, true))
			{
				char next = this._chars[this._charPos + 1];
				if (next == 'u')
				{
					this.ParseNull();
				}
				else
				{
					if (next != 'e')
					{
						throw JsonReaderException.Create(this, "Unexpected character encountered while parsing value: {0}.".FormatWith(CultureInfo.InvariantCulture, this._chars[this._charPos]));
					}
					this.ParseConstructor();
				}
				return true;
			}
			throw JsonReaderException.Create(this, "Unexpected end.");
			IL_193:
			this.ParseNumberNaN();
			return true;
			IL_19B:
			this.ParseNumberPositiveInfinity();
			return true;
			IL_1A3:
			if (this.EnsureChars(1, true) && this._chars[this._charPos + 1] == 'I')
			{
				this.ParseNumberNegativeInfinity();
			}
			else
			{
				this.ParseNumber();
			}
			return true;
			IL_1D0:
			this.ParseComment();
			return true;
			IL_1D8:
			this.ParseUndefined();
			return true;
			IL_1E0:
			this._charPos++;
			base.SetToken(JsonToken.StartObject);
			return true;
			IL_1F7:
			this._charPos++;
			base.SetToken(JsonToken.StartArray);
			return true;
			IL_20E:
			this._charPos++;
			base.SetToken(JsonToken.EndArray);
			return true;
			IL_226:
			base.SetToken(JsonToken.Undefined);
			return true;
			IL_230:
			this._charPos++;
			base.SetToken(JsonToken.EndConstructor);
			return true;
			IL_28D:
			if (char.IsNumber(currentChar) || currentChar == '-' || currentChar == '.')
			{
				this.ParseNumber();
				return true;
			}
			throw JsonReaderException.Create(this, "Unexpected character encountered while parsing value: {0}.".FormatWith(CultureInfo.InvariantCulture, currentChar));
		}
		private void ProcessLineFeed()
		{
			this._charPos++;
			this.OnNewLine(this._charPos);
		}
		private void ProcessCarriageReturn(bool append)
		{
			this._charPos++;
			if (this.EnsureChars(1, append) && this._chars[this._charPos] == '\n')
			{
				this._charPos++;
			}
			this.OnNewLine(this._charPos);
		}
		private bool EatWhitespace(bool oneOrMore)
		{
			bool finished = false;
			bool ateWhitespace = false;
			while (!finished)
			{
				char currentChar = this._chars[this._charPos];
				char c = currentChar;
				if (c != '\0')
				{
					if (c != '\n')
					{
						if (c != '\r')
						{
							if (currentChar == ' ' || char.IsWhiteSpace(currentChar))
							{
								ateWhitespace = true;
								this._charPos++;
							}
							else
							{
								finished = true;
							}
						}
						else
						{
							this.ProcessCarriageReturn(false);
						}
					}
					else
					{
						this.ProcessLineFeed();
					}
				}
				else
				{
					if (this._charsUsed == this._charPos)
					{
						if (this.ReadData(false) == 0)
						{
							finished = true;
						}
					}
					else
					{
						this._charPos++;
					}
				}
			}
			return !oneOrMore || ateWhitespace;
		}
		private void ParseConstructor()
		{
			if (!this.MatchValueWithTrailingSeparator("new"))
			{
				throw JsonReaderException.Create(this, "Unexpected content while parsing JSON.");
			}
			this.EatWhitespace(false);
			int initialPosition = this._charPos;
			char currentChar;
			while (true)
			{
				currentChar = this._chars[this._charPos];
				if (currentChar == '\0')
				{
					if (this._charsUsed != this._charPos)
					{
						goto IL_53;
					}
					if (this.ReadData(true) == 0)
					{
						break;
					}
				}
				else
				{
					if (!char.IsLetterOrDigit(currentChar))
					{
						goto IL_85;
					}
					this._charPos++;
				}
			}
			throw JsonReaderException.Create(this, "Unexpected end while parsing constructor.");
			IL_53:
			int endPosition = this._charPos;
			this._charPos++;
			goto IL_F7;
			IL_85:
			if (currentChar == '\r')
			{
				endPosition = this._charPos;
				this.ProcessCarriageReturn(true);
			}
			else
			{
				if (currentChar == '\n')
				{
					endPosition = this._charPos;
					this.ProcessLineFeed();
				}
				else
				{
					if (char.IsWhiteSpace(currentChar))
					{
						endPosition = this._charPos;
						this._charPos++;
					}
					else
					{
						if (currentChar != '(')
						{
							throw JsonReaderException.Create(this, "Unexpected character while parsing constructor: {0}.".FormatWith(CultureInfo.InvariantCulture, currentChar));
						}
						endPosition = this._charPos;
					}
				}
			}
			IL_F7:
			this._stringReference = new StringReference(this._chars, initialPosition, endPosition - initialPosition);
			string constructorName = this._stringReference.ToString();
			this.EatWhitespace(false);
			if (this._chars[this._charPos] != '(')
			{
				throw JsonReaderException.Create(this, "Unexpected character while parsing constructor: {0}.".FormatWith(CultureInfo.InvariantCulture, this._chars[this._charPos]));
			}
			this._charPos++;
			this.ClearRecentString();
			base.SetToken(JsonToken.StartConstructor, constructorName);
		}
		private void ParseNumber()
		{
			this.ShiftBufferIfNeeded();
			char firstChar = this._chars[this._charPos];
			int initialPosition = this._charPos;
			this.ReadNumberIntoBuffer();
			base.SetPostValueState(true);
			this._stringReference = new StringReference(this._chars, initialPosition, this._charPos - initialPosition);
			bool singleDigit = char.IsDigit(firstChar) && this._stringReference.Length == 1;
			bool nonBase10 = firstChar == '0' && this._stringReference.Length > 1 && this._stringReference.Chars[this._stringReference.StartIndex + 1] != '.' && this._stringReference.Chars[this._stringReference.StartIndex + 1] != 'e' && this._stringReference.Chars[this._stringReference.StartIndex + 1] != 'E';
			object numberValue;
			JsonToken numberType;
			if (this._readType == ReadType.ReadAsInt32)
			{
				if (singleDigit)
				{
					numberValue = (int)(firstChar - '0');
				}
				else
				{
					if (nonBase10)
					{
						string number = this._stringReference.ToString();
						try
						{
							int integer = number.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt32(number, 16) : Convert.ToInt32(number, 8);
							numberValue = integer;
							goto IL_1DE;
						}
						catch (Exception ex)
						{
							throw JsonReaderException.Create(this, "Input string '{0}' is not a valid integer.".FormatWith(CultureInfo.InvariantCulture, number), ex);
						}
					}
					int value;
					ParseResult parseResult = ConvertUtils.Int32TryParse(this._stringReference.Chars, this._stringReference.StartIndex, this._stringReference.Length, out value);
					if (parseResult == ParseResult.Success)
					{
						numberValue = value;
					}
					else
					{
						if (parseResult == ParseResult.Overflow)
						{
							throw JsonReaderException.Create(this, "JSON integer {0} is too large or small for an Int32.".FormatWith(CultureInfo.InvariantCulture, this._stringReference.ToString()));
						}
						throw JsonReaderException.Create(this, "Input string '{0}' is not a valid integer.".FormatWith(CultureInfo.InvariantCulture, this._stringReference.ToString()));
					}
				}
				IL_1DE:
				numberType = JsonToken.Integer;
			}
			else
			{
				if (this._readType == ReadType.ReadAsDecimal)
				{
					if (singleDigit)
					{
						numberValue = firstChar - 48m;
					}
					else
					{
						if (nonBase10)
						{
							string number2 = this._stringReference.ToString();
							try
							{
								long integer2 = number2.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt64(number2, 16) : Convert.ToInt64(number2, 8);
								numberValue = Convert.ToDecimal(integer2);
								goto IL_2D1;
							}
							catch (Exception ex2)
							{
								throw JsonReaderException.Create(this, "Input string '{0}' is not a valid decimal.".FormatWith(CultureInfo.InvariantCulture, number2), ex2);
							}
						}
						string number3 = this._stringReference.ToString();
						decimal value2;
						if (!decimal.TryParse(number3, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out value2))
						{
							throw JsonReaderException.Create(this, "Input string '{0}' is not a valid decimal.".FormatWith(CultureInfo.InvariantCulture, this._stringReference.ToString()));
						}
						numberValue = value2;
					}
					IL_2D1:
					numberType = JsonToken.Float;
				}
				else
				{
					if (singleDigit)
					{
						numberValue = (long)((ulong)firstChar - 48uL);
						numberType = JsonToken.Integer;
					}
					else
					{
						if (nonBase10)
						{
							string number4 = this._stringReference.ToString();
							try
							{
								numberValue = (number4.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt64(number4, 16) : Convert.ToInt64(number4, 8));
							}
							catch (Exception ex3)
							{
								throw JsonReaderException.Create(this, "Input string '{0}' is not a valid number.".FormatWith(CultureInfo.InvariantCulture, number4), ex3);
							}
							numberType = JsonToken.Integer;
						}
						else
						{
							long value3;
							ParseResult parseResult2 = ConvertUtils.Int64TryParse(this._stringReference.Chars, this._stringReference.StartIndex, this._stringReference.Length, out value3);
							if (parseResult2 == ParseResult.Success)
							{
								numberValue = value3;
								numberType = JsonToken.Integer;
							}
							else
							{
								if (parseResult2 == ParseResult.Overflow)
								{
									throw JsonReaderException.Create(this, "JSON integer {0} is too large or small for an Int64.".FormatWith(CultureInfo.InvariantCulture, this._stringReference.ToString()));
								}
								string number5 = this._stringReference.ToString();
								if (this._floatParseHandling == FloatParseHandling.Decimal)
								{
									decimal d;
									if (!decimal.TryParse(number5, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out d))
									{
										throw JsonReaderException.Create(this, "Input string '{0}' is not a valid decimal.".FormatWith(CultureInfo.InvariantCulture, number5));
									}
									numberValue = d;
								}
								else
								{
									double d2;
									if (!double.TryParse(number5, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out d2))
									{
										throw JsonReaderException.Create(this, "Input string '{0}' is not a valid number.".FormatWith(CultureInfo.InvariantCulture, number5));
									}
									numberValue = d2;
								}
								numberType = JsonToken.Float;
							}
						}
					}
				}
			}
			this.ClearRecentString();
			base.SetToken(numberType, numberValue, false);
		}
		private void ParseComment()
		{
			this._charPos++;
			if (!this.EnsureChars(1, false))
			{
				throw JsonReaderException.Create(this, "Unexpected end while parsing comment.");
			}
			bool singlelineComment;
			if (this._chars[this._charPos] == '*')
			{
				singlelineComment = false;
			}
			else
			{
				if (this._chars[this._charPos] != '/')
				{
					throw JsonReaderException.Create(this, "Error parsing comment. Expected: *, got {0}.".FormatWith(CultureInfo.InvariantCulture, this._chars[this._charPos]));
				}
				singlelineComment = true;
			}
			this._charPos++;
			int initialPosition = this._charPos;
			bool commentFinished = false;
			while (!commentFinished)
			{
				char c = this._chars[this._charPos];
				if (c <= '\n')
				{
					if (c != '\0')
					{
						if (c == '\n')
						{
							if (singlelineComment)
							{
								this._stringReference = new StringReference(this._chars, initialPosition, this._charPos - initialPosition);
								commentFinished = true;
							}
							this.ProcessLineFeed();
							continue;
						}
					}
					else
					{
						if (this._charsUsed != this._charPos)
						{
							this._charPos++;
							continue;
						}
						if (this.ReadData(true) != 0)
						{
							continue;
						}
						if (!singlelineComment)
						{
							throw JsonReaderException.Create(this, "Unexpected end while parsing comment.");
						}
						this._stringReference = new StringReference(this._chars, initialPosition, this._charPos - initialPosition);
						commentFinished = true;
						continue;
					}
				}
				else
				{
					if (c == '\r')
					{
						if (singlelineComment)
						{
							this._stringReference = new StringReference(this._chars, initialPosition, this._charPos - initialPosition);
							commentFinished = true;
						}
						this.ProcessCarriageReturn(true);
						continue;
					}
					if (c == '*')
					{
						this._charPos++;
						if (!singlelineComment && this.EnsureChars(0, true) && this._chars[this._charPos] == '/')
						{
							this._stringReference = new StringReference(this._chars, initialPosition, this._charPos - initialPosition - 1);
							this._charPos++;
							commentFinished = true;
							continue;
						}
						continue;
					}
				}
				this._charPos++;
			}
			base.SetToken(JsonToken.Comment, this._stringReference.ToString());
			this.ClearRecentString();
		}
		private bool MatchValue(string value)
		{
			if (!this.EnsureChars(value.Length - 1, true))
			{
				return false;
			}
			for (int i = 0; i < value.Length; i++)
			{
				if (this._chars[this._charPos + i] != value[i])
				{
					return false;
				}
			}
			this._charPos += value.Length;
			return true;
		}
		private bool MatchValueWithTrailingSeparator(string value)
		{
			return this.MatchValue(value) && (!this.EnsureChars(0, false) || this.IsSeparator(this._chars[this._charPos]) || this._chars[this._charPos] == '\0');
		}
		private bool IsSeparator(char c)
		{
			if (c <= ')')
			{
				switch (c)
				{
				case '\t':
				case '\n':
				case '\r':
					break;
				case '\v':
				case '\f':
					goto IL_8E;
				default:
					if (c != ' ')
					{
						if (c != ')')
						{
							goto IL_8E;
						}
						if (base.CurrentState == JsonReader.State.Constructor || base.CurrentState == JsonReader.State.ConstructorStart)
						{
							return true;
						}
						return false;
					}
					break;
				}
				return true;
			}
			if (c <= '/')
			{
				if (c != ',')
				{
					if (c != '/')
					{
						goto IL_8E;
					}
					if (!this.EnsureChars(1, false))
					{
						return false;
					}
					char nextChart = this._chars[this._charPos + 1];
					return nextChart == '*' || nextChart == '/';
				}
			}
			else
			{
				if (c != ']' && c != '}')
				{
					goto IL_8E;
				}
			}
			return true;
			IL_8E:
			if (char.IsWhiteSpace(c))
			{
				return true;
			}
			return false;
		}
		private void ParseTrue()
		{
			if (this.MatchValueWithTrailingSeparator(JsonConvert.True))
			{
				base.SetToken(JsonToken.Boolean, true);
				return;
			}
			throw JsonReaderException.Create(this, "Error parsing boolean value.");
		}
		private void ParseNull()
		{
			if (this.MatchValueWithTrailingSeparator(JsonConvert.Null))
			{
				base.SetToken(JsonToken.Null);
				return;
			}
			throw JsonReaderException.Create(this, "Error parsing null value.");
		}
		private void ParseUndefined()
		{
			if (this.MatchValueWithTrailingSeparator(JsonConvert.Undefined))
			{
				base.SetToken(JsonToken.Undefined);
				return;
			}
			throw JsonReaderException.Create(this, "Error parsing undefined value.");
		}
		private void ParseFalse()
		{
			if (this.MatchValueWithTrailingSeparator(JsonConvert.False))
			{
				base.SetToken(JsonToken.Boolean, false);
				return;
			}
			throw JsonReaderException.Create(this, "Error parsing boolean value.");
		}
		private void ParseNumberNegativeInfinity()
		{
			if (!this.MatchValueWithTrailingSeparator(JsonConvert.NegativeInfinity))
			{
				throw JsonReaderException.Create(this, "Error parsing negative infinity value.");
			}
			if (this._floatParseHandling == FloatParseHandling.Decimal)
			{
				throw new JsonReaderException("Cannot read -Infinity as a decimal.");
			}
			base.SetToken(JsonToken.Float, double.NegativeInfinity);
		}
		private void ParseNumberPositiveInfinity()
		{
			if (!this.MatchValueWithTrailingSeparator(JsonConvert.PositiveInfinity))
			{
				throw JsonReaderException.Create(this, "Error parsing positive infinity value.");
			}
			if (this._floatParseHandling == FloatParseHandling.Decimal)
			{
				throw new JsonReaderException("Cannot read Infinity as a decimal.");
			}
			base.SetToken(JsonToken.Float, double.PositiveInfinity);
		}
		private void ParseNumberNaN()
		{
			if (!this.MatchValueWithTrailingSeparator(JsonConvert.NaN))
			{
				throw JsonReaderException.Create(this, "Error parsing NaN value.");
			}
			if (this._floatParseHandling == FloatParseHandling.Decimal)
			{
				throw new JsonReaderException("Cannot read NaN as a decimal.");
			}
			base.SetToken(JsonToken.Float, double.NaN);
		}
		internal override void Close()
		{
			base.Close();
			if (base.CloseInput && this._reader != null)
			{
				this._reader.Dispose();
			}
			if (this._buffer != null)
			{
				this._buffer.Clear();
			}
		}
		internal bool HasLineInfo()
		{
			return true;
		}
	}
}
