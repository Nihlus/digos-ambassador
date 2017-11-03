using Newtonsoft.Json;

namespace DIGOS.Ambassador.FList.Kinks
{
	public class FListKink
	{
		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("kink_id")]
		public uint KinkId { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
	}
}
