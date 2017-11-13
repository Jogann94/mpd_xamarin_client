using Xamarin.Forms;
using MPDApp.DependencyServices;
using System;
using MPDApp.Droid;
using Android.Speech.Tts;
using Android.Speech;
using Android.Runtime;
using Android.OS;
using Android.Content;
using Android.App;
using Xamarin.Forms.Platform.Android;

[assembly: Dependency(typeof(SpeechHelper))]
namespace MPDApp.Droid
{
	public class SpeechHelper : Java.Lang.Object, ISpeechHelper, TextToSpeech.IOnInitListener
	{
		TextToSpeech speaker;
		string toSpeak;

		public const int VOICE = 10;

		public SpeechHelper() { }

		public event Action<string> Recorded;

		public void OnInit([GeneratedEnum] OperationResult status)
		{
			if (status.Equals(OperationResult.Success))
			{
				speaker.Speak(toSpeak, QueueMode.Flush, null, null);
			}
		}

		public void RecordSpeachToText()
		{
			if(CheckPermissions())
			{
				var voiceIntent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
				voiceIntent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);

				voiceIntent.PutExtra(RecognizerIntent.ExtraPrompt, "Speak now");
				voiceIntent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, 1500);
				voiceIntent.PutExtra(RecognizerIntent.ExtraSpeechInputPossiblyCompleteSilenceLengthMillis, 1500);
				voiceIntent.PutExtra(RecognizerIntent.ExtraSpeechInputMinimumLengthMillis, 1500);
				voiceIntent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);

				voiceIntent.PutExtra(RecognizerIntent.ExtraLanguage, Java.Util.Locale.Default);

				var activity = Forms.Context as MainActivity;
				activity.SpeechActivityResult += OnActivityResult;
				activity.StartActivityForResult(voiceIntent, VOICE);
			}

		}

		public bool CheckPermissions()
		{
			string rec = Android.Content.PM.PackageManager.FeatureMicrophone;
			if(rec == "android.hardware.microphone")
			{
				return true;
			}
			else
			{
				var alert = new AlertDialog.Builder(Forms.Context);
				alert.SetTitle("You have no rights to use the microphone");
				alert.SetPositiveButton("OK", (sender, e) =>
				{
					return;
				});

				return false;
			}
		}

		public void TextToSpeach(string text)
		{
			toSpeak = text;

			if (speaker == null)
			{
				speaker = new TextToSpeech(Forms.Context, this);
			}
			else
			{
				speaker.Speak(toSpeak, QueueMode.Flush, null, null);
			}
		}

		public void OnActivityResult(Result result, Intent data)
		{
			if(result == Result.Ok)
			{
				var matches = data.GetStringArrayListExtra(RecognizerIntent.ExtraResults);
				string text;
				if (matches.Count != 0)
				{
					text = matches[0];
				}
				else
				{
					text = "No speech was recognised";
				}

				Recorded?.Invoke(text);
				var activity = Forms.Context as MainActivity;
				activity.SpeechActivityResult -= OnActivityResult;
			}
		}
		
		
	}
}

