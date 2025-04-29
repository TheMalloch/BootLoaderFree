namespace BOOTLOADERFREE.Models
{
    /// <summary>
    /// Information about a boot entry in the system boot configuration
    /// </summary>
    public class BootEntryInfo
    {
        /// <summary>
        /// Unique identifier for the boot entry
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name shown in the boot menu
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Path to the boot loader
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Device containing the boot loader
        /// </summary>
        public string Device { get; set; }

        /// <summary>
        /// Whether this entry is the default boot option
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Position in the boot menu
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Additional options passed to the boot loader
        /// </summary>
        public string Options { get; set; }

        /// <summary>
        /// Whether this entry was created by our application
        /// </summary>
        public bool IsCreatedByUs { get; set; }
    }
}