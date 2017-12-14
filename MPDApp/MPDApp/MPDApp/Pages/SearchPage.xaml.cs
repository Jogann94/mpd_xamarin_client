using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MPDProtocol;
using MPDProtocol.MPDDataobjects;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SearchPage : ContentPage
	{
		private Dictionary<string, MPDCommands.MPD_SEARCH_TYPE> nameToSearchType =
			new Dictionary<string, MPDCommands.MPD_SEARCH_TYPE>
			{
				{"Album", MPDCommands.MPD_SEARCH_TYPE.MPD_SEARCH_ALBUM },
				{"Any", MPDCommands.MPD_SEARCH_TYPE.MPD_SEARCH_ANY },
				{"Artist", MPDCommands.MPD_SEARCH_TYPE.MPD_SEARCH_ARTIST },
				{"File", MPDCommands.MPD_SEARCH_TYPE.MPD_SEARCH_FILE },
				{"Track", MPDCommands.MPD_SEARCH_TYPE.MPD_SEARCH_TRACK },
			};

		private List<MPDTrack> searchResultList;
		public List<MPDTrack> SearchResultList
		{
			get { return searchResultList; }
			set { searchResultList = value; OnPropertyChanged("SearchResultList"); }
		}

		private string lastSearchTerm = "";
		private MPDCommands.MPD_SEARCH_TYPE lastSearchType = MPDCommands.MPD_SEARCH_TYPE.MPD_SEARCH_ANY;

		private MPDCommands.MPD_SEARCH_TYPE currentType = MPDCommands.MPD_SEARCH_TYPE.MPD_SEARCH_ANY;

		public SearchPage()
		{
			BindingContext = this;
			InitializeComponent();

			foreach (string s in nameToSearchType.Keys)
			{
				TypePicker.Items.Add(s);
			}
			TypePicker.SelectedIndex = 1;
			TypePicker.SelectedIndexChanged += (sender, args) =>
			{
				string typeName = TypePicker.Items[TypePicker.SelectedIndex];
				currentType = nameToSearchType[typeName];
			};
		}

		public async void Search_Clicked(object sender, EventArgs e)
		{
			var con = MPDConnection.GetInstance();

			if (con.IsConnected())
			{
				List<MPDFileEntry> list = await Task.Factory.StartNew(() =>
				{
					List<MPDFileEntry> newList = con.GetSearchedFiles(SearchEntry.Text, currentType);
					lastSearchTerm = SearchEntry.Text;
					lastSearchType = currentType;
					return newList;
				});

				if (list.Count != 0)
				{
					SearchResultList = list.Cast<MPDTrack>().ToList();
				}
				else
				{
					SearchResultList = new List<MPDTrack>();
					await DisplayAlert("Nothing found", "No files found for your search term", "ok");
				}

			}
		}

		public void Add_Clicked(object sender, EventArgs e)
		{
			var con = MPDConnection.GetInstance();

			if (con.IsConnected())
			{
				if (TrackListView.SelectedItem is MPDTrack track)
				{
					Task.Factory.StartNew(() => con.AddSong(track.Path));
				}
			}
		}

		public void AddAll_Clicked(object sender, EventArgs e)
		{
			var con = MPDConnection.GetInstance();

			if (con.IsConnected())
			{
				if (lastSearchTerm != "")
				{
					Task.Factory.StartNew(() =>
						con.AddSearchedFiles(lastSearchTerm, lastSearchType));
				}
			}
		}
	}
}