//
//  RawSpeciesYamlConverter.cs
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

using System;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using JetBrains.Annotations;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace DIGOS.Ambassador.Tools
{
    /// <summary>
    /// YAML deserialization converter for species objects. This just reads a dummy type to allow file verification.
    /// </summary>
    public class RawSpeciesYamlConverter : IYamlTypeConverter
    {
        /// <inheritdoc />
        public bool Accepts(Type type)
        {
            return type == typeof(Species);
        }

        /// <inheritdoc />
        public object? ReadYaml(IParser parser, Type type)
        {
            if (!parser.TryConsume<Scalar>(out var speciesName))
            {
                return null;
            }

            return new Species(speciesName.Value, "Dummy", "dummy");
        }

        /// <inheritdoc />
        public void WriteYaml([NotNull] IEmitter emitter, object? value, Type type)
        {
            if (!(value is Species species))
            {
                return;
            }

            emitter.Emit(new Scalar(species.Name));
        }
    }
}
