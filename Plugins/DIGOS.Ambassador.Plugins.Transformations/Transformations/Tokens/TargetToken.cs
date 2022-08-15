//
//  TargetToken.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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

using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Tokens;

/// <summary>
/// A token that gets replaced with the target's name or nickname.
/// </summary>
[PublicAPI]
[TokenIdentifier("target", "t")]
public sealed class TargetToken : ReplaceableTextToken<TargetToken>
{
    /// <inheritdoc />
    public override string GetText(Appearance appearance, AppearanceComponent? component)
    {
        var character = appearance.Character;

        return character.Nickname.IsNullOrWhitespace() ? character.Name : character.Nickname;
    }

    /// <inheritdoc />
    protected override TargetToken Initialize(string? data)
    {
        return this;
    }
}
