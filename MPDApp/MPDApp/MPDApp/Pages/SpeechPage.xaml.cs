using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MPDApp.DependencyServices;
using MPDApp.Services;
using ApiAiSDK;
using ApiAiSDK.Model;
using MPDApp.Models;
using MPDProtocol;
using MPDProtocol.MPDDataobjects;
using System.Net.Http;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SpeechPage : ContentPage
	{
		private string speechInput;
		public string SpeechInput
		{
			get { return speechInput; }
			set
			{
				OnPropertyChanged("SpeechInput");
				speechInput = value;
			}
		}
		private string responseText;
		public string ResponseText
		{
			get { return responseText; }
			set
			{
				OnPropertyChanged("ResponseText");
				responseText = value;
			}
		}

		private ISpeechHelper speechHelper;
		private bool isRecording;
		private ApiAi ai;

		public SpeechPage()
		{
			InitializeComponent();

			BindingContext = this;
			SpeechInput = "No Input";
			ResponseText = "No Response";
		}

		private void RecordButton_Clicked(object sender, EventArgs e)
		{
			if (!isRecording)
			{
				if (speechHelper == null)
				{
					try
					{
						speechHelper = DependencyService.Get<ISpeechHelper>();
					}
					catch (Exception)
					{
						DisplayAlert("Error", "No Speech Service implemented", "ok");
					}
				}
				isRecording = true;
				speechHelper.Recorded += RecordedListener;
				speechHelper.RecordSpeachToText();
			}
		}

		private void RecordedListener(string text)
		{
			speechHelper.Recorded -= RecordedListener;
			isRecording = false;

			if (text != string.Empty && text != null)
			{
				Device.BeginInvokeOnMainThread(() => { SpeechInput = text; });
				Task.Factory.StartNew(async () =>
				{
					var response = await SendAIRequest(text);
					if (response != null)
					{
						AIFullfillment fullfillment = new AIFullfillment(response);
						fullfillment.OnActionFullfilled += SpeechOutput;
						fullfillment.FullfillAIResponse();
					}
				});
			}
			else
			{
				isRecording = false;
				SpeechInput = "No Text recognized";
			}
		}

		private async Task<AIResponse> SendAIRequest(string text)
		{
			if (ai == null)
			{
				ai = new ApiAi(App.AIConfig);
				var response = ai.TextRequest("HI");
				App.AIConfig.SessionId = response.SessionId;
				await ConfigureAIEntities();

			}

			return ai.TextRequest(text);
		}

		private void SpeechOutput(string text)
		{
			Device.BeginInvokeOnMainThread(
				() => ResponseText = text);

			speechHelper.TextToSpeach(text);
		}

		private async Task<HttpResponseMessage> ConfigureAIEntities()
		{
			AIEntityService entityService = new AIEntityService(App.AIConfig);
			DialogEntityRequest req = new DialogEntityRequest()
			{ SessionId = App.AIConfig.SessionId };

			req.entities.Add(GeneratePlaylistEntity());
			req.entities.Add(GenerateArtistEntity());
			var response = await entityService.PostEntity(req);
			return response;

		}

		private Entity GeneratePlaylistEntity()
		{
			Entity playlistEntity = new Entity() { Name = "Playlist" };

			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
			{
				var playlistCollection = con.GetPlaylists().Cast<MPDPlaylist>().ToList();
				foreach (var list in playlistCollection)
				{
					playlistEntity.AddEntry(
						new EntityEntry(list.Name, new string[] { list.Name }));
				}
			}

			return playlistEntity;
		}

		private Entity GenerateArtistEntity()
		{
			Entity artistEntity = new Entity() { Name = "Artists" };

			var con = MPDConnection.GetInstance();
			if (con.IsConnected())
			{
				List<MPDArtist> artistList = con.GetArtists();
				foreach (var artist in artistList)
				{
					artistEntity.AddEntry(
						new EntityEntry(artist.ArtistName, new string[] { artist.ArtistName }));
				}
			}

			return artistEntity;
		}
	}
}