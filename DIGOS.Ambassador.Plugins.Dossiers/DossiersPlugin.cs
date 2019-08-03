//
//  DossiersPlugin.cs
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

using DIGOS.Ambassador.Plugins.Abstractions;

namespace DIGOS.Ambassador.Plugins.Dossiers
{
    /// <summary>
    /// Describes the Dossiers plugin.
    /// </summary>
    public class DossiersPlugin : PluginDescriptor
    {
        /// <inheritdoc />
        public override string Name => "Dossiers";

        /// <inheritdoc />
        public override string Description => "Provides a way to store and view dossiers about DIGOS units.";
    }
}
