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

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SpeechPage : ContentPage
	{
		private string speechInput;
		public string SpeechInput
		{
			get { return responseText; }
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
				if(speechHelper == null)
				{
					speechHelper = DependencyService.Get<ISpeechHelper>();
					if(speechHelper == null)
					{
						throw new NotImplementedException();
					}
				}
				isRecording = true;
				speechHelper.Recorded += RecordedListener;
				speechHelper.RecordSpeachToText();
			}
		}

		private async void RecordedListener(string text)
		{
			speechHelper.Recorded -= RecordedListener;
			isRecording = false;

			if (text != string.Empty && text != null)
			{
				SpeechInput = text;
				var response = await SendAIRequest(text);

				if (response != null)
				{
					AIFullfillment fullfillment = new AIFullfillment(response);
					fullfillment.OnActionFullfilled += SpeechOutput;
					fullfillment.FullfillAIResponse();
				}
			}
			else
			{
				SpeechInput = "No Text recognized";
			}
		}

		private async Task<AIResponse> SendAIRequest(string text)
		{
			var response = await Task.Factory.StartNew(() =>
			{
				if (ai == null)
				{
					var config = new AIConfiguration("157d22de6510452cbfbcdb85e3215a5b", SupportedLanguage.English);
					ai = new ApiAi(config);
				}
				var res = ai.TextRequest(text);

				return res;
			});

			return response;
		}

		private void SpeechOutput(string text)
		{
			Device.BeginInvokeOnMainThread(
				() => ResponseText = text);

			speechHelper.TextToSpeach(text);
		}
	}
}