using System.Collections.Generic;
using System.Threading.Tasks;
using BOOTLOADERFREE.Models;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Interface pour le service de gestion du bootloader
    /// </summary>
    public interface IBootloaderService
    {
        /// <summary>
        /// Obtient la liste des entrées de démarrage actuelles
        /// </summary>
        /// <returns>Liste des entrées de démarrage</returns>
        Task<List<BootEntryInfo>> GetBootEntriesAsync();
        
        /// <summary>
        /// Installe un bootloader sur un disque
        /// </summary>
        /// <param name="diskNumber">Numéro du disque</param>
        /// <param name="bootloaderType">Type de bootloader (ex: "Windows Boot Manager", "GRUB2")</param>
        /// <returns>Vrai si l'installation a réussi, Faux sinon</returns>
        Task<bool> InstallBootloaderAsync(int diskNumber, string bootloaderType);
        
        /// <summary>
        /// Configure une entrée de démarrage existante ou en crée une nouvelle
        /// </summary>
        /// <param name="bootEntry">Informations sur l'entrée de démarrage</param>
        /// <returns>Vrai si la configuration a réussi, Faux sinon</returns>
        Task<bool> ConfigureBootEntryAsync(BootEntryInfo bootEntry);
        
        /// <summary>
        /// Supprime une entrée de démarrage
        /// </summary>
        /// <param name="entryId">Identifiant de l'entrée à supprimer</param>
        /// <returns>Vrai si la suppression a réussi, Faux sinon</returns>
        Task<bool> RemoveBootEntryAsync(string entryId);
        
        /// <summary>
        /// Définit le système d'exploitation par défaut
        /// </summary>
        /// <param name="entryId">Identifiant de l'entrée à définir comme par défaut</param>
        /// <returns>Vrai si l'opération a réussi, Faux sinon</returns>
        Task<bool> SetDefaultOSAsync(string entryId);
        
        /// <summary>
        /// Définit le délai d'attente du bootloader
        /// </summary>
        /// <param name="timeoutSeconds">Délai d'attente en secondes</param>
        /// <returns>Vrai si l'opération a réussi, Faux sinon</returns>
        Task<bool> SetBootTimeoutAsync(int timeoutSeconds);
        
        /// <summary>
        /// Obtient l'identifiant de l'entrée de démarrage par défaut
        /// </summary>
        /// <returns>Identifiant de l'entrée par défaut</returns>
        Task<string> GetDefaultOSAsync();
        
        /// <summary>
        /// Obtient le délai d'attente actuel du bootloader
        /// </summary>
        /// <returns>Délai d'attente en secondes</returns>
        Task<int> GetBootTimeoutAsync();
        
        /// <summary>
        /// Sauvegarde la configuration du bootloader
        /// </summary>
        /// <param name="backupPath">Chemin où sauvegarder la configuration</param>
        /// <returns>Vrai si la sauvegarde a réussi, Faux sinon</returns>
        Task<bool> BackupBootConfigurationAsync(string backupPath);
        
        /// <summary>
        /// Restaure la configuration du bootloader à partir d'une sauvegarde
        /// </summary>
        /// <param name="backupPath">Chemin de la sauvegarde à restaurer</param>
        /// <returns>Vrai si la restauration a réussi, Faux sinon</returns>
        Task<bool> RestoreBootConfigurationAsync(string backupPath);
    }
}