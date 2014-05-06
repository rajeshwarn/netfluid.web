﻿//
// TnefNameId.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (www.xamarin.com)
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

namespace NetFluid.MIME.Tnef
{
    internal struct TnefNameId
    {
        private readonly Guid guid;
        private readonly int id;
        private readonly TnefNameIdKind kind;
        private readonly string name;

        public TnefNameId(Guid propertySetGuid, int id)
        {
            kind = TnefNameIdKind.Id;
            guid = propertySetGuid;
            this.id = id;
            name = null;
        }

        public TnefNameId(Guid propertySetGuid, string name)
        {
            kind = TnefNameIdKind.Name;
            guid = propertySetGuid;
            this.name = name;
            id = 0;
        }

        public Guid PropertySetGuid
        {
            get { return guid; }
        }

        public TnefNameIdKind Kind
        {
            get { return kind; }
        }

        public string Name
        {
            get { return name; }
        }

        public int Id
        {
            get { return id; }
        }

        public override int GetHashCode()
        {
            int hash = kind == TnefNameIdKind.Id ? id : name.GetHashCode();

            return kind.GetHashCode() ^ guid.GetHashCode() ^ hash;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TnefNameId))
                return false;

            var v = (TnefNameId) obj;

            if (v.kind != kind || v.guid != guid)
                return false;

            return kind == TnefNameIdKind.Id ? v.id == id : v.name == name;
        }
    }
}