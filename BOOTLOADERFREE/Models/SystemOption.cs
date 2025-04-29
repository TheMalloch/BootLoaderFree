using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BOOTLOADERFREE.Models
{
    /// <summary>
    /// Represents an installation option in the application
    /// </summary>
    public class SystemOption : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _version;
        private string _description;
        private bool _isSelected;
        private bool _isAdvanced;
        private InstallationType _installationTypeValue;
        private string _imageUrl;
        private long _requiredSpaceMB;

        /// <summary>
        /// Unique identifier for the option
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// Types d'installation possibles
        /// </summary>
        public enum InstallationType
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
        /// Display name of the option
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Version du système d'exploitation
        /// </summary>
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        /// <summary>
        /// Descriptive text explaining the option
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// Indique si cette option est sélectionnée
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// Indicates if this is an advanced option
        /// </summary>
        public bool IsAdvanced
        {
            get => _isAdvanced;
            set => SetProperty(ref _isAdvanced, value);
        }

        /// <summary>
        /// Type d'installation pour ce système
        /// </summary>
        public InstallationType InstallationTypeValue
        {
            get => _installationTypeValue;
            set => SetProperty(ref _installationTypeValue, value);
        }

        /// <summary>
        /// URL de l'image représentant ce système
        /// </summary>
        public string ImageUrl
        {
            get => _imageUrl;
            set => SetProperty(ref _imageUrl, value);
        }

        /// <summary>
        /// Espace disque requis pour l'installation en Mo
        /// </summary>
        public long RequiredSpaceMB
        {
            get => _requiredSpaceMB;
            set => SetProperty(ref _requiredSpaceMB, value);
        }

        /// <summary>
        /// Espace disque requis formaté pour affichage
        /// </summary>
        public string FormattedRequiredSpace
        {
            get
            {
                if (RequiredSpaceMB < 1024)
                {
                    return $"{RequiredSpaceMB} Mo";
                }
                else
                {
                    double sizeGB = RequiredSpaceMB / 1024.0;
                    return $"{sizeGB:0.#} Go";
                }
            }
        }

        /// <summary>
        /// Options de configuration spécifiques au système
        /// </summary>
        public Dictionary<string, string> ConfigOptions { get; set; } = new Dictionary<string, string>();

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Méthode pour notifier les changements de propriétés
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Méthode pour définir une propriété et notifier les changements
        /// </summary>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Représentation textuelle de l'option de système
        /// </summary>
        public override string ToString()
        {
            return $"{Name} {Version} ({InstallationTypeValue})";
        }
    }
}