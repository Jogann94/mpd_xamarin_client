using System;
using System.ComponentModel;
using System.Threading.Tasks;
using MPDApp.DependencyServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.Generic;
using MPDProtocol;
using MPDProtocol.MPDDataobjects;
using ApiAiSDK;

namespace MPDApp.Pages
{

	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ArtistListPage : ContentPage, INotifyPropertyChanged
	{

		public ArtistListPage()
		{
			InitializeComponent();

			Task t = Task.Factory.StartNew(async () =>
			{
				await Task.Delay(App.PAGE_ANIMATION_DELAY);
				GetArtistsFromMPD();
			});
			BindingContext = this;

		}

		public async void GetArtistsFromMPD()
		{
			var con = MPDConnection.GetInstance();
			while (!con.IsConnected())
			{
				await Task.Delay(200);
				con = MPDConnection.GetInstance();
			}

			List<MPDArtist> artists = con.GetArtists();
			if (artists != null && artists.Count > 0)
			{
				Device.BeginInvokeOnMainThread(() =>
				{
					ArtistListView.ItemsSource = artists;
				});
			}
		}

		private void ArtistListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			MPDArtist a = e.SelectedItem as MPDArtist;
			Navigation.PushAsync(new SongListPage(a.ArtistName,
				MPDCommands.MPD_SEARCH_TYPE.MPD_SEARCH_ARTIST));
		}
	}
}