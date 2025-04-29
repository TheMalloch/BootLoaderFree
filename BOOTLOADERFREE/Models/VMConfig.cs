using System.Collections.Generic;

namespace BOOTLOADERFREE.Models
{
    /// <summary>
    /// Configuration d'une machine virtuelle
    /// </summary>
    public class VMConfig
    {
        /// <summary>
        /// Nom de la machine virtuelle
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Nombre de processeurs virtuels
        /// </summary>
        public int ProcessorCount { get; set; } = 2;
        
        /// <summary>
        /// Quantité de mémoire RAM en Mo
        /// </summary>
        public int MemoryMB { get; set; } = 4096;
        
        /// <summary>
        /// Taille du disque dur virtuel en Go
        /// </summary>
        public int DiskSizeGB { get; set; } = 50;
        
        /// <summary>
        /// Chemin où les fichiers de la VM seront stockés
        /// </summary>
        public string StoragePath { get; set; }
        
        /// <summary>
        /// Indique si le disque virtuel est dynamique
        /// </summary>
        public bool IsDynamicDisk { get; set; } = true;
        
        /// <summary>
        /// Génération de machine virtuelle Hyper-V (1 ou 2)
        /// </summary>
        public int Generation { get; set; } = 2;
        
        /// <summary>
        /// Chemin vers le fichier ISO d'installation
        /// </summary>
        public string InstallationISOPath { get; set; }
        
        /// <summary>
        /// Options de réseau de la machine virtuelle
        /// </summary>
        public string NetworkAdapter { get; set; } = "Default Switch";
        
        /// <summary>
        /// Activer le démarrage sécurisé
        /// </summary>
        public bool EnableSecureBoot { get; set; } = true;
        
        /// <summary>
        /// Options supplémentaires pour la machine virtuelle
        /// </summary>
        public Dictionary<string, string> AdditionalOptions { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Type d'hyperviseur à utiliser (Hyper-V, VirtualBox, etc.)
        /// </summary>
        public string HypervisorType { get; set; } = "Hyper-V";
        
        /// <summary>
        /// Indique si la machine virtuelle doit démarrer automatiquement au démarrage du système
        /// </summary>
        public bool AutoStart { get; set; } = false;
        
        /// <summary>
        /// Priorité de démarrage (détermine l'ordre de démarrage si plusieurs VMs sont configurées pour démarrer automatiquement)
        /// </summary>
        public int StartupPriority { get; set; } = 0;
    }
}