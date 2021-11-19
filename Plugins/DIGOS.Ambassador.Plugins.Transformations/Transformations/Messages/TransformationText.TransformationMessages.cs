//
//  TransformationText.TransformationMessages.cs
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
    /// <summary>
    /// Holds transformation messages.
    /// </summary>
    public sealed partial class TransformationMessages
    {
        /// <summary>
        /// Gets a set of addition messages. These are used when something that did not previously exist is added to
        /// an appearance.
        /// </summary>
        public AddingMessages Adding { get; init; } = new();

        /// <summary>
        /// Gets a set of removal messages. These are used when something that exists is removed from an appearance.
        /// </summary>
        public RemovalMessages Removal { get; init; } = new();

        /// <summary>
        /// Gets a set of shifting messages. These are used when something that exists is transformed into something
        /// else.
        /// </summary>
        public ShiftingMessages Shifting { get; init; } = new();
    }
}
