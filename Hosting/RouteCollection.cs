using System;
using System.Collections.Generic;

namespace Netfluid
{
    public class RouteCollection<T>: List<T> where T: Route, new()
    {
        #region ADD FUNC
        public RouteCollection<T> Add<T1>(string method, string url, Func<T1> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }

        public RouteCollection<T> Add<T1,T2>(string method, string url, Func<T1,T2> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1,T2,T3>(string method, string url, Func<T1,T2,T3> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1,T2,T3,T4>(string method, string url, Func<T1,T2,T3,T4> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4,T5>(string method, string url, Func<T1, T2, T3, T4,T5> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5,T6>(string method, string url, Func<T1, T2, T3, T4, T5,T6> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6,T7>(string method, string url, Func<T1, T2, T3, T4, T5, T6,T7> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6,T7,T8>(string method, string url, Func<T1, T2, T3, T4, T5, T6,T7,T8> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8,T9>(string method, string url, Func<T1, T2, T3, T4, T5, T6, T7, T8,T9> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9,T10>(string method, string url, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9,T10> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10,T11>(string method, string url, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10,T11> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11,T12>(string method, string url, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11,T12> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11,T12,T13>(string method, string url, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11,T12,T13> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13,T14>(string method, string url, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13,T14> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14,T15>(string method, string url, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14,T15> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        #endregion

        #region ADD Action
        public RouteCollection<T> Add<T1>(string method, Action<T1> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }

        public RouteCollection<T> Add<T1, T2>(string method, Action<T1, T2> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3>(string method, Action<T1, T2, T3> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4>(string method, Action<T1, T2, T3, T4> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5>(string method, Action<T1, T2, T3, T4, T5> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6>(string method, Action<T1, T2, T3, T4, T5, T6> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7>(string method, Action<T1, T2, T3, T4, T5, T6, T7> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8>(string method, Action<T1, T2, T3, T4, T5, T6, T7, T8> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string method, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string method, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string method, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string method, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string method, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string method, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string method, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }

        public RouteCollection<T> Add<T1>(string method, string url, Action<T1> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }

        public RouteCollection<T> Add<T1, T2>(string method, string url, Action<T1, T2> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3>(string method, string url, Action<T1, T2, T3> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4>(string method, string url, Action<T1, T2, T3, T4> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5>(string method, string url, Action<T1, T2, T3, T4, T5> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6>(string method, string url, Action<T1, T2, T3, T4, T5, T6> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7>(string method, string url, Action<T1, T2, T3, T4, T5, T6, T7> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8>(string method, string url, Action<T1, T2, T3, T4, T5, T6, T7, T8> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string method, string url, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string method, string url, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string method, string url, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string method, string url, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string method, string url, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string method, string url, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        public RouteCollection<T> Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string method, string url, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> mdelegate)
        {
            var r = (new T()
            {
                Url = url,
                HttpMethod = url,
                Target = mdelegate.Target,
                MethodInfo = mdelegate.Method
            });

            base.Add(r);
            return this;
        }
        #endregion

        public Delegate this[string method, string url]
        {
            set
            {
                var r = (new T()
                {
                    Url = url,
                    HttpMethod = method,
                    Target =  value.Target,
                    MethodInfo = value.Method
                });

                base.Add(r);
            }
        }
    }
}
