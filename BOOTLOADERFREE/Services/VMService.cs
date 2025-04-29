using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BOOTLOADERFREE.Models;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Implémentation du service de gestion des machines virtuelles
    /// </summary>
    public class VMService : IVMService
    {
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Constructeur du service VM
        /// </summary>
        /// <param name="loggingService">Service de journalisation</param>
        public VMService(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _loggingService.Log("Service de machine virtuelle initialisé");
        }

        /// <inheritdoc />
        public async Task<bool> IsVirtualizationSupportedAsync()
        {
            _loggingService.Log("Vérification de la prise en charge de la virtualisation...");
            
            try
            {
                // Exécuter une commande PowerShell pour vérifier si la virtualisation est supportée
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"(Get-ComputerInfo).HyperVisorPresent -or (Get-CimInstance -ClassName Win32_Processor).VirtualizationFirmwareEnabled\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    _loggingService.LogWarning("Impossible de déterminer si la virtualisation est supportée");
                    return false;
                }
                
                bool isSupported = output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
                _loggingService.Log($"La virtualisation est {(isSupported ? "supportée" : "non supportée")}");
                return isSupported;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la vérification de la prise en charge de la virtualisation", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsHyperVEnabledAsync()
        {
            _loggingService.Log("Vérification si Hyper-V est activé...");
            
            try
            {
                // Exécuter une commande PowerShell pour vérifier si Hyper-V est activé
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"(Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V).State -eq 'Enabled'\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    _loggingService.LogWarning("Impossible de déterminer si Hyper-V est activé");
                    return false;
                }
                
                bool isEnabled = output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
                _loggingService.Log($"Hyper-V est {(isEnabled ? "activé" : "non activé")}");
                return isEnabled;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la vérification si Hyper-V est activé", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> EnableHyperVAsync()
        {
            _loggingService.Log("Activation d'Hyper-V...");
            
            try
            {
                // Exécuter PowerShell en tant qu'administrateur pour activer Hyper-V
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All -NoRestart\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // Exécuter en tant qu'administrateur
                };
                
                using var process = Process.Start(processInfo);
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                bool success = process.ExitCode == 0 && !output.Contains("Error");
                
                _loggingService.Log($"Activation d'Hyper-V {(success ? "réussie" : "échouée")}");
                
                if (success)
                {
                    _loggingService.Log("Un redémarrage peut être nécessaire pour terminer l'activation d'Hyper-V");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de l'activation d'Hyper-V", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<string> CreateVirtualMachineAsync(VMConfig vmConfig)
        {
            _loggingService.Log($"Création de la machine virtuelle '{vmConfig.Name}'...");
            
            try
            {
                // Vérifier qu'Hyper-V est activé
                if (!await IsHyperVEnabledAsync())
                {
                    _loggingService.LogWarning("Hyper-V n'est pas activé");
                    return null;
                }
                
                // Créer le répertoire de stockage s'il n'existe pas
                if (!string.IsNullOrEmpty(vmConfig.StoragePath) && !Directory.Exists(vmConfig.StoragePath))
                {
                    Directory.CreateDirectory(vmConfig.StoragePath);
                }
                
                // Générer un ID unique pour la VM
                string vmId = Guid.NewGuid().ToString();
                
                // Exécuter PowerShell pour créer la VM avec Hyper-V
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"New-VM -Name '{vmConfig.Name}' -MemoryStartupBytes {vmConfig.MemoryMB * 1024 * 1024} -Path '{vmConfig.StoragePath}' -Generation 2\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(processInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0 || error.Contains("Error"))
                    {
                        _loggingService.LogError($"Erreur lors de la création de la VM: {error}");
                        return null;
                    }
                }
                
                _loggingService.Log($"Machine virtuelle '{vmConfig.Name}' créée avec succès");
                return vmId;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la création de la machine virtuelle '{vmConfig.Name}'", ex);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> AttachVirtualDiskAsync(string vmId, string vhdPath)
        {
            _loggingService.Log($"Attachement du disque virtuel '{vhdPath}' à la VM {vmId}...");
            
            try
            {
                // Vérifier que le fichier VHD existe
                if (!File.Exists(vhdPath))
                {
                    _loggingService.LogError($"Le fichier VHD '{vhdPath}' n'existe pas");
                    return false;
                }
                
                // Trouver le nom de la VM à partir de son ID
                var vm = await GetVMByIdAsync(vmId);
                if (vm == null)
                {
                    _loggingService.LogError($"Impossible de trouver la VM avec l'ID {vmId}");
                    return false;
                }
                
                // Exécuter PowerShell pour attacher le disque à la VM
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Add-VMHardDiskDrive -VMName '{vm.Name}' -Path '{vhdPath}'\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0 || error.Contains("Error"))
                {
                    _loggingService.LogError($"Erreur lors de l'attachement du disque: {error}");
                    return false;
                }
                
                _loggingService.Log($"Disque virtuel attaché avec succès à la VM '{vm.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de l'attachement du disque virtuel", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<string> CreateVirtualDiskAsync(string path, int sizeInGB, bool isDynamic = true)
        {
            _loggingService.Log($"Création d'un disque virtuel dans '{path}' de {sizeInGB} GB...");
            
            try
            {
                // Vérifier que le répertoire existe
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Type de disque (dynamique ou fixe)
                string diskType = isDynamic ? "Dynamic" : "Fixed";
                
                // Exécuter PowerShell pour créer le disque virtuel
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"New-VHD -Path '{path}' -SizeBytes {sizeInGB * 1024 * 1024 * 1024} -Dynamic:{isDynamic.ToString().ToLower()}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0 || error.Contains("Error"))
                {
                    _loggingService.LogError($"Erreur lors de la création du disque virtuel: {error}");
                    return null;
                }
                
                _loggingService.Log($"Disque virtuel créé avec succès: {path}");
                return path;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la création du disque virtuel", ex);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> StartVirtualMachineAsync(string vmId)
        {
            _loggingService.Log($"Démarrage de la VM {vmId}...");
            
            try
            {
                // Trouver le nom de la VM à partir de son ID
                var vm = await GetVMByIdAsync(vmId);
                if (vm == null)
                {
                    _loggingService.LogError($"Impossible de trouver la VM avec l'ID {vmId}");
                    return false;
                }
                
                // Exécuter PowerShell pour démarrer la VM
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Start-VM -Name '{vm.Name}'\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0 || error.Contains("Error"))
                {
                    _loggingService.LogError($"Erreur lors du démarrage de la VM: {error}");
                    return false;
                }
                
                _loggingService.Log($"VM '{vm.Name}' démarrée avec succès");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors du démarrage de la VM", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<List<VMInfo>> ListVirtualMachinesAsync()
        {
            _loggingService.Log("Listage des machines virtuelles...");
            
            var vms = new List<VMInfo>();
            
            try
            {
                // Exécuter PowerShell pour lister les VMs
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"Get-VM | Select-Object Name, Id, State, Path, AutomaticStartAction, ProcessorCount, MemoryStartup\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    _loggingService.LogWarning("Impossible de lister les machines virtuelles");
                    return vms;
                }
                
                // Analyser la sortie
                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Ignorer les deux premières lignes (en-tête et séparateur)
                for (int i = 2; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    // Analyser la ligne pour extraire les informations de la VM
                    // Dans une implémentation réelle, cela serait plus robuste
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        var vmInfo = new VMInfo
                        {
                            Name = parts[0],
                            Id = parts[1],
                            State = ParseVMState(parts[2]),
                            Path = parts[3],
                            HypervisorType = "Hyper-V"
                        };
                        
                        vms.Add(vmInfo);
                    }
                }
                
                _loggingService.Log($"Trouvé {vms.Count} machines virtuelles");
                return vms;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors du listage des machines virtuelles", ex);
                return vms;
            }
        }

        /// <inheritdoc />
        public async Task<bool> AttachISOAsync(string vmId, string isoPath)
        {
            _loggingService.Log($"Attachement de l'image ISO '{isoPath}' à la VM {vmId}...");
            
            try
            {
                // Vérifier que le fichier ISO existe
                if (!File.Exists(isoPath))
                {
                    _loggingService.LogError($"Le fichier ISO '{isoPath}' n'existe pas");
                    return false;
                }
                
                // Trouver le nom de la VM à partir de son ID
                var vm = await GetVMByIdAsync(vmId);
                if (vm == null)
                {
                    _loggingService.LogError($"Impossible de trouver la VM avec l'ID {vmId}");
                    return false;
                }
                
                // Exécuter PowerShell pour attacher l'ISO à la VM
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Add-VMDvdDrive -VMName '{vm.Name}' -Path '{isoPath}'\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0 || error.Contains("Error"))
                {
                    _loggingService.LogError($"Erreur lors de l'attachement de l'ISO: {error}");
                    return false;
                }
                
                _loggingService.Log($"Image ISO attachée avec succès à la VM '{vm.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de l'attachement de l'image ISO", ex);
                return false;
            }
        }

        #region Méthodes privées
        
        /// <summary>
        /// Obtient les informations sur une VM à partir de son ID
        /// </summary>
        /// <param name="vmId">ID de la VM</param>
        /// <returns>Informations sur la VM ou null si non trouvée</returns>
        private async Task<VMInfo> GetVMByIdAsync(string vmId)
        {
            try
            {
                var vms = await ListVirtualMachinesAsync();
                return vms.Find(vm => vm.Id == vmId);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la recherche de la VM {vmId}", ex);
                return null;
            }
        }

        /// <summary>
        /// Convertit une chaîne d'état de VM en énumération VMState
        /// </summary>
        /// <param name="state">État de la VM sous forme de chaîne</param>
        /// <returns>État de la VM sous forme d'énumération</returns>
        private VMState ParseVMState(string state)
        {
            return state?.ToLowerInvariant() switch
            {
                "running" => VMState.Running,
                "off" => VMState.Off,
                "paused" => VMState.Paused,
                "saved" => VMState.Saved,
                "starting" => VMState.Starting,
                "stopping" => VMState.Stopping,
                "failed" => VMState.Failed,
                _ => VMState.Unknown
            };
        }
        
        #endregion
    }
}