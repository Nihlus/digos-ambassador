//
//  TransformationText.cs
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
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Messages
{
    /// <summary>
    /// Database class for various data-driven transformation messages.
    /// </summary>
    public sealed class TransformationText
    {
        /// <summary>
        /// Gets a set of description messages.
        /// </summary>
        [NotNull]
        [JsonProperty("descriptions", Required = Required.Always)]
        public DescriptionMessages Descriptions { get; private set; } = new DescriptionMessages();

        /// <summary>
        /// Gets a set of transformation messages.
        /// </summary>
        [NotNull]
        [JsonProperty("messages", Required = Required.Always)]
        public TransformationMessages Messages { get; private set; } = new TransformationMessages();

        /// <summary>
        /// Attempts to deserialize a <see cref="TransformationText"/> instance from the given JSON text.
        /// </summary>
        /// <param name="json">The JSON text.</param>
        /// <param name="text">The deserialized database.</param>
        /// <returns>true if the deserialization was successful; otherwise, false.</returns>
        [Pure]
        [ContractAnnotation("=> true, text : notnull; => false, text : null")]
        public static bool TryDeserialize([NotNull] string json, out TransformationText? text)
        {
            text = null;
            try
            {
                text = JsonConvert.DeserializeObject<TransformationText>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Holds description messages.
        /// </summary>
        public sealed class DescriptionMessages
        {
            /// <summary>
            /// Gets a list of sex and species descriptions. These are used in the beginning of appearance descriptions.
            /// </summary>
            [NotNull, ItemNotNull]
            [JsonProperty("sexSpecies", Required = Required.Always)]
            public IReadOnlyList<string> SexSpecies { get; private set; } = new List<string>();

            /// <summary>
            /// Gets a set of singular descriptions.
            /// </summary>
            [NotNull]
            [JsonProperty("single", Required = Required.Always)]
            public SingleDescriptions Single { get; private set; } = new SingleDescriptions();

            /// <summary>
            /// Gets a set of uniform descriptions.
            /// </summary>
            [NotNull]
            [JsonProperty("uniform", Required = Required.Always)]
            public UniformDescriptions Uniform { get; private set; } = new UniformDescriptions();

            /// <summary>
            /// Holds singular descriptions.
            /// </summary>
            public sealed class SingleDescriptions
            {
                /// <summary>
                /// Gets a list of pattern descriptions. These are used when describing patterns on bodyparts.
                /// </summary>
                [NotNull, ItemNotNull]
                [JsonProperty("pattern", Required = Required.Always)]
                public IReadOnlyList<string> Pattern { get; private set; } = new List<string>();
            }

            /// <summary>
            /// Holds uniform descriptions.
            /// </summary>
            public sealed class UniformDescriptions
            {
                /// <summary>
                /// Gets a list of pattern descriptions. These are used when describing patterns on bodyparts.
                /// </summary>
                [NotNull, ItemNotNull]
                [JsonProperty("pattern", Required = Required.Always)]
                public IReadOnlyList<string> Pattern { get; private set; } = new List<string>();
            }
        }

        /// <summary>
        /// Holds transformation messages.
        /// </summary>
        public sealed class TransformationMessages
        {
            /// <summary>
            /// Gets a set of addition messages. These are used when something that did not previously exist is added to
            /// an appearance.
            /// </summary>
            [NotNull]
            [JsonProperty("adding", Required = Required.Always)]
            public AddingMessages Adding { get; private set; } = new AddingMessages();

            /// <summary>
            /// Gets a set of removal messages. These are used when something that exists is removed from an appearance.
            /// </summary>
            [NotNull]
            [JsonProperty("removal", Required = Required.Always)]
            public RemovalMessages Removal { get; private set; } = new RemovalMessages();

            /// <summary>
            /// Gets a set of shifting messages. These are used when something that exists is transformed into something
            /// else.
            /// </summary>
            [NotNull]
            [JsonProperty("shifting", Required = Required.Always)]
            public ShiftingMessages Shifting { get; private set; } = new ShiftingMessages();

            /// <summary>
            /// Holds addition messages.
            /// </summary>
            public sealed class AddingMessages
            {
                /// <summary>
                /// Gets a set of singular messages. These are used when a single part is added.
                /// </summary>
                [NotNull]
                [JsonProperty("single", Required = Required.Always)]
                public SingleMessages Single { get; private set; } = new SingleMessages();

                /// <summary>
                /// Gets a set of uniform messages. These are used when two or more matching parts are added.
                /// </summary>
                [NotNull]
                [JsonProperty("uniform", Required = Required.Always)]
                public UniformMessages Uniform { get; private set; } = new UniformMessages();

                /// <summary>
                /// Holds singular messages.
                /// </summary>
                public sealed class SingleMessages
                {
                    /// <summary>
                    /// Gets a list of pattern addition messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("pattern", Required = Required.Always)]
                    public IReadOnlyList<string> Pattern { get; private set; } = new List<string>();
                }

                /// <summary>
                /// Holds uniform messages.
                /// </summary>
                public sealed class UniformMessages
                {
                    /// <summary>
                    /// Gets a list of pattern addition messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("pattern", Required = Required.Always)]
                    public IReadOnlyList<string> Pattern { get; private set; } = new List<string>();
                }
            }

            /// <summary>
            /// Holds removal messages.
            /// </summary>
            public sealed class RemovalMessages
            {
                /// <summary>
                /// Gets a set of singular messages. These are used when a single part is removed.
                /// </summary>
                [NotNull]
                [JsonProperty("single", Required = Required.Always)]
                public SingleMessages Single { get; private set; } = new SingleMessages();

                /// <summary>
                /// Gets a set of uniform messages. These are used when two or more matching parts are added.
                /// </summary>
                [NotNull]
                [JsonProperty("uniform", Required = Required.Always)]
                public UniformMessages Uniform { get; private set; } = new UniformMessages();

                /// <summary>
                /// Holds singular messages.
                /// </summary>
                public sealed class SingleMessages
                {
                    /// <summary>
                    /// Gets a list of hair removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("hair", Required = Required.Always)]
                    public IReadOnlyList<string> Hair { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of face removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("face", Required = Required.Always)]
                    public IReadOnlyList<string> Face { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of ear removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("ear", Required = Required.Always)]
                    public IReadOnlyList<string> Ear { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of eye removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("eye", Required = Required.Always)]
                    public IReadOnlyList<string> Eye { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of teeth removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("teeth", Required = Required.Always)]
                    public IReadOnlyList<string> Teeth { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of leg removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("leg", Required = Required.Always)]
                    public IReadOnlyList<string> Leg { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of arm removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("arm", Required = Required.Always)]
                    public IReadOnlyList<string> Arm { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of tail removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("tail", Required = Required.Always)]
                    public IReadOnlyList<string> Tail { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of wing removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("wing", Required = Required.Always)]
                    public IReadOnlyList<string> Wing { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of penile removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("penis", Required = Required.Always)]
                    public IReadOnlyList<string> Penis { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of vaginal removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("vagina", Required = Required.Always)]
                    public IReadOnlyList<string> Vagina { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of head removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("head", Required = Required.Always)]
                    public IReadOnlyList<string> Head { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of body removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("body", Required = Required.Always)]
                    public IReadOnlyList<string> Body { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of pattern removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("pattern", Required = Required.Always)]
                    public IReadOnlyList<string> Pattern { get; private set; } = new List<string>();
                }

                /// <summary>
                /// Holds uniform messages.
                /// </summary>
                public sealed class UniformMessages
                {
                    /// <summary>
                    /// Gets a list of uniform ear removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("ears", Required = Required.Always)]
                    public IReadOnlyList<string> Ears { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform eye removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("eyes", Required = Required.Always)]
                    public IReadOnlyList<string> Eyes { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform leg removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("legs", Required = Required.Always)]
                    public IReadOnlyList<string> Legs { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform arm removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("arms", Required = Required.Always)]
                    public IReadOnlyList<string> Arms { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform wing removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("wings", Required = Required.Always)]
                    public IReadOnlyList<string> Wings { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform pattern removal messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("pattern", Required = Required.Always)]
                    public IReadOnlyList<string> Pattern { get; private set; } = new List<string>();
                }
            }

            /// <summary>
            /// Holds shifting messages.
            /// </summary>
            public sealed class ShiftingMessages
            {
                /// <summary>
                /// Gets a set of singular shifting messages. These are used when single parts are transformed.
                /// </summary>
                [NotNull]
                [JsonProperty("single", Required = Required.Always)]
                public SingleMessages Single { get; private set; } = new SingleMessages();

                /// <summary>
                /// Gets a set of uniform shifting messages. These are used when two or more matching parts are
                /// transformed.
                /// </summary>
                [NotNull]
                [JsonProperty("uniform", Required = Required.Always)]
                public UniformMessages Uniform { get; private set; } = new UniformMessages();

                /// <summary>
                /// Holds singular messages.
                /// </summary>
                public sealed class SingleMessages
                {
                    /// <summary>
                    /// Gets a list of colour shifting messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("colour", Required = Required.Always)]
                    public IReadOnlyList<string> Colour { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of pattern shifting messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("pattern", Required = Required.Always)]
                    public IReadOnlyList<string> Pattern { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of pattern colour shifting messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("patternColour", Required = Required.Always)]
                    public IReadOnlyList<string> PatternColour { get; private set; } = new List<string>();
                }

                /// <summary>
                /// Holds uniform messages.
                /// </summary>
                public sealed class UniformMessages
                {
                    /// <summary>
                    /// Gets a list of uniform colour shifting messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("colour", Required = Required.Always)]
                    public IReadOnlyList<string> Colour { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform pattern shifting messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("pattern", Required = Required.Always)]
                    public IReadOnlyList<string> Pattern { get; private set; } = new List<string>();

                    /// <summary>
                    /// Gets a list of uniform pattern colour shifting messages.
                    /// </summary>
                    [NotNull, ItemNotNull]
                    [JsonProperty("patternColour", Required = Required.Always)]
                    public IReadOnlyList<string> PatternColour { get; private set; } = new List<string>();
                }
            }
        }
    }
}
