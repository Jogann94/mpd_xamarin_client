using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using MPDApp.Pages;
using MPDApp.Models;

namespace MPDApp.Pages
{
	public partial class MasterPage : MasterDetailPage
	{
		public ObservableCollection<MasterPageItem> MenuList { get; set; }

		public int lastSelectedIndex;

		public MasterPage()
		{
			InitializeComponent();
			MenuList = new ObservableCollection<MasterPageItem>();
			lastSelectedIndex = 0;

			FillMenuList();
			PageListView.ItemsSource = MenuList;

			var startPage = new NavigationPage(new MainPage())
			{ BarBackgroundColor = Color.OrangeRed };
			Detail = startPage;		
		}

		private void FillMenuList()
		{
			MenuList.Add(new MasterPageItem("Bibliothek", "library_music", typeof(MainPage), true));
			MenuList.Add(new MasterPageItem("Playlists", "queue_music", typeof(PlalistInfoPage), false));
			MenuList.Add(new MasterPageItem("Files", "folder", typeof(FilePage), false));
			MenuList.Add(new MasterPageItem("Search", "search", typeof(SearchPage), false));
			MenuList.Add(new MasterPageItem("Serverproperties", "dvr", typeof(ServerPage), false));
			MenuList.Add(new MasterPageItem("Profiles", "settings_input_antenna", typeof(ProfilePage), false));
			MenuList.Add(new MasterPageItem("Voice Control", "voice", typeof(SpeechPage), false));
			MenuList.Add(new MasterPageItem("Play", "play_arrow", typeof(PlaybackPage), false));
		}

		private void PageListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			MasterPageItem selected = e.SelectedItem as MasterPageItem;
			int selectedListPosition = MenuList.IndexOf(selected);

			var lastItem = MenuList[lastSelectedIndex];

			MenuList[lastSelectedIndex] = new MasterPageItem(lastItem.Title, lastItem.IconNameWithoutColor, lastItem.TargetType, false);
			MenuList[selectedListPosition] = new MasterPageItem(selected.Title, selected.IconNameWithoutColor, selected.TargetType, true);
			lastSelectedIndex = selectedListPosition;

			var type = selected.TargetType;
			var nav = Detail as NavigationPage;
			try
			{
				Detail = CreateNewNavigationPage(Activator.CreateInstance(type) as Page);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.InnerException);
			}

			IsPresented = false;
		}

		private NavigationPage CreateNewNavigationPage(Page p)
		{
			var returnPage = new NavigationPage(p)
			{
				BarBackgroundColor = Color.OrangeRed,
				BarTextColor = Color.White
			};
			return returnPage;
		}

	}
}
