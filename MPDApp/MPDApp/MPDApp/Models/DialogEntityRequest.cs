using System.Collections.Generic;
using ApiAiSDK.Model;

namespace MPDApp.Models
{

	public class DialogEntityRequest
	{
		public IList<Entity> entities { get; set; }
		public string SessionId { get; set; }

		public DialogEntityRequest()
		{
			entities = new List<Entity>();
		}
	}

}
