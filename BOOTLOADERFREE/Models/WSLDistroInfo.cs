namespace BOOTLOADERFREE.Models
{
    /// <summary>
    /// Représente les informations sur une distribution Linux WSL
    /// </summary>
    public class WSLDistroInfo
    {
        /// <summary>
        /// Nom de la distribution
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// État de la distribution
        /// </summary>
        public string State { get; set; }
        
        /// <summary>
        /// Version de WSL utilisée (1 ou 2)
        /// </summary>
        public int Version { get; set; }
        
        /// <summary>
        /// Version de WSL utilisée (1 ou 2) - Propriété alternative pour compatibilité
        /// </summary>
        public int WSLVersion 
        { 
            get => Version;
            set => Version = value; 
        }
        
        /// <summary>
        /// Chemin d'installation de la distribution
        /// </summary>
        public string InstallLocation { get; set; }
        
        /// <summary>
        /// Indique si c'est la distribution par défaut
        /// </summary>
        public bool IsDefault { get; set; }
        
        /// <summary>
        /// Taille en octets utilisée par la distribution
        /// </summary>
        public long SizeInBytes { get; set; }
        
        /// <summary>
        /// Représentation formatée de la taille
        /// </summary>
        public string FormattedSize
        {
            get
            {
                string[] suffixes = { "o", "Ko", "Mo", "Go", "To", "Po" };
                int counter = 0;
                double number = SizeInBytes;
                
                while (number >= 1024 && counter < suffixes.Length - 1)
                {
                    number /= 1024;
                    counter++;
                }
                
                return $"{number:0.##} {suffixes[counter]}";
            }
        }
        
        /// <summary>
        /// Utilisateur par défaut pour la distribution
        /// </summary>
        public string DefaultUser { get; set; }
        
        /// <summary>
        /// Représentation textuelle de l'information sur la distribution
        /// </summary>
        public override string ToString()
        {
            return $"{Name} (WSL{Version}): {State}";
        }
    }
}