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

		private enum ListType { CURRENT, SAVED, SEARCH };
		private ListType listType;

		private SongListPage()
		{
			InitializeComponent();
		}

		public static SongListPage CreateWithCurrentPlaylist()
		{
			SongListPage result = new SongListPage()
			{ Title = "Current Playlist", listType = ListType.CURRENT };
			CreateToolbarItems(result);

			Task t = Task.Factory.StartNew(async () =>
			{
				await Task.Delay(App.PAGE_ANIMATION_DELAY);
				await result.GetSongsFromMPD();
			});

			return result;
		}

		public static SongListPage CreateWithPlaylist(string playlistName)
		{
			SongListPage result = new SongListPage()
			{ Title = playlistName, listType = ListType.SAVED };
			CreateToolbarItems(result);

			Task t = Task.Factory.StartNew(async () =>
			{
				await Task.Delay(App.PAGE_ANIMATION_DELAY);
				await result.GetSongsFromMPD();
			});

			return result;
		}

		public static SongListPage CreateWithSearch(string searchValue, MPDCommands.MPD_SEARCH_TYPE searchType)
		{
			SongListPage result = new SongListPage()
			{ Title = searchValue, type = searchType, listType = ListType.SEARCH };
			CreateToolbarItems(result);

			Task t = Task.Factory.StartNew(async () =>
			{
				await Task.Delay(App.PAGE_ANIMATION_DELAY);
				await result.GetSongsFromMPD();
			});
			
			return result;
		}

		private static void CreateToolbarItems(SongListPage newPage)
		{
			if(newPage.listType == ListType.CURRENT)
			{
				var playItem = new ToolbarItem()
				{
					Icon = "play_arrow_white.png"
				};
				playItem.Clicked += newPage.PlayItem_Clicked;

				var removeItem = new ToolbarItem()
				{
					Icon = "remove_white.png"
				};
				removeItem.Clicked += newPage.RemoveItem_Clicked;

				newPage.ToolbarItems.Add(removeItem);
				newPage.ToolbarItems.Add(playItem);
			}
			else if(newPage.listType == ListType.SAVED)
			{
				var loadPlaylistItem = new ToolbarItem()
				{
					Icon = "library_add_white.png"
				};
				loadPlaylistItem.Clicked += newPage.LoadPlaylist_Clicked;

				newPage.ToolbarItems.Add(loadPlaylistItem);
			}
			else
			{
				var removeItem = new ToolbarItem()
				{
					Icon = "remove_white.png"
				};
				removeItem.Clicked += newPage.RemoveItem_Clicked;

				var add_Item = new ToolbarItem()
				{
					Icon = "add_white.png"
				};
				add_Item.Clicked += newPage.AddItem_Clicked;

				newPage.ToolbarItems.Add(removeItem);
				newPage.ToolbarItems.Add(add_Item);
			}
		}

		private async Task GetSongsFromMPD()
		{
			var con = MPDConnection.GetInstance();
			while (!con.IsConnected())
			{
				await Task.Delay(200);
				con = MPDConnection.GetInstance();
			}

			List<MPDFileEntry> fileList;
			if (listType == ListType.SAVED)
			{
				fileList = con.GetSavedPlaylist(Title);
			}
			else if (listType == ListType.SEARCH)
			{
				fileList = con.GetSearchedFiles(Title, type);
			}
			else
			{
				fileList = con.GetCurrentPlaylist();
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

		private void PlayItem_Clicked(object sender, EventArgs e)
		{
			if(SongListView.SelectedItem is MPDTrack track)
			{
				if(MPDConnection.GetInstance().IsConnected())
				{
					MPDConnection.GetInstance().PlaySongIndex(track.PlaylistPosition);
				}
			}
		}

		private async void AddItem_Clicked(object sender, EventArgs e)
		{
			if (SongListView.SelectedItem is MPDTrack currentSelected)
			{
				MPDConnection.GetInstance().AddSong(currentSelected.Path);
			}

			if(listType == ListType.CURRENT)
			{
				await Task.Delay(200);
				await GetSongsFromMPD();
			}
		}

		private async void RemoveItem_Clicked(object sender, EventArgs e)
		{
			if (SongListView.SelectedItem is MPDTrack currentSelected)
			{
				MPDConnection.GetInstance().RemoveIndex(currentSelected.PlaylistPosition);
			}

			if (listType == ListType.CURRENT)
			{
				await Task.Delay(200);
				await GetSongsFromMPD();
			}
		}

		private void LoadPlaylist_Clicked(object sender, EventArgs e)
		{
			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
			{
				con.ClearPlaylist();
				con.LoadPlaylist(Title);
			}
		}
	}
}