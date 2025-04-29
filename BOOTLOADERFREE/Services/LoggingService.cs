using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace BOOTLOADERFREE.Services
{
    /// <summary>
    /// Implémentation du service de journalisation
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly string _logFilePath;
        private readonly List<LogEntry> _logEntries = new List<LogEntry>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly bool _writeToFile;

        /// <summary>
        /// Constructeur du service de journalisation
        /// </summary>
        /// <param name="logFilePath">Chemin du fichier de journal (null pour désactiver l'écriture dans un fichier)</param>
        public LoggingService(string logFilePath = null)
        {
            _logFilePath = logFilePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DualBootDeployer",
                "Logs",
                $"Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
            
            _writeToFile = logFilePath != null;
            
            if (_writeToFile)
            {
                var logDirectory = Path.GetDirectoryName(_logFilePath);
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);
            }
            
            Log("Service de journalisation initialisé");
        }

        /// <summary>
        /// Enregistre un message dans le journal
        /// </summary>
        /// <param name="message">Message à enregistrer</param>
        public void Log(string message)
        {
            AddLogEntry(LogLevel.Info, message);
        }

        /// <summary>
        /// Enregistre un message d'erreur dans le journal
        /// </summary>
        /// <param name="message">Message d'erreur</param>
        /// <param name="exception">Exception associée</param>
        public void LogError(string message, Exception exception = null)
        {
            string fullMessage = message;
            if (exception != null)
            {
                fullMessage += $" Exception: {exception.Message}";
                fullMessage += $" StackTrace: {exception.StackTrace}";
            }
            
            AddLogEntry(LogLevel.Error, fullMessage);
        }

        /// <summary>
        /// Enregistre un message d'avertissement dans le journal
        /// </summary>
        /// <param name="message">Message d'avertissement</param>
        public void LogWarning(string message)
        {
            AddLogEntry(LogLevel.Warning, message);
        }

        /// <summary>
        /// Obtient le contenu complet du journal
        /// </summary>
        /// <returns>Contenu du journal sous forme de chaîne</returns>
        public string GetLogContent()
        {
            StringBuilder sb = new StringBuilder();
            
            _lock.EnterReadLock();
            try
            {
                foreach (var entry in _logEntries)
                {
                    sb.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Message}");
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Exporte le journal dans un fichier
        /// </summary>
        /// <param name="filePath">Chemin du fichier de sortie</param>
        /// <returns>Vrai si l'exportation a réussi, Faux sinon</returns>
        public bool ExportLogToFile(string filePath)
        {
            try
            {
                string content = GetLogContent();
                File.WriteAllText(filePath, content);
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Erreur lors de l'exportation du journal vers {filePath}", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Ajoute une entrée au journal
        /// </summary>
        /// <param name="level">Niveau de journalisation</param>
        /// <param name="message">Message à journaliser</param>
        private void AddLogEntry(LogLevel level, string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message
            };
            
            _lock.EnterWriteLock();
            try
            {
                _logEntries.Add(entry);
                
                if (_writeToFile)
                {
                    try
                    {
                        string line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Message}";
                        File.AppendAllText(_logFilePath, line + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        // Ne pas déclencher de récursion par LogError ici
                        Console.WriteLine($"Erreur lors de l'écriture dans le fichier journal: {ex.Message}");
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        
        /// <summary>
        /// Structure d'une entrée de journal
        /// </summary>
        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Message { get; set; }
        }
        
        /// <summary>
        /// Niveaux de journalisation
        /// </summary>
        private enum LogLevel
        {
            Info,
            Warning,
            Error
        }
    }
}