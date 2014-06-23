using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetFluid.Service.Run
{
    [Serializable]
    public class RunApp : IDatabaseObject
    {
        public string Id { get; set; }
    }
}
