using Foundation;
using UIKit;

#pragma warning disable IDE0130 // Le namespace ne correspond pas à la structure de dossiers
namespace HSLocationManager;
#pragma warning restore IDE0130 // Le namespace ne correspond pas à la structure de dossiers

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	// iOS-specific lifecycle methods
	public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
	{
		base.FinishedLaunching(application, launchOptions);
		// iOS startup logic here
		return true;
	}

	public override void DidEnterBackground(UIApplication application)
	{
		// iOS-specific background logic
	}
}
