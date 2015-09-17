#region Header
// Copyright (c) 2013-2015 Hans Wolff
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
#endregion


using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Netfluid.Smtp
{
    public class SmtpCommandParser
    {
        private readonly Dictionary<string, Func<string, string, SmtpResponse>> _commandMapping;
        private readonly SmtpServer _server;


        public SmtpSessionInfo SessionInfo { get; private set; }
        public bool InDataMode { get; set; }

        public SmtpCommandParser(SmtpServer server, SmtpSessionInfo sessionInfo)
        {
            if (sessionInfo == null) throw new ArgumentNullException("sessionInfo");

            SessionInfo = sessionInfo;
            _server = server;

            _commandMapping = new Dictionary<string, Func<string, string, SmtpResponse>>
            {
                { "DATA", ProcessCommandDataStart },
                { "EHLO", ProcessCommandEhlo },
                { "HELO", ProcessCommandHelo },
                { "MAIL FROM:", ProcessCommandMailFrom },
                { "NOOP", ProcessCommandNoop },
                { "QUIT", ProcessCommandQuit },
                { "RCPT TO:", ProcessCommandRcptTo },
                { "RSET", ProcessCommandRset },
                { "VRFY", ProcessCommandVrfy },
            };
        }

        SmtpResponse ProcessCommandDataStart(string name, string arguments)
        {
            var notIdentified = CreateResponseIfNotIdentified();
            if (notIdentified.HasValue) return notIdentified;

            var hasNotMailFrom = CreateResponseIfHasNotMailFrom();
            if (hasNotMailFrom.HasValue) return hasNotMailFrom;

            var hasNoRecipients = CreateResponseIfHasNoRecipients();
            if (hasNoRecipients.HasValue) return hasNoRecipients;

            var response = SmtpResponse.DataStart;

            if (response.Success)
            {
                InDataMode = true;
                SessionInfo.HasData = true;
            }

            return response;
        }

        SmtpResponse VerifyIdentification(SmtpSessionInfo sessionInfo, SmtpIdentification smtpIdentification)
        {
            if (smtpIdentification == null) throw new ArgumentNullException("smtpIdentification");

            switch (smtpIdentification.Mode)
            {
                case SmtpIdentificationMode.HELO:
                    return SmtpResponse.OK;

                case SmtpIdentificationMode.EHLO:
                    var smtpCapabilities = new[] { SmtpCapability.Pipelining, SmtpCapability.MaxSizePerEmail(_server.Configuration.MaxMailMessageSize) };

                    if (!smtpCapabilities.Any())
                        return SmtpResponse.OK;

                    var additionalLines = smtpCapabilities.Skip(1).Select(capability => "250-" + capability).ToList();

                    var response = new SmtpResponse(250, smtpCapabilities.First().ToString(), additionalLines);
                    return response;
            }

            return new SmtpResponse(500, "Invalid Identification (" + smtpIdentification.Mode + ")");
        }

        SmtpResponse ProcessCommandEhlo(string name, string arguments)
        {
            if (String.IsNullOrWhiteSpace(arguments))
            {
                return SmtpResponse.EhloMissingDomainAddress;
            }

            var identification = new SmtpIdentification(SmtpIdentificationMode.EHLO, arguments);
            var response = VerifyIdentification(SessionInfo, identification);

            if (response.Success)
            {
                SessionInfo.Identification = identification;
            }

            return response;
        }

        SmtpResponse ProcessCommandHelo(string name, string arguments)
        {
            if (String.IsNullOrWhiteSpace(arguments))
            {
                return SmtpResponse.HeloMissingDomainAddress;
            }

            var identification = new SmtpIdentification(SmtpIdentificationMode.HELO, arguments);
            var response = VerifyIdentification(SessionInfo, identification);

            if (response.Success)
            {
                SessionInfo.Identification = identification;
            }

            return response;
        }

        SmtpResponse ProcessCommandMailFrom(string name, string arguments)
        {
            var notIdentified = CreateResponseIfNotIdentified();
            if (notIdentified.HasValue) return notIdentified;

            var mailFrom = arguments.Trim();
            MailAddress address;

            try
            {
                address = new MailAddress(mailFrom.Contains(' ') ? mailFrom.Substring(0, mailFrom.IndexOf(' ')) : mailFrom);
            }
            catch (FormatException)
            {
                return SmtpResponse.SyntaxError;
            }

            var response = _server.VerifyMailFrom != null ? _server.VerifyMailFrom(SessionInfo, address) : SmtpResponse.OK;

            if (response.Success)
            {
                SessionInfo.MailFrom = address;
            }

            return response;
        }

        SmtpResponse ProcessCommandNoop(string name, string arguments)
        {
            return SmtpResponse.OK;
        }

        SmtpResponse ProcessCommandQuit(string name, string arguments)
        {
            return SmtpResponse.Disconnect;
        }

        SmtpResponse ProcessCommandRcptTo(string name, string arguments)
        {
            var notIdentified = CreateResponseIfNotIdentified();
            if (notIdentified.HasValue) return notIdentified;

            var hasNotMailFrom = CreateResponseIfHasNotMailFrom();
            if (hasNotMailFrom.HasValue) return hasNotMailFrom;

            var recipient = arguments.Trim();
            MailAddress mailAddress;

            try
            {
                mailAddress = new MailAddress(recipient.Contains(' ') ? recipient.Substring(0, recipient.IndexOf(' ')) : recipient);
            }
            catch (FormatException)
            {
                return SmtpResponse.SyntaxError;
            }


            var response = _server.VerifyRecipientTo != null ? _server.VerifyRecipientTo(SessionInfo, mailAddress) : SmtpResponse.OK;

            if (response.Success)
            {
                SessionInfo.Recipients.Add(mailAddress);
            }

            return response;
        }

        SmtpResponse ProcessCommandRset(string name, string arguments)
        {
            SessionInfo.Reset();
            InDataMode = false;
            return SmtpResponse.OK;
        }

        SmtpResponse ProcessCommandVrfy(string name, string arguments)
        {
            var notIdentified = CreateResponseIfNotIdentified();
            if (notIdentified.HasValue) return notIdentified;

            if (String.IsNullOrWhiteSpace(arguments))
            {
                return SmtpResponse.VrfyMissingArguments;
            }
            return SmtpResponse.VerifyDummyResponse;
        }

        private SmtpResponse CreateResponseIfNotIdentified()
        {
            if (SessionInfo.Identification.Mode == SmtpIdentificationMode.NotIdentified)
            {
                return SmtpResponse.NotIdentified;
            }
            return SmtpResponse.None;
        }

        private SmtpResponse CreateResponseIfHasNotMailFrom()
        {
            if (SessionInfo.MailFrom == null)
            {
                return SmtpResponse.UseMailFromFirst;
            }
            return SmtpResponse.None;
        }

        private SmtpResponse CreateResponseIfHasNoRecipients()
        {
            return !SessionInfo.Recipients.Any()
                ? SmtpResponse.MustHaveRecipientFirst
                : SmtpResponse.None;
        }

        SmtpResponse ProcessRawLine(string line)
        {
            _server.Logger.Debug("<<< " + line);
            return SmtpResponse.None;
        }

        SmtpResponse ProcessCommandDataEnd()
        {
            _server.Logger.Debug("DataEnd received");

            var successMessage = String.Format("{0} bytes received", SessionInfo.DataStream.Length);
            var response = SmtpResponse.OK.CloneAndChange(successMessage);

            if (response.Success)
            {
                InDataMode = false;
            }

            if (_server.RequestCompleted != null)
            {
                SessionInfo.DataStream.Seek(0, System.IO.SeekOrigin.Begin);
                Task.Factory.StartNew(x => _server.RequestCompleted(x as SmtpSessionInfo), SessionInfo);
            }
            return response;
        }

        SmtpResponse ProcessDataLine(byte[] line)
        {
            SessionInfo.DataStream.Write(line,0,line.Length);
            SessionInfo.DataStream.WriteByte(13);
            SessionInfo.DataStream.WriteByte(10);

            return SmtpResponse.None;
        }

        public SmtpResponse ProcessLineCommand(byte[] lineBuf)
        {
            try
            {
                return ProcessLineCommandDontCareAboutException(lineBuf);
            }
            catch (Exception ex)
            {
                _server.Logger.Error(ex);
                return SmtpResponse.InternalServerError;
            }
        }

        private SmtpResponse ProcessLineCommandDontCareAboutException(byte[] lineBuf)
        {
            SmtpResponse smtpResponse;

            if (IsLineTooLong(lineBuf, out smtpResponse))
                return smtpResponse;

            if (InDataMode)
                return ProcessLineInDataMode(lineBuf);

            var line = Encoding.UTF8.GetString(lineBuf);

            if (ProcessRawLineHasResponse(line, out smtpResponse))
                return smtpResponse;

            Func<string, string, SmtpResponse> commandFunc;

            var commandWithArguments = GetCommandWithArgumentsAndCommandFunction(line, out commandFunc);

            if (commandFunc != null)
            {
                var response = commandFunc(commandWithArguments.Command, commandWithArguments.Arguments);
                return response;
            }

            return SmtpResponse.NotImplemented;
        }

        private static bool IsLineTooLong(byte[] lineBuf, out SmtpResponse smtpResponse)
        {
            if (lineBuf.Length > 2040)
            {
                smtpResponse = SmtpResponse.LineTooLong;
                return true;
            }

            smtpResponse = SmtpResponse.None;
            return false;
        }

        private bool ProcessRawLineHasResponse(string line, out SmtpResponse smtpResponse)
        {
            smtpResponse = ProcessRawLine(line);
            return (smtpResponse != SmtpResponse.None);
        }

        private SmtpResponse ProcessLineInDataMode(byte[] lineBuf)
        {
            if (lineBuf.Length == 1 && lineBuf[0] == '.')
            {
                return ProcessCommandDataEnd();
            }
            return ProcessDataLine(lineBuf);
        }

        private CommandWithArguments GetCommandWithArgumentsAndCommandFunction(string line, out Func<string, string, SmtpResponse> commandFunc)
        {
            var commandWithArguments = SplitCommandWithArgumentsAtCharacter(line, ':');

            if (commandWithArguments == CommandWithArguments.Empty ||
                !_commandMapping.TryGetValue(commandWithArguments.Command, out commandFunc))
            {
                commandWithArguments = SplitCommandWithArgumentsAtCharacter(line, ' ');
            }

            if (commandWithArguments == CommandWithArguments.Empty ||
                !_commandMapping.TryGetValue(commandWithArguments.Command, out commandFunc))
            {
                commandWithArguments = new CommandWithArguments { Command = line.ToUpperInvariant().Trim() };
                _commandMapping.TryGetValue(commandWithArguments.Command, out commandFunc);
            }
            return commandWithArguments;
        }

        private CommandWithArguments SplitCommandWithArgumentsAtCharacter(string line, char splitChar)
        {
            var pos = line.IndexOf(splitChar);
            if (pos >= 0)
            {
                var command = line.Substring(0, pos + 1).ToUpperInvariant().Trim();
                var arguments = line.Substring(pos + 1);

                return new CommandWithArguments { Command = command, Arguments = arguments };
            }

            return CommandWithArguments.Empty;
        }

        private class CommandWithArguments
        {
            public static readonly CommandWithArguments Empty = new CommandWithArguments();

            public string Command { get; set; }
            public string Arguments { get; set; }
        }
    }
}
