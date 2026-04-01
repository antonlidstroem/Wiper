using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Wiper.Core.Models;

namespace Wiper.WPF.Converters;

/// <summary>
/// Konverterar PipelineState + ConverterParameter (stegnamn) till en SolidColorBrush.
/// Stegnamn: "Scanning", "LockCheck", "Cleaning", "Restarting"
/// </summary>
public class PipelineStepColorConverter : IValueConverter
{
    private static readonly Dictionary<PipelineState, string> StepMap = new()
    {
        { PipelineState.Scanning,   "Scanning"   },
        { PipelineState.LockCheck,  "LockCheck"  },
        { PipelineState.Cleaning,   "Cleaning"   },
        { PipelineState.Restarting, "Restarting" },
    };

    private static readonly Dictionary<PipelineState, int> StepOrder = new()
    {
        { PipelineState.Idle,       0 },
        { PipelineState.Scanning,   1 },
        { PipelineState.LockCheck,  2 },
        { PipelineState.Cleaning,   3 },
        { PipelineState.Restarting, 4 },
        { PipelineState.Done,       5 },
    };

    private static readonly Dictionary<string, int> ParameterOrder = new()
    {
        { "Scanning",   1 },
        { "LockCheck",  2 },
        { "Cleaning",   3 },
        { "Restarting", 4 },
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PipelineState state || parameter is not string stepName)
            return GetResource("PipelineIdle");

        if (state == PipelineState.Error)
            return GetResource("PipelineError");

        if (state == PipelineState.Cancelled)
            return GetResource("PipelineIdle");

        int currentOrder  = StepOrder.GetValueOrDefault(state, 0);
        int parameterStep = ParameterOrder.GetValueOrDefault(stepName, 0);

        // Aktivt steg
        if (StepMap.TryGetValue(state, out var activeStep) && activeStep == stepName)
            return GetResource("PipelineActive");

        // Klart steg (passerat)
        if (state == PipelineState.Done || currentOrder > parameterStep)
            return GetResource("PipelineDone");

        return GetResource("PipelineIdle");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;

    private static SolidColorBrush GetResource(string key)
    {
        if (Application.Current.Resources[key] is SolidColorBrush brush)
            return brush;
        return Brushes.Gray;
    }
}
