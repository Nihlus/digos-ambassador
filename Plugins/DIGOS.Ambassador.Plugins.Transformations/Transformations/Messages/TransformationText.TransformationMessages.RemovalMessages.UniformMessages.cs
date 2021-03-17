//
//  TransformationText.TransformationMessages.RemovalMessages.UniformMessages.cs
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

using System.Collections.Generic;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Messages
{
    public sealed partial class TransformationText
    {
        public sealed partial class TransformationMessages
        {
            public sealed partial class RemovalMessages
            {
                /// <summary>
                /// Holds uniform messages.
                /// </summary>
                public sealed class UniformMessages
                {
                    /// <summary>
                    /// Gets a list of uniform ear removal messages.
                    /// </summary>
                    public IReadOnlyList<string> Ears { get; init; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform eye removal messages.
                    /// </summary>
                    public IReadOnlyList<string> Eyes { get; init; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform leg removal messages.
                    /// </summary>
                    public IReadOnlyList<string> Legs { get; init; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform arm removal messages.
                    /// </summary>
                    public IReadOnlyList<string> Arms { get; init; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform wing removal messages.
                    /// </summary>
                    public IReadOnlyList<string> Wings { get; init; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform pattern removal messages.
                    /// </summary>
                    public IReadOnlyList<string> Pattern { get; init; } = new List<string>();
                }
            }
        }
    }
}
