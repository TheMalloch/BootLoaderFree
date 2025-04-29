using System;
using System.Collections.Generic;

namespace BOOTLOADERFREE.Models
{
    /// <summary>
    /// Représente la configuration d'une installation
    /// </summary>
    public class InstallationConfig
    {
        /// <summary>
        /// Types d'installation possibles
        /// </summary>
        public enum InstallType
        {
            /// <summary>
            /// Installation en dual-boot sur une partition physique
            /// </summary>
            DualBoot,
            
            /// <summary>
            /// Installation via Windows Subsystem for Linux
            /// </summary>
            WSL,
            
            /// <summary>
            /// Installation dans une machine virtuelle
            /// </summary>
            VirtualMachine
        }

        /// <summary>
        /// Type d'installation sélectionné
        /// </summary>
        public InstallType InstallationType { get; set; }
        
        /// <summary>
        /// Nom du système à installer
        /// </summary>
        public string SystemName { get; set; }
        
        /// <summary>
        /// Version du système (si applicable)
        /// </summary>
        public string SystemVersion { get; set; }
        
        /// <summary>
        /// Chemin vers l'image ISO ou le fichier d'installation
        /// </summary>
        public string SourcePath { get; set; }
        
        /// <summary>
        /// Chemin de destination pour l'installation (pour VM ou WSL)
        /// </summary>
        public string InstallationPath { get; set; }
        
        /// <summary>
        /// Numéro du disque sélectionné (pour dual boot)
        /// </summary>
        public int DiskNumber { get; set; }
        
        /// <summary>
        /// Taille de la partition en MB (pour dual boot)
        /// </summary>
        public long PartitionSize { get; set; }
        
        /// <summary>
        /// Lettre de lecteur de la partition (pour dual boot)
        /// </summary>
        public string PartitionLetter { get; set; }
        
        /// <summary>
        /// Indique si une nouvelle partition doit être créée (pour dual boot)
        /// </summary>
        public bool CreateNewPartition { get; set; }
        
        /// <summary>
        /// Numéro de la partition existante à utiliser (pour dual boot)
        /// </summary>
        public int ExistingPartitionNumber { get; set; }
        
        /// <summary>
        /// Taille de la RAM allouée à la VM en MB (pour VM)
        /// </summary>
        public int VmRamSize { get; set; }
        
        /// <summary>
        /// Nombre de processeurs alloués à la VM (pour VM)
        /// </summary>
        public int VmProcessorCount { get; set; }
        
        /// <summary>
        /// Options supplémentaires spécifiques au type d'installation
        /// </summary>
        public Dictionary<string, string> AdditionalOptions { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Date et heure de la création de cette configuration
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Nom de la distribution (pour WSL)
        /// </summary>
        public string DistroName { get; set; }
        
        /// <summary>
        /// Version de la distribution (pour WSL)
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// Configuration WSL
        /// </summary>
        private WSLConfig _wslConfig;
        public WSLConfig WslConfiguration 
        {
            get => _wslConfig ?? (_wslConfig = new WSLConfig
            {
                DistroName = this.DistroName,
                Version = this.Version,
                InstallPath = this.InstallationPath
            });
            set
            {
                _wslConfig = value;
                if (value != null)
                {
                    this.DistroName = value.DistroName;
                    this.Version = value.Version;
                    this.InstallationPath = value.InstallPath;
                }
            }
        }

        /// <summary>
        /// Valide la configuration et retourne un message d'erreur si la configuration est invalide
        /// </summary>
        /// <returns>Message d'erreur ou null si valide</returns>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(SystemName))
                return "Le nom du système est requis.";
                
            switch (InstallationType)
            {
                case InstallType.DualBoot:
                    if (CreateNewPartition && PartitionSize < 10000) // Au moins 10 GB
                        return "La taille de partition doit être d'au moins 10 GB.";
                    if (!CreateNewPartition && ExistingPartitionNumber < 0)
                        return "Veuillez sélectionner une partition existante.";
                    break;
                    
                case InstallType.WSL:
                    if (string.IsNullOrWhiteSpace(InstallationPath))
                        return "Le chemin d'installation est requis pour WSL.";
                    break;
                    
                case InstallType.VirtualMachine:
                    if (string.IsNullOrWhiteSpace(InstallationPath))
                        return "Le chemin d'installation est requis pour la VM.";
                    if (VmRamSize < 1024)
                        return "La VM doit avoir au moins 1 GB de RAM.";
                    break;
            }
            
            return null;
        }
    }

    /// <summary>
    /// Information pour la création d'une partition
    /// </summary>
    public class PartitionCreationInfo
    {
        /// <summary>
        /// Taille de la partition en Mo
        /// </summary>
        public long SizeInMB { get; set; }

        /// <summary>
        /// Système de fichiers pour la partition
        /// </summary>
        public string FileSystem { get; set; } = "NTFS";

        /// <summary>
        /// Étiquette de la partition
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Rendre la partition active (bootable)
        /// </summary>
        public bool MakeActive { get; set; }

        /// <summary>
        /// Lettre de lecteur souhaitée (si disponible)
        /// </summary>
        public string DriveLetter { get; set; }
        
        /// <summary>
        /// Description de l'utilisation de cette partition
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Configuration du bootloader
    /// </summary>
    public class BootloaderConfig
    {
        /// <summary>
        /// Type de bootloader à utiliser
        /// </summary>
        public string Type { get; set; } = "Windows Boot Manager";

        /// <summary>
        /// Délai d'attente du bootloader en secondes
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// ID de l'entrée par défaut
        /// </summary>
        public string DefaultEntryId { get; set; }

        /// <summary>
        /// Titre de l'entrée pour le nouveau système
        /// </summary>
        public string NewSystemEntryTitle { get; set; }
    }

    /// <summary>
    /// Configuration pour une distribution WSL
    /// </summary>
    public class WSLConfig
    {
        /// <summary>
        /// Nom de la distribution
        /// </summary>
        public string DistroName { get; set; }

        /// <summary>
        /// Version de la distribution
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Chemin d'installation souhaité
        /// </summary>
        public string InstallPath { get; set; }

        /// <summary>
        /// Nom d'utilisateur par défaut
        /// </summary>
        public string DefaultUser { get; set; } = "user";

        /// <summary>
        /// Mot de passe pour l'utilisateur (stockage temporaire)
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Définir comme distribution par défaut
        /// </summary>
        public bool SetAsDefault { get; set; }
        
        /// <summary>
        /// Utiliser WSL version 2
        /// </summary>
        public bool UseWSL2 { get; set; } = true;
    }
}