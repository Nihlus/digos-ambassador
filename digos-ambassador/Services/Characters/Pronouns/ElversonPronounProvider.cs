//
//  ElversonPronounProvider.cs
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

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Provides Elverson pronouns.
	/// </summary>
	[UsedImplicitly]
	public class ElversonPronounProvider : IPronounProvider
	{
		/// <inheritdoc />
		public virtual string Family => "Elverson";

		/// <inheritdoc />
		public virtual string GetSubjectForm(bool plural = false, bool withVerb = false) => "ey";

		/// <inheritdoc />
		public virtual string GetObjectForm(bool plural = false) => "em";

		/// <inheritdoc />
		public virtual string GetPossessiveAdjectiveForm(bool plural = false) => "eir";

		/// <inheritdoc />
		public virtual string GetPossessiveForm(bool plural = false) => "eirs";

		/// <inheritdoc />
		public virtual string GetReflexiveForm(bool plural = false) => "emselves";
	}
}
