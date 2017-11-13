using System;
using System.Net;
using ApiAiSDK;
using ApiAiSDK.Model;
using ApiAiSDK.Util;

namespace MPDApp.Services
{
	public class AIRestClient
	{
		public readonly string devAccessKey;

		/// <summary>
		/// Uses the DeveloperAccesKey for REST-Communication with
		/// an Dialogflow-Bot
		/// </summary>
		/// <param name="devAccessKey"></param>
		public AIRestClient(string devAccessKey)
		{
			this.devAccessKey = devAccessKey;
		}
		
		

	}
}