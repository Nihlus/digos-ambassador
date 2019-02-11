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

using DIGOS.Ambassador.Extensions;

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
        private readonly UserFeedbackService Feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpService"/> class.
        /// </summary>
        /// <param name="feedback">The feedback service.</param>
        public HelpService(UserFeedbackService feedback)
        {
            this.Feedback = feedback;
        }

        /// <summary>
        /// Creates an informational embed field about a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>The embed field.</returns>
        public EmbedFieldBuilder CreateCommandInfoEmbed([NotNull] CommandInfo command)
        {
            var relevantAliases = command.Aliases
                .Skip(1)
                .Where(a => a.StartsWith(command.Module.Aliases.First()))
                .ToList();

            var prefix = relevantAliases.Count > 1
                ? "as well as"
                : "or";

            var commandExtraAliases = relevantAliases.Any()
                ? $"({prefix} {relevantAliases.Humanize("or")})"
                : string.Empty;

            var commandDisplayAliases = $"{command.Aliases.First()} {commandExtraAliases}";

            var commandInfoContent = BuildParameterList(command);

            var contexts = command.Preconditions
                .Where(a => a is RequireContextAttribute)
                .Cast<RequireContextAttribute>()
                .SingleOrDefault()?.Contexts;

            if (!(contexts is null))
            {
                var separateContexts = contexts.ToString().Split(',');
                separateContexts = separateContexts.Select(c => c.Pluralize()).ToArray();

                commandInfoContent += '\n';
                commandInfoContent += $"*This command can only be used in {separateContexts.Humanize()}.*"
                    .Transform(To.SentenceCase);
            }

            return new EmbedFieldBuilder().WithName(commandDisplayAliases).WithValue(commandInfoContent);
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
            var eb = this.Feedback.CreateEmbedBase();
            eb.WithTitle("Perhaps you meant one of the following?");

            foreach (var matchingCommand in matchingCommands)
            {
                eb.AddField($"{matchingCommand.Alias}", BuildParameterList(matchingCommand.Command));
            }

            return eb.Build();
        }

        /// <summary>
        /// Builds a human-readable parameter list for a command.
        /// </summary>
        /// <param name="commandInfo">The command to get the parameters from.</param>
        /// <returns>A humanized parameter list.</returns>
        [Pure]
        [NotNull]
        public string BuildParameterList([NotNull] CommandInfo commandInfo)
        {
            var result = string.Join
            (
                ", ",
                commandInfo.Parameters.Select
                (
                    p =>
                    {
                        var parameterInfo = $"{p.Type.Humanize()} {p.Name}";
                        if (p.IsOptional)
                        {
                            parameterInfo = $"[{parameterInfo}]";
                        }

                        return parameterInfo;
                    }
                )
            );

            if (result.IsNullOrWhitespace())
            {
                return "No parameters";
            }

            return result;
        }
    }
}
