using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using BOOTLOADERFREE.Models;
using BOOTLOADERFREE.Services;

namespace BOOTLOADERFREE.ViewModels
{
    public class InstallationViewModel : ViewModelBase
    {
        private readonly ILoggingService _loggingService;
        private readonly IInstallationService _installationService;
        private readonly SystemSelectionViewModel _systemSelectionViewModel;
        private readonly DiskConfigurationViewModel _diskConfigurationViewModel;
        
        private InstallationConfig _installationConfig;
        private string _installationTitle;
        private string _installationDescription;
        private bool _isInstalling;
        private bool _isCompleted;
        private bool _isSuccessful;
        private string _currentOperation;
        private int _progressPercentage;
        private string _errorMessage;
        private ObservableCollection<InstallationStep> _installationSteps;

        public InstallationViewModel(
            ILoggingService loggingService,
            IInstallationService installationService,
            SystemSelectionViewModel systemSelectionViewModel,
            DiskConfigurationViewModel diskConfigurationViewModel)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _installationService = installationService ?? throw new ArgumentNullException(nameof(installationService));
            _systemSelectionViewModel = systemSelectionViewModel ?? throw new ArgumentNullException(nameof(systemSelectionViewModel));
            _diskConfigurationViewModel = diskConfigurationViewModel ?? throw new ArgumentNullException(nameof(diskConfigurationViewModel));
            
            StartInstallationCommand = new RelayCommand(_ => StartInstallation(), _ => CanStartInstallation());
            CancelInstallationCommand = new RelayCommand(_ => CancelInstallation(), _ => _isInstalling && !_isCompleted);
            
            _installationService.ProgressChanged += OnInstallationProgressChanged;
            _installationService.InstallationCompleted += OnInstallationCompleted;
            
            InstallationSteps = new ObservableCollection<InstallationStep>();
            InitializeInstallationSteps();
            
            _loggingService.Log("InstallationViewModel initialisé");
        }

        public string InstallationTitle
        {
            get => _installationTitle;
            set => SetProperty(ref _installationTitle, value);
        }

        public string InstallationDescription
        {
            get => _installationDescription;
            set => SetProperty(ref _installationDescription, value);
        }

