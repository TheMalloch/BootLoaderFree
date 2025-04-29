using System;

namespace BOOTLOADERFREE.Models
{
    /// <summary>
    /// Représente les informations sur une machine virtuelle
    /// </summary>
    public class VMInfo
    {
        /// <summary>
        /// Identifiant unique de la machine virtuelle
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Nom de la machine virtuelle
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// État actuel de la machine virtuelle
        /// </summary>
        public VMState State { get; set; }
        
        /// <summary>
        /// Mémoire RAM allouée en Mo
        /// </summary>
        public int MemoryMB { get; set; }
        
        /// <summary>
        /// Nombre de processeurs virtuels
        /// </summary>
        public int ProcessorCount { get; set; }
        
        /// <summary>
        /// Génération de la machine virtuelle
        /// </summary>
        public int Generation { get; set; }
        
        /// <summary>
        /// Chemin vers les fichiers de la machine virtuelle
        /// </summary>
        public string Path { get; set; }
        
        /// <summary>
        /// Type d'hyperviseur utilisé
        /// </summary>
        public string HypervisorType { get; set; }
        
        /// <summary>
        /// Notes sur la machine virtuelle
        /// </summary>
        public string Notes { get; set; }
        
        /// <summary>
        /// Temps d'activité en minutes
        /// </summary>
        public long UptimeMinutes { get; set; }
        
        /// <summary>
        /// Représentation lisible du temps d'activité
        /// </summary>
        public string FormattedUptime
        {
            get
            {
                if (UptimeMinutes < 60)
                    return $"{UptimeMinutes} min";
                
                long hours = UptimeMinutes / 60;
                long minutes = UptimeMinutes % 60;
                
                if (hours < 24)
                    return $"{hours}h {minutes}min";
                
                long days = hours / 24;
                hours = hours % 24;
                
                return $"{days}j {hours}h {minutes}min";
            }
        }
    }
    
    /// <summary>
    /// Énumération des états possibles d'une machine virtuelle
    /// </summary>
    public enum VMState
    {
        /// <summary>
        /// Machine arrêtée
        /// </summary>
        Off,
        
        /// <summary>
        /// Machine en cours d'exécution
        /// </summary>
        Running,
        
        /// <summary>
        /// Machine en pause
        /// </summary>
        Paused,
        
        /// <summary>
        /// Machine en état de sauvegarde
        /// </summary>
        Saved,
        
        /// <summary>
        /// Machine en cours de démarrage
        /// </summary>
        Starting,
        
        /// <summary>
        /// Machine en cours d'arrêt
        /// </summary>
        Stopping,
        
        /// <summary>
        /// Machine en état d'erreur
        /// </summary>
        Failed,
        
        /// <summary>
        /// État inconnu
        /// </summary>
        Unknown
    }
}