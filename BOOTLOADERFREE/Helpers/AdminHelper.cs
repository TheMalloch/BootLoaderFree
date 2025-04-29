using System;
using System.Diagnostics;
using System.Security.Principal;

namespace BOOTLOADERFREE.Helpers
{
    /// <summary>
    /// Classe utilitaire pour gérer les privilèges administratifs
    /// </summary>
    public static class AdminHelper
    {
        /// <summary>
        /// Vérifie si l'application est exécutée avec des privilèges administratifs
        /// </summary>
        /// <returns>Vrai si l'application est exécutée en tant qu'administrateur, Faux sinon</returns>
        public static bool IsRunningAsAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Redémarre l'application avec des privilèges administratifs
        /// </summary>
        /// <param name="args">Arguments à passer à la nouvelle instance</param>
        /// <returns>Vrai si le redémarrage a été lancé, Faux sinon</returns>
        public static bool RestartAsAdmin(string[] args = null)
        {
            try
            {
                // Obtenir le chemin de l'exécutable actuel
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                
                // Préparer les arguments
                string arguments = args != null ? string.Join(" ", args) : string.Empty;
                
                // Créer un nouveau processus avec privilèges élevés
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas" // Exécuter en tant qu'administrateur
                };
                
                // Démarrer le nouveau processus
                Process.Start(startInfo);
                
                // Terminer le processus actuel
                Environment.Exit(0);
                
                return true;
            }
            catch (Exception)
            {
                return false; // L'utilisateur a annulé l'élévation ou une erreur s'est produite
            }
        }
        
        /// <summary>
        /// Vérifie si des privilèges administratifs sont nécessaires pour une opération
        /// </summary>
        /// <param name="operationType">Type d'opération à vérifier</param>
        /// <returns>Vrai si des privilèges administratifs sont nécessaires</returns>
        public static bool RequiresAdmin(AdminOperationType operationType)
        {
            // Déterminer si l'opération nécessite des privilèges administratifs
            switch (operationType)
            {
                case AdminOperationType.CreatePartition:
                case AdminOperationType.FormatDisk:
                case AdminOperationType.ModifyBootloader:
                case AdminOperationType.InstallSystem:
                case AdminOperationType.InstallWSL:
                    return true;
                
                case AdminOperationType.CreateVirtualMachine:
                case AdminOperationType.ReadSystemInfo:
                case AdminOperationType.BrowseFiles:
                default:
                    return false;
            }
        }
    }
    
    /// <summary>
    /// Types d'opérations qui peuvent nécessiter des privilèges administratifs
    /// </summary>
    public enum AdminOperationType
    {
        CreatePartition,
        FormatDisk,
        ModifyBootloader,
        InstallSystem,
        InstallWSL,
        CreateVirtualMachine,
        ReadSystemInfo,
        BrowseFiles
    }
}