
using System.IO;
using System.Linq;
using System.Threading;

namespace Netfluid.DB
{
    public class FactList<T> : KeyValueStore<T> where T : Fact
    {
        Tree<int, string> hour;
        Tree<int, string> day;
        Tree<int, string> month;
        Tree<int, string> year;
        Tree<string, string> tag;

        ReaderWriterLockSlim locker;

        public FactList(string path):base(path)
        {
            hour = Index.MultipleIntIndex(Path.Combine(Directory,Name+"_hour.sidx"));
            day = Index.MultipleIntIndex(Path.Combine(Directory, Name + "_day.sidx"));
            month = Index.MultipleIntIndex(Path.Combine(Directory, Name + "_month.sidx"));
            year = Index.MultipleIntIndex(Path.Combine(Directory, Name + "_year.sidx"));
            tag = Index.MultipleStringIndex(Path.Combine(Directory, Name + "_tag.sidx"));

            locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        public void Add(T fact)
        {
            var tags = string.IsNullOrEmpty(fact.Description) ? new string[0] : fact.Description.RegexValues("#[ ^]+").ToArray();

            locker.EnterReadLock();

            var id = Push(fact);
            hour.Insert(fact.DateTime.Hour,id);
            day.Insert(fact.DateTime.Day, id);
            month.Insert(fact.DateTime.Month, id);
            year.Insert(fact.DateTime.Year, id);

            tags.ForEach(x=>tag.Insert(x,id));

            locker.ExitReadLock();
        }
    }
}
