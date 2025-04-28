namespace HSLocationManager;

using System;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

public class HSLocationTracking : IHSLocationManagerDelegate, IDisposable
{
    // Constants
    public const int TimeInterval = 30;
    public const double Accuracy = 200;
    
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
        Debug.WriteLine("Please enable location service");
        // Implement platform-specific location enablement prompt if needed
    }

    public async void StartLocationTracking()
    {
        var status = await CheckLocationPermission();
        
        if (status == PermissionStatus.Granted)
        {
            _manager.StartUpdatingLocation(TimeSpan.FromSeconds(TimeInterval), Accuracy);
        }
        else if (status == PermissionStatus.Denied)
        {
            Debug.WriteLine("Location service is disabled");
        }
        else
        {
            await _manager.RequestAlwaysAuthorizationAsync();
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
    
    public void ScheduledLocationManagerDidUpdateLocations(HSLocationManager manager, IEnumerable<Location> locations)
    {
        var recentLocation = locations.LastOrDefault();
        Debug.WriteLine($"Location retrieved successfully: {recentLocation?.ToString() ?? "No location"}");
        
        // Add your location handling logic here
    }

    public void ScheduledLocationManagerDidFailWithError(HSLocationManager manager, Exception error)
    {
        Debug.WriteLine($"Location Error: {error.Message}");
    }

    public void ScheduledLocationManagerDidChangeAuthorization(HSLocationManager manager, PermissionStatus status)
    {
        if (status == PermissionStatus.Denied)
        {
            Debug.WriteLine("Location service is disabled...");
        }
        else
        {
            StartLocationTracking();
        }
    }
    #endregion

    public void Dispose()
    {
        _manager?.Dispose();
        GC.SuppressFinalize(this);
    }
}