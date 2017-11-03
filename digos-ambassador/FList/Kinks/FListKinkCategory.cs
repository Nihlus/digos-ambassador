using Newtonsoft.Json;

namespace DIGOS.Ambassador.FList.Kinks
{
	public class FListKinkCategory
	{
		[JsonProperty("group")]
		public string Group { get; set; }

		[JsonProperty("items")]
		public FListKink[] Kinks { get; set; }
	}
}
