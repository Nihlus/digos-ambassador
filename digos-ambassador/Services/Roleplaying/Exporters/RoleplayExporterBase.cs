//
//  RoleplayExporterBase.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System.Threading.Tasks;

using DIGOS.Ambassador.Database.Roleplaying;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services.Exporters
{
	/// <summary>
	/// Base class for roleplay exporters.
	/// </summary>
	public abstract class RoleplayExporterBase : IRoleplayExporter
	{
		/// <summary>
		/// Gets the context of the export operation.
		/// </summary>
		protected ICommandContext Context { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RoleplayExporterBase"/> class.
		/// </summary>
		/// <param name="context">The context of the export operation.</param>
		protected RoleplayExporterBase(ICommandContext context)
		{
			this.Context = context;
		}

		/// <inheritdoc />
		[ItemNotNull]
		public abstract Task<ExportedRoleplay> ExportAsync(Roleplay roleplay);
	}
}
