# HSLocationManager for MAUI in c#

This is a port of the [swift version](https://github.com/IMHitesh/HSLocationManager) of HSLocationManager.  

Location manager that allows to get background location updates every n seconds with desired location accuracy.

**Advantage:**

 - OS will never kill our app if the location manager is currently running.

 - Give periodically location update when it required(range is between 2 - 170 seconds (limited by max allowed background task time))

 - Customizable location accuracy and time period.

 - Low memory consumption(Singleton class)


Default time to retrive location is 30 sec and accuracy is 200. 

    private const int TimeInterval = 30;
    private const double Accuracy = 200;

# Usage
Configure MAUI project

 - In target Capabilities enable Background Modes and check Location updates

 - In Info.plist add 

    Privacy - Location Always and When In Use Usage Description

    Privacy - Location Always Usage Description

    Privacy - Location When In Use Usage Description

 - key and value that will specify the reason for your app to access the user’s location information at all times.


Now, Add location folder into your project.    
    
# Start Location tracking:

    HSLocationTracking.shared().startLocationTracking()
    
**This method is called in every 30 sec if location is available with specified accuracy(private const int TimeInterval = 30;)**

    public void ScheduledLocationManager(HSLocationManager manager, NSError error)


# Other:    
You can see engine and location log by using 

    HSLogger.Logger.ExportLogFile()
    
# See an example app HSLocationManager in the repository

Note, if you test on a stimulater edit scheme and set default location.

