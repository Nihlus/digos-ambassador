using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.ServerInfo;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Services;

namespace DIGOS.Ambassador.Database.Transformations
{
	/// <summary>
	/// Holds protection data for a specific user on a specific server.
	/// </summary>
	public class ServerUserProtection : IEFEntity
	{
		/// <inheritdoc />
		public uint ID { get; set; }

		/// <summary>
		/// Gets or sets the user that owns this protection data.
		/// </summary>
		public User User { get; set; }

		/// <summary>
		/// Gets or sets the server that this protection data is valid on.
		/// </summary>
		public Server Server { get; set; }

		/// <summary>
		/// Gets or sets the active protection type on this server.
		/// </summary>
		public ProtectionType Type { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the user has opted in to transformations.
		/// </summary>
		public bool HasOptedIn { get; set; }

		/// <summary>
		/// Creates a default server-specific protection object based on the given global protection data.
		/// </summary>
		/// <param name="globalProtection">The global protection data.</param>
		/// <param name="server">The server that the protection should be valid for.</param>
		/// <returns>A server-specific protection object.</returns>
		public static ServerUserProtection CreateDefault(GlobalUserProtection globalProtection, Server server)
		{
			return new ServerUserProtection
			{
				User = globalProtection.User,
				Server = server,
				Type = globalProtection.DefaultType,
				HasOptedIn = false
			};
		}
	}
}
