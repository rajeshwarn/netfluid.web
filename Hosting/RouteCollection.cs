using System;
using System.Linq;
using System.Collections.Generic;

namespace Netfluid
{
    public class RouteCollection<T>: List<T> where T: Route, new()
    {
        public T this[string httpMethod, string url]
        {
            set
            {
                value.HttpMethod = httpMethod;
                value.Url = url;
                Add(value);
            }
            get
            {
                return this.FirstOrDefault(x => x.HttpMethod == httpMethod && x.Url == url);
            }
        }

        public T this[string url]
        {
            set
            {
                value.Url = url;
                Add(value);
            }
            get
            {
                return this.FirstOrDefault(x => x.Url == url);
            }
        }
    }
}
