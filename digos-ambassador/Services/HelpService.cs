//
//  HelpService.cs
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

using System.Collections.Generic;
using System.Linq;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using Discord;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services
{
    /// <summary>
    /// Provides helper methods for building help output.
    /// </summary>
    public class HelpService
    {
        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpService"/> class.
        /// </summary>
        /// <param name="feedback">The feedback service.</param>
        public HelpService(UserFeedbackService feedback)
        {
            _feedback = feedback;
        }

        /// <summary>
        /// Creates a simplified command info embed field.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>The field builder.</returns>
        [NotNull]
        public EmbedFieldBuilder CreateCommandInfoEmbedField([NotNull] CommandInfo command)
        {
            var fieldBuilder = new EmbedFieldBuilder();

            fieldBuilder.WithName(command.GetFullCommand());
            fieldBuilder.WithValue(command.Summary ?? "No summary available.");

            return fieldBuilder;
        }

        /// <summary>
        /// Creates an informational embed field about a command.
        /// </summary>
        /// <param name="commandGroup">The command.</param>
        /// <returns>The embed field.</returns>
        [NotNull]
        public EmbedBuilder CreateDetailedCommandInfoEmbed([NotNull] IGrouping<string, CommandInfo> commandGroup)
        {
            var eb = _feedback.CreateEmbedBase();

            var relevantAliases = commandGroup.SelectMany(c => c.Aliases).Distinct()
                .Skip(1)
                .Where(a => a.StartsWith(commandGroup.First().Module.Aliases.First()))
                .ToList();

            var prefix = relevantAliases.Count > 1
                ? "as well as"
                : "or";

            var commandExtraAliases = relevantAliases.Any()
                ? $"({prefix} {relevantAliases.Humanize("or")})"
                : string.Empty;

            eb.WithTitle($"{commandGroup.Key} {commandExtraAliases}");

            eb.WithDescription
            (
                "All the variants of the command are shown below. Text in italics after a variant are parameters, and" +
                " are listed in more detail below the command itself.\n" +
                "\n" +
                "Parameters inside [brackets] are optional, and can be omitted.\n" +
                "\u200b"
            );

            foreach (var variant in commandGroup)
            {
                eb.AddField($"{variant.GetFullCommand()} {BuildParameterList(variant)}", variant.Summary);

                var parameterList = BuildDetailedParameterList(variant).ToList();
                if (parameterList.Any())
                {
                    eb.AddField("Parameters", string.Join(", \n", parameterList));
                }

                var contexts = variant.Preconditions
                    .Where(a => a is RequireContextAttribute)
                    .Cast<RequireContextAttribute>()
                    .SingleOrDefault()?.Contexts;

                if (!(contexts is null))
                {
                    var separateContexts = contexts.ToString()?.Split(',');
                    if (separateContexts is null)
                    {
                        continue;
                    }

                    separateContexts = separateContexts.Select(c => c.Pluralize()).ToArray();

                    var restrictions = $"*Can only be used in {separateContexts.Humanize()}.*"
                        .Transform(To.SentenceCase);

                    eb.AddField("Restrictions", restrictions);
                }

                if (variant == commandGroup.Last())
                {
                    continue;
                }

                var previousField = eb.Fields.Last();

                // Add a spacer
                previousField.WithValue($"{previousField.Value}\n\u200b");
            }

            return eb;
        }

        /// <summary>
        /// Creates an embed that verbosely describes a set of commands.
        /// </summary>
        /// <param name="matchingCommands">A set of commands that should be included in the embed.</param>
        /// <returns>An embed.</returns>
        [Pure]
        [NotNull]
        public Embed CreateCommandUsageEmbed([NotNull] IEnumerable<CommandMatch> matchingCommands)
        {
            var eb = _feedback.CreateEmbedBase();
            eb.WithTitle("Perhaps you meant one of the following?");
            eb.WithDescription("It's also possible you forgot to enclose something with a space inside it in quotes.");

            var commands = matchingCommands.ToList();

            const int maxCommands = 3;

            foreach (var matchingCommand in commands.Take(maxCommands))
            {
                eb.AddField
                (
                    $"{matchingCommand.Alias} {BuildParameterList(matchingCommand.Command)}",
                    matchingCommand.Command.Summary
                );
            }

            if (commands.Count <= maxCommands)
            {
                return eb.Build();
            }

            var remainingCommands = commands.Count - maxCommands;
            eb.WithFooter
            (
                $"+ {remainingCommands} more {(remainingCommands > 1 ? "command".Pluralize() : "command")}."
            );

            return eb.Build();
        }

        /// <summary>
        /// Builds a simplified human-readable parameter list for a command.
        /// </summary>
        /// <param name="command">The command to get the parameters from.</param>
        /// <returns>A humanized parameter list.</returns>
        [Pure]
        [NotNull]
        private string BuildParameterList([NotNull] CommandInfo command)
        {
            if (!command.Parameters.Any())
            {
                return string.Empty;
            }

            var result = string.Join
            (
                " ",
                command.Parameters.Select
                (
                    p =>
                    {
                        var parameterInfo = $"{p.Name}";
                        if (p.IsOptional)
                        {
                            parameterInfo = $"[{parameterInfo}]";
                        }

                        return parameterInfo;
                    }
                )
            );

            return $"*{result}*";
        }

        /// <summary>
        /// Builds a detailed human-readable parameter list for a command.
        /// </summary>
        /// <param name="command">The command to get the parameters from.</param>
        /// <returns>A humanized parameter list.</returns>
        [Pure]
        [NotNull, ItemNotNull]
        private IEnumerable<string> BuildDetailedParameterList([NotNull] CommandInfo command)
        {
            if (!command.Parameters.Any())
            {
                yield break;
            }

            var parameters = command.Parameters.Select
            (
                p =>
                {
                    var parameterInfo = $"{p.Type.Humanize()} *{p.Name}*";
                    if (p.IsOptional)
                    {
                        parameterInfo = $"[{parameterInfo}]";
                    }

                    return parameterInfo;
                }
            );

            foreach (var parameter in parameters)
            {
                yield return parameter;
            }
        }
    }
}
