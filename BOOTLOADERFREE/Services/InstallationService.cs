using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BOOTLOADERFREE.Models;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Service pour gérer le processus d'installation
    /// </summary>
    public class InstallationService : IInstallationService
    {
        private readonly ILoggingService _loggingService;
        private readonly IDiskService _diskService;
        private readonly IBootloaderService _bootloaderService;
        private readonly IWSLService _wslService;
        private readonly IVMService _vmService;

        private InstallationConfig _config;
        private CancellationTokenSource _cancellationTokenSource;
        private InstallationProgress _currentProgress;
        private bool _isInstallationInProgress;

        /// <summary>
        /// Constructeur du service d'installation
        /// </summary>
        public InstallationService(
            ILoggingService loggingService,
            IDiskService diskService,
            IBootloaderService bootloaderService,
            IWSLService wslService,
            IVMService vmService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _diskService = diskService ?? throw new ArgumentNullException(nameof(diskService));
            _bootloaderService = bootloaderService ?? throw new ArgumentNullException(nameof(bootloaderService));
            _wslService = wslService ?? throw new ArgumentNullException(nameof(wslService));
            _vmService = vmService ?? throw new ArgumentNullException(nameof(vmService));

            _currentProgress = new InstallationProgress
            {
                CurrentStep = 0,
                TotalSteps = 5,
                PercentComplete = 0,
                CurrentOperation = "Pas d'installation en cours",
                IsCompleted = false,
                IsSuccessful = false
            };

            _loggingService.Log("Service d'installation initialisé");
        }

        /// <summary>
        /// Obtient l'état actuel de l'installation en cours
        /// </summary>
        public InstallationProgress CurrentProgress => _currentProgress;

        /// <summary>
        /// Indique si une installation est actuellement en cours
        /// </summary>
        public bool IsInstallationInProgress => _isInstallationInProgress;

        /// <summary>
        /// Événement déclenché lorsque la progression de l'installation change
        /// </summary>
        public event EventHandler<InstallationProgressEventArgs> ProgressChanged;

        /// <summary>
        /// Événement déclenché lorsque l'installation est terminée
        /// </summary>
        public event EventHandler<InstallationCompletedEventArgs> InstallationCompleted;

        /// <summary>
        /// Démarre une installation selon la configuration définie précédemment
        /// </summary>
        /// <returns>Suivi de progression final de l'installation</returns>
        public async Task<InstallationProgress> StartInstallationAsync(InstallationConfig config)
        {
            if (IsInstallationInProgress)
            {
                _loggingService.LogWarning("Une installation est déjà en cours");
                return _currentProgress;
            }

            if (config == null)
            {
                _loggingService.LogError("Impossible de démarrer l'installation: configuration nulle");
                return new InstallationProgress
                {
                    IsCompleted = true,
                    IsSuccessful = false,
                    ErrorMessage = "Configuration d'installation non définie"
                };
            }

            try
            {
                _config = config;
                _isInstallationInProgress = true;
                _cancellationTokenSource = new CancellationTokenSource();
                var token = _cancellationTokenSource.Token;

                // Initialiser la progression
                _currentProgress = new InstallationProgress
                {
                    CurrentStep = 0,
                    TotalSteps = GetTotalSteps(),
                    PercentComplete = 0,
                    CurrentOperation = "Démarrage de l'installation",
                    IsCompleted = false,
                    IsSuccessful = false
                };
                OnProgressChanged();

                _loggingService.Log($"Démarrage de l'installation: {_config.InstallationType}, Système: {_config.SystemName}");

                // Vérifier les prérequis
                UpdateProgress(1, "Vérification des prérequis");
                var prerequisiteIssues = await CheckPrerequisitesAsync(_config);
                if (prerequisiteIssues.Count > 0)
                {
                    string errorMessage = $"Prérequis non satisfaits: {string.Join(", ", prerequisiteIssues)}";
                    _loggingService.LogError(errorMessage);
                    CompleteInstallation(false, errorMessage);
                    return _currentProgress;
                }

                // Exécuter l'installation selon le type
                bool success = false;
                switch (_config.InstallationType)
                {
                    case InstallationConfig.InstallType.DualBoot:
                        success = await PerformDualBootInstallationAsync(token);
                        break;
                    case InstallationConfig.InstallType.WSL:
                        success = await PerformWSLInstallationAsync(token);
                        break;
                    case InstallationConfig.InstallType.VirtualMachine:
                        success = await PerformVMInstallationAsync(token);
                        break;
                    default:
                        CompleteInstallation(false, $"Type d'installation non pris en charge: {_config.InstallationType}");
                        return _currentProgress;
                }

                string finalMessage = success
                    ? $"Installation de {_config.SystemName} terminée avec succès"
                    : $"L'installation de {_config.SystemName} a échoué";

                CompleteInstallation(success, finalMessage);
                return _currentProgress;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Erreur lors de l'installation: {ex.Message}";
                _loggingService.LogError(errorMessage, ex);
                CompleteInstallation(false, errorMessage);
                return _currentProgress;
            }
        }

        /// <summary>
        /// Annule une installation en cours
        /// </summary>
        public async Task<bool> CancelInstallationAsync()
        {
            if (!IsInstallationInProgress)
            {
                _loggingService.LogWarning("Tentative d'annulation alors qu'aucune installation n'est en cours");
                return false;
            }

            try
            {
                _loggingService.Log("Annulation de l'installation en cours");
                _cancellationTokenSource?.Cancel();

                // Attendre un peu pour laisser aux opérations le temps de s'annuler
                await Task.Delay(1000);

                UpdateProgress(_currentProgress.CurrentStep, "Installation annulée", _currentProgress.PercentComplete);
                _isInstallationInProgress = false;
                OnInstallationCompleted(false, "Installation annulée par l'utilisateur");

                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de l'annulation de l'installation", ex);
                return false;
            }
        }

        /// <summary>
        /// Vérifie si les prérequis pour l'installation sont satisfaits
        /// </summary>
        public async Task<List<string>> CheckPrerequisitesAsync(InstallationConfig config)
        {
            var issues = new List<string>();

            if (config == null)
            {
                issues.Add("Configuration d'installation non définie");
                return issues;
            }

            // Validation de base
            if (string.IsNullOrWhiteSpace(config.SystemName))
            {
                issues.Add("Nom du système non spécifié");
            }

            try
            {
                // Vérifications spécifiques selon le type d'installation
                switch (config.InstallationType)
                {
                    case InstallationConfig.InstallType.DualBoot:
                        if (config.CreateNewPartition)
                        {
                            // Vérifier qu'il y a assez d'espace disque
                            bool hasEnoughSpace = await _diskService.HasEnoughFreeSpaceAsync(config.DiskNumber, config.PartitionSize);
                            if (!hasEnoughSpace)
                            {
                                issues.Add($"Espace insuffisant sur le disque {config.DiskNumber} pour créer une partition de {config.PartitionSize} MB");
                            }
                        }
                        else if (string.IsNullOrWhiteSpace(config.PartitionLetter))
                        {
                            issues.Add("Aucune partition existante sélectionnée");
                        }
                        break;

                    case InstallationConfig.InstallType.WSL:
                        // Vérifier que WSL est installé
                        if (!await _wslService.IsWSLInstalledAsync())
                        {
                            issues.Add("Windows Subsystem for Linux n'est pas installé");
                        }
                        
                        // Vérifier l'espace disponible sur le lecteur cible
                        if (!string.IsNullOrEmpty(config.InstallationPath))
                        {
                            string drive = Path.GetPathRoot(config.InstallationPath);
                            DriveInfo driveInfo = new DriveInfo(drive);
                            if (driveInfo.AvailableFreeSpace < config.PartitionSize * 1024 * 1024) // Convertir MB en bytes
                            {
                                issues.Add($"Espace insuffisant sur le lecteur {drive} pour l'installation WSL");
                            }
                        }
                        else
                        {
                            issues.Add("Chemin d'installation non spécifié pour WSL");
                        }
                        break;

                    case InstallationConfig.InstallType.VirtualMachine:
                        // Vérifier que le logiciel de VM est disponible
                        if (!await _vmService.IsVirtualizationSupportedAsync())
                        {
                            issues.Add("La virtualisation n'est pas supportée ou activée sur ce système");
                        }
                        
                        // Vérifier l'espace disponible sur le lecteur cible
                        if (!string.IsNullOrEmpty(config.InstallationPath))
                        {
                            string drive = Path.GetPathRoot(config.InstallationPath);
                            DriveInfo driveInfo = new DriveInfo(drive);
                            if (driveInfo.AvailableFreeSpace < config.PartitionSize * 1024 * 1024) // Convertir MB en bytes
                            {
                                issues.Add($"Espace insuffisant sur le lecteur {drive} pour la machine virtuelle");
                            }
                        }
                        else
                        {
                            issues.Add("Chemin d'installation non spécifié pour la machine virtuelle");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la vérification des prérequis", ex);
                issues.Add($"Erreur lors de la vérification des prérequis: {ex.Message}");
            }

            return issues;
        }

        /// <summary>
        /// Configure l'état de l'installation
        /// </summary>
        /// <param name="config">Configuration de l'installation</param>
        public void SetConfiguration(InstallationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _loggingService.Log($"Configuration d'installation définie: {config.InstallationType}, Système: {config.SystemName}");
        }

        /// <summary>
        /// Annule l'installation en cours
        /// </summary>
        public void CancelInstallation()
        {
            _cancellationTokenSource?.Cancel();
            _loggingService.Log("Installation annulée par l'utilisateur");
        }

        #region Méthodes privées

        /// <summary>
        /// Effectue l'installation en mode dual boot
        /// </summary>
        private async Task<bool> PerformDualBootInstallationAsync(CancellationToken cancellationToken)
        {
            _loggingService.Log($"Démarrage de l'installation dual boot pour {_config.SystemName}");

            try
            {
                // Étape 1: Préparation de la partition
                UpdateProgress(1, "Préparation de la partition", 10);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;

                PartitionInfo targetPartition;
                if (_config.CreateNewPartition)
                {
                    _loggingService.Log($"Création d'une nouvelle partition de {_config.PartitionSize} MB sur le disque {_config.DiskNumber}");
                    targetPartition = await _diskService.CreatePartitionAsync(_config.DiskNumber, _config.PartitionSize);
                    if (targetPartition == null)
                    {
                        _loggingService.LogError("Échec de la création de la partition");
                        return false;
                    }
                    
                    _config.PartitionLetter = targetPartition.DriveLetter;
                    _loggingService.Log($"Partition créée avec succès: {targetPartition.DriveLetter}:");
                }
                else
                {
                    _loggingService.Log($"Utilisation de la partition existante {_config.PartitionLetter}:");
                    // Vérifier que la partition existe
                    var disks = await _diskService.GetAvailableDisksAsync();
                    var disk = disks.Find(d => d.DiskNumber == _config.DiskNumber);
                    targetPartition = disk?.Partitions.Find(p => p.DriveLetter == _config.PartitionLetter);
                    
                    if (targetPartition == null)
                    {
                        _loggingService.LogError($"Partition {_config.PartitionLetter}: non trouvée sur le disque {_config.DiskNumber}");
                        return false;
                    }
                }
                
                // Étape 2: Formatage de la partition (si nouvelle)
                if (_config.CreateNewPartition)
                {
                    UpdateProgress(2, $"Formatage de la partition {_config.PartitionLetter}:", 20);
                    
                    if (cancellationToken.IsCancellationRequested)
                        return false;
                    
                    bool formatSuccess = await _diskService.FormatPartitionAsync(_config.PartitionLetter[0], "NTFS", _config.SystemName);
                    if (!formatSuccess)
                    {
                        _loggingService.LogError($"Échec du formatage de la partition {_config.PartitionLetter}:");
                        return false;
                    }
                    
                    _loggingService.Log($"Partition {_config.PartitionLetter}: formatée avec succès");
                }
                
                // Étape 3: Copie des fichiers d'installation
                UpdateProgress(3, $"Préparation du support d'installation pour {_config.SystemName}", 30);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;
                
                // Simuler la copie des fichiers d'installation (à implémenter)
                await Task.Delay(2000); // Simulation d'une opération longue
                
                // Étape 4: Configuration du bootloader
                UpdateProgress(4, "Configuration du gestionnaire de démarrage", 70);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;
                
                // Sauvegarde de la configuration existante
                string backupPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DualBootDeployer",
                    "Backup",
                    $"bcd_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
                    
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                await _bootloaderService.BackupBootConfigurationAsync(backupPath);
                
                // Créer une entrée de démarrage pour le nouveau système
                var bootEntry = new BootEntryInfo
                {
                    DisplayName = _config.SystemName,
                    Path = $"{_config.PartitionLetter}:\\Windows\\System32\\winload.exe",
                    Device = $"partition={_config.PartitionLetter}:",
                    IsCreatedByUs = true
                };
                
                bool entrySuccess = await _bootloaderService.ConfigureBootEntryAsync(bootEntry);
                if (!entrySuccess)
                {
                    _loggingService.LogError("Échec de la création de l'entrée de démarrage");
                    return false;
                }
                
                // Configurer le délai d'attente du bootloader
                await _bootloaderService.SetBootTimeoutAsync(10);
                
                // Étape 5: Finalisation
                UpdateProgress(5, "Finalisation de l'installation", 90);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;
                
                // Rendre la partition bootable si nécessaire
                if (_config.CreateNewPartition)
                {
                    await _diskService.SetPartitionActiveAsync(_config.DiskNumber, targetPartition.PartitionNumber);
                }
                
                UpdateProgress(5, "Installation terminée avec succès", 100);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de l'installation dual boot", ex);
                return false;
            }
        }

        /// <summary>
        /// Effectue l'installation en mode WSL
        /// </summary>
        private async Task<bool> PerformWSLInstallationAsync(CancellationToken cancellationToken)
        {
            _loggingService.Log($"Démarrage de l'installation WSL pour {_config.SystemName}");

            try
            {
                // Étape 1: Vérification de l'installation WSL
                UpdateProgress(1, "Vérification de WSL", 10);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;

                bool isWSLInstalled = await _wslService.IsWSLInstalledAsync();
                if (!isWSLInstalled)
                {
                    _loggingService.Log("WSL n'est pas installé, tentative d'installation...");
                    bool installResult = await _wslService.InstallWSLAsync();
                    if (!installResult)
                    {
                        _loggingService.LogError("Échec de l'installation de WSL");
                        return false;
                    }
                }
                
                // Étape 2: Téléchargement de la distribution si nécessaire
                UpdateProgress(2, $"Préparation de la distribution {_config.SystemName}", 30);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;

                // Simuler le téléchargement (à implémenter)
                await Task.Delay(2000); // Simulation d'une opération longue
                
                // Étape 3: Installation de la distribution
                UpdateProgress(3, $"Installation de la distribution {_config.SystemName}", 50);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;

                bool installSuccess = await _wslService.InstallDistributionAsync(
                    _config.DistroName,
                    _config.Version,
                    _config.InstallationPath);
                    
                if (!installSuccess)
                {
                    _loggingService.LogError($"Échec de l'installation de la distribution {_config.SystemName}");
                    return false;
                }
                
                // Étape 4: Configuration de la distribution
                UpdateProgress(4, "Configuration de la distribution", 80);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;

                // Effectuer des configurations supplémentaires (à implémenter)
                await Task.Delay(1000); // Simulation d'une opération longue
                
                // Étape 5: Finalisation
                UpdateProgress(5, "Finalisation de l'installation", 95);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;

                // Définir comme distribution par défaut si demandé
                if (_config.AdditionalOptions != null && 
                    _config.AdditionalOptions.ContainsKey("SetAsDefault") &&
                    _config.AdditionalOptions["SetAsDefault"].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    await _wslService.SetDefaultDistributionAsync(_config.DistroName);
                }
                
                UpdateProgress(5, "Installation terminée avec succès", 100);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de l'installation WSL", ex);
                return false;
            }
        }

        /// <summary>
        /// Effectue l'installation en mode machine virtuelle
        /// </summary>
        private async Task<bool> PerformVMInstallationAsync(CancellationToken cancellationToken)
        {
            _loggingService.Log($"Démarrage de l'installation de la machine virtuelle pour {_config.SystemName}");

            try
            {
                // Étape 1: Vérification du support de virtualisation
                UpdateProgress(1, "Vérification du support de virtualisation", 10);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;

                bool isVirtualizationSupported = await _vmService.IsVirtualizationSupportedAsync();
                if (!isVirtualizationSupported)
                {
                    _loggingService.LogError("La virtualisation n'est pas supportée ou activée sur ce système");
                    return false;
                }
                
                // Étape 2: Création du disque virtuel
                UpdateProgress(2, "Création du disque virtuel", 30);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;

                string vmPath = _config.InstallationPath;
                if (string.IsNullOrEmpty(vmPath))
                {
                    vmPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "Virtual Machines",
                        _config.SystemName);
                }
                
                Directory.CreateDirectory(vmPath);
                
                string vhdPath = Path.Combine(vmPath, $"{_config.SystemName}.vhdx");
                string createdVhdPath = await _vmService.CreateVirtualDiskAsync(vhdPath, (int)(_config.PartitionSize / 1024), true);
                if (string.IsNullOrEmpty(createdVhdPath))
                {
                    _loggingService.LogError("Échec de la création du disque virtuel");
                    return false;
                }
                
                // Étape 3: Création de la machine virtuelle
                UpdateProgress(3, "Création de la machine virtuelle", 50);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;

                var vmConfig = new VMConfig
                {
                    Name = _config.SystemName,
                    MemoryMB = _config.VmRamSize > 0 ? _config.VmRamSize : 4096,
                    ProcessorCount = _config.VmProcessorCount > 0 ? _config.VmProcessorCount : 2,
                    StoragePath = vmPath,
                    DiskSizeGB = (int)(_config.PartitionSize / 1024),
                    InstallationISOPath = _config.SourcePath
                };
                
                string vmId = await _vmService.CreateVirtualMachineAsync(vmConfig);
                if (string.IsNullOrEmpty(vmId))
                {
                    _loggingService.LogError("Échec de la création de la machine virtuelle");
                    return false;
                }
                
                // Étape 4: Configuration de la VM
                UpdateProgress(4, "Configuration de la machine virtuelle", 80);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;

                // Configurer le démarrage sur ISO si spécifié
                if (!string.IsNullOrEmpty(_config.SourcePath) && File.Exists(_config.SourcePath))
                {
                    await _vmService.AttachISOAsync(vmId, _config.SourcePath);
                }
                
                // Étape 5: Finalisation
                UpdateProgress(5, "Finalisation de l'installation", 95);
                
                if (cancellationToken.IsCancellationRequested)
                    return false;

                // Créer un raccourci ou des instructions (à implémenter)
                
                UpdateProgress(5, "Machine virtuelle créée avec succès", 100);
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la création de la machine virtuelle", ex);
                return false;
            }
        }

        /// <summary>
        /// Met à jour l'état de progression et déclenche l'événement
        /// </summary>
        /// <param name="step">Étape actuelle</param>
        /// <param name="operation">Description de l'opération en cours</param>
        /// <param name="percent">Pourcentage de progression (optionnel)</param>
        private void UpdateProgress(int step, string operation, int? percent = null)
        {
            _currentProgress.CurrentStep = step;
            _currentProgress.CurrentOperation = operation;
            
            if (percent.HasValue)
            {
                _currentProgress.PercentComplete = percent.Value;
            }
            else
            {
                // Calculer le pourcentage en fonction de l'étape
                _currentProgress.PercentComplete = (step - 1) * 100 / _currentProgress.TotalSteps;
            }
            
            _loggingService.Log($"Progression: {_currentProgress.PercentComplete}% - {operation}");
            OnProgressChanged();
        }

        /// <summary>
        /// Marque l'installation comme terminée
        /// </summary>
        /// <param name="success">Indique si l'installation a réussi</param>
        /// <param name="message">Message final</param>
        private void CompleteInstallation(bool success, string message)
        {
            _currentProgress.IsCompleted = true;
            _currentProgress.IsSuccessful = success;
            _currentProgress.CurrentOperation = message;
            _currentProgress.DetailedStatus = message;
            _currentProgress.PercentComplete = success ? 100 : _currentProgress.PercentComplete;
            
            _isInstallationInProgress = false;
            
            _loggingService.Log($"Installation terminée: {(success ? "Succès" : "Échec")} - {message}");
            OnProgressChanged();
            OnInstallationCompleted(success, message);
        }

        /// <summary>
        /// Déclenche l'événement de progression
        /// </summary>
        private void OnProgressChanged()
        {
            try
            {
                ProgressChanged?.Invoke(this, new InstallationProgressEventArgs
                {
                    Progress = _currentProgress
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors du déclenchement de l'événement ProgressChanged", ex);
            }
        }

        /// <summary>
        /// Déclenche l'événement de fin d'installation
        /// </summary>
        private void OnInstallationCompleted(bool success, string message)
        {
            try
            {
                InstallationCompleted?.Invoke(this, new InstallationCompletedEventArgs
                {
                    Success = success,
                    ErrorMessage = !success ? message : null,
                    FinalProgress = _currentProgress
                });
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors du déclenchement de l'événement InstallationCompleted", ex);
            }
        }

        /// <summary>
        /// Détermine le nombre total d'étapes selon le type d'installation
        /// </summary>
        private int GetTotalSteps()
        {
            return _config.InstallationType switch
            {
                InstallationConfig.InstallType.DualBoot => 5,
                InstallationConfig.InstallType.WSL => 5,
                InstallationConfig.InstallType.VirtualMachine => 5,
                _ => 3
            };
        }

        #endregion
    }
}