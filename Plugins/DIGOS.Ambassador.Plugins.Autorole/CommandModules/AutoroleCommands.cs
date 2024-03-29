//
//  AutoroleCommands.cs
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Permissions;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Discord.Pagination;
using Remora.Discord.Pagination.Extensions;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Autorole.CommandModules;

/// <summary>
/// Commands for creating, using, and interacting with autoroles.
/// </summary>
[UsedImplicitly]
[Group("autorole")]
[Description("Commands for creating, editing, and interacting with automatic roles.")]
public partial class AutoroleCommands : CommandGroup
{
    private readonly AutoroleService _autoroles;
    private readonly FeedbackService _feedback;
    private readonly ICommandContext _context;
    private readonly IDiscordRestGuildAPI _guildAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoroleCommands"/> class.
    /// </summary>
    /// <param name="autoroles">The autorole service.</param>
    /// <param name="feedback">The feedback service.</param>
    /// <param name="context">The command context.</param>
    /// <param name="guildAPI">The guild API.</param>
    public AutoroleCommands
    (
        AutoroleService autoroles,
        FeedbackService feedback,
        ICommandContext context,
        IDiscordRestGuildAPI guildAPI
    )
    {
        _autoroles = autoroles;
        _feedback = feedback;
        _context = context;
        _guildAPI = guildAPI;
    }

