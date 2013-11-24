using System;
using System.Collections.Generic;
using NetFluid;

namespace _3.UserStat.Analytics
{
    class DataCollector:FluidController
    {
        //THIS IS THE METHOD EXECUTED EVERY TIME A CLIENT DO A REQUEST
        public override object Run()
        {
            var data = UserData.Parse(Context.SessionId);

            if (data==null)
            {
                data = new UserData
                {
                    Id = Context.SessionId,
                    Interactions = new List<Interaction>(new[]{new Interaction{Timestamp = DateTime.Now, Url = Request.Url}}),
                    Ip = Context.RemoteEndPoint.ToString(),
                    Languages = Request.UserLanguages,
                    Referer = Request.UrlReferrer,
                    UserAgent = Request.UserAgent
                };
                UserData.Save(data);
            }
            else
            {
                data.Interactions.Add(new Interaction { Timestamp = DateTime.Now, Url = Request.Url });
            }
            return null;
        }

        [Route("/analytics")]
        public FluidTemplate ShowData()
        {
            return new FluidTemplate("./Analytics/UI/Analytics.html",new Report(UserData.All));
        }
    }
}
