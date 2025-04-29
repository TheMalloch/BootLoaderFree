using System.Collections.Generic;
using System.Threading.Tasks;
using BOOTLOADERFREE.Models;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Interface pour le service de gestion des disques
    /// </summary>
    public interface IDiskService
    {
        /// <summary>
        /// Obtient la liste des disques disponibles sur le système
        /// </summary>
        /// <returns>Liste des informations sur les disques</returns>
        Task<List<DiskInfo>> GetAvailableDisksAsync();
        
        /// <summary>
        /// Crée une nouvelle partition sur un disque
        /// </summary>
        /// <param name="diskNumber">Numéro du disque</param>
        /// <param name="sizeInMB">Taille de la partition en MB</param>
        /// <param name="fileSystem">Système de fichiers à utiliser</param>
        /// <returns>Informations sur la partition créée ou null si échec</returns>
        Task<PartitionInfo> CreatePartitionAsync(int diskNumber, long sizeInMB, string fileSystem = "NTFS");
        
        /// <summary>
        /// Formate une partition existante
        /// </summary>
        /// <param name="partitionLetter">Lettre de lecteur de la partition</param>
        /// <param name="fileSystem">Système de fichiers à utiliser</param>
        /// <param name="label">Étiquette de volume</param>
        /// <returns>Vrai si le formatage a réussi, Faux sinon</returns>
        Task<bool> FormatPartitionAsync(char partitionLetter, string fileSystem = "NTFS", string label = "");
        
        /// <summary>
        /// Vérifie s'il y a suffisamment d'espace libre sur un disque
        /// </summary>
        /// <param name="diskNumber">Numéro du disque</param>
        /// <param name="requiredSizeInMB">Taille requise en MB</param>
        /// <returns>Vrai s'il y a suffisamment d'espace, Faux sinon</returns>
        Task<bool> HasEnoughFreeSpaceAsync(int diskNumber, long requiredSizeInMB);
        
        /// <summary>
        /// Redimensionne une partition existante
        /// </summary>
        /// <param name="partitionLetter">Lettre de lecteur de la partition</param>
        /// <param name="newSizeInMB">Nouvelle taille en MB</param>
        /// <returns>Vrai si le redimensionnement a réussi, Faux sinon</returns>
        Task<bool> ResizePartitionAsync(char partitionLetter, long newSizeInMB);
        
        /// <summary>
        /// Rend une partition active (bootable)
        /// </summary>
        /// <param name="diskNumber">Numéro du disque</param>
        /// <param name="partitionNumber">Numéro de la partition</param>
        /// <returns>Vrai si l'opération a réussi, Faux sinon</returns>
        Task<bool> SetPartitionActiveAsync(int diskNumber, int partitionNumber);
    }
}