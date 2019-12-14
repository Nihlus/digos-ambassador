//
//  IAppearanceShifter.cs
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Transformations.Results;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Shifters
{
    /// <summary>
    /// Defines the public interface of a type that can perform a shifting operation on an appearance.
    /// </summary>
    internal interface IAppearanceShifter
    {
        /// <summary>
        /// Shifts the given bodypart.
        /// </summary>
        /// <param name="bodypart">The bodypart to shift.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>A shifting result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        Task<ShiftBodypartResult> ShiftAsync(Bodypart bodypart, Chirality chirality);
    }
}
