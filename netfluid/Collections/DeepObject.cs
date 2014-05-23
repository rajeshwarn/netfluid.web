using System.Collections.Concurrent;
using System.Dynamic;

namespace NetFluid.Collections
{

    /// <summary>
    /// FIXME
    /// </summary>
    class DeepObject : DynamicObject
    {
        private readonly ConcurrentDictionary<object, object> values;

        private DeepObject(object boxed)
        {
            values = new ConcurrentDictionary<object, object>();
            values.TryAdd(0, boxed);
        }

        public DeepObject()
        {
            values = new ConcurrentDictionary<object, object>();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (values.TryGetValue(binder.Name, out result))
                return true;

            result = new DeepObject();

            return values.TryAdd(binder.Name, result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (!(value is DeepObject))
            {
                value = new DeepObject(value);
            }
            values.AddOrUpdate(binder.Name, x => value, (x, y) => value);
            return true;
        }
    }
}
