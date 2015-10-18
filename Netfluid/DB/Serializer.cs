using Netfluid.DB.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netfluid.DB
{
    class Serializer
    {
        static StringSerializer stringSerializer;
        public static ISerializer<string> String
        {
            get
            {
                if (stringSerializer == null) stringSerializer = new StringSerializer();
                return stringSerializer;
            }
        }

        static IntSerializer intSerializer;
        public static ISerializer<int> Int
        {
            get
            {
                if (intSerializer == null) intSerializer = new IntSerializer();
                return intSerializer;
            }
        }

        static LongSerializer longSerializer;
        public static ISerializer<long> Long
        {
            get
            {
                if (longSerializer == null) longSerializer = new LongSerializer();
                return longSerializer;
            }
        }

        static UIntSerializer uintSerializer;
        public static ISerializer<uint> UInt
        {
            get
            {
                if (uintSerializer == null) uintSerializer = new UIntSerializer();
                return uintSerializer;
            }
        }

        static DateTimeSerializer dateTimeSerializer;
        public static ISerializer<DateTime> DateTime
        {
            get
            {
                if (dateTimeSerializer == null) dateTimeSerializer = new DateTimeSerializer();
                return dateTimeSerializer;
            }
        }
    }
}
