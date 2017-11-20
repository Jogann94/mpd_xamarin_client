using MPDProtocol;
using MPDProtocol.MPDDataobjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SongListPage : ContentPage
	{
		private MPDCommands.MPD_SEARCH_TYPE type;

		public SongListPage(string playlistName)
		{
			InitializeComponent();
			Title = playlistName;

			Task t = Task.Factory.StartNew(async () =>
			{
				await Task.Delay(App.PAGE_ANIMATION_DELAY);
				GetSongsFromMPD(true);
			});
		}

		public SongListPage(string searchValue, MPDCommands.MPD_SEARCH_TYPE searchType)
		{
			InitializeComponent();
			Title = searchValue;
			type = searchType;

			Task t = Task.Factory.StartNew(async () =>
			{
				await Task.Delay(App.PAGE_ANIMATION_DELAY);
				GetSongsFromMPD(false);
			});
		}

		private async void GetSongsFromMPD(bool isPlaylist)
		{
			var con = MPDConnection.GetInstance();
			while (!con.IsConnected())
			{
				await Task.Delay(200);
				con = MPDConnection.GetInstance();
			}

			List<MPDFileEntry> fileList;
			if (isPlaylist)
			{
				fileList = con.GetSavedPlaylist(Title);
			}
			else
			{
				fileList = con.GetSearchedFiles(Title, type);
			}

			List<MPDTrack> songList = fileList.Select(x => x as MPDTrack).ToList();

			if (songList != null)
			{
				songList.RemoveAll(x => x == null || x.TrackTitle == "");
			}

			if (songList.Count > 0)
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					SongListView.ItemsSource = songList;
				});
			}
		}

		private void AddItem_Clicked(object sender, EventArgs e)
		{
			if (SongListView.SelectedItem is MPDTrack currentSelected)
			{
				MPDConnection.GetInstance().AddSong(currentSelected.Path);
			}
		}
	}
}