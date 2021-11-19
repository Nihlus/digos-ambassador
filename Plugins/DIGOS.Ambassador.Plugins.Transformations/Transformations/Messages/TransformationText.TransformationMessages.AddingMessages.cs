//
//  TransformationText.TransformationMessages.AddingMessages.cs
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

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Messages;

public sealed partial class TransformationText
{
    public sealed partial class TransformationMessages
    {
        /// <summary>
        /// Holds addition messages.
        /// </summary>
        public sealed partial class AddingMessages
        {
            /// <summary>
            /// Gets a set of singular messages. These are used when a single part is added.
            /// </summary>
            public SingleMessages Single { get; init; } = new();

            /// <summary>
            /// Gets a set of uniform messages. These are used when two or more matching parts are added.
            /// </summary>
            public UniformMessages Uniform { get; init; } = new();
        }
    }
}
