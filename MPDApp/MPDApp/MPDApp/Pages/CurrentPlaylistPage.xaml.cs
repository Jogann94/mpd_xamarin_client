using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using MPDProtocol;
using MPDProtocol.MPDDataobjects;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.ObjectModel;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CurrentPlaylistPage : ContentPage
	{
		private CancellationTokenSource tokenSource = new CancellationTokenSource();
		private Task updateTask;

		public CurrentPlaylistPage()
		{
			InitializeComponent();
			switch (Device.RuntimePlatform)
			{
				case "UWP":
					Title = "Playlist";
					break;
			}
		}

		private void PlayButton_Clicked(object sender, EventArgs e)
		{
			if (SongListView.SelectedItem != null)
			{
				if (SongListView.SelectedItem is MPDTrack)
				{
					MPDTrack selectedTrack = SongListView.SelectedItem as MPDTrack;
					MPDConnection.GetInstance().PlaySongIndex(selectedTrack.PlaylistPosition);
				}
			}
		}

		private void StopButton_Clicked(object sender, EventArgs e)
		{
			MPDConnection.GetInstance().StopPlayback();
		}

		private void SongListPage_Disappearing(object sender, EventArgs e)
		{
			if (updateTask != null)
			{
				tokenSource.Cancel();
				updateTask = null;
			}
		}

		private void SongListPage_Appearing(object sender, EventArgs e)
		{
			var ct = tokenSource.Token;

			updateTask = Task.Factory.StartNew(async () =>
			{
				await Task.Delay(App.PAGE_ANIMATION_DELAY);
				while (true)
				{
					if (ct.IsCancellationRequested)
					{
						Debug.WriteLine("THREAD CANCELED");
						return;
					}
					PlaylistUpdate();
					await Task.Delay(1000);
				}
			}, ct);
		}

		private void PlaylistUpdate()
		{
			var fileEntryList = MPDConnection.GetInstance().GetCurrentPlaylist();
			var newPlayList = new ObservableCollection<MPDTrack>();

			foreach (var fileEntry in fileEntryList)
			{
				newPlayList.Add(fileEntry as MPDTrack);
			}
			if (newPlayList != null && newPlayList.Count > 0)
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					if (SongListView.ItemsSource != null)
					{
						if (!newPlayList.SequenceEqual(SongListView.ItemsSource as ObservableCollection<MPDTrack>))
						{
							SongListView.ItemsSource = newPlayList;
						}
					}
					else
					{
						SongListView.ItemsSource = newPlayList;
					}
				});
			}

		}
	}

}