using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MPDApp.Converter;
using MPDProtocol;
using MPDProtocol.MPDDataobjects;
using System.ComponentModel;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ServerStatsPage : ContentPage
	{
		private MPDStatistics statistics;

		public MPDStatistics Statistics
		{
			get { return statistics; }
			set { statistics = value; OnPropertyChanged("Statistics"); }
		}

		private string uptime;
		public string Uptime
		{
			get { return uptime; }
			set { uptime = value; OnPropertyChanged("Uptime"); }
		}

		private string allSongDuration;
		public string AllSongDuration
		{
			get { return allSongDuration; }
			set { allSongDuration = value; OnPropertyChanged("AllSongDuration"); }
		}

		private string lastDBUpdate;
		public string LastDBUpdate
		{
			get { return lastDBUpdate; }
			set { lastDBUpdate = value; OnPropertyChanged("LastDBUpdate"); }
		}


		public ServerStatsPage()
		{
			InitializeComponent();
			BindingContext = this;
			PropertyChanged += PropChanged_Listener;

			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
			{
				Statistics = con.GetServerStatistics();
			}
			
		}

		protected void PropChanged_Listener(object sender, PropertyChangedEventArgs e)
		{
			if(e.PropertyName == "Statistics")
			{
				Uptime = TimeString.GenerateFromSeconds(Statistics.ServerUptime);
				AllSongDuration = TimeString.GenerateFromSeconds(Statistics.AllSongDuration);

				DateTime time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local)
					.AddSeconds(Statistics.LastDBUpdate);

				LastDBUpdate = time.ToLongDateString() + " " + time.ToLongTimeString();
			}
		}

		protected async void UpdateButton_Clicked(object sender, EventArgs e)
		{
			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
			{
				Statistics = await Task.Factory.StartNew(() =>
				{
					con.UpdateDatabase("");
				}).ContinueWith((lastTask) => 
					{
						return con.GetServerStatistics();
					});
			}
		}

	}
}