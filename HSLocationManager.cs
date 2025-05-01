using CoreLocation;
using Foundation;
using UIKit;

namespace HSLocationManager
{
    public interface IHSLocationManagerDelegate
    {
        void ScheduledLocationManager(HSLocationManager manager, NSError error);
        void ScheduledLocationManager(HSLocationManager manager, CLLocation[] locations);
        void ScheduledLocationManager(HSLocationManager manager, CLAuthorizationStatus status);
    }

    public class HSLocationManager : CLLocationManagerDelegate, IDisposable
    {
        #region Constants
        private const double MaxBGTime = 170;
        private const double MinBGTime = 2;
        private const double MinAcceptableLocationAccuracy = 5;
        private const double WaitForLocationsTime = 3;
        #endregion

        #region Fields
        private readonly IHSLocationManagerDelegate _delegate;
        private readonly CLLocationManager _manager;
        private NSTimer? _checkLocationTimer;
        private NSTimer? _waitTimer;
        private nint _bgTask = UIApplication.BackgroundTaskInvalid;
        private List<CLLocation> _lastLocations = [];
        private bool _isManagerRunning;
        #endregion

        #region Properties
        public double AcceptableLocationAccuracy { get; private set; } = 100;
        public double CheckLocationInterval { get; private set; } = 10;
        public bool IsRunning { get; private set; }
        #endregion

        public HSLocationManager(IHSLocationManagerDelegate delegateImpl)
        {
            _delegate = delegateImpl;
            _manager = new CLLocationManager
            {
                Delegate = this,
                AllowsBackgroundLocationUpdates = true,
                PausesLocationUpdatesAutomatically = false
            };
        }

        public void RequestAlwaysAuthorization() => _manager.RequestAlwaysAuthorization();

        public void StartUpdatingLocation(double interval, double acceptableLocationAccuracy = 100)
        {
            if (IsRunning) StopUpdatingLocation();

            CheckLocationInterval = Math.Max(MinBGTime, Math.Min(MaxBGTime, interval)) - WaitForLocationsTime;
            AcceptableLocationAccuracy = Math.Max(MinAcceptableLocationAccuracy, acceptableLocationAccuracy);

            IsRunning = true;
            AddNotifications();
            StartLocationManager();
        }

        public void StopUpdatingLocation()
        {
            IsRunning = false;
            StopWaitTimer();
            StopLocationManager();
            StopBackgroundTask();
            StopCheckLocationTimer();
            RemoveNotifications();
        }

        #region Location Manager
        private void StartLocationManager()
        {
            _isManagerRunning = true;

            // Use the raw double value
            _manager.DesiredAccuracy = -2.0; // BestForNavigation
            _manager.DistanceFilter = 5; // Meters
            _manager.StartUpdatingLocation();

            HSLogger.Logger.Write($"Location manager started with accuracy: {AcceptableLocationAccuracy} meters");
        }

        private void PauseLocationManager()
        {
            _manager.DesiredAccuracy = 3000.0; // Three kilometers
            _manager.DistanceFilter = 99999;

            HSLogger.Logger.Write("Location manager paused");
        }

        private void StopLocationManager()
        {
            _isManagerRunning = false;
            _manager.StopUpdatingLocation();

            HSLogger.Logger.Write("Location manager stopped");
        }
        #endregion

        #region Event Handlers
        public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
        {
            _delegate.ScheduledLocationManager(this, status);
        }

        public override void Failed(CLLocationManager manager, NSError error)
        {
            _delegate.ScheduledLocationManager(this, error);
        }

        public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        {
            if (!_isManagerRunning || locations.Length == 0) return;

            _lastLocations = [.. locations];
            if (_waitTimer == null) StartWaitTimer();
        }
        #endregion

        #region Timer Methods
        private void StartCheckLocationTimer()
        {
            StopCheckLocationTimer();
            _checkLocationTimer = NSTimer.CreateScheduledTimer(CheckLocationInterval, false, _ => CheckLocationTimerEvent());
        }

        private void StopCheckLocationTimer()
        {
            _checkLocationTimer?.Invalidate();
            _checkLocationTimer?.Dispose();
            _checkLocationTimer = null;
        }

        private void StartWaitTimer()
        {
            StopWaitTimer();
            _waitTimer = NSTimer.CreateScheduledTimer(WaitForLocationsTime, false, _ => WaitTimerEvent());
        }

        private void StopWaitTimer()
        {
            _waitTimer?.Invalidate();
            _waitTimer?.Dispose();
            _waitTimer = null;
        }

        private void CheckLocationTimerEvent()
        {
            StopCheckLocationTimer();
            StartLocationManager();
            NSTimer.CreateScheduledTimer(1, false, _ => StopAndResetBgTaskIfNeeded());
        }

        private void WaitTimerEvent()
        {
            StopWaitTimer();

            if (AcceptableLocationAccuracyRetrieved())
            {
                StartBackgroundTask();
                StartCheckLocationTimer();
                PauseLocationManager();
                _delegate.ScheduledLocationManager(this, [.. _lastLocations]);
            }
            else
            {
                StartWaitTimer();
            }
        }

        private bool AcceptableLocationAccuracyRetrieved()
        {
            bool acceptable = _lastLocations.LastOrDefault()?.HorizontalAccuracy <= AcceptableLocationAccuracy;
            return acceptable;
        }
        #endregion

        #region Background Task
        private void StartBackgroundTask()
        {
            var state = UIApplication.SharedApplication.ApplicationState;
            if ((state == UIApplicationState.Background || state == UIApplicationState.Inactive) &&
                _bgTask == UIApplication.BackgroundTaskInvalid)
            {
                _bgTask = UIApplication.SharedApplication.BeginBackgroundTask(() => CheckLocationTimerEvent());
            }
        }

        private void StopBackgroundTask()
        {
            if (_bgTask == UIApplication.BackgroundTaskInvalid) return;
            UIApplication.SharedApplication.EndBackgroundTask(_bgTask);
            _bgTask = UIApplication.BackgroundTaskInvalid;
        }

        private void StopAndResetBgTaskIfNeeded()
        {
            if (_isManagerRunning)
            {
                StopBackgroundTask();
            }
            else
            {
                StopBackgroundTask();
                StartBackgroundTask();
            }
        }
        #endregion

        #region Application State
        private void AddNotifications()
        {
            RemoveNotifications();
            NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.DidEnterBackgroundNotification,
                notification => ApplicationDidEnterBackground());
            NSNotificationCenter.DefaultCenter.AddObserver(
                UIApplication.DidBecomeActiveNotification,
                notification => ApplicationDidBecomeActive());
        }

        private void RemoveNotifications()
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(this);
        }

        [Export("applicationDidEnterBackground")]
        private void ApplicationDidEnterBackground()
        {
            HSLogger.Logger.Write("Application entered background state");
            StopBackgroundTask();
            StartBackgroundTask();
        }

        [Export("applicationDidBecomeActive")]
        private void ApplicationDidBecomeActive()
        {
            HSLogger.Logger.Write("Application became active");

            StopBackgroundTask();
        }
        #endregion

        public new void Dispose()
        {
            StopUpdatingLocation();
            _manager?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}