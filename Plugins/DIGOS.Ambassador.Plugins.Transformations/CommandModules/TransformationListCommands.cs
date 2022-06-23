//
//  TransformationListCommands.cs
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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Humanizer;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Interactivity.Services;
using Remora.Discord.Pagination;
using Remora.Discord.Pagination.Extensions;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Transformations.CommandModules;

public partial class TransformationCommands
{
    /// <summary>
    /// Contains listing commands for content.
    /// </summary>
    [Group("list")]
    [Description("Various listing commands for content.")]
    public class TransformationListCommands : CommandGroup
    {
        private readonly TransformationService _transformation;
        private readonly InteractiveMessageService _interactivity;
        private readonly ICommandContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationListCommands"/> class.
        /// </summary>
        /// <param name="transformation">The transformation service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="context">The command context.</param>
        public TransformationListCommands
        (
            TransformationService transformation,
            InteractiveMessageService interactivity,
            ICommandContext context
        )
        {
            _transformation = transformation;
            _interactivity = interactivity;
            _context = context;
        }

        /// <summary>
        /// Lists the available transformation species.
        /// </summary>
        [UsedImplicitly]
        [Command("species")]
        [Description("Lists the available transformation species.")]
        public async Task<Result> ListAvailableTransformationsAsync()
        {
            var availableSpecies = await _transformation.GetAvailableSpeciesAsync();

            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                availableSpecies,
                s => $"{s.Name.Humanize(LetterCasing.Title)} ({s.Name})",
                s => $"{s.Description}\nWritten by {s.Author}.",
                "There are no species available."
            );

            pages = pages.Select
                (
                    p =>
                        p with
                        {
                            Title = "Available species",
                            Colour = Color.MediumPurple
                        }
                )
                .ToList();

            if (availableSpecies.Any())
            {
                pages = pages.Select
                    (
                        p =>
                            p with
                            {
                                Description = "Use the name inside the parens when transforming body parts."
                            }
                    )
                    .ToList();
            }

            return (Result)await _interactivity.SendContextualPaginatedMessageAsync
            (
                _context.User.ID,
                pages,
                ct: this.CancellationToken
            );
        }

        /// <summary>
        /// Lists the available bodyparts.
        /// </summary>
        [UsedImplicitly]
        [Command("bodyparts")]
        [Description("Lists the available bodyparts.")]
        public async Task<Result> ListAvailableBodypartsAsync()
        {
            var parts = Enum.GetValues(typeof(Bodypart))
                .Cast<Bodypart>()
                .OrderBy(b => b)
                .ToList();

            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                parts,
                b => b.Humanize(),
                b =>
                {
                    if (b.IsChiral())
                    {
                        return "This part is available in both left and right versions.";
                    }

                    if (!b.IsGenderNeutral())
                    {
                        return "This part is considered NSFW.";
                    }

                    return b.IsComposite()
                        ? "This part is composed of smaller parts."
                        : "This is a normal bodypart.";
                }
            );

            pages = pages.Select
                (
                    p =>
                        p with
                        {
                            Title = "Available bodyparts",
                            Colour = Color.MediumPurple
                        }
                )
                .ToList();

            return (Result)await _interactivity.SendContextualPaginatedMessageAsync
            (
                _context.User.ID,
                pages,
                ct: this.CancellationToken
            );
        }

        /// <summary>
        /// Lists the available shades.
        /// </summary>
        [UsedImplicitly]
        [Command("colours")]
        [Description("Lists the available colours.")]
        public async Task<Result> ListAvailableShadesAsync()
        {
            var parts = Enum.GetValues(typeof(Shade))
                .Cast<Shade>()
                .OrderBy(s => s)
                .ToList();

            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                parts,
                b => b.Humanize(),
                _ => "\u200B"
            );

            pages = pages.Select
                (
                    p =>
                        p with
                        {
                            Title = "Available colours",
                            Colour = Color.MediumPurple
                        }
                )
                .ToList();

            return (Result)await _interactivity.SendContextualPaginatedMessageAsync
            (
                _context.User.ID,
                pages,
                ct: this.CancellationToken
            );
        }

        /// <summary>
        /// Lists the available shade modifiers.
        /// </summary>
        [UsedImplicitly]
        [Command("colour-modifiers")]
        [Description("Lists the available colour modifiers.")]
        public async Task<Result> ListAvailableShadeModifiersAsync()
        {
            var parts = Enum.GetValues(typeof(ShadeModifier))
                .Cast<ShadeModifier>()
                .OrderBy(sm => sm)
                .ToList();

            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                parts,
                b => b.Humanize(),
                _ => "\u200B"
            );

            pages = pages.Select
                (
                    p =>
                        p with
                        {
                            Title = "Available colour modifiers",
                            Colour = Color.MediumPurple
                        }
                )
                .ToList();

            return (Result)await _interactivity.SendContextualPaginatedMessageAsync
            (
                _context.User.ID,
                pages,
                ct: this.CancellationToken
            );
        }

        /// <summary>
        /// Lists the available patterns.
        /// </summary>
        [UsedImplicitly]
        [Command("patterns")]
        [Description("Lists the available patterns.")]
        public async Task<Result> ListAvailablePatternsAsync()
        {
            var parts = Enum.GetValues(typeof(Pattern))
                .Cast<Pattern>()
                .OrderBy(c => c)
                .ToList();

            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                parts,
                b => b.Humanize(),
                _ => "\u200B"
            );

            pages = pages.Select
                (
                    p =>
                        p with
                        {
                            Title = "Available patterns",
                            Colour = Color.MediumPurple
                        }
                )
                .ToList();

            return (Result)await _interactivity.SendContextualPaginatedMessageAsync
            (
                _context.User.ID,
                pages,
                ct: this.CancellationToken
            );
        }

        /// <summary>
        /// Lists the available transformations for a given bodypart.
        /// </summary>
        /// <param name="bodyPart">The part to list available transformations for. Optional.</param>
        [UsedImplicitly]
        [Command("transformations-for-part")]
        [Description("Lists the available transformations for a given bodypart.")]
        public async Task<Result> ListAvailableTransformationsAsync(Bodypart bodyPart)
        {
            var transformations = await _transformation.GetAvailableTransformationsAsync(bodyPart);

            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                transformations,
                tf => $"{tf.Species.Name.Humanize(LetterCasing.Title)} ({tf.Species.Name})",
                tf => tf.Description
            );

            pages = pages.Select
                (
                    p =>
                        p with
                        {
                            Title = "Available transformations",
                            Description = "Use the name inside the parens when transforming body parts.",
                            Colour = Color.MediumPurple
                        }
                )
                .ToList();

            return (Result)await _interactivity.SendContextualPaginatedMessageAsync
            (
                _context.User.ID,
                pages,
                ct: this.CancellationToken
            );
        }
    }
}
