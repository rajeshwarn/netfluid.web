namespace FluidDB
{
    internal class StringScanner
    {
        public string Source { get; private set; }
        public int Index { get; private set; }

        public override string ToString()
        {
            return this.HasTerminated ? "<EOF>" : this.Source.Substring(this.Index);
        }

        public bool HasTerminated
        {
            get { return this.Index >= this.Source.Length; }
        }
    }
}
