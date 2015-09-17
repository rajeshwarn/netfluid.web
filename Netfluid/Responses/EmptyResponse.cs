using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netfluid.Responses
{
    class EmptyResponse : IResponse
    {
        public StatusCode StatusCode { get; set; }

        public EmptyResponse(StatusCode status = StatusCode.Ok)
        {

        }

        public void Dispose()
        {
            
        }

        public void SendResponse(Context cnt)
        {
            throw new NotImplementedException();
        }

        public void SetHeaders(Context cnt)
        {
            cnt.Response.StatusCode = (int)StatusCode;
        }
    }
}
