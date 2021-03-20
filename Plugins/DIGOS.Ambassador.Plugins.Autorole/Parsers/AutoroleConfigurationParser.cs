//
//  AutoroleConfigurationParser.cs
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

using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Parsers
{
    /// <summary>
    /// A type parser for autorole configurations.
    /// </summary>
    public class AutoroleConfigurationParser : AbstractTypeParser<AutoroleConfiguration>
    {
        private readonly AutoroleService _autoroles;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleConfigurationParser"/> class.
        /// </summary>
        /// <param name="autoroles">The autorole service.</param>
        public AutoroleConfigurationParser(AutoroleService autoroles)
        {
            _autoroles = autoroles;
        }

        /// <inheritdoc />
        public override async ValueTask<Result<AutoroleConfiguration>> TryParse(string value, CancellationToken ct)
        {
            if (!Snowflake.TryParse(value, out var roleID))
            {
                return new ParsingError<AutoroleConfiguration>(value);
            }

            var getAutorole = await _autoroles.GetAutoroleAsync(roleID.Value, ct);

            return !getAutorole.IsSuccess
                ? Result<AutoroleConfiguration>.FromError(getAutorole)
                : getAutorole.Entity;
        }
    }
}
