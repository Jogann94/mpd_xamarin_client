using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using MPDApp.ProfileManagement;
using MPDProtocol;
using MPDProtocol.MPDDataobjects;
using System.Threading;
using System.Threading.Tasks;

namespace MPDApp
{
	public partial class App : Application
	{
		private static ServerProfileDatabase database;
		public static bool MPDProfileChanged { get; set; }

		public App()
		{
			InitializeComponent();
			MainPage = new MPDApp.MasterPage();

			Task t = new Task( () => UpdateMPDConnection());
			t.Start();

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

		public async static void UpdateMPDConnection()
		{
			while(true)
			{
				if (!MPDConnection.GetInstance().IsConnected() || MPDProfileChanged)
				{
					ConnectWithActiveProfile();
				}

				await Task.Delay(1000);
			}
		}

		public static ServerProfileDatabase Database
		{
			get
			{
				if(database == null)
				{
					database = new ServerProfileDatabase(
						DependencyService.Get<IFileHelper>().GetLocalFilePath("ServerProfileSQLite.db3"));
				}
				return database;
			}
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
