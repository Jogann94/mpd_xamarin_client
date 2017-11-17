using System;
using System.ComponentModel;
using System.Threading.Tasks;
using MPDApp.DependencyServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ApiAiSDK;
using ApiAiSDK.Model;
using MPDApp.Services;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ArtistListPage : ContentPage, INotifyPropertyChanged
	{

		public ArtistListPage()
		{
			InitializeComponent();

			BindingContext = this;
		}

	}
}