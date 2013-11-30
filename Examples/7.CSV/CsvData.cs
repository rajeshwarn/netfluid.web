using NetFluid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7.CSV
{
    public class CsvData:FluidPage
    {
        IEnumerable<Client> randomList()
        {
            for (int i = 0; i < 10000; i++)
            {
                yield return Client.RandomClient();
            }
        }

        [Route("/write")]
        public CSVResponse<Client> Write()
        {
            return new CSVResponse<Client>(randomList());
        }
    }
}