        public bool IsInstalling
        {
            get => _isInstalling;
            set => SetProperty(ref _isInstalling, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        public bool IsSuccessful
        {
            get => _isSuccessful;
            set => SetProperty(ref _isSuccessful, value);
        }

        public string CurrentOperation
        {
            get => _currentOperation;
            set => SetProperty(ref _currentOperation, value);
        }

        public int ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<InstallationStep> InstallationSteps
        {
            get => _installationSteps;
            set => SetProperty(ref _installationSteps, value);
        }

        public ICommand StartInstallationCommand { get; }
        public ICommand CancelInstallationCommand { get; }

        public void PrepareInstallation()
        {
            try
            {
                var systemOption = _systemSelectionViewModel.GetSelectedOption();
                if (systemOption == null)
                {
                    _loggingService.LogWarning("Aucun système sélectionné");
                    return;
                }

                var diskConfig = _diskConfigurationViewModel.GetDiskConfiguration();
                if (diskConfig == null)
                {
                    _loggingService.LogWarning("Configuration de disque invalide");
                    return;
                }

                // Combine system option with disk configuration
                _installationConfig = diskConfig;
                _installationConfig.SystemName = systemOption.Name;
                _installationConfig.InstallationType = ConvertInstallationType(systemOption.InstallationTypeValue);
                
                // Generate title and description
                InstallationTitle = $"Installation de {systemOption.Name}";
                
                if (_installationConfig.InstallationType == InstallationConfig.InstallType.DualBoot)
                {
                    InstallationDescription = _installationConfig.CreateNewPartition
                        ? $"Une nouvelle partition de {_installationConfig.PartitionSize} Mo va être créée sur le disque {_installationConfig.DiskNumber}."
                        : $"La partition {_installationConfig.PartitionLetter}: sera utilisée pour l'installation.";
                }
                else if (_installationConfig.InstallationType == InstallationConfig.InstallType.WSL)
                {
                    InstallationDescription = "L'installation sera effectuée via Windows Subsystem for Linux.";
                }
                else if (_installationConfig.InstallationType == InstallationConfig.InstallType.VirtualMachine)
                {
                    InstallationDescription = "Une machine virtuelle sera créée pour l'installation.";
                }
                
                _loggingService.Log("Configuration d'installation préparée");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la préparation de l'installation", ex);
            }
        }

        private async void StartInstallation()
        {
            try
            {
                if (_installationConfig == null)
                {
                    PrepareInstallation();
                }

                if (_installationConfig == null)
                {
                    ErrorMessage = "Impossible de démarrer l'installation: configuration invalide";
                    return;
                }

                _loggingService.Log("Démarrage de l'installation");
                
                IsInstalling = true;
                IsCompleted = false;
                IsSuccessful = false;
                ErrorMessage = null;
                ProgressPercentage = 0;
                CurrentOperation = "Initialisation de l'installation...";
                
                ResetInstallationSteps();
                
                // Start the installation process
                await _installationService.StartInstallationAsync(_installationConfig);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Une erreur est survenue lors du démarrage de l'installation";
                _loggingService.LogError("Erreur lors du démarrage de l'installation", ex);
            }
        }

        private async void CancelInstallation()
        {
            try
            {
                _loggingService.Log("Annulation de l'installation...");
                CurrentOperation = "Annulation en cours...";
                
                await _installationService.CancelInstallationAsync();
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de l'annulation de l'installation", ex);
            }
        }

        private bool CanStartInstallation()
        {
            return !_isInstalling;
        }

        private void InitializeInstallationSteps()
        {
            InstallationSteps.Clear();

            var steps = new[]
            {
                new InstallationStep { StepNumber = 1, Description = "Vérification des prérequis", Status = StepStatus.Pending },
                new InstallationStep { StepNumber = 2, Description = "Préparation du disque", Status = StepStatus.Pending },
                new InstallationStep { StepNumber = 3, Description = "Copie des fichiers", Status = StepStatus.Pending },
                new InstallationStep { StepNumber = 4, Description = "Configuration du démarrage", Status = StepStatus.Pending },
                new InstallationStep { StepNumber = 5, Description = "Finalisation", Status = StepStatus.Pending }
            };

            foreach (var step in steps)
            {
                InstallationSteps.Add(step);
            }
        }

        private void ResetInstallationSteps()
        {
            foreach (var step in InstallationSteps)
            {
                step.Status = StepStatus.Pending;
                step.ProgressPercentage = 0;
                step.DetailedStatus = null;
            }
        }

        private void OnInstallationProgressChanged(object sender, InstallationProgressEventArgs e)
        {
            if (e.Progress == null)
                return;

            CurrentOperation = e.Progress.CurrentOperation;
            ProgressPercentage = e.Progress.PercentComplete;
            
            // Update the step status
            if (e.Progress.CurrentStep > 0 && e.Progress.CurrentStep <= InstallationSteps.Count)
            {
                var currentStep = InstallationSteps[e.Progress.CurrentStep - 1];
                
                // Only update the step if it's not completed yet
                if (currentStep.Status != StepStatus.Completed && currentStep.Status != StepStatus.Failed)
                {
                    currentStep.Status = StepStatus.InProgress;
                    currentStep.DetailedStatus = e.Progress.DetailedStatus;
                    currentStep.ProgressPercentage = e.Progress.PercentComplete;
                    
                    // Mark previous steps as completed
                    for (int i = 0; i < e.Progress.CurrentStep - 1; i++)
                    {
                        if (InstallationSteps[i].Status != StepStatus.Failed)
                        {
                            InstallationSteps[i].Status = StepStatus.Completed;
                            InstallationSteps[i].ProgressPercentage = 100;
                        }
                    }
                }
            }
        }

        private void OnInstallationCompleted(object sender, InstallationCompletedEventArgs e)
        {
            IsCompleted = true;
            IsInstalling = false;
            IsSuccessful = e.Success;
            
            if (!e.Success && !string.IsNullOrEmpty(e.ErrorMessage))
            {
                ErrorMessage = e.ErrorMessage;
            }
            
            // Update steps
            if (e.FinalProgress != null && e.FinalProgress.CurrentStep > 0 && 
                e.FinalProgress.CurrentStep <= InstallationSteps.Count)
            {
                // Complete all previous steps
                for (int i = 0; i < e.FinalProgress.CurrentStep - 1; i++)
                {
                    if (InstallationSteps[i].Status != StepStatus.Failed)
                    {
                        InstallationSteps[i].Status = StepStatus.Completed;
                        InstallationSteps[i].ProgressPercentage = 100;
                    }
                }
                
                // Update current step
                var currentStep = InstallationSteps[e.FinalProgress.CurrentStep - 1];
                currentStep.Status = e.Success ? StepStatus.Completed : StepStatus.Failed;
                currentStep.DetailedStatus = e.Success ? "Terminé" : e.ErrorMessage;
                currentStep.ProgressPercentage = e.Success ? 100 : currentStep.ProgressPercentage;
                
                // If successful, mark all remaining steps as skipped
                if (e.Success)
                {
                    for (int i = e.FinalProgress.CurrentStep; i < InstallationSteps.Count; i++)
                    {
                        InstallationSteps[i].Status = StepStatus.Skipped;
                    }
                }
            }
            
            _loggingService.Log($"Installation terminée: {(e.Success ? "Succès" : "Échec")}");
        }

        private InstallationConfig.InstallType ConvertInstallationType(SystemOption.InstallationType type)
        {
            return type switch
            {
                SystemOption.InstallationType.DualBoot => InstallationConfig.InstallType.DualBoot,
                SystemOption.InstallationType.WSL => InstallationConfig.InstallType.WSL,
                SystemOption.InstallationType.VirtualMachine => InstallationConfig.InstallType.VirtualMachine,
                _ => InstallationConfig.InstallType.DualBoot
            };
        }

        public InstallationConfig GetInstallationConfig()
        {
            return _installationConfig;
        }
    }
}