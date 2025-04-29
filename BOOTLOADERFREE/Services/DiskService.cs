using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BOOTLOADERFREE.Helpers;
using BOOTLOADERFREE.Models;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Implémentation du service de gestion de disques
    /// </summary>
    public class DiskService : IDiskService
    {
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Constructeur du service de gestion de disques
        /// </summary>
        /// <param name="loggingService">Service de journalisation</param>
        public DiskService(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _loggingService.Log("Service de gestion de disques initialisé");
        }

        /// <summary>
        /// Obtient la liste des disques disponibles sur le système
        /// </summary>
        /// <returns>Liste des informations sur les disques</returns>
        public async Task<List<DiskInfo>> GetAvailableDisksAsync()
        {
            _loggingService.Log("Recherche des disques disponibles");
            var disks = new List<DiskInfo>();

            try
            {
                // Utilisation de WMI pour récupérer les informations des disques
                using (var diskSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                using (var diskCollection = diskSearcher.Get())
                {
                    foreach (var disk in diskCollection)
                    {
                        // Extraction du numéro de disque à partir de DeviceID (ex: \\.\PHYSICALDRIVE0)
                        string deviceId = disk["DeviceID"].ToString();
                        int diskNumber = int.Parse(Regex.Match(deviceId, @"\d+$").Value);

                        var diskInfo = new DiskInfo
                        {
                            DiskNumber = diskNumber,
                            Model = disk["Model"].ToString(),
                            Size = Convert.ToInt64(disk["Size"]),
                            IsRemovable = Convert.ToBoolean(disk["MediaType"].ToString().Contains("Removable")),
                            FreeSpace = 0, // Sera calculé après avoir obtenu les partitions
                            PartitionStyle = await GetDiskPartitionStyleAsync(diskNumber)
                        };

                        // Vérifier si c'est le disque système
                        diskInfo.IsSystemDisk = await IsSystemDiskAsync(diskNumber);

                        // Récupérer les partitions du disque
                        diskInfo.Partitions = await GetDiskPartitionsAsync(diskNumber);

                        // Calculer l'espace libre
                        diskInfo.FreeSpace = CalculateFreeSpace(diskInfo);

                        disks.Add(diskInfo);
                    }
                }

                _loggingService.Log($"Nombre de disques trouvés : {disks.Count}");
                return disks;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la récupération des disques", ex);
                throw;
            }
        }

        /// <summary>
        /// Crée une nouvelle partition sur un disque
        /// </summary>
        /// <param name="diskNumber">Numéro du disque</param>
        /// <param name="sizeInMB">Taille de la partition en MB</param>
        /// <param name="fileSystem">Système de fichiers à utiliser</param>
        /// <returns>Informations sur la partition créée ou null si échec</returns>
        public async Task<PartitionInfo> CreatePartitionAsync(int diskNumber, long sizeInMB, string fileSystem = "NTFS")
        {
            _loggingService.Log($"Création d'une partition de {sizeInMB} MB sur le disque {diskNumber} avec le système de fichiers {fileSystem}");

            try
            {
                // Vérifier qu'il y a assez d'espace libre
                if (!await HasEnoughFreeSpaceAsync(diskNumber, sizeInMB))
                {
                    _loggingService.LogWarning($"Espace insuffisant sur le disque {diskNumber} pour créer une partition de {sizeInMB} MB");
                    return null;
                }

                // Utiliser DiskPart pour créer la partition
                string diskpartScript = $@"select disk {diskNumber}
create partition primary size={sizeInMB}
format quick fs={fileSystem}
exit";

                string tempScriptPath = Path.Combine(Path.GetTempPath(), $"diskpart_script_{Guid.NewGuid()}.txt");
                File.WriteAllText(tempScriptPath, diskpartScript);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "diskpart.exe",
                    Arguments = $"/s \"{tempScriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // Exécuter en tant qu'administrateur
                };

                string output;
                using (var process = Process.Start(processInfo))
                {
                    output = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string error = await process.StandardError.ReadToEndAsync();
                        _loggingService.LogError($"Erreur lors de la création de la partition : {error}");
                        File.Delete(tempScriptPath);
                        return null;
                    }
                }

                File.Delete(tempScriptPath);

                // Extraire la lettre de lecteur assignée (si disponible)
                string driveLetter = ExtractDriveLetterFromDiskpartOutput(output);

                // Récupérer les informations sur la nouvelle partition
                var partitions = await GetDiskPartitionsAsync(diskNumber);
                var newPartition = partitions.Find(p => p.DriveLetter == driveLetter);

                if (newPartition != null)
                {
                    _loggingService.Log($"Partition créée avec succès : {newPartition.DriveLetter}");
                    return newPartition;
                }
                else
                {
                    _loggingService.LogWarning("Partition créée mais impossible de récupérer ses informations");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de la création de la partition", ex);
                throw;
            }
        }

        /// <summary>
        /// Formate une partition existante
        /// </summary>
        /// <param name="partitionLetter">Lettre de lecteur de la partition</param>
        /// <param name="fileSystem">Système de fichiers à utiliser</param>
        /// <param name="label">Étiquette de volume</param>
        /// <returns>Vrai si le formatage a réussi, Faux sinon</returns>
        public async Task<bool> FormatPartitionAsync(char partitionLetter, string fileSystem = "NTFS", string label = "")
        {
            _loggingService.Log($"Formatage de la partition {partitionLetter}: avec le système de fichiers {fileSystem} et l'étiquette '{label}'");

            try
            {
                // Construire la commande de formatage
                string formatCommand = $"format {partitionLetter}: /fs:{fileSystem} /q";
                
                if (!string.IsNullOrWhiteSpace(label))
                    formatCommand += $" /v:{label}";
                
                formatCommand += " /y"; // Confirmer automatiquement

                // Exécuter la commande de formatage
                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {formatCommand}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // Exécuter en tant qu'administrateur
                };

                using (var process = Process.Start(processInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string error = await process.StandardError.ReadToEndAsync();
                        _loggingService.LogError($"Erreur lors du formatage de la partition : {error}");
                        return false;
                    }
                    
                    if (output.Contains("Le formatage s'est terminé avec succès") || 
                        output.Contains("Format completed successfully"))
                    {
                        _loggingService.Log($"Partition {partitionLetter}: formatée avec succès");
                        return true;
                    }
                    else
                    {
                        _loggingService.LogWarning($"Formatage de la partition {partitionLetter}: terminé mais résultat incertain");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors du formatage de la partition {partitionLetter}:", ex);
                return false;
            }
        }

        /// <summary>
        /// Vérifie s'il y a suffisamment d'espace libre sur un disque
        /// </summary>
        /// <param name="diskNumber">Numéro du disque</param>
        /// <param name="requiredSizeInMB">Taille requise en MB</param>
        /// <returns>Vrai s'il y a suffisamment d'espace, Faux sinon</returns>
        public async Task<bool> HasEnoughFreeSpaceAsync(int diskNumber, long requiredSizeInMB)
        {
            try
            {
                var disks = await GetAvailableDisksAsync();
                var disk = disks.Find(d => d.DiskNumber == diskNumber);

                if (disk == null)
                {
                    _loggingService.LogWarning($"Disque {diskNumber} non trouvé");
                    return false;
                }

                long requiredSizeBytes = requiredSizeInMB * 1024 * 1024;
                bool hasEnoughSpace = disk.FreeSpace >= requiredSizeBytes;

                _loggingService.Log($"Vérification de l'espace sur le disque {diskNumber}: requis {requiredSizeBytes} octets, disponible {disk.FreeSpace} octets, résultat: {hasEnoughSpace}");
                return hasEnoughSpace;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la vérification de l'espace libre sur le disque {diskNumber}", ex);
                return false;
            }
        }

        /// <summary>
        /// Redimensionne une partition existante
        /// </summary>
        /// <param name="partitionLetter">Lettre de lecteur de la partition</param>
        /// <param name="newSizeInMB">Nouvelle taille en MB</param>
        /// <returns>Vrai si le redimensionnement a réussi, Faux sinon</returns>
        public async Task<bool> ResizePartitionAsync(char partitionLetter, long newSizeInMB)
        {
            _loggingService.Log($"Redimensionnement de la partition {partitionLetter}: à {newSizeInMB} MB");

            try
            {
                // Utiliser DiskPart pour redimensionner la partition
                string diskpartScript = $@"select volume {partitionLetter}
extend size={newSizeInMB}
exit";

                string tempScriptPath = Path.Combine(Path.GetTempPath(), $"diskpart_script_{Guid.NewGuid()}.txt");
                File.WriteAllText(tempScriptPath, diskpartScript);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "diskpart.exe",
                    Arguments = $"/s \"{tempScriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // Exécuter en tant qu'administrateur
                };

                using (var process = Process.Start(processInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();

                    File.Delete(tempScriptPath);

                    if (process.ExitCode != 0 || output.Contains("ERREUR:"))
                    {
                        string error = await process.StandardError.ReadToEndAsync();
                        _loggingService.LogError($"Erreur lors du redimensionnement de la partition : {error}");
                        return false;
                    }

                    _loggingService.Log($"Partition {partitionLetter}: redimensionnée avec succès");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors du redimensionnement de la partition {partitionLetter}:", ex);
                return false;
            }
        }

        /// <summary>
        /// Rend une partition active (bootable)
        /// </summary>
        /// <param name="diskNumber">Numéro du disque</param>
        /// <param name="partitionNumber">Numéro de la partition</param>
        /// <returns>Vrai si l'opération a réussi, Faux sinon</returns>
        public async Task<bool> SetPartitionActiveAsync(int diskNumber, int partitionNumber)
        {
            _loggingService.Log($"Définition de la partition {partitionNumber} du disque {diskNumber} comme active");

            try
            {
                // Utiliser DiskPart pour rendre la partition active
                string diskpartScript = $@"select disk {diskNumber}
select partition {partitionNumber}
active
exit";

                string tempScriptPath = Path.Combine(Path.GetTempPath(), $"diskpart_script_{Guid.NewGuid()}.txt");
                File.WriteAllText(tempScriptPath, diskpartScript);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "diskpart.exe",
                    Arguments = $"/s \"{tempScriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // Exécuter en tant qu'administrateur
                };

                using (var process = Process.Start(processInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();

                    File.Delete(tempScriptPath);

                    if (process.ExitCode != 0 || output.Contains("ERREUR:"))
                    {
                        string error = await process.StandardError.ReadToEndAsync();
                        _loggingService.LogError($"Erreur lors de la définition de la partition active : {error}");
                        return false;
                    }

                    _loggingService.Log($"Partition {partitionNumber} du disque {diskNumber} définie comme active avec succès");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la définition de la partition {partitionNumber} du disque {diskNumber} comme active", ex);
                return false;
            }
        }

        #region Méthodes privées

        /// <summary>
        /// Obtient la liste des partitions d'un disque
        /// </summary>
        /// <param name="diskNumber">Numéro du disque</param>
        /// <returns>Liste des informations sur les partitions</returns>
        private async Task<List<PartitionInfo>> GetDiskPartitionsAsync(int diskNumber)
        {
            var partitions = new List<PartitionInfo>();

            try
            {
                // Requête WMI pour récupérer les partitions du disque spécifié
                string query = $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='\\\\\\\\.\\\\PHYSICALDRIVE{diskNumber}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition";
                
                using (var partitionSearcher = new ManagementObjectSearcher(query))
                using (var partitionCollection = partitionSearcher.Get())
                {
                    foreach (var partition in partitionCollection)
                    {
                        string partitionDeviceId = partition["DeviceID"].ToString();
                        
                        // Récupérer les informations du volume logique associé à cette partition
                        using (var logicalDiskSearcher = new ManagementObjectSearcher(
                            $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partitionDeviceId}'}} WHERE AssocClass=Win32_LogicalDiskToPartition"))
                        using (var logicalDisks = logicalDiskSearcher.Get())
                        {
                            foreach (var logicalDisk in logicalDisks)
                            {
                                string driveLetter = logicalDisk["DeviceID"].ToString().TrimEnd(':');
                                
                                var partitionInfo = new PartitionInfo
                                {
                                    PartitionNumber = int.Parse(partition["Index"].ToString()),
                                    DriveLetter = driveLetter,
                                    Size = Convert.ToInt64(partition["Size"]),
                                    PartitionType = GetPartitionTypeString(Convert.ToUInt32(partition["Type"])),
                                    Offset = Convert.ToInt64(partition["StartingOffset"]),
                                    FileSystem = logicalDisk["FileSystem"].ToString(),
                                    Label = logicalDisk["VolumeName"].ToString(),
                                    FreeSpace = Convert.ToInt64(logicalDisk["FreeSpace"])
                                };
                                
                                // Vérifier si la partition est active, système, ou boot
                                partitionInfo.IsActive = Convert.ToBoolean(partition["BootPartition"]);
                                partitionInfo.IsSystem = IsSystemPartition(driveLetter);
                                partitionInfo.IsBoot = IsBootPartition(driveLetter);
                                
                                partitions.Add(partitionInfo);
                            }
                        }
                    }
                }
                
                return partitions;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la récupération des partitions du disque {diskNumber}", ex);
                return partitions;
            }
        }
        
        /// <summary>
        /// Vérifie si un disque est le disque système
        /// </summary>
        /// <param name="diskNumber">Numéro du disque</param>
        /// <returns>Vrai si c'est le disque système, Faux sinon</returns>
        private async Task<bool> IsSystemDiskAsync(int diskNumber)
        {
            try
            {
                // Le disque système contient la partition système ou la partition de démarrage
                var partitions = await GetDiskPartitionsAsync(diskNumber);
                return partitions.Exists(p => p.IsSystem || p.IsBoot);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la vérification si le disque {diskNumber} est le disque système", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Vérifie si une partition est la partition système
        /// </summary>
        /// <param name="driveLetter">Lettre de lecteur</param>
        /// <returns>Vrai si c'est la partition système, Faux sinon</returns>
        private bool IsSystemPartition(string driveLetter)
        {
            if (string.IsNullOrEmpty(driveLetter))
                return false;
                
            try
            {
                string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string systemDrive = Path.GetPathRoot(systemRoot).TrimEnd('\\', ':');
                
                return driveLetter.Equals(systemDrive, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la vérification si {driveLetter} est la partition système", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Vérifie si une partition est la partition de démarrage
        /// </summary>
        /// <param name="driveLetter">Lettre de lecteur</param>
        /// <returns>Vrai si c'est la partition de démarrage, Faux sinon</returns>
        private bool IsBootPartition(string driveLetter)
        {
            if (string.IsNullOrEmpty(driveLetter))
                return false;
                
            try
            {
                // En général, la partition de démarrage est aussi la partition système dans les systèmes modernes
                // Pour une détection plus précise, on pourrait vérifier la présence de bootmgr, BCD, etc.
                string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string systemDrive = Path.GetPathRoot(systemRoot).TrimEnd('\\', ':');
                
                return driveLetter.Equals(systemDrive, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la vérification si {driveLetter} est la partition de démarrage", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Récupère le style de partition d'un disque (MBR ou GPT)
        /// </summary>
        /// <param name="diskNumber">Numéro du disque</param>
        /// <returns>Style de partition ("MBR", "GPT" ou "Unknown")</returns>
        private async Task<string> GetDiskPartitionStyleAsync(int diskNumber)
        {
            try
            {
                // Utiliser DiskPart pour obtenir des informations détaillées sur le disque
                string diskpartScript = $@"select disk {diskNumber}
detail disk
exit";

                string tempScriptPath = Path.Combine(Path.GetTempPath(), $"diskpart_script_{Guid.NewGuid()}.txt");
                File.WriteAllText(tempScriptPath, diskpartScript);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "diskpart.exe",
                    Arguments = $"/s \"{tempScriptPath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas" // Exécuter en tant qu'administrateur
                };

                using (var process = Process.Start(processInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();

                    File.Delete(tempScriptPath);

                    if (output.Contains("Style de partition : MBR") || output.Contains("Partition Style: MBR"))
                        return "MBR";
                    else if (output.Contains("Style de partition : GPT") || output.Contains("Partition Style: GPT"))
                        return "GPT";
                    else
                        return "Unknown";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors de la détermination du style de partition du disque {diskNumber}", ex);
                return "Unknown";
            }
        }
        
        /// <summary>
        /// Calcule l'espace libre total sur un disque
        /// </summary>
        /// <param name="diskInfo">Informations sur le disque</param>
        /// <returns>Espace libre en octets</returns>
        private long CalculateFreeSpace(DiskInfo diskInfo)
        {
            try
            {
                // Obtenir l'espace total non alloué
                long allocatedSpace = 0;
                foreach (var partition in diskInfo.Partitions)
                {
                    allocatedSpace += partition.Size;
                }

                long unallocatedSpace = diskInfo.Size - allocatedSpace;
                
                // Ajouter l'espace libre dans les partitions existantes
                long freeSpaceInPartitions = 0;
                foreach (var partition in diskInfo.Partitions)
                {
                    freeSpaceInPartitions += partition.FreeSpace;
                }
                
                _loggingService.Log($"Disque {diskInfo.DiskNumber}: espace non alloué = {unallocatedSpace} octets, espace libre dans les partitions = {freeSpaceInPartitions} octets");
                
                // L'espace libre total est l'espace non alloué plus l'espace libre dans les partitions
                return unallocatedSpace + freeSpaceInPartitions;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Erreur lors du calcul de l'espace libre sur le disque {diskInfo.DiskNumber}", ex);
                return 0;
            }
        }
        
        /// <summary>
        /// Convertit le code numérique du type de partition en chaîne descriptive
        /// </summary>
        /// <param name="partitionType">Code du type de partition</param>
        /// <returns>Description du type de partition</returns>
        private string GetPartitionTypeString(uint partitionType)
        {
            switch (partitionType)
            {
                case 0x01: return "FAT12 Primary";
                case 0x04: return "FAT16 Primary";
                case 0x05: return "Extended";
                case 0x06: return "FAT16 Primary";
                case 0x07: return "NTFS Primary";
                case 0x0B: return "FAT32 Primary";
                case 0x0C: return "FAT32 Primary (LBA)";
                case 0x0E: return "FAT16 Primary (LBA)";
                case 0x0F: return "Extended (LBA)";
                case 0x11: return "Hidden FAT12";
                case 0x14: return "Hidden FAT16";
                case 0x16: return "Hidden FAT16";
                case 0x17: return "Hidden NTFS";
                case 0x1B: return "Hidden FAT32";
                case 0x1C: return "Hidden FAT32 (LBA)";
                case 0x1E: return "Hidden FAT16 (LBA)";
                case 0x27: return "Hidden NTFS (Recovery)";
                case 0x42: return "Dynamic Disk";
                case 0x80: return "MBR Protective";
                case 0x82: return "Linux Swap";
                case 0x83: return "Linux Native";
                case 0x84: return "Hibernation";
                case 0x85: return "Linux Extended";
                case 0x86: return "NTFS Volume Set";
                case 0x87: return "NTFS Volume Set";
                case 0xA0: return "Hibernation";
                case 0xEE: return "GPT Protective";
                case 0xEF: return "EFI System Partition";
                default: return $"Unknown ({partitionType})";
            }
        }
        
        /// <summary>
        /// Extrait la lettre de lecteur assignée d'après la sortie de DiskPart
        /// </summary>
        /// <param name="diskpartOutput">Sortie de la commande DiskPart</param>
        /// <returns>Lettre de lecteur ou chaîne vide si non trouvée</returns>
        private string ExtractDriveLetterFromDiskpartOutput(string diskpartOutput)
        {
            if (string.IsNullOrEmpty(diskpartOutput))
                return string.Empty;
                
            try
            {
                // Recherche de motifs comme "Volume 3 is the selected volume. Drive letter is D"
                var match = Regex.Match(diskpartOutput, @"Drive\s+letter\s+is\s+(\w)");
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
                
                // Tentative alternative
                match = Regex.Match(diskpartOutput, @"Volume\s+\d+\s+(?:.*\s+)?(\w):");
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Erreur lors de l'extraction de la lettre de lecteur", ex);
                return string.Empty;
            }
        }
        
        #endregion
    }
}