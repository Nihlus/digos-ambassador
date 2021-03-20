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

using System.ComponentModel;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Kinks.Model
{
    /// <summary>
    /// Represents kink categories. Values are taken from the F-list API.
    /// </summary>
    [PublicAPI]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public enum KinkCategory
    {
        /// <summary>
        /// Kinks that don't really belong to one specific category.
        /// </summary>
        [Description("Kinks that don't really belong to one specific category.")]
        General = 27,

        /// <summary>
        /// Kinks that relate to the physical appearance of characters.
        /// </summary>
        [Description("Kinks that relate to the physical appearance of characters.")]
        Character = 28,

        /// <summary>
        /// Kinks that relate to the gender of a character.
        /// </summary>
        [Description("Kinks that relate to the gender of a character.")]
        Gender = 29,

        /// <summary>
        /// Kinks that relate to the species of a character.
        /// </summary>
        [Description("Kinks that relate to the species of a character.")]
        Species = 30,

        /// <summary>
        /// Kinks that relate to the age of a character.
        /// </summary>
        [Description("Kinks that relate to the age of a character.")]
        Age = 31,

        /// <summary>
        /// Kinks that relate to anal sex and play.
        /// </summary>
        [Description("Kinks that relate to anal sex and play.")]
        AnalPlay = 32,

        /// <summary>
        /// Kinks that relate to vaginal sex and play.
        /// </summary>
        [Description("Kinks that relate to vaginal sex and play.")]
        VaginalPlay = 33,

        /// <summary>
        /// Kinks that relate to oral sex and play.
        /// </summary>
        [Description("Kinks that relate to oral sex and play.")]
        OralPlay = 34,

        /// <summary>
        /// Kinks that relate to penile sex and play.
        /// </summary>
        [Description("Kinks that relate to penile sex and play.")]
        CockPlay = 35,

        /// <summary>
        /// Kinks that relate to semen and its various uses.
        /// </summary>
        [Description("Kinks that relate to semen and its various uses.")]
        Cum = 36,

        /// <summary>
        /// Kinks that relate to different types of sexual penetration.
        /// </summary>
        [Description("Kinks that relate to different types of sexual penetration.")]
        SexualPenetration = 37,

        /// <summary>
        /// Kinks that relate to how roleplay is approached or performed.
        /// </summary>
        [Description("Kinks that relate to how roleplay is approached or performed.")]
        Roleplay = 38,

        /// <summary>
        /// Kinks that relate to roleplaying themes or settings.
        /// </summary>
        [Description("Kinks that relate to roleplaying themes or settings.")]
        ThemesAndSettings = 39,

        /// <summary>
        /// Kinks that relate to worship or play targeting specific body parts.
        /// </summary>
        [Description("Kinks that relate to worship or play targeting specific body parts.")]
        BodyWorship = 40,

        /// <summary>
        /// Kinks that relate to clothing or the materials they are made of.
        /// </summary>
        [Description("Kinks that relate to clothing or the materials they are made of.")]
        DressUpAndMaterials = 41,

        /// <summary>
        /// Kinks that relate to the psychological relationship between characters.
        /// </summary>
        [Description("Kinks that relate to the psychologial relationship between characters.")]
        DomSubAndPsyche = 42,

        /// <summary>
        /// Kinks that relate to bondage, domination, and sadomasochism.
        /// </summary>
        [Description("Kinks that relate to bondage, domination, and sadomasochism.")]
        BDSM = 43,

        /// <summary>
        /// Kinks that relate to the roughness or painfulness of sexual acts.
        /// </summary>
        [Description("Kinks that relate to the roughness or painfulness of sexual acts.")]
        Roughness = 44,

        /// <summary>
        /// Kinks that relate to violence, gore, or torture.
        /// </summary>
        [Description("Kinks that relate to violence, gore, or torture.")]
        GoreAndTorture = 45,

        /// <summary>
        /// Kinks that relate to character size.
        /// </summary>
        [Description("Kinks that relate to character size.")]
        InflationGrowthAndSize = 46,

        /// <summary>
        /// Kinks that relate to transformation of characters.
        /// </summary>
        [Description("Kinks that relate to transformation of characters.")]
        Transformation = 47,

        /// <summary>
        /// Kinks that relate to impregnation or breeding.
        /// </summary>
        [Description("Kinks that relate to impregnation or breeding.")]
        Pregnancy = 48,

        /// <summary>
        /// Kinks that relate to vore or unbirth.
        /// </summary>
        [Description("Kinks that relate to vore or unbirth.")]
        VoreAndUnbirth = 49,

        /// <summary>
        /// Kinks that relate to unclean behaviour.
        /// </summary>
        [Description("Kinks that relate to unclean behaviour.")]
        UncleanPlay = 50,

        /// <summary>
        /// Kinks that relate to bodily waste.
        /// </summary>
        [Description("Kinks that relate to bodily waste.")]
        WatersportsAndScat = 51,
    }
}
