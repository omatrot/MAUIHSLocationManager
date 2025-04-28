using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

#pragma warning disable IDE0130 // Le namespace ne correspond pas à la structure de dossiers
namespace HSLocationManager;
#pragma warning restore IDE0130 // Le namespace ne correspond pas à la structure de dossiers

public class MainPage : ContentPage
{
    private readonly Button _trackingButton;
    private bool _isTracking;

    public MainPage()
    {
        // iOS-style page configuration
        On<iOS>().SetUseSafeArea(true);
        BackgroundColor = Colors.White;

        // Create UI
        var layout = new VerticalStackLayout
        {
            Spacing = 20,
            Padding = new Thickness(20),
            VerticalOptions = LayoutOptions.Center
        };

        _trackingButton = new Button
        {
            Text = "Start Tracking",
            BackgroundColor = Colors.Blue,
            TextColor = Colors.White,
            CornerRadius = 8,
            WidthRequest = 200
        };
        _trackingButton.Clicked += OnTrackingToggled;

        var exportButton = new Button
        {
            Text = "Export Logs",
            BackgroundColor = Colors.Gray,
            TextColor = Colors.White,
            CornerRadius = 8,
            WidthRequest = 200
        };
        exportButton.Clicked += OnExportLogs;

        layout.Children.Add(_trackingButton);
        layout.Children.Add(exportButton);

        Content = layout;
    }

    private void OnExportLogs(object? sender, EventArgs e)
    {
        HSLogger.Logger.ExportLogFile();
    }

    private void OnTrackingToggled(object? sender, EventArgs e)
    {
        _isTracking = !_isTracking;

        if (_isTracking)
        {
            HSLocationTracking.Instance.StartLocationTracking();
            _trackingButton.Text = "Stop Tracking";
            _trackingButton.BackgroundColor = Colors.Red;
        }
        else
        {
            HSLocationTracking.Instance.StopLocationTracking();
            _trackingButton.Text = "Start Tracking";
            _trackingButton.BackgroundColor = Colors.Blue;
        }
    }
}