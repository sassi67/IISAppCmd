using System;
using System.IO;

namespace IISAppCmd.Config
{
    /// <summary>
    /// Copies an applicationHost.config to a working location so it can be
    /// tailored without touching the original.
    /// </summary>
    public static class ApplicationHostConfig
    {
        public const string ScratchDirectoryName = "iisconfig";

        /// <summary>
        /// The applicationHost.config a local build has next to the executable,
        /// used when --source names no other one. It is a fixture for tests and
        /// local runs: a deployed copy of the tool ships without it, and is
        /// pointed at the configuration of an installed IIS Express instead.
        /// </summary>
        public static string BundledPath =>
            Path.Combine(AppContext.BaseDirectory, "Resources", "applicationHost.config");

        /// <summary>
        /// Where a run's working copy goes when --config is not given: the same
        /// scratch folder and name pattern iisexpressstarter uses.
        /// </summary>
        /// <param name="id">Short identifier of the current run, used to name the copy.</param>
        public static string DefaultWorkingCopyPath(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("The run id must not be empty.", nameof(id));

            return Path.Combine(Path.GetTempPath(), ScratchDirectoryName, $"applicationhost-{id}.config");
        }

        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="destination"/>,
        /// creating the destination folder. An existing destination is never
        /// overwritten.
        /// </summary>
        public static bool CreateWorkingCopy(string source, string destination, out string error)
        {
            if (string.IsNullOrEmpty(source))
            {
                error = "the source configuration path is empty.";
                return false;
            }

            if (string.IsNullOrEmpty(destination))
            {
                error = "the destination configuration path is empty.";
                return false;
            }

            if (!File.Exists(source))
            {
                error = $"the source configuration is missing: '{source}'.";
                return false;
            }

            if (File.Exists(destination))
            {
                error = $"the destination '{destination}' already exists and is not overwritten.";
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(destination));
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(source, destination);

                // File.Copy carries the source's timestamp over; the copy should
                // show the age of this run instead.
                File.SetLastWriteTimeUtc(destination, DateTime.UtcNow);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            {
                error = $"could not copy '{source}' to '{destination}': {ex.Message}";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
