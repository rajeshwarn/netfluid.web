
namespace NetFluid
{
    /// <summary>
    /// Interface to override Engine public folder managing
    /// </summary>
    public interface IPublicFolder
    {
        /// <summary>
        ///  If the requested URI is mapped by the pubblic folder, serve and close the context
        /// </summary>
        /// <param name="cnt">Context to serve</param>
        /// <return>true if context has been mapped and served</returns>
        bool TryGetFile(Context cnt);

    }
}