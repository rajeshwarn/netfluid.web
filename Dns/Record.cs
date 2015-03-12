// Stuff records are made of

using System;
using System.Linq;
using NetFluid.DNS.Records;

namespace NetFluid.DNS
{
    /// <summary>
    /// DNS record base class
    /// </summary>
    [Serializable]
    public class Record: IDatabaseObject
    {
        static Type[] types;

        static Record()
        {
            types = typeof(Record).Assembly.GetTypes().Where(x=>x.Inherit(typeof(Record))).ToArray();
        }

        /// <summary>
        /// Database id to store and handle record
        /// </summary>
        public string Id { get; set; }
   
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
            set
            {

            }
        }

        /// <summary>
        /// Return .net type of record from enum RecordType
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type Type(RecordType type)
        {
            return types.FirstOrDefault(x => x.Name == "Record"+type.ToString()) ?? typeof(RecordUnknown);
        }

        /// <summary>
        /// Instance a new record from the given type
        /// </summary>
        /// <param name="type">Record type enum or QType casted</param>
        /// <returns>new record of the given type</returns>
        public static Record FromType(RecordType type)
        {
            return types.FirstOrDefault(x => x.Name == "Record" + type.ToString()).CreateIstance() as Record ?? new RecordUnknown();
        }
    }
}