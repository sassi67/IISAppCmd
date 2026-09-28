namespace IISAppCmd.IIS
{
    /// <summary>
    /// The server-wide sections a run sets, the way
    /// <c>appcmd set config /section:...</c> does: each one is null when the
    /// command line left it alone, and so is each attribute within it, so that
    /// only what was asked for is written.
    /// </summary>
    public class ServerConfig
    {
        public Caching Caching { get; set; }

        public RequestFiltering RequestFiltering { get; set; }

        public DirectoryBrowse DirectoryBrowse { get; set; }
    }
}
