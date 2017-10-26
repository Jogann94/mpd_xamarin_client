using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MPDApp.ProfileManagement;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class EditProfilePage : ContentPage
	{
		public MPDServerProfile CurrentProfile { get; set; }

		public EditProfilePage()
		{
			InitializeComponent();
			BindingContext = this;
			CurrentProfile = new MPDServerProfile();
		}

		private async void ToolbarChecked_Clicked(object sender, EventArgs e)
		{
			CheckCurrentProfileValues();
			CurrentProfile.IsActiveProfile = true;

			var activeProfile = await App.Database.GetActiveProfile();
			if (activeProfile != null)
			{
				if (!activeProfile.Equals(CurrentProfile))
					await SetProfileInactive(activeProfile);
			}
			else
			{
				await App.Database.SaveProfileAsync(CurrentProfile);
				App.ConnectWithActiveProfile();
			}

			await Navigation.PopAsync();
		}

		private void CheckCurrentProfileValues()
		{
			if (portEntry.Text == null || portEntry.Text == "")
			{
				CurrentProfile.Port = 6600;
			}
			if (CurrentProfile.Password == null)
			{
				CurrentProfile.Password = "";
			}
		}

		private async Task SetProfileInactive(MPDServerProfile p)
		{
			p.IsActiveProfile = false;
			await App.Database.UpdateProfile(p);
			await App.Database.SaveProfileAsync(CurrentProfile);
			App.ConnectWithActiveProfile();
		}
	}
}