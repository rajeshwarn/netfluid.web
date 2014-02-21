using System.IO;
using System.Text;
using System.Threading;

namespace NetFluid.IO
{
    public class SharedFile
    {
        Timer timer;
        Thread thread;

        string path;

        long position;
        Stream writer;

        public SharedFile(string path)
        {
            this.path = Path.GetFullPath(path);
            writer = new FileStream(path, FileMode.Append, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        public void Write(byte[] payload)
        {
        }

        public void Write(string str)
        {
            Write(Encoding.UTF8.GetBytes(str));
        }

        public void WriteLine(string str)
        {
            Write(Encoding.UTF8.GetBytes(str+"\r\n"));
        }
    }
}
