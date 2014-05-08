using System;
using System.Collections.Generic;
using System.Linq;
using NetFluid.DNS.Records;

namespace NetFluid.DNS
{
        [Serializable]
    public class Response
    {
        /// <summary>
        ///     List of Record records
        /// </summary>
        public List<Record> Additionals;

        /// <summary>
        ///     List of Record records
        /// </summary>
        public List<Record> Answers;

        /// <summary>
        ///     List of Record records
        /// </summary>
        public List<Record> Authorities;

        public Header Header;

        /// <summary>
        ///     The Size of the message
        /// </summary>
        public int MessageSize;

        /// <summary>
        ///     List of Question records
        /// </summary>
        public List<Question> Questions;

        public Response()
        {
            Questions = new List<Question>();
            Answers = new List<Record>();
            Authorities = new List<Record>();
            Additionals = new List<Record>();

            MessageSize = 0;
            Header = new Header();
        }

        /// <summary>
        ///     List of RecordMX in Response.Answers
        /// </summary>
        public Record[] Records
        {
            get { return Answers.ToArray(); }
        }

        /// <summary>
        ///     List of RecordMX in Response.Answers
        /// </summary>
        public RecordMX[] RecordsMX
        {
            get { return Answers.OfType<RecordMX>().OrderBy(x => x).ToArray(); }
        }

        /// <summary>
        ///     List of RecordTXT in Response.Answers
        /// </summary>
        public RecordTXT[] RecordsTXT
        {
            get { return Answers.OfType<RecordTXT>().ToArray(); }
        }

        /// <summary>
        ///     List of RecordA in Response.Answers
        /// </summary>
        public RecordA[] RecordsA
        {
            get { return Answers.OfType<RecordA>().ToArray(); }
        }

        /// <summary>
        ///     List of RecordPTR in Response.Answers
        /// </summary>
        public RecordPTR[] RecordsPTR
        {
            get { return Answers.OfType<RecordPTR>().ToArray(); }
        }

        /// <summary>
        ///     List of RecordCNAME in Response.Answers
        /// </summary>
        public RecordCNAME[] RecordsCNAME
        {
            get { return Answers.OfType<RecordCNAME>().ToArray(); }
        }

        /// <summary>
        ///     List of RecordAAAA in Response.Answers
        /// </summary>
        public RecordAAAA[] RecordsAAAA
        {
            get { return Answers.OfType<RecordAAAA>().ToArray(); }
        }

        /// <summary>
        ///     List of RecordNS in Response.Answers
        /// </summary>
        public RecordNS[] RecordsNS
        {
            get { return Answers.OfType<RecordNS>().ToArray(); }
        }

        /// <summary>
        ///     List of RecordSOA in Response.Answers
        /// </summary>
        public RecordSOA[] RecordsSOA
        {
            get { return Answers.OfType<RecordSOA>().ToArray(); }
        }

        public Record[] RecordsRR
        {
            get { return Answers.Concat(Authorities.Concat(Additionals)).ToArray(); }
        }
    }
}