using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ApiAiSDK;
using Newtonsoft.Json;
using MPDApp.Models;

namespace MPDApp.Services
{
	public class AIEntityService
	{
		public readonly string baseURI = "https://api.dialogflow.com/v1/userEntities";
		private string protocolVersion;
		private string sessionID;
		private HttpClient client;

		public AIEntityService(AIConfiguration config)
		{
			this.sessionID = config.SessionId;
			protocolVersion = config.ProtocolVersion;
			
			client = new HttpClient();
			client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", config.ClientAccessToken);
		}

		public async Task<HttpResponseMessage> PostEntity(DialogEntityRequest req)
		{
			if (req.SessionId != sessionID)
			{
				sessionID = req.SessionId;
			}

			string uri = CreatePostURI(req);
			string content = JsonConvert.SerializeObject(req, Formatting.Indented);

			var httpContent = new StringContent(content, Encoding.UTF8, "application/json");
			var httpResponse = await client.PostAsync(uri, httpContent);

			if (!httpResponse.IsSuccessStatusCode)
			{
				System.Diagnostics.Debug.WriteLine("Entity Post Request not succesfull");
			}

			return httpResponse;
		}

		public async Task<DialogEntityResponse> GetEntity(string name)
		{
			string uri = CreateGetURI(name);
			string responseJSON = await client.GetStringAsync(uri);
			DialogEntityResponse result = JsonConvert.DeserializeObject<DialogEntityResponse>(responseJSON);
			return result;
		}

		private string CreatePostURI(DialogEntityRequest req)
		{
			return String.Format("{0}?v={1}&sessionId={2}", baseURI,
				protocolVersion, sessionID);
		}

		private string CreateGetURI(string name)
		{
			return String.Format("{0}/{1}?v={2}&sessionId={3}", baseURI, name,
				protocolVersion, sessionID);
		}
	}
}
