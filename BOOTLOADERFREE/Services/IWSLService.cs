using System.Collections.Generic;
using System.Threading.Tasks;
using BOOTLOADERFREE.Models;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Interface pour le service de gestion de Windows Subsystem for Linux
    /// </summary>
    public interface IWSLService
    {
        /// <summary>
        /// Vérifie si WSL est supporté sur le système
        /// </summary>
        /// <returns>Vrai si WSL est supporté, Faux sinon</returns>
        Task<bool> IsWSLSupportedAsync();

        /// <summary>
        /// Vérifie si WSL est installé sur le système
        /// </summary>
        /// <returns>Vrai si WSL est installé, Faux sinon</returns>
        Task<bool> IsWSLInstalledAsync();
        
        /// <summary>
        /// Vérifie si WSL est disponible sur le système
        /// </summary>
        /// <returns>Vrai si WSL est disponible, Faux sinon</returns>
        Task<bool> IsWSLAvailableAsync();
        
        /// <summary>
        /// Active la fonctionnalité WSL sur le système (nécessite un redémarrage)
        /// </summary>
        /// <returns>Vrai si l'opération a réussi, Faux sinon</returns>
        Task<bool> EnableWSLAsync();
        
        /// <summary>
        /// Met à jour WSL vers la version 2
        /// </summary>
        /// <returns>Vrai si l'opération a réussi, Faux sinon</returns>
        Task<bool> UpdateToWSL2Async();
        
        /// <summary>
        /// Liste toutes les distributions Linux installées via WSL
        /// </summary>
        /// <returns>Liste des informations sur les distributions</returns>
        Task<List<WSLDistroInfo>> ListDistributionsAsync();
        
        /// <summary>
        /// Installe une nouvelle distribution Linux
        /// </summary>
        /// <param name="distroName">Nom de la distribution (Ubuntu, Debian, etc.)</param>
        /// <param name="version">Version de la distribution</param>
        /// <param name="installPath">Chemin d'installation</param>
        /// <returns>Vrai si l'installation a réussi, Faux sinon</returns>
        Task<bool> InstallDistributionAsync(string distroName, string version, string installPath);
        
        /// <summary>
        /// Importe une distribution à partir d'un fichier tar
        /// </summary>
        /// <param name="tarFilePath">Chemin vers le fichier tar</param>
        /// <param name="installPath">Chemin d'installation</param>
        /// <param name="distroName">Nom à donner à la distribution</param>
        /// <returns>Vrai si l'importation a réussi, Faux sinon</returns>
        Task<bool> ImportDistributionAsync(string tarFilePath, string installPath, string distroName);
        
        /// <summary>
        /// Définit une distribution comme distribution par défaut
        /// </summary>
        /// <param name="distroName">Nom de la distribution</param>
        /// <returns>Vrai si l'opération a réussi, Faux sinon</returns>
        Task<bool> SetDefaultDistributionAsync(string distroName);
        
        /// <summary>
        /// Exécute une commande dans une distribution WSL
        /// </summary>
        /// <param name="distroName">Nom de la distribution</param>
        /// <param name="command">Commande à exécuter</param>
        /// <returns>Résultat de la commande</returns>
        Task<string> ExecuteCommandAsync(string distroName, string command);
        
        /// <summary>
        /// Installe WSL sur le système
        /// </summary>
        /// <returns>Vrai si l'installation a réussi, Faux sinon</returns>
        Task<bool> InstallWSLAsync();
    }
}