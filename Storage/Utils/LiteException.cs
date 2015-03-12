using System;

namespace FluidDB
{
    /// <summary>
    /// The main exception for LiteDB
    /// </summary>
    public class LiteException : Exception
    {
        public int ErrorCode { get; private set; }

        public LiteException(string message)
            : base(message)
        {
        }

        public LiteException(int code, string message, params object[] args)
            : base(string.Format(message, args))
        {
            this.ErrorCode = code;
        }
    }
}
