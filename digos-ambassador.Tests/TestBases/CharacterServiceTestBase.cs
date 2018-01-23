//
//  CharacterServiceTestBase.cs
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
using DIGOS.Ambassador.Services;
using Discord.Commands;
using Xunit;

namespace DIGOS.Ambassador.Tests.TestBases
{
	/// <summary>
	/// Serves as a test base for character service tests.
	/// </summary>
	public abstract class CharacterServiceTestBase : DatabaseDependantTestBase, IAsyncLifetime
	{
		/// <summary>
		/// Gets the character service object.
		/// </summary>
		protected CharacterService Characters { get; }

		/// <summary>
		/// Gets the command service dependency.
		/// </summary>
		protected CommandService Commands { get; }

		private readonly TransformationService Transformations;

		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterServiceTestBase"/> class.
		/// </summary>
		protected CharacterServiceTestBase()
		{
			this.Commands = new CommandService();
			var content = new ContentService();
			this.Transformations = new TransformationService(content);

			this.Characters = new CharacterService(this.Commands, new OwnedEntityService(), content, this.Transformations);
		}

		/// <inheritdoc />
		public virtual async Task InitializeAsync()
		{
			await this.Transformations.UpdateTransformationDatabaseAsync(this.Database);
		}

		/// <inheritdoc />
		public virtual Task DisposeAsync()
		{
			return Task.CompletedTask;
		}
	}
}
