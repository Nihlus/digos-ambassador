//
//  ZeZirPronounProvider.cs
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

namespace DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;

/// <summary>
/// Provides ze and zir pronouns.
/// </summary>
public class ZeZirPronounProvider : PronounProvider
{
    /// <inheritdoc />
    public override string Family => "Ze and zir";

    /// <inheritdoc />
    public override string GetSubjectForm(bool withVerb = false) => withVerb ? "ze is" : "ze";

    /// <inheritdoc />
    public override string GetObjectForm() => "zir";

    /// <inheritdoc />
    public override string GetPossessiveAdjectiveForm() => "zir";

    /// <inheritdoc />
    public override string GetPossessiveForm(bool withVerb = false) => withVerb ? "ze has" : "zirs";

    /// <inheritdoc />
    public override string GetReflexiveForm() => "zirself";
}
