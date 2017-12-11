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
		public ObservableCollection<MasterPageItem> menuList { get; set; }

		public int lastSelectedIndex;

		public MasterPage()
		{
			InitializeComponent();
			menuList = new ObservableCollection<MasterPageItem>();
			lastSelectedIndex = 0;

			FillMenuList();
			PageListView.ItemsSource = menuList;

			var startPage = new NavigationPage(new MainPage());
			startPage.BarBackgroundColor = Color.OrangeRed;
			Detail = startPage;		
		}

		private void FillMenuList()
		{
			menuList.Add(new MasterPageItem("Bibliothek", "library_music", typeof(MainPage), true));
			menuList.Add(new MasterPageItem("Playlists", "queue_music", typeof(PlalistInfoPage), false));
			menuList.Add(new MasterPageItem("Files", "folder", typeof(FilePage), false));
			menuList.Add(new MasterPageItem("Search", "search", typeof(SearchPage), false));
			menuList.Add(new MasterPageItem("Serverproperties", "dvr", typeof(ServerPage), false));
			menuList.Add(new MasterPageItem("Profiles", "settings_input_antenna", typeof(ProfilePage), false));
			menuList.Add(new MasterPageItem("Voice Control", "voice", typeof(SpeechPage), false));
			menuList.Add(new MasterPageItem("Play", "play_arrow", typeof(PlaybackPage), false));
		}

		private void PageListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			MasterPageItem selected = e.SelectedItem as MasterPageItem;
			int selectedListPosition = menuList.IndexOf(selected);

			var lastItem = menuList[lastSelectedIndex];

			menuList[lastSelectedIndex] = new MasterPageItem(lastItem.Title, lastItem.IconNameWithoutColor, lastItem.TargetType, false);
			menuList[selectedListPosition] = new MasterPageItem(selected.Title, selected.IconNameWithoutColor, selected.TargetType, true);
			lastSelectedIndex = selectedListPosition;

			var type = selected.TargetType;
			var nav = Detail as NavigationPage;
			Detail = CreateNewNavigationPage(Activator.CreateInstance(type) as Page);

			IsPresented = false;
		}

		private NavigationPage CreateNewNavigationPage(Page p)
		{
			var returnPage = new NavigationPage(p);
			returnPage.BarBackgroundColor = Color.OrangeRed;
			returnPage.BarTextColor = Color.White;
			return returnPage;
		}

	}
}
