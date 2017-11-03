namespace DIGOS.Ambassador.Database.UserInfo
{
	/// <summary>
	/// Represents a user's kink, along with their preference for it.
	/// </summary>
	public class UserKink
	{
		/// <summary>
		/// Gets or sets the kink.
		/// </summary>
		public Kink Kink { get; set; }

		/// <summary>
		/// Gets or sets the user's preference for the kink.
		/// </summary>
		public KinkPreference Preference { get; set; }
	}
}