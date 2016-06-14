
using System;
using System.Threading;
using System.Threading.Tasks;
namespace Netfluid.Smtp
{
	class DataCommand : SmtpCommand
	{
		readonly Func<SmtpSession,string> OnMessage;

		public DataCommand(Func<SmtpSession, string> onMessage)
		{
			OnMessage = onMessage;
		}
		public override async Task ExecuteAsync(SmtpSession context, CancellationToken cancellationToken)
		{
			if (context.To.Count == 0)
			{
				await context.NetworkTextStream.ReplyAsync(SmtpResponse.NoValidRecipientsGiven, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				await context.NetworkTextStream.ReplyAsync(new SmtpResponse(SmtpReplyCode.StartMailInput, "end with <CRLF>.<CRLF>"), cancellationToken).ConfigureAwait(false);
				string text;
				while ((text = await context.NetworkTextStream.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != ".")
				{
					context.AppendLine(text.TrimStart(new char[]
					{
						'.'
					}));
				}
				try
				{
					string arg = OnMessage(context);
					await context.NetworkTextStream.ReplyAsync(new SmtpResponse(SmtpReplyCode.Ok, string.Format("mail accepted ({0})", arg)), cancellationToken).ConfigureAwait(false);
				}
				catch (Exception)
				{
					await context.NetworkTextStream.ReplyAsync(new SmtpResponse(SmtpReplyCode.MailboxUnavailable, null), cancellationToken).ConfigureAwait(false);
				}
			}
		}
	}
}
