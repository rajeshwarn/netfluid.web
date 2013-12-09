using NetFluid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7.CSV
{
    [Route("dataservice")]
    public class CsvData:FluidPage
    {
        #region GENERATES IN-MEMRY RANDOM LIST
        static IEnumerable<Client> clients;
        static CsvData()
        {
            clients = randomList();
        }

        static IEnumerable<Client> randomList()
        {
            for (int i = 0; i < 10000; i++)
            {
                yield return Client.RandomClient();
            }
        }
        #endregion

        [Route("/csv")]
        public CSVResponse<Client> CSV()
        {
            return new CSVResponse<Client>(CsvData.clients);
        }


        [Route("/xml")]
        public XMLResponse XML()
        {
            return new XMLResponse(CsvData.clients);
        }

        [Route("/json")]
        public JSONResponse JSON()
        {
            return new JSONResponse(CsvData.clients);
        }
    }
}
