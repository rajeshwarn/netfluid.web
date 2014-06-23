using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetFluid.Collections;

namespace NetFluid.Service.Run
{
    [Route("run")]
    public class RunManager:FluidPage
    {
        public static IRepository<RunApp> Run;

        public override void OnLoad()
        {
            Run = new XMLRepository<RunApp>("run.xml");
        }

        [Route("update")]
        [Route("add")]
        public IResponse Save()
        {
            return null;
        }

        [Route("delete")]
        public IResponse Delete()
        {
            return null;
        }
    }
}
