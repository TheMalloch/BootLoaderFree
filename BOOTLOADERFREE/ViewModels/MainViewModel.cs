using System;
using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using BOOTLOADERFREE.Services;

namespace BOOTLOADERFREE.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggingService _loggingService;
        private ViewModelBase _currentViewModel;
        private readonly List<Type> _navigationHistory = new List<Type>();
        private int _currentViewIndex = -1;
        private string _statusMessage;
        private string _currentPageTitle = "Bienvenue";
        private string _nextButtonText = "Suivant";

        public MainViewModel(IServiceProvider serviceProvider, ILoggingService loggingService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            
            NavigateBackCommand = new RelayCommand(NavigateBack, _ => CanNavigateBack);
            NavigateForwardCommand = new RelayCommand(NavigateForward, _ => CanNavigateForward);
            
            // Naviguer vers la première vue
            NavigateTo<WelcomeViewModel>();
        }

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            private set => SetProperty(ref _currentViewModel, value);
        }

        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set => SetProperty(ref _currentPageTitle, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string NextButtonText
        {
            get => _nextButtonText;
            set => SetProperty(ref _nextButtonText, value);
        }

        public bool CanNavigateBack => _currentViewIndex > 0;
        public bool CanNavigateForward => CurrentViewModel != null;

        public ICommand NavigateBackCommand { get; }
        public ICommand NavigateForwardCommand { get; }

        private void NavigateBack(object parameter)
        {
            if (_currentViewIndex > 0)
            {
                _currentViewIndex--;
                Type viewModelType = _navigationHistory[_currentViewIndex];
                SetCurrentViewModel(viewModelType);
                UpdateNavigationState();
            }
        }

        private void NavigateForward(object parameter)
        {
            // Logique de navigation selon la vue actuelle
            if (CurrentViewModel is WelcomeViewModel)
            {
                NavigateTo<SystemSelectionViewModel>();
                CurrentPageTitle = "Sélection du Système";
            }
            else if (CurrentViewModel is SystemSelectionViewModel)
            {
                NavigateTo<DiskConfigurationViewModel>();
                CurrentPageTitle = "Configuration du Disque";
            }
            else if (CurrentViewModel is DiskConfigurationViewModel)
            {
                NavigateTo<InstallationViewModel>();
                CurrentPageTitle = "Installation";
                NextButtonText = "Installer";
            }
            else if (CurrentViewModel is InstallationViewModel)
            {
                NavigateTo<SummaryViewModel>();
                CurrentPageTitle = "Résumé";
                NextButtonText = "Terminer";
            }
            else if (CurrentViewModel is SummaryViewModel)
            {
                // Fermer l'application ou redémarrer le processus
                StatusMessage = "Installation terminée";
            }
        }

        public void NavigateTo<T>() where T : ViewModelBase
        {
            var viewModelType = typeof(T);
            SetCurrentViewModel(viewModelType);
            
            // Si nous naviguons à un nouvel index, effacer l'historique en avant
            if (_currentViewIndex < _navigationHistory.Count - 1)
            {
                _navigationHistory.RemoveRange(_currentViewIndex + 1, _navigationHistory.Count - _currentViewIndex - 1);
            }
            
            _navigationHistory.Add(viewModelType);
            _currentViewIndex = _navigationHistory.Count - 1;
            
            UpdateNavigationState();
        }

        private void SetCurrentViewModel(Type viewModelType)
        {
            if (_serviceProvider.GetService(viewModelType) is ViewModelBase viewModel)
            {
                CurrentViewModel = viewModel;
                _loggingService.Log($"Navigation vers {viewModelType.Name}");
            }
        }

        private void UpdateNavigationState()
        {
            RaisePropertyChanged(nameof(CanNavigateBack));
            RaisePropertyChanged(nameof(CanNavigateForward));
        }

        public void SetStatusMessage(string message)
        {
            StatusMessage = message;
            _loggingService.Log(message);
        }
    }
}