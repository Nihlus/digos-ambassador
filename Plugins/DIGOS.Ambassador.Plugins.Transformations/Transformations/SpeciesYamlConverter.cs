//
//  SpeciesYamlConverter.cs
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
using DIGOS.Ambassador.Plugins.Transformations.Services;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations
{
    /// <summary>
    /// YAML deserialization converter for species objects.
    /// </summary>
    internal class SpeciesYamlConverter : IYamlTypeConverter
    {
        private TransformationService Transformation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeciesYamlConverter"/> class.
        /// </summary>
        /// <param name="transformation">The transformation service.</param>
        public SpeciesYamlConverter(TransformationService transformation)
        {
            this.Transformation = transformation;
        }

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

            var getSpeciesResult = this.Transformation.GetSpeciesByName(speciesName.Value);
            if (!getSpeciesResult.IsSuccess)
            {
                throw new InvalidOperationException(getSpeciesResult.Unwrap().Message);
            }

            return getSpeciesResult.Entity;
        }

        /// <inheritdoc />
        public void WriteYaml(IEmitter emitter, object? value, Type type)
        {
            if (value is null)
            {
                return;
            }

            var species = (Species)value;

            emitter.Emit(new Scalar(species.Name));
        }
    }
}
