using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Netfluid.Db
{
    public class ThreadSafeEnumerator<T> : IEnumerable<T>, IEnumerator<T>
    {
        IEnumerator<T> inc;
        ReaderWriterLockSlim locker;


        public ThreadSafeEnumerator(IEnumerable<T> inc, ReaderWriterLockSlim locker)
        {
            this.inc = inc.GetEnumerator();
            this.locker = locker;
        }

        public ThreadSafeEnumerator(IEnumerator<T> inc, ReaderWriterLockSlim locker)
        {
            this.inc = inc;
            this.locker = locker;
        }

        public T Current
        {
            get
            {
                T obj;
                locker.EnterReadLock();
                obj = inc.Current;
                locker.ExitReadLock();
                return obj;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                T obj;
                locker.EnterReadLock();
                obj = inc.Current;
                locker.ExitWriteLock();
                return obj;
            }
        }

        public void Dispose()
        {
            try
            {
                locker.EnterWriteLock();
                inc.Dispose();
                locker.ExitWriteLock();
            }
            catch (Exception)
            {
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            bool obj;
            locker.EnterReadLock();
            obj = inc.MoveNext();
            locker.ExitReadLock();
            return obj;
        }

        public void Reset()
        {
            locker.EnterWriteLock();
            inc.Reset();
            locker.ExitWriteLock();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }
    }
}
