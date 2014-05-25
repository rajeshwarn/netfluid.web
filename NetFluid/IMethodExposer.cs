namespace NetFluid
{
    /// <summary>
    /// Web-app clas must implements this interface to be exposed by the Engine
    /// </summary>
    public interface IMethodExposer
    {
        /// <summary>
        /// Used by the Engine to pass the context to instances
        /// </summary>
        Context Context { get; set; }

        /// <summary>
        /// Called when this type is loaded by NetFluid Engine
        /// </summary>
        void OnLoad();
    }
}