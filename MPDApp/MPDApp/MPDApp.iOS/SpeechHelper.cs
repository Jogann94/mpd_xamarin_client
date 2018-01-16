using System;
using System.Collections.Generic;
using System.Text;
using MPDApp.DependencyServices;
using AVFoundation;
using Speech;
using Foundation;
using Xamarin.Forms;
using MPDApp.iOS;
using UIKit;
using System.Timers;

[assembly: Dependency(typeof(SpeechHelper))]
namespace MPDApp.iOS
{
	public class SpeechHelper : ISpeechHelper
	{
		public event Action<string> Recorded;

		private bool recording = false;
		private AVAudioEngine audioEngine = new AVAudioEngine();
		private SFSpeechRecognizer speechRecognizer;
		private SFSpeechAudioBufferRecognitionRequest liveSpeechRequest;
		private AVAudioInputNode node;
		private string lastSpokenString;
		private SFSpeechRecognitionTask RecognitionTask;

		public void RecordSpeachToText()
		{

			if (SFSpeechRecognizer.AuthorizationStatus == SFSpeechRecognizerAuthorizationStatus.Authorized)
			{
				StartSpeechRecognizer();

			}
			else
			{
				SFSpeechRecognizer.RequestAuthorization((SFSpeechRecognizerAuthorizationStatus status) =>
				{
					if (status == SFSpeechRecognizerAuthorizationStatus.Authorized)
					{
						StartSpeechRecognizer();
					}
					else // No Permission to recognize Speech
					{
						var alert = UIAlertController.Create("No Permission",
							"Permission for Audio Recording denied", UIAlertControllerStyle.Alert);
						alert.AddAction(UIAlertAction.Create("Ok", UIAlertActionStyle.Cancel, null));
						UIApplication.SharedApplication.KeyWindow
							.RootViewController.PresentViewController(alert, true, null);
					}
				});
			}

		}

		private void StartSpeechRecognizer()
		{


			if (!recording)
			{
				speechRecognizer = new SFSpeechRecognizer();
				node = audioEngine.InputNode;
				var recordingFormat = node.GetBusOutputFormat(0);
				liveSpeechRequest = new SFSpeechAudioBufferRecognitionRequest();

				node.InstallTapOnBus(0, 1024, recordingFormat,
					(AVAudioPcmBuffer buffer, AVAudioTime when) =>
					{
						liveSpeechRequest.Append(buffer);
					});
				recording = true;

				audioEngine.Prepare();
				audioEngine.StartAndReturnError(out NSError error);
				if (error != null)
				{
					return;
				}

				Timer timer = new Timer(2000);
				timer.Start();
				timer.Elapsed += EndRecognition;
				RecognitionTask = speechRecognizer.GetRecognitionTask(liveSpeechRequest,
					(SFSpeechRecognitionResult result, NSError err) =>
					{
						if (err != null)
						{
							Recorded?.Invoke("");
							return;
						}
						else
						{
							lastSpokenString = result.BestTranscription.FormattedString;
							timer.Stop();
							timer.Interval = 2000;
							timer.Start();
						}
					});
			}
			else
			{
				Recorded?.Invoke("");
			}
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

		private void EndRecognition(object sender, ElapsedEventArgs e)
		{
			node.RemoveTapOnBus(0);
			audioEngine.Stop();
			liveSpeechRequest.EndAudio();
			RecognitionTask.Cancel();
			recording = false;
			Recorded?.Invoke(lastSpokenString);
		}
	}

}
