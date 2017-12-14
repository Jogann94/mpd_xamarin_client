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
using System.Windows.Input;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PlaybackPage : ContentPage
	{
		private MPDTrack currentSong;
		public MPDTrack CurrentSong
		{
			get { return currentSong; }
			set{ OnPropertyChanged("CurrentSong"); currentSong = value; }
		}

		private MPDCurrentStatus status;
		public MPDCurrentStatus Status
		{
			get { return status; }
			set { OnPropertyChanged("Status"); status = value;}
		}

		private string repeatImageSource = "repeat_white.png";
		public string RepeatImageSource
		{
			get { return repeatImageSource; }
			set { OnPropertyChanged("RepeatImageSource"); repeatImageSource = value;}
		}

		private string shuffleImageSource = "shuffle_white.png";
		public string ShuffleImageSource
		{
			get { return shuffleImageSource; }
			set	{ OnPropertyChanged("ShuffleImageSource"); shuffleImageSource = value;}
		}

		private double promilleElapsed = 0;
		public double PromilleElapsed
		{
			get	{ return promilleElapsed; }
			set { OnPropertyChanged("PromilleElapsed"); promilleElapsed = value;}
		}

		private string elapsedTimeString = "00:00";
		public string ElapsedTimeString
		{
			get
			{
				return elapsedTimeString;
			}
			set
			{
				if (elapsedTimeString != value)
				{
					elapsedTimeString = value;
					OnPropertyChanged("ElapsedTimeString");
				}
			}
		}

		private string tracklengthString = "00:00";
		public string TracklengthString
		{
			get
			{
				return tracklengthString;
			}
			set
			{
				if(tracklengthString != value)
				{
					tracklengthString = value;
					OnPropertyChanged("TracklengthString");
				}
			}
		}

		private MPDConnection con;

		private string volumeImageSource = "volume_off_white.png";
		public string VolumeImageSource
		{
			get { return volumeImageSource; }
			set
			{
				if(volumeImageSource != value)
				{
					OnPropertyChanged("VolumeImageSource");
					volumeImageSource = value;
				}
			}
		}

		private bool jumped = false;
		private CancellationTokenSource tokenSource = new CancellationTokenSource();

		public PlaybackPage()
		{
			InitializeComponent();
			BindingContext = this;
		}

		public async void Page_Appearing(object sender, EventArgs e)
		{
			tokenSource = new CancellationTokenSource();
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

				await WaitIfTimeJumped();
				if (CheckAndSetConnection())
				{
					lock (this)
					{
						Status = con.GetCurrentServerStatus();
						GetCurrentSongFromMPD();
					}

				}

				Device.BeginInvokeOnMainThread(SetUpdatedValues);

				while (!ct.IsCancellationRequested && i < 4)
				{
					await Task.Delay(200);
					i++;
				}
			}

		}

		private async Task WaitIfTimeJumped()
		{
			if (jumped)
			{
				await Task.Delay(300);
				jumped = false;
			}
		}

		private void SetUpdatedValues()
		{
			if (Status != null)
			{
				string imageSource = (status.Repeat == 1) ? "repeat_blue.png" : "repeat_white.png";
				if (RepeatImageSource != imageSource)
				{
					RepeatImageSource = imageSource;
				}

				imageSource = (status.Random == 1) ? "shuffle_blue.png" : "shuffle_white.png";
				if (ShuffleImageSource != imageSource)
				{
					ShuffleImageSource = imageSource;
				}

				if (status.Volume >= 60)
				{
					VolumeImageSource = "volume_up_white.png";
				}
				else if (status.Volume > 0)
				{
					VolumeImageSource = "volume_down_white.png";
				}
				else
				{
					VolumeImageSource = "volume_off_white.png";
				}

				PromilleElapsed = (double)status.ElapsedTime / (1000.0 * Status.CurrentTrackLength);
				TracklengthString = (CurrentSong != null) ?
					GenerateTimeString(CurrentSong.LengthInSeconds) : GenerateTimeString(Status.CurrentTrackLength);

				ElapsedTimeString = GenerateTimeString(Status.ElapsedTimeInSec);
				
			}

		}

		private void GetCurrentSongFromMPD()
		{
			if(CheckAndSetConnection())
			{
				var song = con.GetCurrentSong();
				if (song == null)
				{
					var currentList = con.GetCurrentPlaylist();
					if (currentList.Count > 0)
					{
						song = currentList.ElementAt(0) as MPDTrack;
					}
					else
					{
						song = null;
					}
				}

				CurrentSong = song;
			}
		}

		private bool CheckAndSetConnection()
		{
			con = MPDConnection.GetInstance();
			if (con.IsConnected())
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private string GenerateTimeString(int seconds)
		{
			int remainingSeconds = seconds % 60;
			int minutes = (seconds / 60) % 60;
			int hours = (seconds / 60 / 60);
			if (hours > 0)
			{
				return String.Format("{0:00}:{1:00}:{2:00}", hours, minutes, remainingSeconds);
			}
			else
			{
				return String.Format("{0:00}:{1:00}", minutes, remainingSeconds);
			}
		}

		private async void Playlist_Clicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(SongListPage.CreateWithCurrentPlaylist());
		}

		private void Time_Changed(object sender, EventArgs e)
		{
			lock (this)
			{
				var s = sender as Slider;
				int jumpToSecond = (int)(s.Value * Status.CurrentTrackLength);
				if ((Math.Abs(jumpToSecond - Status.ElapsedTimeInSec) >= 4)
						&& CheckAndSetConnection())
				{
						con.SeekSeconds(jumpToSecond);
						jumped = true;
				}
			}
		}

		private void Volume_Changed(object sender, EventArgs e)
		{
			lock (this)
			{
				var s = sender as Slider;
				int volume = (int)s.Value;
				if (CheckAndSetConnection())
				{
					con.SetVolume(volume);
				}
			}
		}

		public ICommand Repeat_Clicked
		{
			get
			{
				return new Command(() =>
				{
					if (CheckAndSetConnection() && Status != null)
					{
						if (Status.Repeat == 0)
							con.SetRepeat(true);
						else
							con.SetRepeat(false);
					}
				});
			}
		}

		public ICommand Previous_Clicked
		{
			get
			{
				return new Command(() =>
			 {
				 if (CheckAndSetConnection())
					 con.PreviousSong();
			 });
			}
		}

		public ICommand Play_Clicked
		{
			get
			{
				return new Command(() =>
				{
					if (CheckAndSetConnection())
					{
						if (CurrentSong != null)
						{
							con.PlaySongIndex(CurrentSong.PlaylistPosition);
						}
					}
				});
			}
		}

		public ICommand Stop_Clicked
		{
			get
			{
				return new Command(() =>
				{
					if (CheckAndSetConnection())
						con.StopPlayback();
				});
			}
		}

		public ICommand Next_Clicked
		{
			get
			{
				return new Command(() =>
				{
					if (CheckAndSetConnection())
						con.NextSong();
				});
			}
		}

		public ICommand Shuffle_Clicked
		{
			get
			{
				return new Command(() =>
				{
					if (CheckAndSetConnection())
					{
						if (Status.Random == 0)
							con.SetRandom(true);
						else
							con.SetRandom(false);
					}
				});
			}
		}

	}

}