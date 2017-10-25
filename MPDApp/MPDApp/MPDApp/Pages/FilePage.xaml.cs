using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MPDProtocol;
using MPDProtocol.MPDDataobjects;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class FilePage : ContentPage
	{

		public FilePage()
		{
			InitializeComponent();

			Task.Factory.StartNew(GetAllFilesOnServer);
		}

		private async void AddItem_Clicked(object sender, EventArgs e)
		{
			if (FileListView.SelectedItem == null || !(FileListView.SelectedItem is MPDTrack))
			{
				return;
			}

			var song = FileListView.SelectedItem as MPDTrack;

			await Task.Factory.StartNew(() =>
			{
				var con = MPDConnection.GetInstance();
				con.AddSong(song.Path);
			});
		}

		private async void GetAllFilesOnServer()
		{
			await Task.Delay(200);
			await Task.Factory.StartNew(() =>
			{
				var con = MPDConnection.GetInstance();
				var files = con.GetAllTracks();
				var trackList = new List<MPDTrack>();
				foreach (var file in files)
				{
					if (file is MPDTrack)
						trackList.Add(file as MPDTrack);
				}
				Device.BeginInvokeOnMainThread(() =>
				{
					FileListView.ItemsSource = trackList;
				});
			});
		}
	}
}