using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MPDApp.ProfileManagement;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ProfilePage : ContentPage
	{
		public ObservableCollection<MPDServerProfile> ProfileList { get; set; }

		public ProfilePage()
		{
			InitializeComponent();
		}

		private void ToolbarAdd_Clicked(object sender, EventArgs e)
		{
			Navigation.PushAsync(new EditProfilePage());
		}

		private async void RefreshList()
		{
			ProfileList = new ObservableCollection<MPDServerProfile>
				(await App.Database.GetProfilesAsync());
			ProfileListView.ItemsSource = ProfileList;
		}

		private void PageAppearing_Listener(object sender, EventArgs e)
		{
			RefreshList();
		}

		private async void ProfileListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			var selected = ProfileListView.SelectedItem as MPDServerProfile;

			selected.IsActiveProfile = true;

			var lastActive = await App.Database.GetActiveProfile();
			lastActive.IsActiveProfile = false;

			await App.Database.UpdateProfile(lastActive);
			await App.Database.UpdateProfile(selected);

			RefreshList();
			App.MPDProfileChanged = true;
		}

	}
}