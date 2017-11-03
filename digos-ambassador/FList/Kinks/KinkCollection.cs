using System.Collections.Generic;
using Newtonsoft.Json;

namespace DIGOS.Ambassador.FList.Kinks
{
	public class KinkCollection
	{
		[JsonProperty("error")]
		public string Error { get; set; }

		[JsonProperty("kinks")]
		public Dictionary<string, FListKinkCategory> KinkCategories { get; set; }

		public static KinkCollection FromJson(string json)
		{
			var settings = new JsonSerializerSettings
			{
				MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
				DateParseHandling = DateParseHandling.None,
			};

			return JsonConvert.DeserializeObject<KinkCollection>(json, settings);
		}
	}
}
