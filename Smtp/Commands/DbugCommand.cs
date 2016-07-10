using System;
using System.Threading;
using System.Threading.Tasks;
namespace Netfluid.Smtp
{
	class DbugCommand : SmtpCommand
	{
		public override async Task ExecuteAsync(SmtpSession context, CancellationToken cancellationToken)
		{
			await context.NetworkTextStream.ReplyAsync(SmtpResponse.Ok, cancellationToken).ConfigureAwait(false);
		}
	}
}
