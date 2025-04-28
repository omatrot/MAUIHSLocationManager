namespace HSLocationManager;
public interface IHSLocationManagerDelegate
{
    void ScheduledLocationManagerDidFailWithError(HSLocationManager manager, Exception error);
    void ScheduledLocationManagerDidUpdateLocations(HSLocationManager manager, IEnumerable<Location> locations);
    void ScheduledLocationManagerDidChangeAuthorization(HSLocationManager manager, PermissionStatus status);
}

public class HSLocationManager : IDisposable
{
    private readonly TimeSpan MaxBGTime = TimeSpan.FromSeconds(170);
    private readonly TimeSpan MinBGTime = TimeSpan.FromSeconds(2);
    private const double MinAcceptableLocationAccuracy = 5;
    private readonly TimeSpan WaitForLocationsTime = TimeSpan.FromSeconds(3);

    private readonly IHSLocationManagerDelegate _delegate;
    private bool _isManagerRunning;
    private Timer? _checkLocationTimer;
    private Timer? _waitTimer;
    private readonly List<Location> _lastLocations = [];
    private GeolocationRequest? _currentRequest;

    public double AcceptableLocationAccuracy { get; private set; } = 100;
    public TimeSpan CheckLocationInterval { get; private set; } = TimeSpan.FromSeconds(10);
    public bool IsRunning { get; private set; }

    public HSLocationManager(IHSLocationManagerDelegate delegateImpl)
    {
        _delegate = delegateImpl;
        ConfigureLifecycleHandlers();
    }

    private void ConfigureLifecycleHandlers()
    {
        // Platform-specific lifecycle handling needed here
        // Subscribe to app background/foreground events using platform-specific code
    }

    public async Task RequestAlwaysAuthorizationAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationAlways>();
        }
        _delegate.ScheduledLocationManagerDidChangeAuthorization(this, status);
    }

    public void StartUpdatingLocation(TimeSpan interval, double acceptableLocationAccuracy = 100)
    {
        if (IsRunning) StopUpdatingLocation();

        CheckLocationInterval = interval switch
        {
            _ when interval > MaxBGTime => MaxBGTime,
            _ when interval < MinBGTime => MinBGTime,
            _ => interval
        } - WaitForLocationsTime;

        AcceptableLocationAccuracy = Math.Max(acceptableLocationAccuracy, MinAcceptableLocationAccuracy);
        IsRunning = true;

        StartLocationManager();
    }

    private CancellationTokenSource? _listenTokenSource;

    private async void StartLocationManager()
    {
        if (_isManagerRunning) return;

        _isManagerRunning = true;

        try
        {
            var request = new GeolocationListeningRequest(
                GeolocationAccuracy.Best,
                TimeSpan.FromSeconds(1));

            _listenTokenSource?.Cancel();
            _listenTokenSource = new CancellationTokenSource();

            Geolocation.LocationChanged += HandleLocationChanged;

            // This is the correct MAUI listening method
            var success = await Geolocation.StartListeningForegroundAsync(request);

            if (!success)
            {
                _delegate.ScheduledLocationManagerDidFailWithError(
                    this, new Exception("Failed to start location listening"));
            }
        }
        catch (Exception ex)
        {
            _delegate.ScheduledLocationManagerDidFailWithError(this, ex);
        }
    }

    private void PauseLocationManager()
    {
        _isManagerRunning = false;
        _listenTokenSource?.Cancel();
        Geolocation.LocationChanged -= HandleLocationChanged;
    }
    private void HandleLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        if (!_isManagerRunning) return;

        _lastLocations.Add(e.Location);
        if (_waitTimer == null) StartWaitTimer();
    }

    private void StartWaitTimer()
    {
        _waitTimer?.Dispose();
        _waitTimer = new Timer(_ => WaitTimerElapsed(), null, WaitForLocationsTime, Timeout.InfiniteTimeSpan);
    }

    private void WaitTimerElapsed()
    {
        var acceptableLocationFound = _lastLocations.Exists(l => l.Accuracy.HasValue &&
                                                               l.Accuracy <= AcceptableLocationAccuracy);

        if (acceptableLocationFound)
        {
            NotifyDelegateAndPause();
        }
        else
        {
            StartWaitTimer();
        }
    }

    private void NotifyDelegateAndPause()
    {
        _delegate.ScheduledLocationManagerDidUpdateLocations(this, _lastLocations);
        _lastLocations.Clear();
        PauseLocationManager();
        StartCheckLocationTimer();
    }

    private void StartCheckLocationTimer()
    {
        _checkLocationTimer?.Dispose();
        _checkLocationTimer = new Timer(_ => CheckLocationTimerElapsed(), null, CheckLocationInterval, Timeout.InfiniteTimeSpan);
    }

    private void CheckLocationTimerElapsed()
    {
        StartLocationManager();
    }

    public void StopUpdatingLocation()
    {
        IsRunning = false;
        _waitTimer?.Dispose();
        _checkLocationTimer?.Dispose();
        PauseLocationManager();
    }

    public void Dispose()
    {
        StopUpdatingLocation();
        _waitTimer?.Dispose();
        _checkLocationTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    // Platform-specific background task methods would go here
    private void StartBackgroundTask()
    {
        // Implement using platform-specific code
    }

    private void StopBackgroundTask()
    {
        // Implement using platform-specific code
    }
}