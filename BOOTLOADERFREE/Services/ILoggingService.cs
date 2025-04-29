using System;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Interface pour le service de journalisation
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Enregistre un message dans le journal
        /// </summary>
        /// <param name="message">Message à enregistrer</param>
        void Log(string message);

        /// <summary>
        /// Enregistre un message d'erreur dans le journal
        /// </summary>
        /// <param name="message">Message d'erreur</param>
        /// <param name="exception">Exception associée</param>
        void LogError(string message, Exception exception = null);

        /// <summary>
        /// Enregistre un message d'avertissement dans le journal
        /// </summary>
        /// <param name="message">Message d'avertissement</param>
        void LogWarning(string message);

        /// <summary>
        /// Obtient le contenu complet du journal
        /// </summary>
        /// <returns>Contenu du journal sous forme de chaîne</returns>
        string GetLogContent();

        /// <summary>
        /// Exporte le journal dans un fichier
        /// </summary>
        /// <param name="filePath">Chemin du fichier de sortie</param>
        /// <returns>Vrai si l'exportation a réussi, Faux sinon</returns>
        bool ExportLogToFile(string filePath);
    }
}