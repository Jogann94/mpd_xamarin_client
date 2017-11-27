using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MPDProtocol;
using MPDProtocol.MPDDataobjects;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PlaybackPage : ContentPage
	{
		private MPDTrack currentSong;
		public MPDTrack CurrentSong
		{
			get { return currentSong; }
			set
			{
				OnPropertyChanged("CurrentSong");
				currentSong = value;
			}
		}

		private MPDCurrentStatus status;
		public MPDCurrentStatus Status
		{
			get { return status; }
			set
			{
				OnPropertyChanged("Status");
				status = value;
			}
		}

		private string repeatImageSource = "repeat_white.png";
		public string RepeatImageSource
		{
			get { return repeatImageSource; }
			set
			{
				OnPropertyChanged("RepeatImageSource");
				repeatImageSource = value;
			}
			
		}

		private CancellationTokenSource tokenSource = new CancellationTokenSource();

		public PlaybackPage ()
		{
			var con = MPDConnection.GetInstance();
			if(con.IsConnected())
			{
				Status = con.GetCurrentServerStatus();
				if (status != null)
					CurrentSong = con.GetCurrentSong();
			}

			InitializeComponent ();
			
		}

		public async void Page_Appearing(object sender, EventArgs e)
		{
			var token = tokenSource.Token;
			await Task.Factory.StartNew(() => UpdateStatus(token));
		}

		public void Page_Disappearing(object sender, EventArgs e)
		{
			tokenSource.Cancel();
		}

		public async Task UpdateStatus(CancellationToken ct)
		{

			while (!ct.IsCancellationRequested)
			{
				int i = 0;

				while(!ct.IsCancellationRequested && i < 6)
				{
					await Task.Delay(200);
					i++;
				}

				var con = MPDConnection.GetInstance();
				if(con.IsConnected())
					Status = con.GetCurrentServerStatus();
			}
		}

		public void Repeat_Clicked(object sender, EventArgs e)
		{
			var con = MPDConnection.GetInstance();
			if(con.IsConnected() && Status != null)
			{
				if (Status.Repeat == 0)
					con.SetRepeat(true);
				else
					con.SetRepeat(false);
			}
		}

		public void Previous_Clicked(object sender, EventArgs e)
		{
			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
				con.PreviousSong();
		}

		public void Play_Clicked(object sender, EventArgs e)
		{
			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
				con.PlaySongIndex(CurrentSong.PlaylistPosition);
		}

		public void Stop_Clicked(object sender, EventArgs e)
		{
			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
				con.StopPlayback();
		}

		public void Next_Clicked(object sender, EventArgs e)
		{
			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
				con.NextSong();
		}

		public void Shuffle_Clicked(object sender, EventArgs e)
		{
			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
			{
				if (Status.Random == 0)
					con.SetRandom(true);
				else
					con.SetRandom(false);
			}
		}
	}


}