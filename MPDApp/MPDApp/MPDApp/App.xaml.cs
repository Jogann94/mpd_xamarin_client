using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using MPDApp.DependencyServices;
using MPDProtocol;
using MPDApp.Services;
using System.Threading.Tasks;
using ApiAiSDK;

namespace MPDApp
{
	public partial class App : Application
	{
		private static ServerProfileDatabase database;
		public static ServerProfileDatabase Database
		{
			get
			{
				if (database == null)
				{
					database = new ServerProfileDatabase(
						DependencyService.Get<IFileHelper>().GetLocalFilePath("ServerProfileSQLite.db3"));
				}
				return database;
			}
		}

		public static bool MPDProfileChanged { get; set; }
		public static AIConfiguration AIConfig { get; private set; }

		public App()
		{
			InitializeComponent();
			MainPage = new Pages.MasterPage();
			AIConfig = new AIConfiguration("157d22de6510452cbfbcdb85e3215a5b", SupportedLanguage.English);
			Task t = new Task(() => UpdateMPDConnection());
			t.Start();
		}

		public async static void UpdateMPDConnection()
		{
			while (true)
			{
				if (!MPDConnection.GetInstance().IsConnected() || MPDProfileChanged)
				{
					ConnectWithActiveProfile();
				}

				await Task.Delay(1000);
			}
		}

		public static void ConnectWithActiveProfile()
		{
			var activeProfile = Database.GetActiveProfile().Result;
			var con = MPDConnection.GetInstance();

			if (activeProfile != null)
			{
				con.hostname = activeProfile.Hostname;
				con.port = activeProfile.Port;
				con.password = activeProfile.Password;

				con.ConnectToServer();
			}
			else
			{
				if (con.IsConnected())
				{
					con.DisconnectFromServer();
				}
			}

			MPDProfileChanged = false;
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}
