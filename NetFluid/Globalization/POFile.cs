using System.Collections.Generic;
using System.IO;

namespace NetFluid.Globalization
{
    /// <summary>
    /// Read locallization PO file
    /// </summary>
    public class PoFile : Dictionary<string, PoFile.Entry>
    {
        public class Entry
        {
            //#  translator-comments
            public string TranlatorComments;

            //#. extracted-comments
            public string ExtractedComments;

            //#: reference...
            public string Reference;

            //#, flag...
            // NOT IMPLEMENTED (flags are just for for compiler use)

            //#| msgid previous-untranslated-string
            // NOT IMPLEMENTED (is it senseful to keep this data?)

            //msgid untranslated-string
            public string Untranslated;

            //msgstr translated-string
            public string Translated;

            public static implicit operator string(Entry e)
            {
                return e.Translated;
            }

            public override string ToString()
            {
                return Translated;
            }
        }

        public PoFile(string path)
        {
            using (var stream=new FileStream(path,FileMode.Open))
            {
                using (var reader= new StreamReader(stream))
                {
                    var entry = new Entry();
                    while (!reader.EndOfStream)
                    {
                        #region READ AND PARSE ALL LINES
                        // ReSharper disable once PossibleNullReferenceException
                        var line = reader.ReadLine().Trim();

                        if (string.IsNullOrEmpty(line))
                        {
                            if(entry.Untranslated != null)
                                base[entry.Untranslated] = entry;
                            entry = new Entry();
                        }
                        else if (line.StartsWith("#."))
                        {
                            entry.ExtractedComments = line.Substring("#.".Length).Trim();
                        }
                        else if (line.StartsWith("#:"))
                        {
                            entry.Reference = line.Substring("#:".Length).Trim();
                        }
                        else if (line.StartsWith("#"))
                        {
                            entry.Reference = line.Substring("#".Length).Trim();
                        }
                        else if (line.StartsWith("msgid"))
                        {
                            entry.Untranslated = line.Substring("msgid".Length).Trim();
                        }
                        else if (line.StartsWith("msgstr"))
                        {
                            entry.Translated = line.Substring("msgstr".Length).Trim();
                        }
                        #endregion
                    }

                    if (entry.Untranslated!=null)
                    {
                        base[entry.Untranslated] = entry;
                    }
                }
            }
        }
    }
}
