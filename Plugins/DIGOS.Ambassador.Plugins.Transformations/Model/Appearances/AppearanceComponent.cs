//
//  AppearanceComponent.cs
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

using static DIGOS.Ambassador.Plugins.Transformations.Transformations.Chirality;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;

/// <summary>
/// Represents a distinct part of a character's appearance.
/// </summary>
[Owned]
[PublicAPI]
public class AppearanceComponent
{
    /// <summary>
    /// Gets the component's current transformation.
    /// </summary>
    public virtual Transformation Transformation { get; internal set; } = null!;

    /// <summary>
    /// Gets the chirality of the component.
    /// </summary>
    public Chirality Chirality { get; private set; }

    /// <summary>
    /// Gets the base colour of the component.
    /// </summary>
    public virtual Colour BaseColour { get; internal set; } = null!;

    /// <summary>
    /// Gets the pattern of the component's secondary colour (if any).
    /// </summary>
    public Pattern? Pattern { get; internal set; }

    /// <summary>
    /// Gets the component's pattern colour.
    /// </summary>
    public virtual Colour? PatternColour { get; internal set; }

    /// <summary>
    /// Gets the bodypart that the component is.
    /// </summary>
    public Bodypart Bodypart => this.Transformation.Part;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppearanceComponent"/> class.
    /// </summary>
    /// <remarks>
    /// Required by EF Core.
    /// </remarks>
    protected AppearanceComponent()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppearanceComponent"/> class.
    /// </summary>
    /// <param name="transformation">The transformation that the component has.</param>
    /// <param name="chirality">The chirality of the component.</param>
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
    public AppearanceComponent(Transformation transformation, Chirality chirality)
    {
        this.Transformation = transformation;
        this.Chirality = chirality;

        this.BaseColour = transformation.DefaultBaseColour.Clone();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return
            $"{(this.Chirality == Center ? string.Empty : $"{this.Chirality.Humanize()} ")}{this.Bodypart} ({this.Transformation.Species.Name})";
    }

    /// <summary>
    /// Copies the appearance of the given component and creates a new component based on it.
    /// </summary>
    /// <param name="other">The other component.</param>
    /// <returns>A new component with the same settings.</returns>
    [Pure]
    public static AppearanceComponent CopyFrom(AppearanceComponent other)
    {
        return new AppearanceComponent(other.Transformation, other.Chirality)
        {
            BaseColour = Colour.CopyFrom(other.BaseColour),
            Pattern = other.Pattern,
            PatternColour = other.PatternColour is null ? null : Colour.CopyFrom(other.PatternColour),
        };
    }

    /// <summary>
    /// Creates a new <see cref="AppearanceComponent"/> from a transformation of a bodypart.
    /// </summary>
    /// <param name="transformation">The transformation.</param>
    /// <param name="chirality">The chirality of the transformation, if any.</param>
    /// <returns>A new component.</returns>
    [Pure]
    public static AppearanceComponent CreateFrom(Transformation transformation, Chirality chirality = Center)
    {
        if (transformation.Part.IsChiral() && chirality == Center)
        {
            throw new ArgumentException("A chiral transformation requires you to specify the chirality.", nameof(transformation));
        }

        if (!transformation.Part.IsChiral() && chirality != Center)
        {
            throw new ArgumentException("A nonchiral transformation cannot have chirality.", nameof(transformation));
        }

        return new AppearanceComponent(transformation, chirality)
        {
            Pattern = transformation.DefaultPattern,
            PatternColour = transformation.DefaultPatternColour?.Clone()
        };
    }

    /// <summary>
    /// Creates a set of chiral appearance components from a chiral transformation.
    /// </summary>
    /// <param name="transformation">The transformation.</param>
    /// <returns>A set of appearance components.</returns>
    [Pure]
    public static IEnumerable<AppearanceComponent> CreateFromChiral(Transformation transformation)
    {
        if (!transformation.Part.IsChiral())
        {
            throw new ArgumentException("The transformation was not chiral.", nameof(transformation));
        }

        var chiralities = new[] { Left, Right };

        foreach (var chirality in chiralities)
        {
            yield return new AppearanceComponent(transformation, chirality)
            {
                Pattern = transformation.DefaultPattern,
                PatternColour = transformation.DefaultPatternColour?.Clone()
            };
        }
    }
}
