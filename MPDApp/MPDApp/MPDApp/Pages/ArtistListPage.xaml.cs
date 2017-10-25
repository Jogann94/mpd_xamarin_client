using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPDApp.Speech;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ApiAiSDK;
using System.IO;

namespace MPDApp.Pages
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ArtistListPage : ContentPage
	{
		public string ToSpeak { get; set; }
		public string SpeechInputText { get; set; }

		private bool IsRecording { get; set; }
		private ISpeechHelper speachHelper;

		public ArtistListPage()
		{
			var config = new AIConfiguration("YOUR_CLIENT_ACCESS_TOKEN", SupportedLanguage.English);
			var apiAi = new ApiAi(config);

			BindingContext = this;
			speachHelper = DependencyService.Get<ISpeechHelper>();
		}

		private void TextToSpeechButton_Clicked(object sender, EventArgs e)
		{
			speachHelper.TextToSpeach(ToSpeak);
		}

		private void RecordButton_Clicked(object sender, EventArgs e)
		{
			if (!IsRecording)
			{
				speachHelper.Recorded += RecordedListener;
				speachHelper.RecordSpeachToText();
			}
		}

		private void RecordedListener(string text)
		{
			
			SpeechInputText = text;
			speachHelper.Recorded -= RecordedListener;
		}

	}
}