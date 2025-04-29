using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using BOOTLOADERFREE.Models;
using BOOTLOADERFREE.Services;

namespace BOOTLOADERFREE.ViewModels
{
    public class SummaryViewModel : ViewModelBase
    {
        private readonly ILoggingService _loggingService;
        private readonly IBootloaderService _bootloaderService;
        private readonly InstallationViewModel _installationViewModel;
        
        private string _summaryTitle;
        private string _summaryDescription;
        private bool _isSuccess;
        private string _errorMessage;
        private ObservableCollection<BootEntryInfo> _bootEntries;
        private bool _showBootEntries;
        private bool _isLoading;
        private string _restartInstructions;

        public SummaryViewModel(
            ILoggingService loggingService,
            IBootloaderService bootloaderService,
            InstallationViewModel installationViewModel)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _bootloaderService = bootloaderService ?? throw new ArgumentNullException(nameof(bootloaderService));
            _installationViewModel = installationViewModel ?? throw new ArgumentNullException(nameof(installationViewModel));
            
            RefreshBootEntriesCommand = new RelayCommand(_ => _ = LoadBootEntriesAsync());
            
            _loggingService.Log("SummaryViewModel initialisé");
        }

        public string SummaryTitle
        {
            get => _summaryTitle;
            set => SetProperty(ref _summaryTitle, value);
        }

        public string SummaryDescription
        {
            get => _summaryDescription;
            set => SetProperty(ref _summaryDescription, value);
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<BootEntryInfo> BootEntries
        {
            get => _bootEntries;
            set => SetProperty(ref _bootEntries, value);
        }

        public bool ShowBootEntries
        {
            get => _showBootEntries;
            set => SetProperty(ref _showBootEntries, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string RestartInstructions
        {
            get => _restartInstructions;
            set => SetProperty(ref _restartInstructions, value);
        }

        public ICommand RefreshBootEntriesCommand { get; }

        public async void PrepareViewModel()
        {
            var installConfig = _installationViewModel.GetInstallationConfig();
            
            if (installConfig == null)
            {
                SummaryTitle = "Informations manquantes";
                SummaryDescription = "Impossible de récupérer les détails de l'installation.";
                IsSuccess = false;
                return;
            }
            
            // Get installation success from installation view model
            IsSuccess = _installationViewModel.IsSuccessful;
            ErrorMessage = _installationViewModel.ErrorMessage;
            
            // Set title and description based on success/failure and installation type
            if (IsSuccess)
            {
                SummaryTitle = $"Installation de {installConfig.SystemName} réussie";
                
                switch (installConfig.InstallationType)
                {
                    case InstallationConfig.InstallType.DualBoot:
                        SummaryDescription = "L'installation en dual boot a été effectuée avec succès. " +
                                            "Vous pouvez maintenant redémarrer votre ordinateur et sélectionner le système d'exploitation à démarrer.";
                        ShowBootEntries = true;
                        RestartInstructions = "Redémarrez votre ordinateur pour lancer votre nouveau système d'exploitation. " +
                                            "À l'écran de sélection du système d'exploitation, choisissez " +
                                            $"\"{installConfig.SystemName}\" pour démarrer le système que vous venez d'installer.";
                        break;
                        
                    case InstallationConfig.InstallType.WSL:
                        SummaryDescription = "L'installation de la distribution Windows Subsystem for Linux a été effectuée avec succès. " +
                                            "Vous pouvez maintenant ouvrir une console WSL pour utiliser votre nouvelle distribution Linux.";
                        ShowBootEntries = false;
                        RestartInstructions = "Ouvrez un terminal Windows et tapez 'wsl' pour accéder à votre distribution Linux.";
                        break;
                        
                    case InstallationConfig.InstallType.VirtualMachine:
                        SummaryDescription = "La machine virtuelle a été créée avec succès. " +
                                            "Vous pouvez maintenant utiliser Hyper-V ou l'application de virtualisation pour démarrer votre machine virtuelle.";
                        ShowBootEntries = false;
                        RestartInstructions = "Ouvrez Hyper-V Manager pour accéder à votre machine virtuelle.";
                        break;
                        
                    default:
                        SummaryDescription = "L'installation a été effectuée avec succès.";
                        ShowBootEntries = false;
                        RestartInstructions = "L'installation est terminée.";
                        break;
                }
            }
            else
            {
                SummaryTitle = "Erreur d'installation";
                SummaryDescription = "L'installation n'a pas pu être terminée. Consultez le message d'erreur pour plus d'informations.";
                ShowBootEntries = false;
            }
            
            if (ShowBootEntries)
            {
                await LoadBootEntriesAsync();
            }
            
            _loggingService.Log($"Résumé d'installation préparé: Succès={IsSuccess}");
        }

        public async Task LoadBootEntriesAsync()
        {
            try
            {
                IsLoading = true;
                
                // Load boot entries if needed
                var entries = await _bootloaderService.GetBootEntriesAsync();
                BootEntries = new ObservableCollection<BootEntryInfo>(entries);
                
                _loggingService.Log($"Entrées de démarrage chargées: {entries.Count}");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors du chargement des entrées de démarrage", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}