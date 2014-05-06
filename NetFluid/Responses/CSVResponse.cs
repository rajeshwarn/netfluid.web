using System.Collections.Generic;

namespace NetFluid
{
    public class CSVResponse<T> : IResponse
    {
        public char TextQualifier;
        public char FieldSeparator;
        public IEnumerable<T> Collection;

        public CSVResponse(IEnumerable<T> collection)
        {
            Collection = collection;
            FieldSeparator = ';';
            TextQualifier = '"';
        }

        public void SetHeaders(Context cnt)
        {
            cnt.Response.ContentType = "application/csv";
        }

        public void SendResponse(Context cnt)
        {
            CSV.Serialize(Collection, cnt.Writer, FieldSeparator, TextQualifier);
            cnt.Writer.Flush();
            cnt.Close();
        }
    }
}
