using System;
using System.Threading;
using System.Threading.Tasks;
namespace Netfluid.Smtp
{
	class NoopCommand : SmtpCommand
	{
		public override Task ExecuteAsync(SmtpSession context, CancellationToken cancellationToken)
		{
			return context.NetworkTextStream.ReplyAsync(SmtpResponse.Ok, cancellationToken);
		}
	}
}
