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
	public partial class AlbumListPage : ContentPage
	{

		public AlbumListPage()
		{
			InitializeComponent();
			switch (Device.RuntimePlatform)
			{
				case "UWP":
					Title = "Playlist";
					break;
			}
		}

		private async void Page_Appearing(object sender, EventArgs e)
		{
			await Task.Factory.StartNew(PlaylistUpdate);
		}

		private void PlaylistUpdate()
		{
			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
			{
				var albumList = MPDConnection.GetInstance().GetAlbums();
				if (albumList.Count == 0)
				{
					Device.BeginInvokeOnMainThread(async () =>
					{
						await DisplayAlert("No Albums", "No Album found", "ok");
					});
				}
				else
				{
					Device.BeginInvokeOnMainThread(() =>
					{
						AlbumListView.ItemsSource = albumList;
					});
				}

			}
		}

		private async void Item_Selected(object sender, SelectedItemChangedEventArgs e)
		{
			if (e.SelectedItem is MPDAlbum album)
			{
				await Navigation.PushAsync(SongListPage.CreateWithSearch
					(album.Name, MPDCommands.MPD_SEARCH_TYPE.MPD_SEARCH_ALBUM));
			}
		}

	}

}