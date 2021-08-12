//
//  ColourTypeParser.cs
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
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Transformations.TypeParsers
{
    /// <summary>
    /// Reads colours as command arguments.
    /// </summary>
    public sealed class ColourTypeParser : AbstractTypeParser<Colour>
    {
        /// <inheritdoc />
        public override ValueTask<Result<Colour>> TryParse(string value, CancellationToken ct)
        {
            value = value.Trim();

            return Colour.TryParse(value, out var colour)
                ? new ValueTask<Result<Colour>>(colour)
                : new ValueTask<Result<Colour>>(new ParsingError<Colour>(value));
        }
    }
}
