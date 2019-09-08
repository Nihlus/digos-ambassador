//
//  AmbassadorPlugin.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Abstractions.Attributes
{
    /// <summary>
    /// Represents an attribute that can be applied to an assembly in order to mark it as an assembly containing a
    /// plugin.
    /// </summary>
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class AmbassadorPlugin : Attribute
    {
        /// <summary>
        /// Gets the plugin descriptor that the assembly exports.
        /// </summary>
        [NotNull]
        public Type PluginDescriptor { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorPlugin"/> class.
        /// </summary>
        /// <param name="pluginDescriptor">The descriptor type.</param>
        public AmbassadorPlugin([NotNull] Type pluginDescriptor)
        {
            this.PluginDescriptor = pluginDescriptor;
        }
    }
}
