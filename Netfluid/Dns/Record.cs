// Stuff records are made of

using System;
using System.Linq;
using Netfluid.Dns.Records;
using System.Collections.Generic;

namespace Netfluid.Dns
{
    /// <summary>
    /// DNS record base class
    /// </summary>
    [Serializable]
    public class Record
    {
        public static Type[] Types { get; private set; }

        static Record()
        {
            Types = typeof(Record).Assembly.GetTypes().Where(x=>x.Inherit(typeof(Record))).ToArray();
        }
        
        public string RecordId { get; set; }

        /// <summary>
        ///     Specifies type class of resource record, mostly IN but can be CS, CH or HS
        /// </summary>
        public Class Class;

        /// <summary>
        /// The name of the node to which this resource record pertains
        /// </summary>
        public string Name;

        /// <summary>
        /// Time lived
        /// </summary>
        public int TimeLived;

        /// <summary>
        /// Create a new generic DNS record
        /// </summary>
        public Record()
        {
            TimeLived = 0;
            Name = "netfluid.org";
            Class = Class.IN;
            TTL = int.MaxValue - 1;
        }

        /// <summary>
        ///     Time to live, the time interval that the resource record may be cached
        /// </summary>
        public uint TTL { get; set; }

        /// <summary>
        /// Return the type of this record as RecordType enum
        /// </summary>
        public RecordType RecordType
        {
            get
            {
                var r = (RecordType)Enum.Parse(typeof(RecordType), this.GetType().Name.Substring("Record".Length));
                return r;
            }
        }

        /// <summary>
        /// Zone parts of the domain (www.example.com, .example.com, .com)
        /// </summary>
        public string[] Zones
        {
            get
            {
                var list = new List<string>();
                var parts = Name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Reverse().ToArray();

                for (int i = 0; i < parts.Length; i++)
                {
                    var zone = string.Join(".", parts.Take(i + 1).Reverse());
                    list.Add(zone);
                }
                return list.ToArray();
            }
        }

        /// <summary>
        /// Return .net type of record from enum RecordType
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type Type(RecordType type)
        {
            return Types.FirstOrDefault(x => x.Name == "Record"+type.ToString()) ?? typeof(RecordUnknown);
        }

        /// <summary>
        /// Instance a new record from the given type
        /// </summary>
        /// <param name="type">Record type enum or QType casted</param>
        /// <returns>new record of the given type</returns>
        public static Record FromType(RecordType type)
        {
            return Types.FirstOrDefault(x => x.Name == "Record" + type.ToString()).CreateIstance() as Record ?? new RecordUnknown();
        }
    }
}