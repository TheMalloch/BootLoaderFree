using System.Windows;
using BOOTLOADERFREE.ViewModels;

namespace BOOTLOADERFREE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // DataContext is set in App.xaml.cs through dependency injection
        }
    }
}