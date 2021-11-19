//
//  TransformationText.TransformationMessages.RemovalMessages.SingleMessages.cs
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

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Messages;

public sealed partial class TransformationText
{
    public sealed partial class TransformationMessages
    {
        public sealed partial class RemovalMessages
        {
            /// <summary>
            /// Holds singular messages.
            /// </summary>
            public sealed class SingleMessages
            {
                /// <summary>
                /// Gets a list of hair removal messages.
                /// </summary>
                public IReadOnlyList<string> Hair { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of face removal messages.
                /// </summary>
                public IReadOnlyList<string> Face { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of ear removal messages.
                /// </summary>
                public IReadOnlyList<string> Ear { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of eye removal messages.
                /// </summary>
                public IReadOnlyList<string> Eye { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of teeth removal messages.
                /// </summary>
                public IReadOnlyList<string> Teeth { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of leg removal messages.
                /// </summary>
                public IReadOnlyList<string> Leg { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of arm removal messages.
                /// </summary>
                public IReadOnlyList<string> Arm { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of tail removal messages.
                /// </summary>
                public IReadOnlyList<string> Tail { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of wing removal messages.
                /// </summary>
                public IReadOnlyList<string> Wing { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of penile removal messages.
                /// </summary>
                public IReadOnlyList<string> Penis { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of vaginal removal messages.
                /// </summary>
                public IReadOnlyList<string> Vagina { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of head removal messages.
                /// </summary>
                public IReadOnlyList<string> Head { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of body removal messages.
                /// </summary>
                public IReadOnlyList<string> Body { get; init; } = new List<string>();

                /// <summary>
                /// Gets a list of pattern removal messages.
                /// </summary>
                public IReadOnlyList<string> Pattern { get; init; } = new List<string>();
            }
        }
    }
}
