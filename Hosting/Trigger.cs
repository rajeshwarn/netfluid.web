using System.Threading.Tasks;

namespace Netfluid
{
    public class Trigger:Route
    {
        internal override dynamic Handle(Context cnt)
        {
            Task.Factory.StartNew(() => base.Handle(cnt));
            return true;
        }
    }
}
