//
//  KinkCategory.cs
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

namespace DIGOS.Ambassador.Database.Kinks
{
	/// <summary>
	/// Represents kink categories. Values are taken from the F-list API.
	/// </summary>
	public enum KinkCategory
	{
		/// <summary>
		/// Kinks that don't really belong to one specific category.
		/// </summary>
		General = 27,

		/// <summary>
		/// Kinks that relate to the physical appearance of characters.
		/// </summary>
		Character = 28,

		/// <summary>
		/// Kinks that relate to the gender of a character.
		/// </summary>
		Gender = 29,

		/// <summary>
		/// Kinks that relate to the species of a character.
		/// </summary>
		Species = 30,

		/// <summary>
		/// Kinks that relate to the age of a character.
		/// </summary>
		Age = 31,

		/// <summary>
		/// Kinks that relate to anal sex and play.
		/// </summary>
		AnalPlay = 32,

		/// <summary>
		/// Kinks that relate to vaginal sex and play.
		/// </summary>
		VaginalPlay = 33,

		/// <summary>
		/// Kinks that relate to oral sex and play.
		/// </summary>
		OralPlay = 34,

		/// <summary>
		/// Kinks that relate to penile sex and play.
		/// </summary>
		CockPlay = 35,

		/// <summary>
		/// Kinks that relate to semen and its various uses.
		/// </summary>
		Cum = 36,

		/// <summary>
		/// Kinks that relate to different types of sexual penetration.
		/// </summary>
		SexualPenetration = 37,

		/// <summary>
		/// Kinks that relate to how roleplay is approached or performed.
		/// </summary>
		Roleplay = 38,

		/// <summary>
		/// Kinks that relate to roleplaying themes or settings.
		/// </summary>
		ThemesAndSettings = 39,

		/// <summary>
		/// Kinks that relate to worship or play targeting specific body parts.
		/// </summary>
		BodyWorship = 40,

		/// <summary>
		/// Kinks that relate to clothing or the materials they are made of.
		/// </summary>
		DressUpAndMaterials = 41,

		/// <summary>
		/// Kinks that relate to the psychologial relationship between characters.
		/// </summary>
		DomSubAndPsyche = 42,

		/// <summary>
		/// Kinks that relate to bondage, domination, and sadomasochism.
		/// </summary>
		BDSM = 43,

		/// <summary>
		/// Kinks that relate to the roughness or painfulness of sexual acts.
		/// </summary>
		Roughness = 44,

		/// <summary>
		/// Kinks that relate to violence, gore, or torture.
		/// </summary>
		GoreAndTorture = 45,

		/// <summary>
		/// Kinks that relate to character size.
		/// </summary>
		InflationGrowthAndSize = 46,

		/// <summary>
		/// Kinks that relate to transformation of characters.
		/// </summary>
		Transformation = 47,

		/// <summary>
		/// Kinks that relate to impregnation or breeding.
		/// </summary>
		Pregnancy = 48,

		/// <summary>
		/// Kinks that relate to vore or unbirth.
		/// </summary>
		VoreAndUnbirth = 49,

		/// <summary>
		/// Kinks that relate to unclean behaviour.
		/// </summary>
		UncleanPlay = 50,

		/// <summary>
		/// Kinks that relate to bodily waste.
		/// </summary>
		WatersportsAndScat = 51,
	}
}
