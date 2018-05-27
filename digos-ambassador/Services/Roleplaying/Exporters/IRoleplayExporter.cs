//
//  IRoleplayExporter.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services.Exporters
{
	/// <summary>
	/// Public interface for a class that can export a roleplay to a file.
	/// </summary>
	public interface IRoleplayExporter
	{
		/// <summary>
		/// Exports the given roleplay, handing back an object that wraps the exported data.
		/// </summary>
		/// <param name="roleplay">The roleplay to export.</param>
		/// <returns>An exported roleplay.</returns>
		[NotNull, Pure]
		Task<ExportedRoleplay> ExportAsync([NotNull] Roleplay roleplay);
	}
}
