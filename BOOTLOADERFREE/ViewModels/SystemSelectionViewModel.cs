using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BOOTLOADERFREE.Models;
using BOOTLOADERFREE.Services;

namespace BOOTLOADERFREE.ViewModels
{
    public class SystemSelectionViewModel : ViewModelBase
    {
        private readonly ILoggingService _loggingService;
        private readonly IWSLService _wslService;
        private readonly IVMService _vmService;
        
        private ObservableCollection<SystemOption> _availableOptions;
        private SystemOption _selectedOption;
        private bool _isLoading;
        private string _statusMessage;

        public SystemSelectionViewModel(
            ILoggingService loggingService,
            IWSLService wslService,
            IVMService vmService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
            _vmService = vmService ?? throw new ArgumentNullException(nameof(vmService));
            
            _loggingService.Log("SystemSelectionViewModel initialisé");
            
            // Initial load
            _ = LoadSystemOptionsAsync();
        }

        public ObservableCollection<SystemOption> AvailableOptions
        {
            get => _availableOptions;
            set => SetProperty(ref _availableOptions, value);
        }

        public SystemOption SelectedOption
        {
            get => _selectedOption;
            set
            {
                if (SetProperty(ref _selectedOption, value) && value != null)
                {
                    _loggingService.Log($"Option de système sélectionnée: {value.Name} ({value.InstallationTypeValue})");
                }
            }
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

        public async Task LoadSystemOptionsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Chargement des options d'installation...";
                
                var options = new ObservableCollection<SystemOption>();
                
                // Option 1: Installation en dual boot
                options.Add(new SystemOption
                {
                    Id = "dualboot-windows",
                    Name = "Dual Boot",
                    Description = "Installer un système alternatif sur une partition séparée. Cette option créera une entrée dans le menu de démarrage.",
                    InstallationTypeValue = SystemOption.InstallationType.DualBoot,
                    IsAdvanced = false,
                    RequiredSpaceMB = 20000 // 20 GB
                });
                
                // Option 2: WSL (si disponible)
                bool wslAvailable = await _wslService.IsWSLSupportedAsync();
                if (wslAvailable)
                {
                    options.Add(new SystemOption
                    {
                        Id = "wsl",
                        Name = "Windows Subsystem for Linux (WSL)",
                        Description = "Installer une distribution Linux directement dans Windows sans redémarrage ni partition séparée.",
                        InstallationTypeValue = SystemOption.InstallationType.WSL,
                        IsAdvanced = false,
                        RequiredSpaceMB = 5000 // 5 GB
                    });
                }
                
                // Option 3: Machine virtuelle (si la virtualisation est disponible)
                bool vmSupported = await _vmService.IsVirtualizationSupportedAsync();
                if (vmSupported)
                {
                    options.Add(new SystemOption
                    {
                        Id = "vm",
                        Name = "Machine Virtuelle",
                        Description = "Créer une machine virtuelle pour installer un système sans modifier votre configuration actuelle.",
                        InstallationTypeValue = SystemOption.InstallationType.VirtualMachine,
                        IsAdvanced = true,
                        RequiredSpaceMB = 20000 // 20 GB
                    });
                }
                
                AvailableOptions = options;
                SelectedOption = options.FirstOrDefault();
                
                StatusMessage = $"{options.Count} options disponibles";
                
                _loggingService.Log($"Options de système chargées: {options.Count}");
            }
            catch (Exception ex)
            {
                StatusMessage = "Erreur lors du chargement des options d'installation";
                _loggingService.LogError("Erreur lors du chargement des options de système", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public SystemOption GetSelectedOption()
        {
            return _selectedOption;
        }
    }
}