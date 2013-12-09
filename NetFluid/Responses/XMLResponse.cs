using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetFluid
{
    public class XMLResponse:IResponse
    {
        object obj;

        public XMLResponse(object obj)
        {
            this.obj = obj;
        }

        public void SendResponse(Context cnt)
        {
            cnt.Response.ContentType = "application/xml";
            cnt.SendHeaders();

            obj.ToXML(cnt.OutputStream);
        }
    }
}
