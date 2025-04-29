namespace BOOTLOADERFREE.Models
{
    /// <summary>
    /// Représente les informations d'une partition de disque
    /// </summary>
    public class PartitionInfo
    {
        /// <summary>
        /// Numéro de la partition
        /// </summary>
        public int PartitionNumber { get; set; }
        
        /// <summary>
        /// Lettre de lecteur assignée (s'il y en a une)
        /// </summary>
        public string DriveLetter { get; set; }
        
        /// <summary>
        /// Taille de la partition en octets
        /// </summary>
        public long Size { get; set; }
        
        /// <summary>
        /// Taille de la partition en MB
        /// </summary>
        public long SizeMB 
        {
            get => Size / (1024 * 1024);
            set => Size = value * 1024 * 1024;
        }
        
        /// <summary>
        /// Espace libre dans la partition en octets
        /// </summary>
        public long FreeSpace { get; set; }
        
        /// <summary>
        /// Système de fichiers (NTFS, FAT32, etc.)
        /// </summary>
        public string FileSystem { get; set; }
        
        /// <summary>
        /// Étiquette de volume
        /// </summary>
        public string Label { get; set; }
        
        /// <summary>
        /// Indique si la partition est active (bootable)
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Type de partition (Primaire, Étendue, Logique)
        /// </summary>
        public string PartitionType { get; set; }
        
        /// <summary>
        /// Offset de début de la partition en octets
        /// </summary>
        public long Offset { get; set; }
        
        /// <summary>
        /// Indique si la partition est la partition système
        /// </summary>
        public bool IsSystem { get; set; }
        
        /// <summary>
        /// Indique si la partition est la partition de démarrage
        /// </summary>
        public bool IsBoot { get; set; }
        
        /// <summary>
        /// Format la taille en chaîne lisible
        /// </summary>
        public string FormattedSize
        {
            get
            {
                string[] suffixes = { "o", "Ko", "Mo", "Go", "To", "Po" };
                int counter = 0;
                double number = Size;
                while (number >= 1024 && counter < suffixes.Length - 1)
                {
                    number /= 1024;
                    counter++;
                }
                return $"{number:0.##} {suffixes[counter]}";
            }
        }
        
        /// <summary>
        /// Format l'espace libre en chaîne lisible
        /// </summary>
        public string FormattedFreeSpace
        {
            get
            {
                string[] suffixes = { "o", "Ko", "Mo", "Go", "To", "Po" };
                int counter = 0;
                double number = FreeSpace;
                while (number >= 1024 && counter < suffixes.Length - 1)
                {
                    number /= 1024;
                    counter++;
                }
                return $"{number:0.##} {suffixes[counter]}";
            }
        }
        
        /// <summary>
        /// Représentation lisible de la partition
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrEmpty(DriveLetter) 
                ? $"Partition {PartitionNumber} ({FormattedSize})"
                : $"{DriveLetter}: {Label} ({FormattedSize}, {FileSystem})";
        }
    }
}