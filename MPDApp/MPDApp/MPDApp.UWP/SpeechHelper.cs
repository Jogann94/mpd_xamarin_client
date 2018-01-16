using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPDApp.DependencyServices;
using Xamarin.Forms;
using MPDApp.UWP;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml.Controls;

[assembly: Dependency(typeof(SpeechHelper))]
namespace MPDApp.UWP
{
	public class SpeechHelper : ISpeechHelper
	{
		public event Action<string> Recorded;

		public void RecordSpeachToText()
		{
			Task.Factory.StartNew(async () =>
			{
				try
				{
					var language = new Windows.Globalization.Language("en-US");

					var speechRecognizer = new SpeechRecognizer(language);
					await speechRecognizer.CompileConstraintsAsync();
					var result = await speechRecognizer.RecognizeWithUIAsync();

					Recorded?.Invoke(result.Text);
				}
				catch (System.Runtime.InteropServices.COMException)
				{

					Device.BeginInvokeOnMainThread(async () =>
					{
						var messageDialog = new Windows.UI.Popups.MessageDialog("Please download en-US Language-Pack", "Language not found");
						await messageDialog.ShowAsync();
						Recorded?.Invoke("");
					});
				}
				catch
				{
					Device.BeginInvokeOnMainThread(async () =>
					{
						var messageDialog = new Windows.UI.Popups.MessageDialog("No permission to record", "Permission denied");
						await messageDialog.ShowAsync();
						Recorded?.Invoke("");
					});
				}

			});

		}

		public async void TextToSpeach(string text)
		{


			var voice = SpeechSynthesizer.AllVoices.First(a => a.Language.Contains("en"));
			var speechSynthezizer = new SpeechSynthesizer();

			if (voice == null)
			{
				Device.BeginInvokeOnMainThread(async () =>
				{
					var messageDialog = new Windows.UI.Popups.MessageDialog("Please download en-US Language-Pack", "Language not found");
					await messageDialog.ShowAsync();
					Recorded?.Invoke("");
				});
			}
			else
			{
				speechSynthezizer.Voice = voice;
				var stream = await speechSynthezizer.SynthesizeTextToStreamAsync(text);
				Device.BeginInvokeOnMainThread(() =>
				{
					MediaElement e = new MediaElement();
					e.SetSource(stream, stream.ContentType);
					e.Play();
				});

			}

		}
	}
}

