using System;
using BOOTLOADERFREE.Helpers;
using BOOTLOADERFREE.Services;

namespace BOOTLOADERFREE.ViewModels
{
    public class WelcomeViewModel : ViewModelBase
    {
        private readonly ILoggingService _loggingService;
        
        private string _welcomeMessage;
        private string _applicationDescription;
        private bool _isAdminMode;
        private string _systemInfo;

        public WelcomeViewModel(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            
            WelcomeMessage = "Bienvenue dans DualBootDeployer";
            ApplicationDescription = "Cet assistant vous guidera à travers le processus d'installation d'un système d'exploitation alternatif sur votre machine, sans nécessiter de périphérique externe. Vous pourrez choisir entre différentes méthodes d'installation : dual boot traditionnel, Windows Subsystem for Linux ou machine virtuelle.";
            IsAdminMode = AdminHelper.IsRunningAsAdmin();
            SystemInfo = GetSystemInfo();
            
            _loggingService.Log("WelcomeViewModel initialisé");
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public string ApplicationDescription
        {
            get => _applicationDescription;
            set => SetProperty(ref _applicationDescription, value);
        }

        public bool IsAdminMode
        {
            get => _isAdminMode;
            set => SetProperty(ref _isAdminMode, value);
        }

        public string SystemInfo
        {
            get => _systemInfo;
            set => SetProperty(ref _systemInfo, value);
        }

        private string GetSystemInfo()
        {
            string osVersion = Environment.OSVersion.ToString();
            string machineName = Environment.MachineName;
            string processorCount = Environment.ProcessorCount.ToString();
            string is64BitOs = Environment.Is64BitOperatingSystem ? "Oui" : "Non";

            return $"Système: {osVersion}\n" +
                   $"Machine: {machineName}\n" +
                   $"Processeurs: {processorCount}\n" +
                   $"64 bits: {is64BitOs}";
        }
    }
}