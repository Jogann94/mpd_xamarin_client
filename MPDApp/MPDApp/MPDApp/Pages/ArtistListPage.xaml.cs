using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPDApp.Speech;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ApiAiSDK;
using System.IO;
using ApiAiSDK.Util;
using ApiAiSDK.Model;
namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ArtistListPage : ContentPage, INotifyPropertyChanged
	{
		public string ToSpeak { get; set; }
		public string speechInputText;
		public string SpeechInputText
		{
			get
			{
				return speechInputText;
			}
			set
			{
				if (speechInputText != value)
				{
					speechInputText = value;
					OnPropertyChanged("SpeechInputText");
				}
			}
		}

		private ApiAi ai;
		private bool IsRecording { get; set; }
		private ISpeechHelper speechHelper;

		public ArtistListPage()
		{
			InitializeComponent();
			speechHelper = DependencyService.Get<ISpeechHelper>();
			SpeechInputText = "Test";

			BindingContext = this;
		}

		private void TextToSpeechButton_Clicked(object sender, EventArgs e)
		{
			speechHelper.TextToSpeach(ToSpeak);
		}

		private void RecordButton_Clicked(object sender, EventArgs e)
		{
			if (!IsRecording)
			{
				IsRecording = true;
				speechHelper.Recorded += RecordedListener;
				speechHelper.RecordSpeachToText();
			}

		}

		private async void RecordedListener(string text)
		{
			speechHelper.Recorded -= RecordedListener;
			IsRecording = false;

			if (text != string.Empty && text != null)
			{
				var response = await SendAIRequest(text);

				if (response != null)
				{
					Device.BeginInvokeOnMainThread(
						() => SpeechInputText = response.Result.Fulfillment.Speech);

					speechHelper.TextToSpeach(response.Result.Fulfillment.Speech);
				}

			}
		}

		private async void SendToBot_Clicked(object sender, EventArgs e)
		{

			var response = await SendAIRequest(ToSpeak);

			SpeechInputText = response.Result.Fulfillment.Speech;

		}

		private async Task<AIResponse> SendAIRequest(string text)
		{

			var response = await Task.Factory.StartNew(() =>
			{
				if (ai == null)
				{
					var config = new AIConfiguration("157d22de6510452cbfbcdb85e3215a5b", SupportedLanguage.German);
					ai = new ApiAi(config);
				}
				var res = ai.TextRequest(text);

				return res;
			});

			return response;
		}

	}
}