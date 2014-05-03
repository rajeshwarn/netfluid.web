using System;
using System.Collections.Generic;
using System.Linq;

namespace NetFluid.Collections
{
    public class InMemoryOlap
    {
        private readonly OlapNode root = new OlapNode();

        public object Get(object value, params object[] coordinates)
        {
            return root.GetValue(coordinates);
        }

        public void Set(object value, params object[] coordinates)
        {
            root.SetValue(value,coordinates);
        }

        public int Count {
            get { return root.Count; }
        }
    }

    class OlapNode
    {
        private Dictionary<object, OlapNode> childs;
        private Dictionary<object, object> values;

        public static readonly object Jolly = new object();

        Dictionary<object, OlapNode> Childs
        {
            //Istanzia i sottonodi solo quando necessario
            get { return childs ?? (childs = new Dictionary<object, OlapNode>()); }
        }

        Dictionary<object, object> Values
        {
            //Istanzia i valori solo quando necessario
            get { return values ?? (values = new Dictionary<object, object>()); }
        }

        public int Count
        {
            get { return Values.Count + Childs.Sum(x => x.Value.Count); }
        }

        public void SetValue(object value, object[] coords, int index=0)
        {
            //Memorizza o aggiorna il valore per la coordinata corrente
            Values[coords[index]] = value;

            //Se non sono finite le coordinate passa il lavoro alla dimensione
            //successiva
            if (index < coords.Length)
            {
                OlapNode child;
                if (!Childs.TryGetValue(coords[index], out child))
                {
                    child = new OlapNode();
                    Childs.Add(coords[index], child);
                }

                index++;
                if (index < coords.Length)
                    child.SetValue(value, coords,index);
            }
        }

        public object GetValue(object[] coords, int index = 0)
        {
            //Se la coordinata attuale è l'ultima delle richieste
            //ritorna il relativo valore
            if (index != coords.Length - 1)
            {
                OlapNode child;
                if (childs.TryGetValue(coords[index], out child))
                    return child.GetValue(coords, ++index);

                throw new IndexOutOfRangeException("Wrong number of coordinates " + coords);
            }

            //Altrimenti passa il compito alla coordinata successiva
            object value;
            if (values != null && values.TryGetValue(coords[coords.Length - 1], out value))
                return value;

            throw new IndexOutOfRangeException("Missing value on coordinates "+coords);
        }
    }
}
