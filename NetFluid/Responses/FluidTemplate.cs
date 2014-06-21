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

using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using NetFluid.Responses;

namespace NetFluid
{
    /// <summary>
    /// Netfluid default HTML template response
    /// </summary>
    public class FluidTemplate : IResponse
    {
        private readonly string path;
        private readonly object[] _param;
        private readonly MethodInfo _template;

        private Delegate template;
        /// <summary>
        /// HTML template path
        /// </summary>
        /// <param name="path">path to the template (ex: ./UI/index.html)</param>
        /// <param name="parameters">optional parameters</param>
        public FluidTemplate(string path, params object[] parameters)
        {
            var s = new System.Diagnostics.StackFrame(1);
            _template = TemplateCompiler.Get(path, s.GetMethod().DeclaringType);

            template = _template.ToDelegate(null); 

            if (parameters == null)
            {
                _param = new object[2];
                _param[1] = null;
                return;
            }

            var defaultValues = _template.GetParameters().Select(x => x.ParameterType.DefaultValue()).ToArray();

            if (defaultValues.Length > 1)
            {
                if (parameters.Length == 0 && defaultValues.Length != 0)
                {
                    _param = defaultValues;
                    return;
                }

                _param = new object[parameters.Length + 1];
                Array.Copy(parameters, 0, _param, 1, parameters.Length);
                return;
            }
            _param = new object[1];

            this.path = path;
        }

        #region IResponse Members

        public void SetHeaders(Context cnt)
        {
        }

        public void SendResponse(Context cnt)
        {
            try
            {
                _param[0] = cnt;
                //_template.Invoke(null, _param);
                template.DynamicInvoke(_param);
            }
            catch (Exception ex)
            {
                Engine.Logger.Log(LogLevel.Exception,"Exception in "+path+" template",ex.InnerException);

                if (Engine.DevMode)
                {
                    try
                    {
                        cnt.Writer.Write("<html><body>");
                        cnt.Writer.Write("<h1>Exception</h1>");
                        cnt.Writer.Write("<div>To turn off this view set the DevMode value on false in AppConfig on by code with NetFluid.Engine.DevMode</div>");
                        cnt.Writer.Write("<h2>"+ex.InnerException.Message+"</h2>");
                        cnt.Writer.Write("</body></html>");
                    }
                    catch (Exception)
                    {
                    }
                }
            }

        }

        #endregion

        /// <summary>
        /// Invoked if a compilation error is present
        /// </summary>
        public static event Action<object, CompilerErrorCollection> OnCompilationError;

        internal static void OnError(CompilerErrorCollection errors)
        {
            if (OnCompilationError != null)
            {
                OnCompilationError(null, errors);
            }
        }
    }
}