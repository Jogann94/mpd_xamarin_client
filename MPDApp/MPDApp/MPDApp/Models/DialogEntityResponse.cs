using ApiAiSDK.Model;
using System.Collections.Generic;

namespace MPDApp.Models
{
	public class DialogEntityResponse
	{
		public string SessionId { get; set; }
		public bool Extend { get; set; }
		public EntityEntry[] Entries { get; set; }
		public string Name { get; set; }
		public bool IsOverridable { get; set; }
		public bool IsEnum { get; set; }
		public bool AutomatedExpansion { get; set; }

		public Entity ConvertToEntity()
		{
			var result = new Entity() { Name = Name };
			result.Entries = new List<EntityEntry>(Entries);

			return result;
		}
	}

}
