using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BOOTLOADERFREE.Models;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Interface pour le service de gestion des installations
    /// </summary>
    public interface IInstallationService
    {
        /// <summary>
        /// Lance une installation en fonction de la configuration fournie
        /// </summary>
        /// <param name="config">Configuration de l'installation</param>
        /// <returns>Suivi de la progression de l'installation</returns>
        Task<InstallationProgress> StartInstallationAsync(InstallationConfig config);
        
        /// <summary>
        /// Annule une installation en cours
        /// </summary>
        /// <returns>Vrai si l'annulation a réussi, Faux sinon</returns>
        Task<bool> CancelInstallationAsync();
        
        /// <summary>
        /// Vérifie si les prérequis pour l'installation sont satisfaits
        /// </summary>
        /// <param name="config">Configuration de l'installation à vérifier</param>
        /// <returns>Liste des problèmes détectés (vide si tout est OK)</returns>
        Task<List<string>> CheckPrerequisitesAsync(InstallationConfig config);
        
        /// <summary>
        /// Obtient l'état actuel de l'installation en cours
        /// </summary>
        InstallationProgress CurrentProgress { get; }
        
        /// <summary>
        /// Indique si une installation est actuellement en cours
        /// </summary>
        bool IsInstallationInProgress { get; }
        
        /// <summary>
        /// Événement déclenché lorsque la progression de l'installation change
        /// </summary>
        event EventHandler<InstallationProgressEventArgs> ProgressChanged;
        
        /// <summary>
        /// Événement déclenché lorsque l'installation est terminée
        /// </summary>
        event EventHandler<InstallationCompletedEventArgs> InstallationCompleted;
    }
    
    /// <summary>
    /// Arguments pour l'événement de progression de l'installation
    /// </summary>
    public class InstallationProgressEventArgs : EventArgs
    {
        /// <summary>
        /// État actuel de la progression
        /// </summary>
        public InstallationProgress Progress { get; set; }
    }
    
    /// <summary>
    /// Arguments pour l'événement de fin d'installation
    /// </summary>
    public class InstallationCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Indique si l'installation s'est terminée avec succès
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Message d'erreur en cas d'échec
        /// </summary>
        public string ErrorMessage { get; set; }
        
        /// <summary>
        /// État final de la progression
        /// </summary>
        public InstallationProgress FinalProgress { get; set; }
    }
}