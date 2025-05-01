namespace HSLocationManager;

using System;
using CoreLocation;
using Foundation;
using Microsoft.Maui.ApplicationModel;

public class HSLocationTracking : IHSLocationManagerDelegate, IDisposable
{
    // Constants
    private const int TimeInterval = 30;
    private const double Accuracy = 200;

    private static HSLocationTracking? _instance;
    private readonly HSLocationManager _manager;
    private bool _statusCheckedOnce;

    private HSLocationTracking()
    {
        _manager = new HSLocationManager(this);
    }

    public static HSLocationTracking Instance => _instance ??= new HSLocationTracking();

    public static void Destroy()
    {
        _instance?.Dispose();
        _instance = null;
    }

    public bool IsLocationServiceEnabled()
    {
        return CheckLocationPermission().Result == PermissionStatus.Granted;
    }

    public void ShowLocationAlert()
    {
        HSLogger.Logger.Write("Please enable location service");
        // Implement platform-specific location enablement prompt if needed
    }

    public async void StartLocationTracking()
    {
        var status = await CheckLocationPermission();

        if (status == PermissionStatus.Granted)
        {
            _manager.StartUpdatingLocation(TimeSpan.FromSeconds(TimeInterval).TotalSeconds, Accuracy);
        }
        else if (status == PermissionStatus.Denied)
        {
            HSLogger.Logger.Write("Location service is disabled");
        }
        else
        {
            _manager.RequestAlwaysAuthorization();
        }
    }

    public void StopLocationTracking()
    {
        _manager.StopUpdatingLocation();
    }

    private async Task<PermissionStatus> CheckLocationPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted)
            return PermissionStatus.Granted;

        return await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
    }

    #region IHSLocationManagerDelegate Implementation

    public void ScheduledLocationManager(HSLocationManager manager, NSError error)
    {
        if (error != null)
        {
            HSLogger.Logger.Write($"Location Error: {error.LocalizedDescription}");
            return;
        }

        if (!_statusCheckedOnce)
        {
            _statusCheckedOnce = true;
            var status = Task.Run(() => CheckLocationPermission()).GetAwaiter().GetResult();
            if (status == PermissionStatus.Denied)
            {
                ShowLocationAlert();
            }
        }
    }

    public void ScheduledLocationManager(HSLocationManager manager, CLLocation[] locations)
    {
        if (locations.Length == 0) return;

        var recentLocation = locations.LastOrDefault();
        HSLogger.Logger.Write($"Location retrieved successfully: {recentLocation?.ToString() ?? "No location"}");

        // Add your location handling logic here
    }

    public void ScheduledLocationManager(HSLocationManager manager, CLAuthorizationStatus status)
    {
        if (status == CLAuthorizationStatus.Denied)
        {
            HSLogger.Logger.Write("Location service is disabled...");
        }
        else if (status == CLAuthorizationStatus.AuthorizedAlways || status == CLAuthorizationStatus.AuthorizedWhenInUse)
        {
            StartLocationTracking();
        }
        else if (status == CLAuthorizationStatus.NotDetermined)
        {
            HSLogger.Logger.Write("Location permission not determined yet.");
        }
        else if (status == CLAuthorizationStatus.Restricted)
        {
            HSLogger.Logger.Write("Location permission is restricted.");
        }
        else if (status == CLAuthorizationStatus.Authorized)
        {
            HSLogger.Logger.Write("Location permission granted.");
            StartLocationTracking();
        }
        else
        {
            HSLogger.Logger.Write("Unknown location permission status.");
        }
    }

    #endregion

    public void Dispose()
    {
        _manager?.Dispose();
        GC.SuppressFinalize(this);
    }
}