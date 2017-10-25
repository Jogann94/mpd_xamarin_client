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
	public partial class ServerPage : TabbedPage
	{
		public ServerPage()
		{
			InitializeComponent();
			SetTitleByCurrentPageType();
			CurrentPageChanged += TabbedPage_PagesChanged;
		}

		private void TabbedPage_PagesChanged(object sender, EventArgs e)
		{
			SetTitleByCurrentPageType();
		}

		private void SetTitleByCurrentPageType()
		{
			if (CurrentPage is ServerStatsPage)
			{
				Title = "Stats";
			}
			else
			{
				Title = "Outputs";
			}
		}
	}
}