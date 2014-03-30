//
// ParserOptions.cs
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
using System.Reflection;
using System.Text;

namespace MimeKit
{
    /// <summary>
    ///     Parser options as used by <see cref="MimeParser" /> as well as various Parse and TryParse methods in MimeKit.
    /// </summary>
    /// <remarks>
    ///     <see cref="ParserOptions" /> allows you to change and/or override default parsing options
    ///     used by methods such as <see cref="MimeMessage.Load(ParserOptions,System.IO.Stream)" /> and others.
    /// </remarks>
    public sealed class ParserOptions
    {
        private static readonly Type[] ConstructorArgTypes = {typeof (MimeEntityConstructorInfo)};

        /// <summary>
        ///     The default parser options.
        /// </summary>
        /// <remarks>
        ///     If a <see cref="ParserOptions" /> is not supplied to <see cref="MimeParser" /> or other Parse and TryParse
        ///     methods throughout MimeKit, <see cref="ParserOptions.Default" /> will be used.
        /// </remarks>
        public static readonly ParserOptions Default = new ParserOptions();

        private readonly Dictionary<string, ConstructorInfo> mimeTypes = new Dictionary<string, ConstructorInfo>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="MimeKit.ParserOptions" /> class.
        /// </summary>
        /// <remarks>
        ///     By default, new instances of <see cref="ParserOptions" /> enable rfc2047 work-arounds
        ///     (which are needed for maximum interoperability with mail software used in the wild)
        ///     and do not respect the Content-Length header value.
        /// </remarks>
        public ParserOptions()
        {
            CharsetEncoding = Encoding.Default;
            EnableRfc2047Workarounds = true;
            RespectContentLength = false;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether rfc2047 workarounds should be used.
        /// </summary>
        /// <remarks>
        ///     In general, you'll probably want this value to be <c>true</c> (the default) as it
        ///     allows maximum interoperability with existing (broken) mail clients and other mail
        ///     software such as sloppily written perl scripts (aka spambots).
        /// </remarks>
        /// <value><c>true</c> if rfc2047 workarounds are enabled; otherwise, <c>false</c>.</value>
        public bool EnableRfc2047Workarounds { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the Content-Length value should be
        ///     respected when parsing mbox streams.
        /// </summary>
        /// <remarks>
        ///     For more details about why this may be useful, you can find more information
        ///     at http://www.jwz.org/doc/content-length.html
        /// </remarks>
        /// <value>
        ///     <c>true</c> if the Content-Length value should be respected;
        ///     otherwise, <c>false</c>.
        /// </value>
        public bool RespectContentLength { get; set; }

        /// <summary>
        ///     Gets or sets the charset encoding to use as a fallback for 8bit headers.
        /// </summary>
        /// <remarks>
        ///     <see cref="MimeKit.Utils.Rfc2047.DecodeText(ParserOptions, byte[])" /> and
        ///     <see cref="MimeKit.Utils.Rfc2047.DecodePhrase(ParserOptions, byte[])" />
        ///     use this charset encoding as a fallback when decoding 8bit text into unicode. The first
        ///     charset encoding attempted is UTF-8, followed by this charset encoding, before finally
        ///     falling back to iso-8859-1.
        /// </remarks>
        /// <value>The charset encoding.</value>
        public Encoding CharsetEncoding { get; set; }

        /// <summary>
        ///     Clones an instance of <see cref="MimeKit.ParserOptions" />.
        /// </summary>
        /// <remarks>
        ///     Clones a set of options, allowing you to change a specific option
        ///     without requiring you to change the original.
        /// </remarks>
        /// <returns>An identical copy of the current instance.</returns>
        public ParserOptions Clone()
        {
            var options = new ParserOptions();
            options.EnableRfc2047Workarounds = EnableRfc2047Workarounds;
            options.RespectContentLength = RespectContentLength;
            options.CharsetEncoding = CharsetEncoding;

            foreach (var mimeType in mimeTypes)
                options.mimeTypes.Add(mimeType.Key, mimeType.Value);

            return options;
        }

        /// <summary>
        ///     Registers the <see cref="MimeEntity" /> subclass for the specified mime-type.
        /// </summary>
        /// <param name="mimeType">The MIME type.</param>
        /// <param name="type">A custom subclass of <see cref="MimeEntity" />.</param>
        /// <remarks>
        ///     Your custom <see cref="MimeEntity" /> class should not subclass
        ///     <see cref="MimeEntity" /> directly, but rather it should subclass
        ///     <see cref="Multipart" />, <see cref="MimePart" />,
        ///     <see cref="MessagePart" />, or one of their derivatives.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        ///     <para><paramref name="mimeType" /> is <c>null</c>.</para>
        ///     <para>-or-</para>
        ///     <para><paramref name="type" /> is <c>null</c>.</para>
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     <para>
        ///         <paramref name="type" /> is not a subclass of <see cref="Multipart" />,
        ///         <see cref="MimePart" />, or <see cref="MessagePart" />.
        ///     </para>
        ///     <para>-or-</para>
        ///     <para>
        ///         <paramref name="type" /> does not have a constructor that takes
        ///         only a <see cref="MimeEntityConstructorInfo" /> argument.
        ///     </para>
        /// </exception>
        public void RegisterMimeType(string mimeType, Type type)
        {
            if (mimeType == null)
                throw new ArgumentNullException("mimeType");

            if (type == null)
                throw new ArgumentNullException("type");

            mimeType = mimeType.ToLowerInvariant();

            if (!type.IsSubclassOf(typeof (MessagePart)) &&
                !type.IsSubclassOf(typeof (Multipart)) &&
                !type.IsSubclassOf(typeof (MimePart)))
                throw new ArgumentException(
                    "The specified type must be a subclass of MessagePart, Multipart, or MimePart.", "type");

            ConstructorInfo ctor = type.GetConstructor(ConstructorArgTypes);
            if (ctor == null)
                throw new ArgumentException(
                    "The specified type must have a constructor that takes a MimeEntityConstructorInfo argument.",
                    "type");

            mimeTypes[mimeType] = ctor;
        }

        internal MimeEntity CreateEntity(ContentType contentType, IEnumerable<Header> headers, bool toplevel)
        {
            var entity = new MimeEntityConstructorInfo(this, contentType, headers, toplevel);
            string subtype = contentType.MediaSubtype.ToLowerInvariant();
            string type = contentType.MediaType.ToLowerInvariant();

            if (mimeTypes.Count > 0)
            {
                string mimeType = string.Format("{0}/{1}", type, subtype);
                ConstructorInfo ctor;

                if (mimeTypes.TryGetValue(mimeType, out ctor))
                    return (MimeEntity) ctor.Invoke(new object[] {entity});
            }

            if (type == "message")
            {
                if (subtype == "partial")
                    return new MessagePartial(entity);

                return new MessagePart(entity);
            }

            if (type == "multipart")
            {
                return new Multipart(entity);
            }

            if (type == "text")
                return new TextPart(entity);

            return new MimePart(entity);
        }
    }
}