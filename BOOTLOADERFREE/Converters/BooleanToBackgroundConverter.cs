using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BOOTLOADERFREE.Converters
{
    /// <summary>
    /// Convertit une valeur booléenne en Brush pour l'arrière-plan
    /// </summary>
    public class BooleanToBackgroundConverter : IValueConverter
    {
        /// <summary>
        /// Couleur pour les valeurs True (succès)
        /// </summary>
        public Brush SuccessBrush { get; set; } = new SolidColorBrush(Color.FromRgb(230, 255, 230));

        /// <summary>
        /// Couleur pour les valeurs False (échec)
        /// </summary>
        public Brush ErrorBrush { get; set; } = new SolidColorBrush(Color.FromRgb(255, 230, 230));

        /// <summary>
        /// Couleur par défaut (neutre)
        /// </summary>
        public Brush DefaultBrush { get; set; } = new SolidColorBrush(Color.FromRgb(240, 240, 240));

        /// <summary>
        /// Convertit une valeur booléenne en Brush pour l'arrière-plan
        /// </summary>
        /// <param name="value">Valeur booléenne à convertir</param>
        /// <param name="targetType">Type cible (non utilisé)</param>
        /// <param name="parameter">Paramètre de conversion (peut être "Invert" pour inverser la logique)</param>
        /// <param name="culture">Culture (non utilisée)</param>
        /// <returns>Brush de succès si la valeur est True (ou False si inversé), sinon Brush d'erreur</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DefaultBrush;

            bool isInverse = parameter != null && parameter.ToString().Equals("Invert", StringComparison.OrdinalIgnoreCase);
            bool boolValue = value is bool val && val;
            
            // Si inversion est demandée, inverser la valeur booléenne
            if (isInverse)
                boolValue = !boolValue;
            
            return boolValue ? SuccessBrush : ErrorBrush;
        }

        /// <summary>
        /// Convertit une couleur d'arrière-plan en valeur booléenne (non implémenté)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Cette conversion inverse n'est pas implémentée
            return Binding.DoNothing;
        }
    }
}