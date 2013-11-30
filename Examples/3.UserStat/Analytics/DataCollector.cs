using System;
using System.Collections.Generic;
using NetFluid;

namespace _3.UserStat.Analytics
{
    class DataCollector
    {
        static DataCollector()
        {
            //We collect data every time the server recieve a request
            Engine.SetController(Context =>
            {
                var data = UserData.Parse(Context.SessionId);

                if (data == null)
                {
                    data = new UserData
                    {
                        Id = Context.SessionId,
                        Interactions = new List<Interaction>(new[] { new Interaction { Timestamp = DateTime.Now, Url = Context.Request.Url } }),
                        Ip = Context.RemoteEndPoint.ToString(),
                        Languages = Context.Request.UserLanguages,
                        Referer = Context.Request.UrlReferrer,
                        UserAgent = Context.Request.UserAgent
                    };
                    UserData.Save(data);
                }
                else
                {
                    data.Interactions.Add(new Interaction { Timestamp = DateTime.Now, Url = Context.Request.Url });
                }
            });
        
        }


        [Route("/analytics")]
        public FluidTemplate ShowData()
        {
            return new FluidTemplate("./Analytics/UI/Analytics.html",new Report(UserData.All));
        }
    }
}
