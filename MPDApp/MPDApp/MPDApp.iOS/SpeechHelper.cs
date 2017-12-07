using System;
using System.Collections.Generic;
using System.Text;
using MPDApp.DependencyServices;
using AVFoundation;
using Speech;
using Foundation;
using Xamarin.Forms;
using MPDApp.iOS;

[assembly: Dependency(typeof(SpeechHelper))]
namespace MPDApp.iOS
{
	public class SpeechHelper : ISpeechHelper
	{
		public event Action<string> Recorded;

		private AVAudioEngine audioEngine = new AVAudioEngine();
		private SFSpeechRecognizer speechRecognizer = new SFSpeechRecognizer();
		private SFSpeechAudioBufferRecognitionRequest liveSpeechRequest
			= new SFSpeechAudioBufferRecognitionRequest();
		private SFSpeechRecognitionTask RecognitionTask;

		public void RecordSpeachToText()
		{
			var node = audioEngine.InputNode;
			var recordingFormat = node.GetBusOutputFormat(0);
			node.InstallTapOnBus(0, 1024, recordingFormat,
				(AVAudioPcmBuffer buffer, AVAudioTime when) =>
				{
					liveSpeechRequest.Append(buffer);
				});

			audioEngine.Prepare();
			audioEngine.StartAndReturnError(out NSError error);

			if (error != null)
			{
				return;
			}

			RecognitionTask = speechRecognizer.GetRecognitionTask(liveSpeechRequest,
				(SFSpeechRecognitionResult result, NSError err) =>
				{
					if (error != null)
					{
						return;
					}
					else
					{
						if (result.Final)
						{
							Recorded?.Invoke(result.BestTranscription.FormattedString);
						}
					}
				});
		}

		public void TextToSpeach(string text)
		{
			var speechSynthesizer = new AVSpeechSynthesizer();
			var speechUtterance = new AVSpeechUtterance(text)
			{
				Rate = AVSpeechUtterance.MaximumSpeechRate / 4,
				Voice = AVSpeechSynthesisVoice.FromLanguage("en-US"),
				Volume = 0.5f,
				PitchMultiplier = 1.0f
			};

			speechSynthesizer.SpeakUtterance(speechUtterance);
		}
	}
}
