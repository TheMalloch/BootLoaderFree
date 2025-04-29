using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BOOTLOADERFREE.Converters
{
    /// <summary>
    /// Convertit une valeur booléenne en Visibility
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Convertit une valeur booléenne en valeur de visibilité
        /// </summary>
        /// <param name="value">Valeur booléenne à convertir</param>
        /// <param name="targetType">Type cible (non utilisé)</param>
        /// <param name="parameter">Paramètre de conversion (peut être "Invert" pour inverser la logique)</param>
        /// <param name="culture">Culture (non utilisée)</param>
        /// <returns>Visibility.Visible si la valeur est True (ou False si inversé), sinon Visibility.Collapsed</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverse = parameter != null && parameter.ToString().Equals("Invert", StringComparison.OrdinalIgnoreCase);
            bool boolValue = value is bool val && val;
            
            // Si inversion est demandée, inverser la valeur booléenne
            if (isInverse)
                boolValue = !boolValue;
            
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Convertit une valeur de visibilité en valeur booléenne (non implémenté)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Cette conversion inverse n'est généralement pas utilisée
            if (!(value is Visibility visibility))
                return false;
            
            bool isInverse = parameter != null && parameter.ToString().Equals("Invert", StringComparison.OrdinalIgnoreCase);
            bool result = visibility == Visibility.Visible;
            
            // Si inversion est demandée, inverser le résultat
            if (isInverse)
                result = !result;
            
            return result;
        }
    }
}