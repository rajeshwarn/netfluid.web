using Netfluid.DB;
using Netfluid.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace Netfluid.OLAP
{
    public class FactCube<T> where T : Fact
    {
        KeyValueStore<Fact> disk;
        Tree<int, string> hour;
        Tree<int, string> day;
        Tree<int, string> month;
        Tree<int, string> year;

        ReaderWriterLockSlim locker;

        public FactCube(string path)
        {
            disk = new KeyValueStore<Fact>(path);
            hour = Index.MultipleIntIndex(Path.Combine(disk.Directory,disk.Name+"_h.sidx"));
            day = Index.MultipleIntIndex(Path.Combine(disk.Directory, disk.Name + "_d.sidx"));
            month = Index.MultipleIntIndex(Path.Combine(disk.Directory, disk.Name + "_m.sidx"));
            year = Index.MultipleIntIndex(Path.Combine(disk.Directory, disk.Name + "_y.sidx"));

            locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        public void Add(T fact)
        {
            locker.EnterReadLock();

            var f = hour.EqualTo(fact.Hour).Intersect(day.EqualTo(fact.Day))
                                           .Intersect(month.EqualTo(fact.Month))
                                           .Intersect(month.EqualTo(fact.Year));



            locker.ExitReadLock();
        }
    }
}
