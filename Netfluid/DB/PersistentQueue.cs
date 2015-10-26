
namespace Netfluid.DB
{
    public class PersistentQueue<T>
    {
        DiskCollection disk;

        public PersistentQueue(string path)
        {
            disk = new DiskCollection(path);
        }

        public string Directory => disk.Directory;
        public string Name => disk.Name;
        public long Count => disk.Count;

        public void Push(T obj)
        {
            disk.Push(BSON.Serialize(obj));
        }

        public T Pop()
        {
            var obj = disk.Pop();

            if (obj == null) return default(T);

            return BSON.Deserialize<T>(obj);
        }
    }
}
