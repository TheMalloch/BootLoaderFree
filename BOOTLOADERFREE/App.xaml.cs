using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using BOOTLOADERFREE.Services;
using BOOTLOADERFREE.ViewModels;

namespace BOOTLOADERFREE
{
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;

        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Enregistrer les services
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IDiskService, DiskService>();
            services.AddSingleton<IBootloaderService, BootloaderService>();
            services.AddSingleton<IInstallationService, InstallationService>();
            services.AddSingleton<IVMService, VMService>();
            services.AddSingleton<IWSLService, WSLService>();

            // Enregistrer les ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<WelcomeViewModel>();
            services.AddTransient<SystemSelectionViewModel>();
            services.AddTransient<DiskConfigurationViewModel>();
            services.AddTransient<InstallationViewModel>();
            services.AddTransient<SummaryViewModel>();

            // Enregistrer les fenêtres principales
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = serviceProvider.GetService<MainWindow>();
            mainWindow.DataContext = serviceProvider.GetService<MainViewModel>();
            mainWindow.Show();
        }
    }
}