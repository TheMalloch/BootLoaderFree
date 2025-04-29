using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BOOTLOADERFREE.Models;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Service pour la gestion du bootloader
    /// </summary>
    public class BootloaderService : IBootloaderService
    {
        private readonly ILoggingService _loggingService;

        public BootloaderService(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _loggingService.Log("Service de gestion du bootloader initialisé");
        }

        /// <summary>
        /// Obtient la liste des entrées de démarrage actuelles
        /// </summary>
        public async Task<List<BootEntryInfo>> GetBootEntriesAsync()
        {
            _loggingService.Log("Récupération des entrées de démarrage");
            var entries = new List<BootEntryInfo>();

            try
            {
                // Exécuter bcdedit pour obtenir les entrées existantes
                string output = await ExecuteCommandAsync("bcdedit /enum");

                // Identifier les blocs d'entrées
                string pattern = @"(^-+\s*Windows Boot (Loader|Manager)\s*-+$\s*^identifier\s*\{[^}]+\}.*?)(?=^-+|$)";
                var matches = Regex.Matches(output, pattern, RegexOptions.Multiline | RegexOptions.Singleline);

                foreach (Match match in matches)
                {
                    string entryText = match.Groups[1].Value;
                    
                    // Extraire les informations de l'entrée
                    var entry = new BootEntryInfo
                    {
                        Id = ExtractValue(entryText, @"identifier\s+\{([^}]+)\}"),
                        DisplayName = ExtractValue(entryText, @"description\s+(.+?)$"),
                        Path = ExtractValue(entryText, @"path\s+(.+?)$"),
                        Device = ExtractValue(entryText, @"device\s+(.+?)$"),
                        IsCreatedByUs = entryText.Contains("DualBootDeployer")
                    };

                    entries.Add(entry);
                }

                // Récupérer l'entrée par défaut
                string defaultOutput = await ExecuteCommandAsync("bcdedit /enum {default}");
                string defaultId = ExtractValue(defaultOutput, @"identifier\s+\{([^}]+)\}");

                // Marquer l'entrée par défaut
                var defaultEntry = entries.FirstOrDefault(e => e.Id.Equals(defaultId, StringComparison.OrdinalIgnoreCase));
                if (defaultEntry != null)
                {
                    defaultEntry.IsDefault = true;
                }

                // Récupérer l'ordre des entrées
                string bootOrderOutput = await ExecuteCommandAsync("bcdedit /enum {bootmgr}");
                string bootOrderLine = ExtractValue(bootOrderOutput, @"displayorder\s+(.+?)$");
                
                if (!string.IsNullOrEmpty(bootOrderLine))
                {
                    // Extraire les IDs d'entrées dans l'ordre
                    var orderMatches = Regex.Matches(bootOrderLine, @"\{([^}]+)\}");
                    for (int i = 0; i < orderMatches.Count; i++)
                    {
                        string entryId = orderMatches[i].Groups[1].Value;
                        var entry = entries.FirstOrDefault(e => e.Id.Contains(entryId));
                        if (entry != null)
                        {
                            entry.Order = i + 1;
                        }
                    }
                }

                _loggingService.Log($"Entrées de démarrage récupérées : {entries.Count}");
                return entries;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la récupération des entrées de démarrage", ex);
                return entries;
            }
        }

        /// <summary>
        /// Installe un bootloader sur un disque
        /// </summary>
        public async Task<bool> InstallBootloaderAsync(int diskNumber, string bootloaderType)
        {
            _loggingService.Log($"Installation du bootloader {bootloaderType} sur le disque {diskNumber}");

            try
            {
                string output;
                bool success = false;

                // Installer le type de bootloader spécifié
                switch (bootloaderType.ToLower())
                {
                    case "windows boot manager":
                        // Utiliser bootsect et bcdboot pour configurer un bootloader Windows
                        output = await ExecuteCommandAsync($"bootsect /nt60 all /force");
                        success = !output.Contains("ERREUR") && !output.Contains("ERROR");
                        
                        if (success)
                        {
                            // Trouver le lecteur système Windows
                            string systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
                            systemDrive = systemDrive.TrimEnd('\\');
                            
                            output = await ExecuteCommandAsync($"bcdboot {systemDrive}\\Windows /s {systemDrive}:");
                            success = !output.Contains("ERREUR") && !output.Contains("ERROR");
                        }
                        break;
                        
                    case "grub2":
                        // Installation de GRUB2 - nécessiterait d'autres outils
                        _loggingService.LogWarning("L'installation de GRUB2 n'est pas encore implémentée");
                        return false;
                        
                    default:
                        _loggingService.LogWarning($"Type de bootloader non reconnu: {bootloaderType}");
                        return false;
                }

                if (success)
                {
                    _loggingService.Log($"Installation du bootloader {bootloaderType} réussie");
                    return true;
                }
                else
                {
                    _loggingService.LogError($"Échec de l'installation du bootloader: {output}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de l'installation du bootloader", ex);
                return false;
            }
        }

        /// <summary>
        /// Configure une entrée de démarrage existante ou en crée une nouvelle
        /// </summary>
        public async Task<bool> ConfigureBootEntryAsync(BootEntryInfo bootEntry)
        {
            _loggingService.Log($"Configuration de l'entrée de démarrage: {bootEntry.DisplayName}");

            try
            {
                // Vérifier si l'entrée existe déjà
                var existingEntries = await GetBootEntriesAsync();
                var existingEntry = existingEntries.FirstOrDefault(e => e.Id == bootEntry.Id);

                if (existingEntry != null)
                {
                    // Mettre à jour l'entrée existante
                    string output = await ExecuteCommandAsync($"bcdedit /set {{{bootEntry.Id}}} description \"{bootEntry.DisplayName}\"");
                    bool success = !output.Contains("erreur") && !output.Contains("error");
                    
                    if (success && !string.IsNullOrEmpty(bootEntry.Path))
                    {
                        output = await ExecuteCommandAsync($"bcdedit /set {{{bootEntry.Id}}} path \"{bootEntry.Path}\"");
                        success = success && !output.Contains("erreur") && !output.Contains("error");
                    }

                    if (success && !string.IsNullOrEmpty(bootEntry.Options))
                    {
                        output = await ExecuteCommandAsync($"bcdedit /set {{{bootEntry.Id}}} systemroot \"{bootEntry.Options}\"");
                        success = success && !output.Contains("erreur") && !output.Contains("error");
                    }

                    if (success)
                        _loggingService.Log($"Entrée de démarrage {bootEntry.DisplayName} mise à jour avec succès");
                    else
                        _loggingService.LogError($"Échec de la mise à jour de l'entrée de démarrage {bootEntry.DisplayName}");

                    return success;
                }
                else
                {
                    // Créer une nouvelle entrée
                    // Copier l'entrée actuelle comme modèle
                    string output = await ExecuteCommandAsync($"bcdedit /copy {{current}} /d \"{bootEntry.DisplayName}\"");
                    
                    // Récupérer l'ID de la nouvelle entrée
                    var match = Regex.Match(output, @"\{([^}]+)\}");
                    if (!match.Success)
                    {
                        _loggingService.LogError($"Impossible de créer une nouvelle entrée de démarrage: {output}");
                        return false;
                    }

                    string newId = match.Groups[1].Value;
                    bootEntry.Id = newId;

                    // Configurer la nouvelle entrée
                    bool success = true;
                    
                    if (!string.IsNullOrEmpty(bootEntry.Path))
                    {
                        output = await ExecuteCommandAsync($"bcdedit /set {{{newId}}} path \"{bootEntry.Path}\"");
                        success = success && !output.Contains("erreur") && !output.Contains("error");
                    }

                    if (success && !string.IsNullOrEmpty(bootEntry.Options))
                    {
                        output = await ExecuteCommandAsync($"bcdedit /set {{{newId}}} systemroot \"{bootEntry.Options}\"");
                        success = success && !output.Contains("erreur") && !output.Contains("error");
                    }

                    if (success && !string.IsNullOrEmpty(bootEntry.Device))
                    {
                        output = await ExecuteCommandAsync($"bcdedit /set {{{newId}}} device \"{bootEntry.Device}\"");
                        success = success && !output.Contains("erreur") && !output.Contains("error");
                    }

                    // Ajouter l'entrée à l'ordre d'affichage
                    output = await ExecuteCommandAsync($"bcdedit /displayorder {{{newId}}} /addlast");
                    success = success && !output.Contains("erreur") && !output.Contains("error");

                    if (success)
                        _loggingService.Log($"Entrée de démarrage {bootEntry.DisplayName} créée avec succès, ID: {{{newId}}}");
                    else
                        _loggingService.LogError($"Échec de la création de l'entrée de démarrage {bootEntry.DisplayName}");

                    return success;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la configuration de l'entrée de démarrage", ex);
                return false;
            }
        }

        /// <summary>
        /// Supprime une entrée de démarrage
        /// </summary>
        public async Task<bool> RemoveBootEntryAsync(string entryId)
        {
            _loggingService.Log($"Suppression de l'entrée de démarrage: {{{entryId}}}");

            try
            {
                string output = await ExecuteCommandAsync($"bcdedit /delete {{{entryId}}}");
                bool success = !output.Contains("erreur") && !output.Contains("error");

                if (success)
                    _loggingService.Log($"Entrée de démarrage {{{entryId}}} supprimée avec succès");
                else
                    _loggingService.LogError($"Échec de la suppression de l'entrée de démarrage {{{entryId}}}: {output}");

                return success;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la suppression de l'entrée de démarrage", ex);
                return false;
            }
        }

        /// <summary>
        /// Définit le système d'exploitation par défaut
        /// </summary>
        public async Task<bool> SetDefaultOSAsync(string entryId)
        {
            _loggingService.Log($"Définition de l'entrée de démarrage par défaut: {{{entryId}}}");

            try
            {
                string output = await ExecuteCommandAsync($"bcdedit /default {{{entryId}}}");
                bool success = !output.Contains("erreur") && !output.Contains("error");

                if (success)
                    _loggingService.Log($"Entrée de démarrage par défaut définie sur {{{entryId}}}");
                else
                    _loggingService.LogError($"Échec de la définition de l'entrée de démarrage par défaut: {output}");

                return success;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la définition de l'entrée de démarrage par défaut", ex);
                return false;
            }
        }

        /// <summary>
        /// Définit le délai d'attente du bootloader
        /// </summary>
        public async Task<bool> SetBootTimeoutAsync(int timeoutSeconds)
        {
            _loggingService.Log($"Définition du délai d'attente du bootloader: {timeoutSeconds} secondes");

            try
            {
                string output = await ExecuteCommandAsync($"bcdedit /timeout {timeoutSeconds}");
                bool success = !output.Contains("erreur") && !output.Contains("error");

                if (success)
                    _loggingService.Log($"Délai d'attente du bootloader défini sur {timeoutSeconds} secondes");
                else
                    _loggingService.LogError($"Échec de la définition du délai d'attente du bootloader: {output}");

                return success;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la définition du délai d'attente du bootloader", ex);
                return false;
            }
        }

        /// <summary>
        /// Obtient l'identifiant de l'entrée de démarrage par défaut
        /// </summary>
        public async Task<string> GetDefaultOSAsync()
        {
            _loggingService.Log("Récupération de l'entrée de démarrage par défaut");

            try
            {
                string output = await ExecuteCommandAsync("bcdedit /enum {default}");
                string defaultId = ExtractValue(output, @"identifier\s+\{([^}]+)\}");

                _loggingService.Log($"Entrée de démarrage par défaut: {{{defaultId}}}");
                return defaultId;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la récupération de l'entrée de démarrage par défaut", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Obtient le délai d'attente actuel du bootloader
        /// </summary>
        public async Task<int> GetBootTimeoutAsync()
        {
            _loggingService.Log("Récupération du délai d'attente du bootloader");

            try
            {
                string output = await ExecuteCommandAsync("bcdedit /enum {bootmgr}");
                string timeoutStr = ExtractValue(output, @"timeout\s+(\d+)");

                if (int.TryParse(timeoutStr, out int timeout))
                {
                    _loggingService.Log($"Délai d'attente du bootloader: {timeout} secondes");
                    return timeout;
                }
                else
                {
                    _loggingService.LogWarning($"Impossible de récupérer le délai d'attente du bootloader: {output}");
                    return 30; // Valeur par défaut si non trouvée
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la récupération du délai d'attente du bootloader", ex);
                return 30; // Valeur par défaut
            }
        }

        /// <summary>
        /// Sauvegarde la configuration du bootloader
        /// </summary>
        public async Task<bool> BackupBootConfigurationAsync(string backupPath)
        {
            _loggingService.Log($"Sauvegarde de la configuration du bootloader vers {backupPath}");

            try
            {
                string output = await ExecuteCommandAsync($"bcdedit /export \"{backupPath}\"");
                bool success = !output.Contains("erreur") && !output.Contains("error");

                if (success)
                {
                    _loggingService.Log($"Configuration du bootloader sauvegardée avec succès");
                    return true;
                }
                else
                {
                    _loggingService.LogError($"Échec de la sauvegarde de la configuration du bootloader: {output}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la sauvegarde de la configuration du bootloader", ex);
                return false;
            }
        }

        /// <summary>
        /// Restaure la configuration du bootloader à partir d'une sauvegarde
        /// </summary>
        public async Task<bool> RestoreBootConfigurationAsync(string backupPath)
        {
            _loggingService.Log($"Restauration de la configuration du bootloader depuis {backupPath}");

            try
            {
                if (!File.Exists(backupPath))
                {
                    _loggingService.LogError($"Le fichier de sauvegarde {backupPath} n'existe pas");
                    return false;
                }

                string output = await ExecuteCommandAsync($"bcdedit /import \"{backupPath}\"");
                bool success = !output.Contains("erreur") && !output.Contains("error");

                if (success)
                {
                    _loggingService.Log($"Configuration du bootloader restaurée avec succès");
                    return true;
                }
                else
                {
                    _loggingService.LogError($"Échec de la restauration de la configuration du bootloader: {output}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la restauration de la configuration du bootloader", ex);
                return false;
            }
        }

        #region Méthodes privées

        /// <summary>
        /// Exécute une commande système et retourne sa sortie
        /// </summary>
        private async Task<string> ExecuteCommandAsync(string command)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // Exécuter en tant qu'admin
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());

                if (!string.IsNullOrWhiteSpace(error))
                    output += "\n" + error;

                return output;
            }
        }

        /// <summary>
        /// Extrait une valeur à partir d'un pattern regex dans un texte
        /// </summary>
        private string ExtractValue(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.Multiline);
            if (match.Success && match.Groups.Count > 1)
                return match.Groups[1].Value.Trim();

            return string.Empty;
        }

        #endregion
    }
}