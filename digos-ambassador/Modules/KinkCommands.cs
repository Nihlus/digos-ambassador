//
//  DossierCommands.cs
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
using Discord.Commands;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
	/// <summary>
	/// Commands for viewing and configuring user kinks.
	/// </summary>
	[Group("kink")]
	[Summary("Commands for viewing and configuring user kinks.")]
	public class KinkCommands : ModuleBase<SocketCommandContext>
	{
		public async Task ShowKinkAsync()
		{

		}

		public async Task ShowKinkOverlap()
		{

		}

		public async Task ShowKinksByPreferenceAsync()
		{

		}

		public async Task SetKinkPreferenceAsync()
		{

		}

		public async Task RunKinkWizardAsync()
		{

		}
	}
}