using System.Threading.Tasks;

namespace Netfluid
{
    public class Trigger:Route
    {
        public override dynamic Handle(Context cnt)
        {
            Task.Factory.StartNew(() => base.Handle(cnt));
            return true;
        }
    }
}
