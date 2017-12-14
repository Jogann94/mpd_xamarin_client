﻿using System;
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
				if(speechHelper == null)
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

		private Task<AIResponse> SendAIRequest(string text)
		{
			if (ai == null)
			{
				var config = new AIConfiguration("157d22de6510452cbfbcdb85e3215a5b", SupportedLanguage.English);
				ai = new ApiAi(config);
			}

			return Task.Factory.StartNew(() => ai.TextRequest(text));
		}

		private void SpeechOutput(string text)
		{
			Device.BeginInvokeOnMainThread(
				() => ResponseText = text);

			speechHelper.TextToSpeach(text);
		}
	}
}