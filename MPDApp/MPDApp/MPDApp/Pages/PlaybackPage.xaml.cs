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

		private string shuffleImageSource = "shuffle_white.png";
		public string ShuffleImageSource
		{
			get { return shuffleImageSource; }
			set
			{
				OnPropertyChanged("ShuffleImageSource");
				shuffleImageSource = value;
			}
		}

		private string volumeImageSource;
		public string VolumeImagesource
		{
			get { return volumeImageSource; }
			set
			{
				OnPropertyChanged("VolumeImageSource");
				volumeImageSource = value;
			}
		}

		private CancellationTokenSource tokenSource = new CancellationTokenSource();

		public PlaybackPage()
		{
			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
			{
				Status = con.GetCurrentServerStatus();
				if (status != null)
					CurrentSong = con.GetCurrentSong();
			}

			InitializeComponent();
			BindingContext = this;
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
				System.Diagnostics.Debug.WriteLine("UPDATED");
				int i = 0;

				while (!ct.IsCancellationRequested && i < 4)
				{
					await Task.Delay(200);
					i++;
				}

				var con = MPDConnection.GetInstance();
				if (con.IsConnected())
				{
					Status = con.GetCurrentServerStatus();
					CurrentSong = con.GetCurrentSong();
				}

				Device.BeginInvokeOnMainThread(SetUpdatedValues);
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

				System.Diagnostics.Debug.WriteLine(Status.Volume);
			}

		}

		public ICommand Repeat_Clicked
		{
			get
			{
				return new Command(() =>
				{
					System.Diagnostics.Debug.WriteLine("HOLI CHIT");
					var con = MPDConnection.GetInstance();
					if (con.IsConnected() && Status != null)
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
					var con = MPDConnection.GetInstance();
					if (con.IsConnected())
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
					var con = MPDConnection.GetInstance();
					if (con.IsConnected())
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
					var con = MPDConnection.GetInstance();
					if (con.IsConnected())
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
					var con = MPDConnection.GetInstance();
					if (con.IsConnected())
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
					var con = MPDConnection.GetInstance();
					if (con.IsConnected())
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