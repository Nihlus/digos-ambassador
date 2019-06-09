//
//  RoleplayServiceTestBase.cs
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

using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Users;
using Discord.Commands;

namespace DIGOS.Ambassador.Tests.TestBases
{
    /// <summary>
    /// Serves as a test base for roleplay service tests.
    /// </summary>
    public class RoleplayServiceTestBase
    {
        /// <summary>
        /// Gets the roleplay service object.
        /// </summary>
        protected RoleplayService Roleplays { get; }

        /// <summary>
        /// Gets the command service dependency.
        /// </summary>
        protected CommandService Commands { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayServiceTestBase"/> class.
        /// </summary>
        protected RoleplayServiceTestBase()
        {
            this.Commands = new CommandService();
            this.Roleplays = new RoleplayService(this.Commands, new OwnedEntityService(), new UserService());
        }
    }
}
