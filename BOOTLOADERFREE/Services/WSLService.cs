using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BOOTLOADERFREE.Models;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Implémentation du service de gestion de Windows Subsystem for Linux
    /// </summary>
    public class WSLService : IWSLService
    {
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Constructeur du service WSL
        /// </summary>
        /// <param name="loggingService">Service de journalisation</param>
        public WSLService(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _loggingService.Log("Service WSL initialisé");
        }

        /// <inheritdoc />
        public async Task<bool> IsWSLSupportedAsync()
        {
            _loggingService.Log("Vérification de la prise en charge de WSL");
            
            try
            {
                // Vérifier que le système est Windows 10 (1607) ou plus récent
                var osVersion = Environment.OSVersion.Version;
                bool isVersionSupported = osVersion.Major > 10 || (osVersion.Major == 10 && osVersion.Build >= 14393);
                
                if (!isVersionSupported)
                {
                    _loggingService.LogWarning($"Version de Windows non supportée pour WSL: {osVersion}");
                    return false;
                }
                
                // Vérifier que le système est 64 bits
                bool is64BitOperatingSystem = Environment.Is64BitOperatingSystem;
                if (!is64BitOperatingSystem)
                {
                    _loggingService.LogWarning("WSL n'est pas supporté sur les systèmes 32 bits");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la vérification de la prise en charge de WSL", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsWSLInstalledAsync()
        {
            _loggingService.Log("Vérification de l'installation de WSL");
            
            try
            {
                // Vérifier si wsl.exe existe
                string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string wslPath = Path.Combine(systemRoot, "wsl.exe");
                
                if (!File.Exists(wslPath))
                {
                    _loggingService.Log("L'exécutable WSL n'a pas été trouvé");
                    return false;
                }
                
                // Exécuter wsl --status pour vérifier si WSL est installé
                var processInfo = new ProcessStartInfo
                {
                    FileName = wslPath,
                    Arguments = "--status",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                // Si la sortie contient une erreur comme "WSL n'est pas installé", WSL n'est pas installé
                if (process.ExitCode != 0 || error.Contains("WSL n'est pas installé") || error.Contains("WSL is not installed"))
                {
                    _loggingService.Log("WSL n'est pas installé");
                    return false;
                }
                
                _loggingService.Log("WSL est installé");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la vérification de l'installation de WSL", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsWSLAvailableAsync()
        {
            _loggingService.Log("Vérification de la disponibilité de WSL");
            
            if (!await IsWSLSupportedAsync())
            {
                _loggingService.Log("WSL n'est pas supporté sur ce système");
                return false;
            }
            
            if (!await IsWSLInstalledAsync())
            {
                _loggingService.Log("WSL n'est pas installé");
                return false;
            }
            
            // Vérifier si au moins une distribution est installée
            var distributions = await ListDistributionsAsync();
            bool hasDistributions = distributions != null && distributions.Count > 0;
            
            _loggingService.Log($"WSL est {(hasDistributions ? "disponible" : "installé mais aucune distribution n'est installée")}");
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> EnableWSLAsync()
        {
            _loggingService.Log("Activation de la fonctionnalité WSL...");
            
            try
            {
                // Exécuter PowerShell en tant qu'administrateur pour activer WSL
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux -NoRestart\"",
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
                
                _loggingService.Log($"Activation de WSL {(success ? "réussie" : "échouée")}");
                return success;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de l'activation de WSL", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateToWSL2Async()
        {
            _loggingService.Log("Mise à jour vers WSL 2...");
            
            try
            {
                // Exécuter PowerShell en tant qu'administrateur pour activer la virtualisation
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform -NoRestart\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // Exécuter en tant qu'administrateur
                };
                
                using (var process = Process.Start(processInfo))
                {
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode != 0)
                    {
                        _loggingService.LogError("Erreur lors de l'activation de la plateforme de machine virtuelle");
                        return false;
                    }
                }
                
                // Définir WSL 2 comme version par défaut
                var wslSetDefaultProcessInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--set-default-version 2",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(wslSetDefaultProcessInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    bool success = process.ExitCode == 0 && !output.Contains("Error") && !error.Contains("Error");
                    
                    _loggingService.Log($"Définition de WSL 2 comme version par défaut {(success ? "réussie" : "échouée")}");
                    return success;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la mise à jour vers WSL 2", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<List<WSLDistroInfo>> ListDistributionsAsync()
        {
            _loggingService.Log("Listage des distributions WSL...");
            
            var distributions = new List<WSLDistroInfo>();
            
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--list --verbose",
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
                    _loggingService.LogError("Erreur lors du listage des distributions WSL");
                    return distributions;
                }
                
                // Analyser la sortie
                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Ignorer la première ligne (en-tête)
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    // Format attendu: "Ubuntu-20.04 Running 2"
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        var distro = new WSLDistroInfo
                        {
                            Name = parts[0],
                            State = parts[1],
                            WSLVersion = int.TryParse(parts[2], out int version) ? version : 0
                        };
                        
                        // Obtenir plus d'informations sur la distribution
                        await GetDistributionDetailsAsync(distro);
                        
                        distributions.Add(distro);
                    }
                }
                
                _loggingService.Log($"Trouvé {distributions.Count} distributions WSL");
                return distributions;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors du listage des distributions WSL", ex);
                return distributions;
            }
        }

        /// <inheritdoc />
        public async Task<bool> InstallDistributionAsync(string distroName, string version, string installPath)
        {
            _loggingService.Log($"Installation de la distribution {distroName} {version}...");
            
            try
            {
                // Vérifier si WSL est installé
                if (!await IsWSLInstalledAsync())
                {
                    _loggingService.LogWarning("WSL n'est pas installé");
                    return false;
                }
                
                // Vérifier si la distribution est déjà installée
                var distros = await ListDistributionsAsync();
                if (distros.Exists(d => d.Name.Equals(distroName, StringComparison.OrdinalIgnoreCase)))
                {
                    _loggingService.LogWarning($"La distribution {distroName} est déjà installée");
                    return false;
                }
                
                // Créer le répertoire d'installation si nécessaire
                if (!string.IsNullOrEmpty(installPath) && !Directory.Exists(installPath))
                {
                    Directory.CreateDirectory(installPath);
                }
                
                // Installer la distribution (utiliser --root ou --import selon la méthode)
                var processInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"--install -d {distroName}",
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
                    _loggingService.LogError($"Erreur lors de l'installation de {distroName}: {error}");
                    return false;
                }
                
                _loggingService.Log($"Installation de {distroName} réussie");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de l'installation de {distroName}", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ImportDistributionAsync(string tarFilePath, string installPath, string distroName)
        {
            _loggingService.Log($"Importation de la distribution {distroName} depuis {tarFilePath}...");
            
            try
            {
                // Vérifier si WSL est installé
                if (!await IsWSLInstalledAsync())
                {
                    _loggingService.LogWarning("WSL n'est pas installé");
                    return false;
                }
                
                // Vérifier si le fichier tar existe
                if (!File.Exists(tarFilePath))
                {
                    _loggingService.LogError($"Le fichier {tarFilePath} n'existe pas");
                    return false;
                }
                
                // Vérifier si la distribution est déjà installée
                var distros = await ListDistributionsAsync();
                if (distros.Exists(d => d.Name.Equals(distroName, StringComparison.OrdinalIgnoreCase)))
                {
                    _loggingService.LogWarning($"La distribution {distroName} est déjà installée");
                    return false;
                }
                
                // Créer le répertoire d'installation si nécessaire
                if (!string.IsNullOrEmpty(installPath) && !Directory.Exists(installPath))
                {
                    Directory.CreateDirectory(installPath);
                }
                
                // Importer la distribution
                var processInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"--import {distroName} \"{installPath}\" \"{tarFilePath}\"",
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
                    _loggingService.LogError($"Erreur lors de l'importation de {distroName}: {error}");
                    return false;
                }
                
                _loggingService.Log($"Importation de {distroName} réussie");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de l'importation de {distroName}", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetDefaultDistributionAsync(string distroName)
        {
            _loggingService.Log($"Définition de {distroName} comme distribution par défaut...");
            
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"--set-default {distroName}",
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
                    _loggingService.LogError($"Erreur lors de la définition de {distroName} comme distribution par défaut: {error}");
                    return false;
                }
                
                _loggingService.Log($"{distroName} défini comme distribution par défaut");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la définition de {distroName} comme distribution par défaut", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<string> ExecuteCommandAsync(string distroName, string command)
        {
            _loggingService.Log($"Exécution de la commande '{command}' dans {distroName}...");
            
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"-d {distroName} {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    _loggingService.LogError($"Erreur lors de l'exécution de la commande dans {distroName}: {error}");
                    return error;
                }
                
                return output;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de l'exécution de la commande dans {distroName}", ex);
                return $"Erreur: {ex.Message}";
            }
        }
        
        /// <inheritdoc />
        public async Task<bool> InstallWSLAsync()
        {
            _loggingService.Log("Installation de WSL...");
            
            try
            {
                // Utiliser la commande wsl --install qui installe automatiquement tout ce qui est nécessaire
                var processInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--install",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // Exécuter en tant qu'administrateur
                };
                
                using var process = Process.Start(processInfo);
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0 || error.Contains("Error"))
                {
                    _loggingService.LogError($"Erreur lors de l'installation de WSL: {error}");
                    return false;
                }
                
                _loggingService.Log("Installation de WSL réussie. Un redémarrage peut être nécessaire.");
                return true;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de l'installation de WSL", ex);
                return false;
            }
        }

        #region Méthodes privées
        
        /// <summary>
        /// Obtient des détails supplémentaires sur une distribution WSL
        /// </summary>
        /// <param name="distro">Information sur la distribution à compléter</param>
        private async Task GetDistributionDetailsAsync(WSLDistroInfo distro)
        {
            try
            {
                // Obtenir l'emplacement d'installation
                var processInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"-d {distro.Name} --exec echo $WSL_DISTRO_NAME",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(processInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        // Chercher l'emplacement d'installation dans le registre ou les fichiers de configuration
                        // Pour l'instant, on utilise une valeur fictive
                        distro.InstallLocation = $"C:\\WSL\\{distro.Name}";
                    }
                }
                
                // Obtenir l'utilisateur par défaut
                processInfo = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"-d {distro.Name} --exec whoami",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(processInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        distro.DefaultUser = output.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de l'obtention des détails de la distribution {distro.Name}", ex);
            }
        }
        
        #endregion
    }
}