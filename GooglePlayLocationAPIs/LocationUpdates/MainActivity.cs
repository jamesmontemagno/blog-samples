using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Android.Gms.Location;
using System.Threading.Tasks;
using Android.Locations;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Android.Support.V4.App;
using Android;
using Android.Support.V4.Content;
using Android.Gms.Common.Apis;
using Debug = System.Diagnostics.Debug;

namespace LocationUpdates
{
    [Activity(Label = "Location Updates", 
		MainLauncher = true,
		LaunchMode = Android.Content.PM.LaunchMode.SingleTop, 
		Icon = "@drawable/icon")]
    public class MainActivity : AppCompatActivity
    {
		const string KeyRequesting = "requesting-location-updates";
		const string KeyLocation = "location";
		const string KeyLastUpdated = "last-updated";

		const int UpdateInterval = 10000;
		const int FastestUpdateInterval = 5000;

		const int RequestCheckSettings = 0x1;

		TextView latitudeTextView, longitudeTextView, timeTextView;
		Button startButton, stopButton;

		bool requestingUpdates;

		FusedLocationProviderClient fusedLocationClient;
		SettingsClient settingsClient;
		Location currentLocation;
		LocationRequest locationRequest;
		LocationSettingsRequest locationSettingsRequest;
		MyLocationCallback locationCallback;

		string lastUpdateTime = string.Empty;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.main);
            var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            if (toolbar != null)
            {
                SetSupportActionBar(toolbar);
                SupportActionBar.SetDisplayHomeAsUpEnabled(false);
                SupportActionBar.SetHomeButtonEnabled(false);
            }


			startButton = FindViewById<Button>(Resource.Id.start_updates_button);
			startButton.Click += async (sender, args) => await StartLocationUpdatesAsync();

			stopButton = FindViewById<Button>(Resource.Id.stop_updates_button);
			stopButton.Click += (sender, args) => StopLocationUpdates();

			latitudeTextView = FindViewById<TextView>(Resource.Id.latitude_text);
			longitudeTextView = FindViewById<TextView>(Resource.Id.longitude_text);
			timeTextView = FindViewById<TextView>(Resource.Id.last_update_time_text);

			fusedLocationClient = LocationServices.GetFusedLocationProviderClient(this);
			settingsClient = LocationServices.GetSettingsClient(this);

			if(bundle != null)
			{
				var keys = bundle.KeySet();
				if (keys.Contains(KeyRequesting))
					requestingUpdates = bundle.GetBoolean(KeyRequesting);

				if (keys.Contains(KeyLocation))
					currentLocation = bundle.GetParcelable(KeyLocation) as Location;

				if (keys.Contains(KeyLastUpdated))
					lastUpdateTime = bundle.GetString(KeyLastUpdated);
					
			}

			locationCallback = new MyLocationCallback();

			//Create request and set intervals:
			//Interval: Desired interval for active location updates, it is inexact and you may not receive upates at all if no location servers are available
			//Fastest: Interval is exact and app will never receive updates faster than this value
			locationRequest = new LocationRequest()
								.SetInterval(UpdateInterval)
								.SetFastestInterval(FastestUpdateInterval)
								.SetPriority(LocationRequest.PriorityHighAccuracy);

			locationSettingsRequest = new LocationSettingsRequest.Builder().AddLocationRequest(locationRequest).Build();

			UpdateUI();
		}

		protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
		{
			switch(requestCode)
			{
				case RequestCheckSettings:

					switch(requestCode)
					{
						case (int)Result.Ok:
							Debug.WriteLine("User agreed to make required location settings changes");
							break;
						case (int)Result.Canceled:
							Debug.WriteLine("User chose not to make required location settings changes");
							requestingUpdates = false;
							UpdateUI();
							break;

					}

					break;

			}
		}

		void OnLocationResult(object sender, Location location)
		{
			currentLocation = location;
			lastUpdateTime = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
			RunOnUiThread(UpdateUI);
		}

		class MyLocationCallback : LocationCallback
		{
			public EventHandler<Location> LocationUpdated;
			public override void OnLocationResult(LocationResult result)
			{
				base.OnLocationResult(result);
				LocationUpdated?.Invoke(this, result.LastLocation);
			}
		}

		void UpdateUI()
		{
			startButton.Enabled = !requestingUpdates;
			stopButton.Enabled = requestingUpdates;
			latitudeTextView.Text = $"Latitude: {(currentLocation == null ? "Unknown" : currentLocation.Latitude.ToString())}";
			longitudeTextView.Text = $"Longitude: {(currentLocation == null ? "Unknown" : currentLocation.Longitude.ToString())}";
			timeTextView.Text = $"Last Update: {(string.IsNullOrWhiteSpace(lastUpdateTime) ? "Unknown" : lastUpdateTime)}";
		}

		
		



		async Task StartLocationUpdatesAsync()
		{
			if (requestingUpdates)
				return;

			try
			{
				await settingsClient.CheckLocationSettingsAsync(locationSettingsRequest);
				await fusedLocationClient.RequestLocationUpdatesAsync(locationRequest, locationCallback);
				locationCallback.LocationUpdated += OnLocationResult;
				requestingUpdates = true;
				UpdateUI();
			}
			catch(ApiException ex)
			{
				switch(ex.StatusCode)
				{
					case CommonStatusCodes.ResolutionRequired:
						try
						{
							// Show the dialog by calling startResolutionForResult(), and check the
							// result in onActivityResult().
							var rae = (ResolvableApiException)ex;
							rae.StartResolutionForResult(this, RequestCheckSettings);
						}
						catch (IntentSender.SendIntentException sie)
						{
							Debug.WriteLine("PendingIntent unable to execute request.");
						}
						break;
					case LocationSettingsStatusCodes.SettingsChangeUnavailable:
						var message = "Location settings are inadequate and cannot be changed, please fix in settings.";
						Toast.MakeText(this, message, ToastLength.Long).Show();
						break;
				}
			}
		}
		
		void StopLocationUpdates()
		{
			if (!requestingUpdates)
				return;



			fusedLocationClient.RemoveLocationUpdatesAsync(locationCallback).ContinueWith((r) =>
			{
				locationCallback.LocationUpdated -= OnLocationResult;
				requestingUpdates = false;
				UpdateUI();

			}, TaskScheduler.FromCurrentSynchronizationContext());

		}

		protected override void OnResume()
		{
			base.OnResume();
			var hasPermission = ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Android.Content.PM.Permission.Granted;
			if (requestingUpdates && hasPermission)
				StartLocationUpdatesAsync().ContinueWith((r) => { });
			else if (!hasPermission)
				RequestPermissionsAsync().ContinueWith((r) => { });
		}

		protected override void OnPause()
		{
			base.OnPause();
			StopLocationUpdates();
		}

		protected override void OnSaveInstanceState(Bundle outState)
		{
			if (currentLocation != null)
			{

				outState.PutBoolean(KeyRequesting, requestingUpdates);
				outState.PutParcelable(KeyLocation, currentLocation);
				outState.PutString(KeyLastUpdated, lastUpdateTime);
			}

			base.OnSaveInstanceState(outState);
		}

		async Task<bool> RequestPermissionsAsync()
		{
			var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
			if (status != PermissionStatus.Granted)
			{
				Console.WriteLine("Currently does not have Location permissions, requesting permissions");

				var request = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Location);

				if (request[Permission.Location] != PermissionStatus.Granted)
				{
					Console.WriteLine("Location permission denied, can not get positions async.");
					return false;
				}
			}

			return true;
		}
		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
		{
			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
			PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}

	}
}

