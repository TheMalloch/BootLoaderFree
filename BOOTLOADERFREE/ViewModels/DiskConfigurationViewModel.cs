using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BOOTLOADERFREE.Models;
using BOOTLOADERFREE.Services;

namespace BOOTLOADERFREE.ViewModels
{
    public class DiskConfigurationViewModel : ViewModelBase
    {
        private readonly ILoggingService _loggingService;
        private readonly IDiskService _diskService;
        private readonly SystemOption _selectedSystemOption;

        private ObservableCollection<DiskInfo> _availableDisks;
        private DiskInfo _selectedDisk;
        private bool _createNewPartition = true;
        private bool _useExistingPartition;
        private ObservableCollection<PartitionInfo> _availableExistingPartitions;
        private PartitionInfo _selectedExistingPartition;
        private long _requestedPartitionSize = 30000; // 30 GB default
        private bool _isLoading;
        private string _statusMessage;

        public DiskConfigurationViewModel(
            ILoggingService loggingService,
            IDiskService diskService,
            SystemSelectionViewModel systemSelectionViewModel)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _diskService = diskService ?? throw new ArgumentNullException(nameof(diskService));
            _selectedSystemOption = systemSelectionViewModel?.GetSelectedOption() ?? 
                                    throw new ArgumentNullException(nameof(systemSelectionViewModel));

            RefreshDisksCommand = new RelayCommand(_ => _ = LoadDisksAsync());
            
            // Set initial minimum partition size based on system requirements
            if (_selectedSystemOption != null && _selectedSystemOption.RequiredSpaceMB > 0)
            {
                RequestedPartitionSize = Math.Max(20000, _selectedSystemOption.RequiredSpaceMB); // At least 20 GB
            }
            
            _loggingService.Log("DiskConfigurationViewModel initialisé");
            
            // Load disks information
            _ = LoadDisksAsync();
        }

        public ObservableCollection<DiskInfo> AvailableDisks
        {
            get => _availableDisks;
            set => SetProperty(ref _availableDisks, value);
        }

        public DiskInfo SelectedDisk
        {
            get => _selectedDisk;
            set
            {
                if (SetProperty(ref _selectedDisk, value) && value != null)
                {
                    _loggingService.Log($"Disque sélectionné: {value.Model} (Disque {value.DiskNumber})");
                    UpdateAvailablePartitions();
                }
            }
        }

        public bool CreateNewPartition
        {
            get => _createNewPartition;
            set
            {
                if (SetProperty(ref _createNewPartition, value) && value)
                {
                    UseExistingPartition = !value;
                }
            }
        }

        public bool UseExistingPartition
        {
            get => _useExistingPartition;
            set
            {
                if (SetProperty(ref _useExistingPartition, value) && value)
                {
                    CreateNewPartition = !value;
                }
            }
        }

        public ObservableCollection<PartitionInfo> AvailableExistingPartitions
        {
            get => _availableExistingPartitions;
            set => SetProperty(ref _availableExistingPartitions, value);
        }

        public PartitionInfo SelectedExistingPartition
        {
            get => _selectedExistingPartition;
            set
            {
                if (SetProperty(ref _selectedExistingPartition, value) && value != null)
                {
                    _loggingService.Log($"Partition sélectionnée: {value.DriveLetter}: (Partition {value.PartitionNumber})");
                }
            }
        }

        public long RequestedPartitionSize
        {
            get => _requestedPartitionSize;
            set => SetProperty(ref _requestedPartitionSize, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshDisksCommand { get; }

        public async Task LoadDisksAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Chargement des disques...";
                
                var disks = await _diskService.GetAvailableDisksAsync();
                AvailableDisks = new ObservableCollection<DiskInfo>(disks);
                
                if (disks.Count > 0)
                {
                    SelectedDisk = disks.FirstOrDefault(d => !d.IsRemovable); // Sélectionner le premier disque fixe
                    if (SelectedDisk == null)
                    {
                        SelectedDisk = disks.First(); // Ou le premier disponible si pas de disque fixe
                    }
                    
                    StatusMessage = $"{disks.Count} disques disponibles";
                }
                else
                {
                    StatusMessage = "Aucun disque disponible";
                }
                
                _loggingService.Log($"Disques chargés: {disks.Count}");
            }
            catch (Exception ex)
            {
                StatusMessage = "Erreur lors du chargement des disques";
                _loggingService.LogError("Erreur lors du chargement des disques", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateAvailablePartitions()
        {
            if (SelectedDisk == null)
            {
                AvailableExistingPartitions = new ObservableCollection<PartitionInfo>();
                return;
            }

            // Filter partitions that are suitable for installation (only include those with drive letters)
            var suitablePartitions = SelectedDisk.Partitions
                .Where(p => !string.IsNullOrEmpty(p.DriveLetter) && 
                           (!string.IsNullOrEmpty(p.FileSystem) && p.FileSystem.Equals("NTFS", StringComparison.OrdinalIgnoreCase)) &&
                           p.SizeMB >= _selectedSystemOption.RequiredSpaceMB)
                .ToList();
            
            AvailableExistingPartitions = new ObservableCollection<PartitionInfo>(suitablePartitions);
            
            if (AvailableExistingPartitions.Count > 0)
            {
                SelectedExistingPartition = AvailableExistingPartitions.First();
            }
            else
            {
                SelectedExistingPartition = null;
                
                // If no suitable partitions are available, default to creating a new one
                CreateNewPartition = true;
                UseExistingPartition = false;
            }
        }

        public InstallationConfig GetDiskConfiguration()
        {
            if (SelectedDisk == null)
            {
                return null;
            }

            var config = new InstallationConfig
            {
                DiskNumber = SelectedDisk.DiskNumber
            };

            if (CreateNewPartition)
            {
                config.CreateNewPartition = true;
                config.PartitionSize = RequestedPartitionSize;
            }
            else if (UseExistingPartition && SelectedExistingPartition != null)
            {
                config.CreateNewPartition = false;
                config.PartitionLetter = SelectedExistingPartition.DriveLetter;
                config.ExistingPartitionNumber = SelectedExistingPartition.PartitionNumber;
            }
            else
            {
                _loggingService.LogWarning("Configuration de disque invalide");
                return null;
            }

            return config;
        }
    }
}