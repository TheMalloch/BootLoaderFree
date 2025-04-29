using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BOOTLOADERFREE.Models
{
    /// <summary>
    /// Représente la progression d'une installation en cours
    /// </summary>
    public class InstallationProgress
    {
        /// <summary>
        /// Étape actuelle de l'installation
        /// </summary>
        public int CurrentStep { get; set; }
        
        /// <summary>
        /// Nombre total d'étapes pour cette installation
        /// </summary>
        public int TotalSteps { get; set; }
        
        /// <summary>
        /// Pourcentage de progression (0-100)
        /// </summary>
        public int PercentComplete { get; set; }
        
        /// <summary>
        /// Description de l'opération en cours
        /// </summary>
        public string CurrentOperation { get; set; }
        
        /// <summary>
        /// Message d'état détaillé
        /// </summary>
        public string DetailedStatus { get; set; }
        
        /// <summary>
        /// Indique si l'installation est terminée
        /// </summary>
        public bool IsCompleted { get; set; }
        
        /// <summary>
        /// Indique si l'installation a réussi (utilisé uniquement quand IsCompleted est vrai)
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// Message d'erreur en cas d'échec
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Représente une étape individuelle dans le processus d'installation
    /// </summary>
    public class InstallationStep : INotifyPropertyChanged
    {
        private int _stepNumber;
        private string _description;
        private StepStatus _status;
        private int _progressPercentage;
        private string _detailedStatus;
        private DateTime _startTime;
        private DateTime _endTime;

        /// <summary>
        /// Numéro de l'étape
        /// </summary>
        public int StepNumber
        {
            get => _stepNumber;
            set => SetProperty(ref _stepNumber, value);
        }

        /// <summary>
        /// Description de l'étape
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// Statut de l'étape
        /// </summary>
        public StepStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// Pourcentage de progression de l'étape
        /// </summary>
        public int ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        /// <summary>
        /// Détails sur le statut actuel de l'étape
        /// </summary>
        public string DetailedStatus
        {
            get => _detailedStatus;
            set => SetProperty(ref _detailedStatus, value);
        }

        /// <summary>
        /// Heure de début de l'étape
        /// </summary>
        public DateTime StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        /// <summary>
        /// Heure de fin de l'étape
        /// </summary>
        public DateTime EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        /// <summary>
        /// Durée de l'étape
        /// </summary>
        public TimeSpan Duration => Status == StepStatus.Completed ? EndTime - StartTime : DateTime.Now - StartTime;

        /// <summary>
        /// Marque l'étape comme étant en cours
        /// </summary>
        /// <param name="detailedStatus">Détails optionnels sur le statut</param>
        public void Start(string detailedStatus = null)
        {
            Status = StepStatus.InProgress;
            DetailedStatus = detailedStatus;
            StartTime = DateTime.Now;
        }

        /// <summary>
        /// Marque l'étape comme terminée
        /// </summary>
        /// <param name="detailedStatus">Détails optionnels sur le statut final</param>
        public void Complete(string detailedStatus = null)
        {
            Status = StepStatus.Completed;
            ProgressPercentage = 100;
            DetailedStatus = detailedStatus ?? "Terminé";
            EndTime = DateTime.Now;
        }

        /// <summary>
        /// Marque l'étape comme ayant échoué
        /// </summary>
        /// <param name="errorMessage">Message d'erreur</param>
        public void Fail(string errorMessage)
        {
            Status = StepStatus.Failed;
            DetailedStatus = errorMessage;
            EndTime = DateTime.Now;
        }

        /// <summary>
        /// Met à jour la progression de l'étape
        /// </summary>
        /// <param name="percentage">Pourcentage de progression</param>
        /// <param name="detailedStatus">Détails optionnels sur le statut</param>
        public void UpdateProgress(int percentage, string detailedStatus = null)
        {
            ProgressPercentage = percentage;
            if (detailedStatus != null)
                DetailedStatus = detailedStatus;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// Statuts possibles pour une étape d'installation
    /// </summary>
    public enum StepStatus
    {
        /// <summary>
        /// En attente de démarrage
        /// </summary>
        Pending,
        
        /// <summary>
        /// En cours d'exécution
        /// </summary>
        InProgress,
        
        /// <summary>
        /// Terminée avec succès
        /// </summary>
        Completed,
        
        /// <summary>
        /// Terminée avec une erreur
        /// </summary>
        Failed,
        
        /// <summary>
        /// Ignorée
        /// </summary>
        Skipped
    }
}