using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using MPDApp.Pages;

namespace MPDApp
{
	public partial class MasterPage : MasterDetailPage
	{
		public ObservableCollection<MenuPageItem> menuList { get; set; }

		public int lastSelectedIndex;

		public MasterPage()
		{
			InitializeComponent();

			menuList = new ObservableCollection<MenuPageItem>();

			lastSelectedIndex = 0;

			FillMenuList();

			PageListView.ItemsSource = menuList;

			var startPage = new NavigationPage(new MainPage());

			startPage.BarBackgroundColor = Color.OrangeRed;
			Detail = startPage;
			
		}

		private void FillMenuList()
		{
			menuList.Add(new MenuPageItem("Bibliothek", "library_music", typeof(MainPage), true));
			menuList.Add(new MenuPageItem("Playlists", "queue_music", typeof(PlalistInfoPage), false));
			menuList.Add(new MenuPageItem("Files", "folder", typeof(FilePage), false));
			menuList.Add(new MenuPageItem("Search", "search", typeof(SearchPage), false));
			menuList.Add(new MenuPageItem("Serverproperties", "dvr", typeof(ServerPage), false));
			menuList.Add(new MenuPageItem("Profiles", "settings_input_antenna", typeof(ProfilePage), false));
			menuList.Add(new MenuPageItem("Appsettings", "settings", typeof(SettingsPage), false));
		}

		private void PageListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
		{
			MenuPageItem selected = e.SelectedItem as MenuPageItem;
			int selectedListPosition = menuList.IndexOf(selected);

			var lastItem = menuList[lastSelectedIndex];

			menuList[lastSelectedIndex] = new MenuPageItem(lastItem.Title, lastItem.IconNameWithoutColor, lastItem.TargetType, false);
			menuList[selectedListPosition] = new MenuPageItem(selected.Title, selected.IconNameWithoutColor, selected.TargetType, true);

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
