using System.Collections.Generic;

namespace BOOTLOADERFREE.Models
{
    /// <summary>
    /// Représente les informations d'un disque physique
    /// </summary>
    public class DiskInfo
    {
        /// <summary>
        /// Numéro du disque
        /// </summary>
        public int DiskNumber { get; set; }
        
        /// <summary>
        /// Modèle du disque
        /// </summary>
        public string Model { get; set; }
        
        /// <summary>
        /// Taille totale du disque en octets
        /// </summary>
        public long Size { get; set; }
        
        /// <summary>
        /// Indique si le disque est amovible
        /// </summary>
        public bool IsRemovable { get; set; }
        
        /// <summary>
        /// Indique si le disque est la destination du système d'exploitation
        /// </summary>
        public bool IsSystemDisk { get; set; }
        
        /// <summary>
        /// Liste des partitions sur le disque
        /// </summary>
        public List<PartitionInfo> Partitions { get; set; } = new List<PartitionInfo>();
        
        /// <summary>
        /// Espace libre disponible en octets
        /// </summary>
        public long FreeSpace { get; set; }
        
        /// <summary>
        /// Type de table de partition (MBR ou GPT)
        /// </summary>
        public string PartitionStyle { get; set; }
        
        /// <summary>
        /// Représentation lisible de la taille du disque
        /// </summary>
        public string FormattedSize => FormatSize(Size);
        
        /// <summary>
        /// Représentation lisible de l'espace libre du disque
        /// </summary>
        public string FormattedFreeSpace => FormatSize(FreeSpace);
        
        /// <summary>
        /// Formate une taille en octets en chaîne lisible
        /// </summary>
        /// <param name="bytes">Taille en octets</param>
        /// <returns>Chaîne formatée</returns>
        private string FormatSize(long bytes)
        {
            string[] suffixes = { "o", "Ko", "Mo", "Go", "To", "Po" };
            int counter = 0;
            double number = bytes;
            while (number >= 1024 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:0.##} {suffixes[counter]}";
        }
    }
}