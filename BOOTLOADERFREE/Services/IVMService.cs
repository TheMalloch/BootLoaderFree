using System.Collections.Generic;
using System.Threading.Tasks;
using BOOTLOADERFREE.Models;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Interface pour le service de gestion des machines virtuelles
    /// </summary>
    public interface IVMService
    {
        /// <summary>
        /// Vérifie si la virtualisation est supportée sur le système
        /// </summary>
        /// <returns>Vrai si la virtualisation est supportée, Faux sinon</returns>
        Task<bool> IsVirtualizationSupportedAsync();
        
        /// <summary>
        /// Vérifie si Hyper-V est activé sur le système
        /// </summary>
        /// <returns>Vrai si Hyper-V est activé, Faux sinon</returns>
        Task<bool> IsHyperVEnabledAsync();
        
        /// <summary>
        /// Active Hyper-V sur le système (nécessite un redémarrage)
        /// </summary>
        /// <returns>Vrai si l'opération a réussi, Faux sinon</returns>
        Task<bool> EnableHyperVAsync();
        
        /// <summary>
        /// Crée une nouvelle machine virtuelle
        /// </summary>
        /// <param name="vmConfig">Configuration de la machine virtuelle</param>
        /// <returns>ID de la machine virtuelle créée ou null si échec</returns>
        Task<string> CreateVirtualMachineAsync(VMConfig vmConfig);
        
        /// <summary>
        /// Attache un disque virtuel à une machine virtuelle
        /// </summary>
        /// <param name="vmId">ID de la machine virtuelle</param>
        /// <param name="vhdPath">Chemin vers le disque virtuel</param>
        /// <returns>Vrai si l'opération a réussi, Faux sinon</returns>
        Task<bool> AttachVirtualDiskAsync(string vmId, string vhdPath);
        
        /// <summary>
        /// Crée un nouveau disque virtuel
        /// </summary>
        /// <param name="path">Chemin où créer le disque virtuel</param>
        /// <param name="sizeInGB">Taille du disque en GB</param>
        /// <param name="isDynamic">Vrai pour un disque à taille dynamique, Faux pour une taille fixe</param>
        /// <returns>Chemin du disque créé ou null si échec</returns>
        Task<string> CreateVirtualDiskAsync(string path, int sizeInGB, bool isDynamic = true);
        
        /// <summary>
        /// Démarre une machine virtuelle
        /// </summary>
        /// <param name="vmId">ID de la machine virtuelle</param>
        /// <returns>Vrai si l'opération a réussi, Faux sinon</returns>
        Task<bool> StartVirtualMachineAsync(string vmId);
        
        /// <summary>
        /// Liste toutes les machines virtuelles
        /// </summary>
        /// <returns>Liste des informations sur les machines virtuelles</returns>
        Task<List<VMInfo>> ListVirtualMachinesAsync();

        /// <summary>
        /// Attache une image ISO à une machine virtuelle
        /// </summary>
        /// <param name="vmId">ID de la machine virtuelle</param>
        /// <param name="isoPath">Chemin vers le fichier ISO</param>
        /// <returns>Vrai si l'opération a réussi, Faux sinon</returns>
        Task<bool> AttachISOAsync(string vmId, string isoPath);
    }
}