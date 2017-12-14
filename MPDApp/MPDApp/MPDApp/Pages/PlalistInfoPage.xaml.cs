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
	public partial class PlalistInfoPage : ContentPage
	{
		public PlalistInfoPage()
		{
			InitializeComponent();

			Task t = Task.Factory.StartNew(async () =>
			{
				await Task.Delay(App.PAGE_ANIMATION_DELAY);
				GetPlaylistsFromMPD();
			});
		}

		public async void GetPlaylistsFromMPD()
		{
			var con = MPDConnection.GetInstance();
			while (!con.IsConnected())
			{
				await Task.Delay(200);
				con = MPDConnection.GetInstance();
			}

			List<MPDFileEntry> fileList = con.GetPlaylists();
			List<MPDPlaylist> playlists = fileList.Select(x => x as MPDPlaylist).ToList();
			playlists.RemoveAll(x => x == null);

			if (playlists != null && playlists.Count > 0)
			{
				playlists.RemoveAll(x => x == null);

				Device.BeginInvokeOnMainThread(() =>
				{
					PlaylistView.ItemsSource = playlists;
				});
			}
		}

		private void PlaylistView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			var playlist = e.SelectedItem as MPDPlaylist;
			
			Navigation.PushAsync(SongListPage.CreateWithPlaylist(playlist.Name));
		}
	}
}