//
// MimeMessage.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013-2014 Xamarin Inc. (www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
using MimeKit.IO;
using MimeKit.Utils;

namespace MimeKit
{
    /// <summary>
    ///     A MIME message.
    /// </summary>
    public sealed class MimeMessage
    {
        private static readonly StringComparer icase = StringComparer.OrdinalIgnoreCase;

        private static readonly string[] StandardAddressHeaders =
        {
            "Resent-From", "Resent-Reply-To", "Resent-To", "Resent-Cc", "Resent-Bcc",
            "From", "Reply-To", "To", "Cc", "Bcc"
        };

        private readonly Dictionary<string, InternetAddressList> addresses;
        private readonly MessageIdList references;
        private DateTimeOffset date;
        private string inreplyto;
        private string messageId;
        private DateTimeOffset resentDate;
        private string resentMessageId;
        private MailboxAddress resentSender;
        private MailboxAddress sender;
        private Version version;

        internal MimeMessage(ParserOptions options, IEnumerable<Header> headers)
        {
            addresses = new Dictionary<string, InternetAddressList>(icase);
            Headers = new HeaderList(options);

            // initialize our address lists
            foreach (string name in StandardAddressHeaders)
            {
                var list = new InternetAddressList();
                list.Changed += InternetAddressListChanged;
                addresses.Add(name, list);
            }

            references = new MessageIdList();
            references.Changed += ReferencesChanged;
            inreplyto = null;

            Headers.Changed += HeadersChanged;

            // add all of our message headers...
            foreach (Header header in headers)
            {
                if (header.Field.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                    continue;

                Headers.Add(header);
            }
        }

        internal MimeMessage(ParserOptions options)
        {
            addresses = new Dictionary<string, InternetAddressList>(icase);
            Headers = new HeaderList(options);

            // initialize our address lists
            foreach (string name in StandardAddressHeaders)
            {
                var list = new InternetAddressList();
                list.Changed += InternetAddressListChanged;
                addresses.Add(name, list);
            }

            references = new MessageIdList();
            references.Changed += ReferencesChanged;
            inreplyto = null;

            Headers.Changed += HeadersChanged;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MimeKit.MimeMessage" /> class.
        ///     <param name="args">An array of initialization parameters: headers and message parts.</param>
        /// </summary>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="args" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     <para><paramref name="args" /> contains more than one <see cref="MimeKit.MimeEntity" />.</para>
        ///     <para>-or-</para>
        ///     <para><paramref name="args" /> contains one or more arguments of an unknown type.</para>
        /// </exception>
        public MimeMessage(params object[] args) : this(ParserOptions.Default.Clone())
        {
            if (args == null)
                throw new ArgumentNullException("args");

            MimeEntity body = null;

            foreach (object obj in args)
            {
                if (obj == null)
                    continue;

                // Just add the headers and let the events (already setup) keep the
                // addresses in sync.

                var header = obj as Header;
                if (header != null)
                {
                    if (!header.Field.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                        Headers.Add(header);

                    continue;
                }

                var headers = obj as IEnumerable<Header>;
                if (headers != null)
                {
                    foreach (Header h in headers)
                    {
                        if (!h.Field.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                            Headers.Add(h);
                    }

                    continue;
                }

                var entity = obj as MimeEntity;
                if (entity != null)
                {
                    if (body != null)
                        throw new ArgumentException("Message body should not be specified more than once.");

                    body = entity;
                    continue;
                }

                throw new ArgumentException("Unknown initialization parameter: " + obj.GetType());
            }

            if (body != null)
                Body = body;

            // Do exactly as in the parameterless constructor but avoid setting a default
            // value if an header already provided one.

            if (!Headers.Contains("From"))
                Headers["From"] = string.Empty;
            if (!Headers.Contains("To"))
                Headers["To"] = string.Empty;
            if (date == default (DateTimeOffset))
                Date = DateTimeOffset.Now;
            if (!Headers.Contains("Subject"))
                Subject = string.Empty;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MimeKit.MimeMessage" /> class.
        /// </summary>
        public MimeMessage() : this(ParserOptions.Default.Clone())
        {
            Headers["From"] = string.Empty;
            Headers["To"] = string.Empty;
            Date = DateTimeOffset.Now;
            Subject = string.Empty;
        }

        /// <summary>
        ///     Gets the list of headers.
        /// </summary>
        /// <value>The list of headers.</value>
        public HeaderList Headers { get; private set; }

        /// <summary>
        ///     Gets or sets the address in the Sender header.
        /// </summary>
        /// <remarks>
        ///     The sender may differ from the addresses in <see cref="From" /> if
        ///     the message was sent by someone on behalf of someone else.
        /// </remarks>
        /// <value>The address in the Sender header.</value>
        public MailboxAddress Sender
        {
            get { return sender; }
            set
            {
                if (value == sender)
                    return;

                if (value == null)
                {
                    Headers.Changed -= HeadersChanged;
                    Headers.RemoveAll(HeaderId.Sender);
                    Headers.Changed += HeadersChanged;
                    sender = null;
                    return;
                }

                var builder = new StringBuilder();
                int len = "Sender: ".Length;

                value.Encode(FormatOptions.Default, builder, ref len);
                builder.Append(FormatOptions.Default.NewLine);

                byte[] raw = Encoding.ASCII.GetBytes(builder.ToString());

                Headers.Changed -= HeadersChanged;
                Headers.Replace(new Header(Headers.Options, "Sender", raw));
                Headers.Changed += HeadersChanged;

                sender = value;
            }
        }

        /// <summary>
        ///     Gets or sets the address in the Resent-Sender header.
        /// </summary>
        /// <remarks>
        ///     The resent sender may differ from the addresses in <see cref="ResentFrom" /> if
        ///     the message was sent by someone on behalf of someone else.
        /// </remarks>
        /// <value>The address in the Resent-Sender header.</value>
        public MailboxAddress ResentSender
        {
            get { return resentSender; }
            set
            {
                if (value == resentSender)
                    return;

                if (value == null)
                {
                    Headers.Changed -= HeadersChanged;
                    Headers.RemoveAll(HeaderId.ResentSender);
                    Headers.Changed += HeadersChanged;
                    resentSender = null;
                    return;
                }

                var builder = new StringBuilder();
                int len = "Resent-Sender: ".Length;

                value.Encode(FormatOptions.Default, builder, ref len);
                builder.Append(FormatOptions.Default.NewLine);

                byte[] raw = Encoding.ASCII.GetBytes(builder.ToString());

                Headers.Changed -= HeadersChanged;
                Headers.Replace(new Header(Headers.Options, "Resent-Sender", raw));
                Headers.Changed += HeadersChanged;

                resentSender = value;
            }
        }

        /// <summary>
        ///     Gets the list of addresses in the From header.
        /// </summary>
        /// <value>The list of addresses in the From header.</value>
        public InternetAddressList From
        {
            get { return addresses["From"]; }
        }

        /// <summary>
        ///     Gets the list of addresses in the Resent-From header.
        /// </summary>
        /// <value>The list of addresses in the Resent-From header.</value>
        public InternetAddressList ResentFrom
        {
            get { return addresses["Resent-From"]; }
        }

        /// <summary>
        ///     Gets the list of addresses in the Reply-To header.
        /// </summary>
        /// <value>The list of addresses in the Reply-To header.</value>
        public InternetAddressList ReplyTo
        {
            get { return addresses["Reply-To"]; }
        }

        /// <summary>
        ///     Gets the list of addresses in the Resent-Reply-To header.
        /// </summary>
        /// <value>The list of addresses in the Resent-Reply-To header.</value>
        public InternetAddressList ResentReplyTo
        {
            get { return addresses["Resent-Reply-To"]; }
        }

        /// <summary>
        ///     Gets the list of addresses in the To header.
        /// </summary>
        /// <value>The list of addresses in the To header.</value>
        public InternetAddressList To
        {
            get { return addresses["To"]; }
        }

        /// <summary>
        ///     Gets the list of addresses in the Resent-To header.
        /// </summary>
        /// <value>The list of addresses in the Resent-To header.</value>
        public InternetAddressList ResentTo
        {
            get { return addresses["Resent-To"]; }
        }

        /// <summary>
        ///     Gets the list of addresses in the Cc header.
        /// </summary>
        /// <value>The list of addresses in the Cc header.</value>
        public InternetAddressList Cc
        {
            get { return addresses["Cc"]; }
        }

        /// <summary>
        ///     Gets the list of addresses in the Resent-Cc header.
        /// </summary>
        /// <value>The list of addresses in the Resent-Cc header.</value>
        public InternetAddressList ResentCc
        {
            get { return addresses["Resent-Cc"]; }
        }

        /// <summary>
        ///     Gets the list of addresses in the Bcc header.
        /// </summary>
        /// <remarks>
        ///     Recipients in the Blind-Carpbon-Copy list will not be visible to
        ///     the other recipients of the message.
        /// </remarks>
        /// <value>The list of addresses in the Bcc header.</value>
        public InternetAddressList Bcc
        {
            get { return addresses["Bcc"]; }
        }

        /// <summary>
        ///     Gets the list of addresses in the Resent-Bcc header.
        /// </summary>
        /// <remarks>
        ///     Recipients in the Resent Blind-Carpbon-Copy list will not be visible to
        ///     the other recipients of the message.
        /// </remarks>
        /// <value>The list of addresses in the Resent-Bcc header.</value>
        public InternetAddressList ResentBcc
        {
            get { return addresses["Resent-Bcc"]; }
        }

        /// <summary>
        ///     Gets or sets the subject of the message.
        /// </summary>
        /// <value>The subject of the message.</value>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="value" /> is <c>null</c>.
        /// </exception>
        public string Subject
        {
            get { return Headers["Subject"]; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Headers.Changed -= HeadersChanged;
                Headers["Subject"] = value;
                Headers.Changed += HeadersChanged;
            }
        }

        /// <summary>
        ///     Gets or sets the date of the message.
        /// </summary>
        /// <remarks>
        ///     If the date is not explicitly set before the message is written to a stream,
        ///     the date will default to the exact moment when it is written to said stream.
        /// </remarks>
        /// <value>The date of the message.</value>
        public DateTimeOffset Date
        {
            get { return date; }
            set
            {
                if (date == value)
                    return;

                Headers.Changed -= HeadersChanged;
                Headers["Date"] = DateUtils.FormatDate(value);
                Headers.Changed += HeadersChanged;

                date = value;
            }
        }

        /// <summary>
        ///     Gets or sets the Resent-Date of the message.
        /// </summary>
        /// <value>The Resent-Date of the message.</value>
        public DateTimeOffset ResentDate
        {
            get { return resentDate; }
            set
            {
                if (resentDate == value)
                    return;

                Headers.Changed -= HeadersChanged;
                Headers["Resent-Date"] = DateUtils.FormatDate(value);
                Headers.Changed += HeadersChanged;

                resentDate = value;
            }
        }

        /// <summary>
        ///     Gets or sets the list of references to other messages.
        /// </summary>
        /// <remarks>
        ///     The References header contains a chain of Message-Ids back to the
        ///     original message that started the thread.
        /// </remarks>
        /// <value>The references.</value>
        public MessageIdList References
        {
            get { return references; }
        }

        /// <summary>
        ///     Gets or sets the message-id that this message is in reply to.
        /// </summary>
        /// <value>The message id that this message is in reply to.</value>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="value" /> is improperly formatted.
        /// </exception>
        public string InReplyTo
        {
            get { return inreplyto; }
            set
            {
                if (inreplyto == value)
                    return;

                if (value == null)
                {
                    Headers.Changed -= HeadersChanged;
                    Headers.RemoveAll(HeaderId.InReplyTo);
                    Headers.Changed += HeadersChanged;
                    inreplyto = null;
                    return;
                }

                byte[] buffer = Encoding.ASCII.GetBytes(value);
                InternetAddress addr;
                int index = 0;

                if (!InternetAddress.TryParse(Headers.Options, buffer, ref index, buffer.Length, false, out addr) ||
                    !(addr is MailboxAddress))
                    throw new ArgumentException("Invalid Message-Id format.", "value");

                inreplyto = ((MailboxAddress) addr).Address;

                Headers.Changed -= HeadersChanged;
                Headers["In-Reply-To"] = "<" + inreplyto + ">";
                Headers.Changed += HeadersChanged;
            }
        }

        /// <summary>
        ///     Gets or sets the message identifier.
        /// </summary>
        /// <value>The message identifier.</value>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="value" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="value" /> is improperly formatted.
        /// </exception>
        public string MessageId
        {
            get { return messageId; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (messageId == value)
                    return;

                byte[] buffer = Encoding.ASCII.GetBytes(value);
                InternetAddress addr;
                int index = 0;

                if (!InternetAddress.TryParse(Headers.Options, buffer, ref index, buffer.Length, false, out addr) ||
                    !(addr is MailboxAddress))
                    throw new ArgumentException("Invalid Message-Id format.", "value");

                messageId = ((MailboxAddress) addr).Address;

                Headers.Changed -= HeadersChanged;
                Headers["Message-Id"] = "<" + messageId + ">";
                Headers.Changed += HeadersChanged;
            }
        }

        /// <summary>
        ///     Gets or sets the Resent-Message-Id header.
        /// </summary>
        /// <value>The Resent-Message-Id.</value>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="value" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     <paramref name="value" /> is improperly formatted.
        /// </exception>
        public string ResentMessageId
        {
            get { return resentMessageId; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (resentMessageId == value)
                    return;

                byte[] buffer = Encoding.ASCII.GetBytes(value);
                InternetAddress addr;
                int index = 0;

                if (!InternetAddress.TryParse(Headers.Options, buffer, ref index, buffer.Length, false, out addr) ||
                    !(addr is MailboxAddress))
                    throw new ArgumentException("Invalid Resent-Message-Id format.", "value");

                resentMessageId = ((MailboxAddress) addr).Address;

                Headers.Changed -= HeadersChanged;
                Headers["Resent-Message-Id"] = "<" + resentMessageId + ">";
                Headers.Changed += HeadersChanged;
            }
        }

        /// <summary>
        ///     Gets or sets the MIME-Version.
        /// </summary>
        /// <value>The MIME version.</value>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="value" /> is <c>null</c>.
        /// </exception>
        public Version MimeVersion
        {
            get { return version; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (version != null && version.CompareTo(value) == 0)
                    return;

                version = value;

                Headers.Changed -= HeadersChanged;
                Headers["MIME-Version"] = version.ToString();
                Headers.Changed += HeadersChanged;
            }
        }

        /// <summary>
        ///     Gets or sets the body of the message.
        /// </summary>
        /// <value>The body of the message.</value>
        public MimeEntity Body { get; set; }

        /// <summary>
        ///     Writes the message to the specified output stream.
        /// </summary>
        /// <param name="options">The formatting options.</param>
        /// <param name="stream">The output stream.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <para><paramref name="options" /> is <c>null</c>.</para>
        ///     <para>-or-</para>
        ///     <para><paramref name="stream" /> is <c>null</c>.</para>
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        ///     The operation was canceled via the cancellation token.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public void WriteTo(FormatOptions options, Stream stream, CancellationToken cancellationToken)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!Headers.Contains("Date"))
                Date = DateTimeOffset.Now;

            if (messageId == null)
                MessageId = MimeUtils.GenerateMessageId();

            if (version == null && Body != null && Body.Headers.Count > 0)
                MimeVersion = new Version(1, 0);

            if (Body == null)
            {
                Headers.WriteTo(options, stream, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                stream.Write(options.NewLineBytes, 0, options.NewLineBytes.Length);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var filtered = new FilteredStream(stream))
                {
                    filtered.Add(options.CreateNewLineFilter());

                    foreach (Header header in MergeHeaders())
                    {
                        if (options.HiddenHeaders.Contains(header.Id))
                            continue;

                        cancellationToken.ThrowIfCancellationRequested();

                        byte[] name = Encoding.ASCII.GetBytes(header.Field);

                        filtered.Write(name, 0, name.Length);
                        filtered.WriteByte((byte) ':');
                        filtered.Write(header.RawValue, 0, header.RawValue.Length);
                    }

                    filtered.Flush();
                }

                options.WriteHeaders = false;
                Body.WriteTo(options, stream, cancellationToken);
            }
        }

        /// <summary>
        ///     Writes the message to the specified output stream.
        /// </summary>
        /// <param name="options">The formatting options.</param>
        /// <param name="stream">The output stream.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <para><paramref name="options" /> is <c>null</c>.</para>
        ///     <para>-or-</para>
        ///     <para><paramref name="stream" /> is <c>null</c>.</para>
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public void WriteTo(FormatOptions options, Stream stream)
        {
            WriteTo(options, stream, CancellationToken.None);
        }

        /// <summary>
        ///     Writes the message to the specified output stream.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="stream" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        ///     The operation was canceled via the cancellation token.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public void WriteTo(Stream stream, CancellationToken cancellationToken)
        {
            WriteTo(FormatOptions.Default, stream, cancellationToken);
        }

        /// <summary>
        ///     Writes the message to the specified output stream.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="stream" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public void WriteTo(Stream stream)
        {
            WriteTo(FormatOptions.Default, stream);
        }

        private MailboxAddress GetMessageSigner()
        {
            if (ResentSender != null)
                return ResentSender;

            if (ResentFrom.Count > 0)
                return ResentFrom.Mailboxes.FirstOrDefault();

            if (Sender != null)
                return Sender;

            if (From.Count > 0)
                return From.Mailboxes.FirstOrDefault();

            return null;
        }

        private IList<MailboxAddress> GetMessageRecipients(bool includeSenders)
        {
            var recipients = new List<MailboxAddress>();

            if (ResentSender != null || ResentFrom.Count > 0)
            {
                if (includeSenders)
                {
                    if (ResentSender != null)
                        recipients.Add(ResentSender);

                    if (ResentFrom.Count > 0)
                        recipients.AddRange(ResentFrom.Mailboxes);
                }

                recipients.AddRange(ResentTo.Mailboxes);
                recipients.AddRange(ResentCc.Mailboxes);
                recipients.AddRange(ResentBcc.Mailboxes);
            }
            else
            {
                if (includeSenders)
                {
                    if (Sender != null)
                        recipients.Add(Sender);

                    if (From.Count > 0)
                        recipients.AddRange(From.Mailboxes);
                }

                recipients.AddRange(To.Mailboxes);
                recipients.AddRange(Cc.Mailboxes);
                recipients.AddRange(Bcc.Mailboxes);
            }

            return recipients;
        }

        private IEnumerable<Header> MergeHeaders()
        {
            int mesgIndex = 0, bodyIndex = 0;

            while (mesgIndex < Headers.Count && bodyIndex < Body.Headers.Count)
            {
                Header bodyHeader = Body.Headers[bodyIndex];
                if (!bodyHeader.Offset.HasValue)
                    break;

                Header mesgHeader = Headers[mesgIndex];

                if (mesgHeader.Offset.HasValue && mesgHeader.Offset < bodyHeader.Offset)
                {
                    yield return mesgHeader;

                    mesgIndex++;
                }
                else
                {
                    yield return bodyHeader;

                    bodyIndex++;
                }
            }

            while (mesgIndex < Headers.Count)
                yield return Headers[mesgIndex++];

            while (bodyIndex < Body.Headers.Count)
                yield return Body.Headers[bodyIndex++];
        }

        private void SerializeAddressList(string field, InternetAddressList list)
        {
            var builder = new StringBuilder(" ");
            int lineLength = field.Length + 2;

            list.Encode(FormatOptions.Default, builder, ref lineLength);
            builder.Append(FormatOptions.Default.NewLine);

            byte[] raw = Encoding.ASCII.GetBytes(builder.ToString());

            Headers.Changed -= HeadersChanged;
            Headers.Replace(new Header(Headers.Options, field, raw));
            Headers.Changed += HeadersChanged;
        }

        private void InternetAddressListChanged(object addrlist, EventArgs e)
        {
            var list = (InternetAddressList) addrlist;

            foreach (string name in StandardAddressHeaders)
            {
                if (addresses[name] == list)
                {
                    SerializeAddressList(name, list);
                    break;
                }
            }
        }

        private void ReferencesChanged(object o, EventArgs e)
        {
            if (references.Count > 0)
            {
                int lineLength = "References".Length + 1;
                FormatOptions options = FormatOptions.Default;
                var builder = new StringBuilder();

                for (int i = 0; i < references.Count; i++)
                {
                    if (lineLength + references[i].Length >= options.MaxLineLength)
                    {
                        builder.Append(options.NewLine);
                        builder.Append('\t');
                        lineLength = 1;
                    }
                    else
                    {
                        builder.Append(' ');
                        lineLength++;
                    }

                    lineLength += references[i].Length;
                    builder.Append(references[i]);
                }

                builder.Append(options.NewLine);

                byte[] raw = Encoding.UTF8.GetBytes(builder.ToString());

                Headers.Changed -= HeadersChanged;
                Headers.Replace(new Header(Headers.Options, "References", raw));
                Headers.Changed += HeadersChanged;
            }
            else
            {
                Headers.Changed -= HeadersChanged;
                Headers.RemoveAll(HeaderId.References);
                Headers.Changed += HeadersChanged;
            }
        }

        private void AddAddresses(Header header, InternetAddressList list)
        {
            int length = header.RawValue.Length;
            List<InternetAddress> parsed;
            int index = 0;

            // parse the addresses in the new header and add them to our address list
            if (
                !InternetAddressList.TryParse(Headers.Options, header.RawValue, ref index, length, false, false,
                    out parsed))
                return;

            list.Changed -= InternetAddressListChanged;
            list.AddRange(parsed);
            list.Changed += InternetAddressListChanged;
        }

        private void ReloadAddressList(HeaderId id, InternetAddressList list)
        {
            // clear the address list and reload
            list.Changed -= InternetAddressListChanged;
            list.Clear();

            foreach (Header header in Headers)
            {
                if (header.Id != id)
                    continue;

                int length = header.RawValue.Length;
                List<InternetAddress> parsed;
                int index = 0;

                if (
                    !InternetAddressList.TryParse(Headers.Options, header.RawValue, ref index, length, false, false,
                        out parsed))
                    continue;

                list.AddRange(parsed);
            }

            list.Changed += InternetAddressListChanged;
        }

        private void ReloadHeader(HeaderId id)
        {
            if (id == HeaderId.Unknown)
                return;

            switch (id)
            {
                case HeaderId.ResentMessageId:
                    resentMessageId = null;
                    break;
                case HeaderId.ResentSender:
                    resentSender = null;
                    break;
                case HeaderId.ResentDate:
                    resentDate = DateTimeOffset.MinValue;
                    break;
                case HeaderId.References:
                    references.Changed -= ReferencesChanged;
                    references.Clear();
                    references.Changed += ReferencesChanged;
                    break;
                case HeaderId.InReplyTo:
                    inreplyto = null;
                    break;
                case HeaderId.MessageId:
                    messageId = null;
                    break;
                case HeaderId.Sender:
                    sender = null;
                    break;
                case HeaderId.Date:
                    date = DateTimeOffset.MinValue;
                    break;
            }

            foreach (Header header in Headers)
            {
                if (header.Id != id)
                    continue;

                byte[] rawValue = header.RawValue;
                InternetAddress address;
                int index = 0;

                switch (id)
                {
                    case HeaderId.MimeVersion:
                        if (MimeUtils.TryParseVersion(rawValue, 0, rawValue.Length, out version))
                            return;
                        break;
                    case HeaderId.References:
                        references.Changed -= ReferencesChanged;
                        foreach (string msgid in MimeUtils.EnumerateReferences(rawValue, 0, rawValue.Length))
                            references.Add(msgid);
                        references.Changed += ReferencesChanged;
                        break;
                    case HeaderId.InReplyTo:
                        inreplyto = MimeUtils.EnumerateReferences(rawValue, 0, rawValue.Length).FirstOrDefault();
                        break;
                    case HeaderId.ResentMessageId:
                        resentMessageId = MimeUtils.EnumerateReferences(rawValue, 0, rawValue.Length).FirstOrDefault();
                        if (resentMessageId != null)
                            return;
                        break;
                    case HeaderId.MessageId:
                        messageId = MimeUtils.EnumerateReferences(rawValue, 0, rawValue.Length).FirstOrDefault();
                        if (messageId != null)
                            return;
                        break;
                    case HeaderId.ResentSender:
                        if (InternetAddress.TryParse(Headers.Options, rawValue, ref index, rawValue.Length, false,
                            out address))
                            resentSender = address as MailboxAddress;
                        if (resentSender != null)
                            return;
                        break;
                    case HeaderId.Sender:
                        if (InternetAddress.TryParse(Headers.Options, rawValue, ref index, rawValue.Length, false,
                            out address))
                            sender = address as MailboxAddress;
                        if (sender != null)
                            return;
                        break;
                    case HeaderId.ResentDate:
                        if (DateUtils.TryParseDateTime(rawValue, 0, rawValue.Length, out resentDate))
                            return;
                        break;
                    case HeaderId.Date:
                        if (DateUtils.TryParseDateTime(rawValue, 0, rawValue.Length, out date))
                            return;
                        break;
                }
            }
        }

        private void HeadersChanged(object o, HeaderListChangedEventArgs e)
        {
            InternetAddressList list;
            InternetAddress address;
            byte[] rawValue;
            int index = 0;

            switch (e.Action)
            {
                case HeaderListChangedAction.Added:
                    if (addresses.TryGetValue(e.Header.Field, out list))
                    {
                        AddAddresses(e.Header, list);
                        break;
                    }

                    rawValue = e.Header.RawValue;

                    switch (e.Header.Id)
                    {
                        case HeaderId.MimeVersion:
                            MimeUtils.TryParseVersion(rawValue, 0, rawValue.Length, out version);
                            break;
                        case HeaderId.References:
                            references.Changed -= ReferencesChanged;
                            foreach (string msgid in MimeUtils.EnumerateReferences(rawValue, 0, rawValue.Length))
                                references.Add(msgid);
                            references.Changed += ReferencesChanged;
                            break;
                        case HeaderId.InReplyTo:
                            inreplyto = MimeUtils.EnumerateReferences(rawValue, 0, rawValue.Length).FirstOrDefault();
                            break;
                        case HeaderId.ResentMessageId:
                            resentMessageId =
                                MimeUtils.EnumerateReferences(rawValue, 0, rawValue.Length).FirstOrDefault();
                            break;
                        case HeaderId.MessageId:
                            messageId = MimeUtils.EnumerateReferences(rawValue, 0, rawValue.Length).FirstOrDefault();
                            break;
                        case HeaderId.ResentSender:
                            if (InternetAddress.TryParse(Headers.Options, rawValue, ref index, rawValue.Length, false,
                                out address))
                                resentSender = address as MailboxAddress;
                            break;
                        case HeaderId.Sender:
                            if (InternetAddress.TryParse(Headers.Options, rawValue, ref index, rawValue.Length, false,
                                out address))
                                sender = address as MailboxAddress;
                            break;
                        case HeaderId.ResentDate:
                            DateUtils.TryParseDateTime(rawValue, 0, rawValue.Length, out resentDate);
                            break;
                        case HeaderId.Date:
                            DateUtils.TryParseDateTime(rawValue, 0, rawValue.Length, out date);
                            break;
                    }
                    break;
                case HeaderListChangedAction.Changed:
                case HeaderListChangedAction.Removed:
                    if (addresses.TryGetValue(e.Header.Field, out list))
                    {
                        ReloadAddressList(e.Header.Id, list);
                        break;
                    }

                    ReloadHeader(e.Header.Id);
                    break;
                case HeaderListChangedAction.Cleared:
                    foreach (var kvp in addresses)
                    {
                        kvp.Value.Changed -= InternetAddressListChanged;
                        kvp.Value.Clear();
                        kvp.Value.Changed += InternetAddressListChanged;
                    }

                    references.Changed -= ReferencesChanged;
                    references.Clear();
                    references.Changed += ReferencesChanged;

                    resentMessageId = null;
                    resentSender = null;
                    inreplyto = null;
                    messageId = null;
                    version = null;
                    sender = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Load a <see cref="MimeMessage" /> from the specified stream.
        /// </summary>
        /// <returns>The parsed message.</returns>
        /// <param name="options">The parser options.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <para><paramref name="options" /> is <c>null</c>.</para>
        ///     <para>-or-</para>
        ///     <para><paramref name="stream" /> is <c>null</c>.</para>
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        ///     The operation was canceled via the cancellation token.
        /// </exception>
        /// <exception cref="System.FormatException">
        ///     There was an error parsing the entity.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public static MimeMessage Load(ParserOptions options, Stream stream, CancellationToken cancellationToken)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            if (stream == null)
                throw new ArgumentNullException("stream");

            var parser = new MimeParser(options, stream, MimeFormat.Entity);

            return parser.ParseMessage(cancellationToken);
        }

        /// <summary>
        ///     Load a <see cref="MimeMessage" /> from the specified stream.
        /// </summary>
        /// <returns>The parsed message.</returns>
        /// <param name="stream">The stream.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="stream" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        ///     The operation was canceled via the cancellation token.
        /// </exception>
        /// <exception cref="System.FormatException">
        ///     There was an error parsing the entity.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public static MimeMessage Load(Stream stream, CancellationToken cancellationToken)
        {
            return Load(ParserOptions.Default, stream, cancellationToken);
        }

        /// <summary>
        ///     Load a <see cref="MimeMessage" /> from the specified stream.
        /// </summary>
        /// <returns>The parsed message.</returns>
        /// <param name="options">The parser options.</param>
        /// <param name="stream">The stream.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <para><paramref name="options" /> is <c>null</c>.</para>
        ///     <para>-or-</para>
        ///     <para><paramref name="stream" /> is <c>null</c>.</para>
        /// </exception>
        /// <exception cref="System.FormatException">
        ///     There was an error parsing the entity.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public static MimeMessage Load(ParserOptions options, Stream stream)
        {
            return Load(options, stream, CancellationToken.None);
        }

        /// <summary>
        ///     Load a <see cref="MimeMessage" /> from the specified stream.
        /// </summary>
        /// <returns>The parsed message.</returns>
        /// <param name="stream">The stream.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="stream" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.FormatException">
        ///     There was an error parsing the entity.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public static MimeMessage Load(Stream stream)
        {
            return Load(ParserOptions.Default, stream, CancellationToken.None);
        }

        /// <summary>
        ///     Load a <see cref="MimeMessage" /> from the specified file.
        /// </summary>
        /// <returns>The parsed message.</returns>
        /// <param name="options">The parser options.</param>
        /// <param name="fileName">The name of the file to load.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <para><paramref name="options" /> is <c>null</c>.</para>
        ///     <para>-or-</para>
        ///     <para><paramref name="fileName" /> is <c>null</c>.</para>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     The specified file path is empty.
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException">
        ///     The specified file could not be found.
        /// </exception>
        /// <exception cref="System.UnauthorizedAccessException">
        ///     The user does not have access to read the specified file.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        ///     The operation was canceled via the cancellation token.
        /// </exception>
        /// <exception cref="System.FormatException">
        ///     There was an error parsing the entity.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public static MimeMessage Load(ParserOptions options, string fileName, CancellationToken cancellationToken)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            if (fileName == null)
                throw new ArgumentNullException("fileName");

            using (FileStream stream = File.OpenRead(fileName))
            {
                return Load(options, stream, cancellationToken);
            }
        }

        /// <summary>
        ///     Load a <see cref="MimeMessage" /> from the specified file.
        /// </summary>
        /// <returns>The parsed message.</returns>
        /// <param name="fileName">The name of the file to load.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="fileName" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     The specified file path is empty.
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException">
        ///     The specified file could not be found.
        /// </exception>
        /// <exception cref="System.UnauthorizedAccessException">
        ///     The user does not have access to read the specified file.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        ///     The operation was canceled via the cancellation token.
        /// </exception>
        /// <exception cref="System.FormatException">
        ///     There was an error parsing the entity.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public static MimeMessage Load(string fileName, CancellationToken cancellationToken)
        {
            return Load(ParserOptions.Default, fileName, cancellationToken);
        }

        /// <summary>
        ///     Load a <see cref="MimeMessage" /> from the specified file.
        /// </summary>
        /// <returns>The parsed message.</returns>
        /// <param name="options">The parser options.</param>
        /// <param name="fileName">The name of the file to load.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <para><paramref name="options" /> is <c>null</c>.</para>
        ///     <para>-or-</para>
        ///     <para><paramref name="fileName" /> is <c>null</c>.</para>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     The specified file path is empty.
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException">
        ///     The specified file could not be found.
        /// </exception>
        /// <exception cref="System.UnauthorizedAccessException">
        ///     The user does not have access to read the specified file.
        /// </exception>
        /// <exception cref="System.FormatException">
        ///     There was an error parsing the entity.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public static MimeMessage Load(ParserOptions options, string fileName)
        {
            return Load(options, fileName, CancellationToken.None);
        }

        /// <summary>
        ///     Load a <see cref="MimeMessage" /> from the specified file.
        /// </summary>
        /// <returns>The parsed message.</returns>
        /// <param name="fileName">The name of the file to load.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="fileName" /> is <c>null</c>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     The specified file path is empty.
        /// </exception>
        /// <exception cref="System.IO.FileNotFoundException">
        ///     The specified file could not be found.
        /// </exception>
        /// <exception cref="System.UnauthorizedAccessException">
        ///     The user does not have access to read the specified file.
        /// </exception>
        /// <exception cref="System.FormatException">
        ///     There was an error parsing the entity.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        ///     An I/O error occurred.
        /// </exception>
        public static MimeMessage Load(string fileName)
        {
            return Load(ParserOptions.Default, fileName, CancellationToken.None);
        }

        #region System.Net.Mail support

        private static System.Net.Mime.ContentType GetContentType(ContentType contentType)
        {
            var ctype = new System.Net.Mime.ContentType();
            ctype.MediaType = string.Format("{0}/{1}", contentType.MediaType, contentType.MediaSubtype);

            foreach (Parameter param in contentType.Parameters)
                ctype.Parameters.Add(param.Name, param.Value);

            return ctype;
        }

        private static TransferEncoding GetTransferEncoding(ContentEncoding encoding)
        {
            switch (encoding)
            {
                case ContentEncoding.QuotedPrintable:
                case ContentEncoding.EightBit:
                    return TransferEncoding.QuotedPrintable;
                case ContentEncoding.SevenBit:
                    return TransferEncoding.SevenBit;
                default:
                    return TransferEncoding.Base64;
            }
        }

        private static void AddBodyPart(MailMessage message, MimeEntity entity)
        {
            if (entity is MessagePart)
            {
                // FIXME: how should this be converted into a MailMessage?
            }
            else if (entity is Multipart)
            {
                var multipart = (Multipart) entity;

                if (multipart.ContentType.Matches("multipart", "alternative"))
                {
                    foreach (MimePart part in multipart.OfType<MimePart>())
                    {
                        // clone the content
                        var content = new MemoryStream();
                        part.ContentObject.DecodeTo(content);
                        content.Position = 0;

                        var view = new AlternateView(content, GetContentType(part.ContentType));
                        view.TransferEncoding = GetTransferEncoding(part.ContentTransferEncoding);
                        if (!string.IsNullOrEmpty(part.ContentId))
                            view.ContentId = part.ContentId;

                        message.AlternateViews.Add(view);
                    }
                }
                else
                {
                    foreach (MimeEntity part in multipart)
                        AddBodyPart(message, part);
                }
            }
            else
            {
                var part = (MimePart) entity;

                if (part.IsAttachment || !string.IsNullOrEmpty(message.Body) || !(part is TextPart))
                {
                    // clone the content
                    var content = new MemoryStream();
                    part.ContentObject.DecodeTo(content);
                    content.Position = 0;

                    var attachment = new Attachment(content, GetContentType(part.ContentType));

                    if (part.ContentDisposition != null)
                    {
                        attachment.ContentDisposition.DispositionType = part.ContentDisposition.Disposition;
                        foreach (Parameter param in part.ContentDisposition.Parameters)
                            attachment.ContentDisposition.Parameters.Add(param.Name, param.Value);
                    }

                    attachment.TransferEncoding = GetTransferEncoding(part.ContentTransferEncoding);

                    if (!string.IsNullOrEmpty(part.ContentId))
                        attachment.ContentId = part.ContentId;

                    message.Attachments.Add(attachment);
                }
                else
                {
                    message.IsBodyHtml = part.ContentType.Matches("text", "html");
                    message.Body = ((TextPart) part).Text;
                }
            }
        }

        /// <summary>
        ///     Explicit cast to convert a <see cref="MimeMessage" /> to a
        ///     <see cref="System.Net.Mail.MailMessage" />.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Casting a <see cref="MimeMessage" /> to a <see cref="System.Net.Mail.MailMessage" />
        ///         makes it possible to use MimeKit with <see cref="System.Net.Mail.SmtpClient" />.
        ///     </para>
        ///     <para>
        ///         It should be noted, however, that <see cref="System.Net.Mail.MailMessage" />
        ///         cannot represent all MIME structures that can be constructed using MimeKit,
        ///         so the conversion may not be perfect.
        ///     </para>
        /// </remarks>
        /// <returns>A <see cref="System.Net.Mail.MailMessage" />.</returns>
        /// <param name="message">The message.</param>
        /// <exception cref="System.ArgumentNullException">
        ///     The <paramref name="message" /> is <c>null</c>.
        /// </exception>
        public static explicit operator MailMessage(MimeMessage message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            MailboxAddress from = message.From.Mailboxes.FirstOrDefault();
            var msg = new MailMessage();
            MailboxAddress sender = message.Sender;

            foreach (Header header in message.Headers)
                msg.Headers.Add(header.Field, header.Value);

            if (sender != null)
                msg.Sender = (MailAddress) sender;

            if (from != null)
                msg.From = (MailAddress) from;

            foreach (MailboxAddress mailbox in message.ReplyTo.Mailboxes)
                msg.ReplyToList.Add((MailAddress) mailbox);

            foreach (MailboxAddress mailbox in message.To.Mailboxes)
                msg.To.Add((MailAddress) mailbox);

            foreach (MailboxAddress mailbox in message.Cc.Mailboxes)
                msg.CC.Add((MailAddress) mailbox);

            foreach (MailboxAddress mailbox in message.Bcc.Mailboxes)
                msg.Bcc.Add((MailAddress) mailbox);

            msg.Subject = message.Subject;

            if (message.Body != null)
                AddBodyPart(msg, message.Body);

            return msg;
        }

        #endregion
    }
}