    /// <summary>
    /// Creates a new autorole configuration for the given Discord role.
    /// </summary>
    /// <param name="discordRole">The discord role.</param>
    [UsedImplicitly]
    [Command("create")]
    [Description("Creates a new autorole configuration for the given Discord role.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(CreateAutorole), PermissionTarget.Self)]
    public async Task<Result<FeedbackMessage>> CreateAutoroleAsync(IRole discordRole)
    {
        if (!_context.TryGetGuildID(out var guildID))
        {
            throw new InvalidOperationException();
        }

        var create = await _autoroles.CreateAutoroleAsync
        (
            guildID,
            discordRole.ID,
            this.CancellationToken
        );

        return !create.IsSuccess
            ? Result<FeedbackMessage>.FromError(create)
            : new FeedbackMessage("Autorole configuration created.", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Deletes an existing autorole configuration for the given Discord role.
    /// </summary>
    /// <param name="autorole">The autorole.</param>
    [UsedImplicitly]
    [Command("delete")]
    [Description("Deletes an existing autorole configuration for the given Discord role.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(DeleteAutorole), PermissionTarget.Self)]
    public async Task<Result<FeedbackMessage>> DeleteAutoroleAsync
    (
        [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole
    )
    {
        var deleteAutorole = await _autoroles.DeleteAutoroleAsync(autorole);
        return !deleteAutorole.IsSuccess
            ? Result<FeedbackMessage>.FromError(deleteAutorole)
            : new FeedbackMessage("Autorole configuration deleted.", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Enables the given autorole, allowing it to be added to users.
    /// </summary>
    /// <param name="autorole">The autorole.</param>
    [UsedImplicitly]
    [Command("enable")]
    [Description("Enables the given autorole, allowing it to be added to users.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(EditAutorole), PermissionTarget.Self)]
    public async Task<Result<FeedbackMessage>> EnableAutoroleAsync
    (
        [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole
    )
    {
        var enableAutorole = await _autoroles.EnableAutoroleAsync(autorole);

        return !enableAutorole.IsSuccess
            ? Result<FeedbackMessage>.FromError(enableAutorole)
            : new FeedbackMessage("Autorole enabled.", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Disables the given autorole, preventing it from being added to users.
    /// </summary>
    /// <param name="autorole">The autorole.</param>
    [UsedImplicitly]
    [Command("disable")]
    [Description("Disables the given autorole, preventing it from being added to users.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(EditAutorole), PermissionTarget.Self)]
    public async Task<Result<FeedbackMessage>> DisableAutoroleAsync
    (
        [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole
    )
    {
        var disableAutorole = await _autoroles.DisableAutoroleAsync(autorole);

        return !disableAutorole.IsSuccess
            ? Result<FeedbackMessage>.FromError(disableAutorole)
            : new FeedbackMessage("Autorole disabled.", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Show the settings for the given autorole.
    /// </summary>
    /// <param name="autorole">The autorole.</param>
    [UsedImplicitly]
    [Command("show")]
    [Description("Show the settings for the given autorole.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(ViewAutorole), PermissionTarget.Self)]
    public async Task<Result> ShowAutoroleAsync
    (
        [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole
    )
    {
        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var embedFields = new List<IEmbedField>();
        var embed = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Title = "Autorole Configuration",
            Description = $"<@&{autorole.DiscordRoleID}>",
            Fields = embedFields
        };

        embedFields.Add(new EmbedField("Requires confirmation", autorole.RequiresConfirmation.ToString(), true));
        embedFields.Add(new EmbedField("Is enabled", autorole.IsEnabled.ToString(), true));

        if (!autorole.Conditions.Any())
        {
            embedFields.Add(new EmbedField("Conditions", "No conditions"));

            var send = await _feedback.SendContextualEmbedAsync(embed);
            return send.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(send);
        }

        var conditionFields = autorole.Conditions.Select
        (
            c => new EmbedField
            (
                $"Condition #{autorole.Conditions.IndexOf(c)} (ID {c.ID})",
                c.GetDescriptiveUIText()
            )
        );

        var pages = PageFactory.FromFields(conditionFields, pageBase: embed);

        return (Result)await _feedback.SendContextualPaginatedMessageAsync
        (
            userID,
            pages,
            ct: this.CancellationToken
        );
    }

    /// <summary>
    /// Lists configured autoroles.
    /// </summary>
    [UsedImplicitly]
    [Command("list")]
    [Description("Lists configured autoroles.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(ViewAutorole), PermissionTarget.Self)]
    public async Task<Result> ListAutorolesAsync()
    {
        if (!_context.TryGetGuildID(out var guildID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var autoroles = await _autoroles.GetAutorolesAsync(guildID, ct: this.CancellationToken);

        var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
        (
            autoroles,
            at => $"<@&{at.DiscordRoleID}>",
            at => at.IsEnabled ? "Enabled" : "Disabled",
            "There are no autoroles configured."
        );

        return (Result)await _feedback.SendContextualPaginatedMessageAsync
        (
            userID,
            pages,
            ct: this.CancellationToken
        );
    }

    /// <summary>
    /// Affirms a user's qualification for an autorole.
    /// </summary>
    /// <param name="autorole">The autorole.</param>
    /// <param name="user">The user.</param>
    [UsedImplicitly]
    [Command("confirm")]
    [Description("Affirms a user's qualification for an autorole.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(AffirmDenyAutorole), PermissionTarget.All)]
    public async Task<Result<FeedbackMessage>> AffirmAutoroleForUserAsync
    (
        [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole,
        IUser user
    )
    {
        var confirmResult = await _autoroles.ConfirmAutoroleAsync(autorole, user.ID);

        return !confirmResult.IsSuccess
            ? Result<FeedbackMessage>.FromError(confirmResult)
            : new FeedbackMessage("Qualification affirmed.", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Affirms all currently qualifying users for the given autorole.
    /// </summary>
    /// <param name="autorole">The autorole.</param>
    [UsedImplicitly]
    [Command("confirm-all")]
    [Description("Confirms all currently qualifying users for the given autorole.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(AffirmDenyAutorole), PermissionTarget.All)]
    public async Task<Result<FeedbackMessage>> AffirmAutoroleForAllAsync
    (
        [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole
    )
    {
        var affirmResult = await _autoroles.AffirmAutoroleForAllAsync(autorole);

        return !affirmResult.IsSuccess
            ? Result<FeedbackMessage>.FromError(affirmResult)
            : new FeedbackMessage("Qualifications confirmed.", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Denies a user's qualification for an autorole.
    /// </summary>
    /// <param name="autorole">The autorole.</param>
    /// <param name="user">The user.</param>
    [UsedImplicitly]
    [Command("deny")]
    [Description("Denies a user's qualification for an autorole.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(AffirmDenyAutorole), PermissionTarget.Self)]
    public async Task<Result<FeedbackMessage>> DenyAutoroleForUserAsync
    (
        [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole,
        IUser user
    )
    {
        var denyResult = await _autoroles.DenyAutoroleAsync(autorole, user.ID);

        return !denyResult.IsSuccess
            ? Result<FeedbackMessage>.FromError(denyResult)
            : new FeedbackMessage("Qualification denied.", _feedback.Theme.Secondary);
    }

    /// <summary>
    /// Sets whether the given autorole require confirmation for the assignment after a user has qualified.
    /// </summary>
    /// <param name="autorole">The autorole.</param>
    /// <param name="requireAffirmation">Whether confirmation is required.</param>
    [UsedImplicitly]
    [Command("require-confirmation")]
    [Description("Sets whether the given autorole require confirmation for the assignment after a user has qualified.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(EditAutorole), PermissionTarget.Self)]
    public async Task<Result<FeedbackMessage>> SetAffirmationRequirementAsync
    (
        [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole,
        bool requireAffirmation = true
    )
    {
        var setRequirementResult = await _autoroles.SetAffirmationRequiredAsync(autorole, requireAffirmation);
        if (!setRequirementResult.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(setRequirementResult);
        }

        return new FeedbackMessage
        (
            requireAffirmation
                ? "Affirmation is now required."
                : "Affirmation is no longer required.",
            _feedback.Theme.Secondary
        );
    }

    /// <summary>
    /// Lists users that haven't been confirmed yet for the given autorole.
    /// </summary>
    /// <param name="autorole">The autorole.</param>
    [UsedImplicitly]
    [Command("unconfirmed")]
    [Description("Lists users that haven't been confirmed yet for the given autorole.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(AffirmDenyAutorole), PermissionTarget.All)]
    public async Task<Result> ListUnconfirmedUsersAsync
    (
        [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole
    )
    {
        if (!_context.TryGetGuildID(out var guildID))
        {
            throw new InvalidOperationException();
        }

        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        var getUsers = await _autoroles.GetUnconfirmedUsersAsync(autorole);
        if (!getUsers.IsSuccess)
        {
            return Result.FromError(getUsers);
        }

        var users = getUsers.Entity.ToList();
        var getDiscordUsers = await Task.WhenAll
        (
            users.Select(u => _guildAPI.GetGuildMemberAsync(guildID, u.DiscordID))
        );

        var discordUsers = getDiscordUsers
            .Where(r => r.IsSuccess)
            .Select(r => r.Entity)
            .Where(u => u.User.HasValue)
            .ToList();

        var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
        (
            discordUsers,
            u => !u.Nickname.HasValue
                ? $"{u.User.Value.Username}#{u.User.Value.Discriminator} | {u.User.Value.ID}"
                : $"{u.Nickname} ({u.User.Value.Username}#{u.User.Value.Discriminator} | {u.User.Value.ID})",
            _ => "Not confirmed",
            "There are no users that haven't been confirmed for that role."
        );

        return (Result)await _feedback.SendContextualPaginatedMessageAsync
        (
            userID,
            pages,
            ct: this.CancellationToken
        );
    }
}
