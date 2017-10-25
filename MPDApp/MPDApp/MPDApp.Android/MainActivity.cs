using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content;

namespace MPDApp.Droid
{
	[Activity(Label = "MPDApp", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		public event Action<Result, Intent> SpeechActivityResult;

		protected override void OnCreate(Bundle bundle)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate(bundle);

			global::Xamarin.Forms.Forms.Init(this, bundle);
			LoadApplication(new App());
			
			Window.SetStatusBarColor(Android.Graphics.Color.OrangeRed);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			if(requestCode == SpeechHelper.VOICE)
			{
				SpeechActivityResult?.Invoke(resultCode, data);
			}
			
			base.OnActivityResult(requestCode, resultCode, data);
		}

	}
}

