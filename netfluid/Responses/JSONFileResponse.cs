// ********************************************************************************************************
// <copyright company="NetFluid">
//     Copyright (c) 2013 Matteo Fabbri. All rights reserved.
// </copyright>
// ********************************************************************************************************
// The contents of this file are subject to the GNU AGPL v3.0 (the "License"); 
// you may not use this file except in compliance with the License. You may obtain a copy of the License at 
// http://www.fsf.org/licensing/licenses/agpl-3.0.html 
// 
// Commercial licenses are also available from http://netfluid.org/, including free evaluation licenses.
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF 
// ANY KIND, either express or implied. See the License for the specific language governing rights and 
// limitations under the License. 
// 
// The Initial Developer of this file is Matteo Fabbri.
// 
// Contributor(s): (Open source contributors should list themselves and their modifications here). 
// Change Log: 
// Date           Changed By      Notes
// 23/10/2013    Matteo Fabbri      Inital coding
// ********************************************************************************************************

using System.IO;

namespace NetFluid
{
    public class JSONFileResponse : IResponse
    {
        public JSONFileResponse(object obj, string filename)
        {   
            FileName = filename;
            Object = obj;
        }

        public string FileName { get; set; }
        public object Object { get; set; }

        #region IResponse Members

        public void SetHeaders(Context cnt)
        {
            cnt.Response.Headers["Content-Disposition"] = "attachment; filename=\"" + FileName + "\"";
            cnt.Response.ContentType = "application/json";
        }

        public void SendResponse(Context cnt)
        {
            if (Object is string)
                cnt.Writer.Write(Object as string);
            else
                JSON.Serialize(Object, cnt.Writer,true,true);

            cnt.Close();
        }

        #endregion
    }
}