//
//  ServerCommands.cs
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

using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Permissions;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Core.CommandModules;

/// <summary>
/// Server-related commands, such as viewing or editing info about a specific server.
/// </summary>
[UsedImplicitly]
[Group("server")]
[Description("Server-related commands, such as viewing or editing info about a specific server.")]
public class ServerCommands : CommandGroup
{
    private readonly FeedbackService _feedback;
    private readonly ServerService _servers;
    private readonly ICommandContext _context;
    private readonly IDiscordRestGuildAPI _guildAPI;
    private readonly IDiscordRestChannelAPI _channelAPI;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerCommands"/> class.
    /// </summary>
    /// <param name="feedback">The user feedback service.</param>
    /// <param name="servers">The servers service.</param>
    /// <param name="context">The command context.</param>
    /// <param name="guildAPI">The guild API.</param>
    /// <param name="channelAPI">The channel API.</param>
    public ServerCommands
    (
        FeedbackService feedback,
        ServerService servers,
        ICommandContext context,
        IDiscordRestGuildAPI guildAPI,
        IDiscordRestChannelAPI channelAPI
    )
    {
        _feedback = feedback;
        _servers = servers;
        _context = context;
        _guildAPI = guildAPI;
        _channelAPI = channelAPI;
    }

    /// <summary>
    /// Shows general information about the current server.
    /// </summary>
    [UsedImplicitly]
    [Command("show")]
    [Description("Shows general information about the current server.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(ShowServerInfo), PermissionTarget.Self)]
    public async Task<IResult> ShowServerAsync()
    {
        var getServerResult = await _servers.GetOrRegisterServerAsync(_context.GuildID.Value);
        if (!getServerResult.IsSuccess)
        {
            return getServerResult;
        }

        var getGuild = await _guildAPI.GetGuildAsync(_context.GuildID.Value, ct: this.CancellationToken);
        if (!getGuild.IsSuccess)
        {
            return getGuild;
        }

        var guild = getGuild.Entity;

        var server = getServerResult.Entity;

        var fields = new[]
        {
            new EmbedField("Permission Warnings", server.SuppressPermissionWarnings ? "On" : "Off", true),
            new EmbedField("NSFW", server.IsNSFW ? "Yes" : "No"),
            new EmbedField("Send first-join message", server.SendJoinMessage ? "Yes" : "No"),
            new EmbedField
            (
                "First-join message",
                server.JoinMessage is null ? "Not set" : server.JoinMessage.Ellipsize(1024)
            )
        };

        var embed = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Title = guild.Name,
            Fields = fields
        };

        var getGuildSplash = CDN.GetGuildSplashUrl(guild);
        if (getGuildSplash.IsSuccess)
        {
            embed = embed with
            {
                Thumbnail = new EmbedThumbnail(getGuildSplash.Entity.ToString())
            };
        }
        else
        {
            var getGuildIcon = CDN.GetGuildIconUrl(guild);
            if (getGuildIcon.IsSuccess)
            {
                embed = embed with
                {
                    Thumbnail = new EmbedThumbnail(getGuildIcon.Entity.ToString())
                };
            }
        }

        var getDescription = ServerService.GetDescription(server);
        embed = embed with
        {
            Description = getDescription.IsSuccess
                ? getDescription.Entity
                : "The server doesn't have a description set."
        };

        var sendResult = await _channelAPI.CreateMessageAsync
        (
            _context.ChannelID,
            embeds: new[] { embed },
            ct: this.CancellationToken
        );

        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Shows the server's join message.
    /// </summary>
    [UsedImplicitly]
    [Command("join-message")]
    [Description("Shows the server's join message.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(ShowServerInfo), PermissionTarget.Self)]
    public async Task<IResult> ShowJoinMessageAsync()
    {
        var getServerResult = await _servers.GetOrRegisterServerAsync(_context.GuildID.Value);
        if (!getServerResult.IsSuccess)
        {
            return getServerResult;
        }

        var server = getServerResult.Entity;

        var getJoinMessageResult = ServerService.GetJoinMessage(server);
        if (!getJoinMessageResult.IsSuccess)
        {
            return getJoinMessageResult;
        }

        var embed = new Embed
        {
            Colour = _feedback.Theme.Secondary,
            Title = "Welcome!",
            Description = getJoinMessageResult.Entity
        };

        var sendResult = await _channelAPI.CreateMessageAsync
        (
            _context.ChannelID,
            embeds: new[] { embed },
            ct: this.CancellationToken
        );

        return sendResult.IsSuccess
            ? Result.FromSuccess()
            : Result.FromError(sendResult);
    }

    /// <summary>
    /// Clears the join message.
    /// </summary>
    [UsedImplicitly]
    [Command("clear-join-message")]
    [Description("Clears the join message.")]
    [RequireContext(ChannelContext.Guild)]
    [RequirePermission(typeof(EditServerInfo), PermissionTarget.Self)]
    public async Task<Result<FeedbackMessage>> ClearJoinMessageAsync()
    {
        var getServerResult = await _servers.GetOrRegisterServerAsync(_context.GuildID.Value);
        if (!getServerResult.IsSuccess)
        {
            return Result<FeedbackMessage>.FromError(getServerResult);
        }

        var server = getServerResult.Entity;

        var result = await _servers.ClearJoinMessageAsync(server);
        return result.IsSuccess
            ? new FeedbackMessage("Join message cleared.", _feedback.Theme.Secondary)
            : Result<FeedbackMessage>.FromError(result);
    }

    /// <summary>
    /// Server info setter commands.
    /// </summary>
    [UsedImplicitly]
    [Group("set")]
    public class SetCommands : CommandGroup
    {
        private readonly ServerService _servers;
        private readonly ICommandContext _context;
        private readonly FeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetCommands"/> class.
        /// </summary>
        /// <param name="servers">The servers service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="feedback">The feedback service.</param>
        public SetCommands(ServerService servers, ICommandContext context, FeedbackService feedback)
        {
            _servers = servers;
            _context = context;
            _feedback = feedback;
        }

        /// <summary>
        /// Sets the server's description.
        /// </summary>
        /// <param name="newDescription">The new description.</param>
        [Command("description")]
        [Description("Sets the server's description.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditServerInfo), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> SetDescriptionAsync(string newDescription)
        {
            var getServerResult = await _servers.GetOrRegisterServerAsync(_context.GuildID.Value);
            if (!getServerResult.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            var result = await _servers.SetDescriptionAsync(server, newDescription);
            return result.IsSuccess
                ? new FeedbackMessage("Server description set.", _feedback.Theme.Secondary)
                : Result<FeedbackMessage>.FromError(result);
        }

        /// <summary>
        /// Sets the server's first-join message.
        /// </summary>
        /// <param name="newJoinMessage">The new join message.</param>
        [Command("join-message")]
        [Description("Sets the server's first-join message.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditServerInfo), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> SetJoinMessageAsync(string newJoinMessage)
        {
            var getServerResult = await _servers.GetOrRegisterServerAsync(_context.GuildID.Value);
            if (!getServerResult.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            var result = await _servers.SetJoinMessageAsync(server, newJoinMessage);
            return result.IsSuccess
                ? new FeedbackMessage("Server first-join message set.", _feedback.Theme.Secondary)
                : Result<FeedbackMessage>.FromError(result);
        }

        /// <summary>
        /// Sets whether the server is NSFW.
        /// </summary>
        /// <param name="isNsfw">Whether the server is NSFW.</param>
        [Command("is-nsfw")]
        [Description("Sets whether the server is NSFW.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditServerInfo), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> SetIsNSFWAsync(bool isNsfw)
        {
            var getServerResult = await _servers.GetOrRegisterServerAsync(_context.GuildID.Value);
            if (!getServerResult.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            var result = await _servers.SetIsNSFWAsync(server, isNsfw);
            if (!result.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(result);
            }

            return new FeedbackMessage
            (
                $"The server is {(isNsfw ? "now set as NSFW" : "no longer NSFW")}.",
                _feedback.Theme.Secondary
            );
        }

        /// <summary>
        /// Sets whether the bot sends join messages to new users.
        /// </summary>
        /// <param name="sendJoinMessage">Whether the bot sends join messages to new users.</param>
        [Command("send-join-messages")]
        [Description("Sets whether the bot sends join messages to new users.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditServerInfo), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> SetSendJoinMessagesAsync(bool sendJoinMessage)
        {
            var getServerResult = await _servers.GetOrRegisterServerAsync(_context.GuildID.Value);
            if (!getServerResult.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            var result = await _servers.SetSendJoinMessageAsync(server, sendJoinMessage);
            if (!result.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(result);
            }

            var willDo = sendJoinMessage
                ? "will now send first-join messages to new users"
                : "no longer sends first-join messages";

            return new FeedbackMessage($"The server {willDo}.", _feedback.Theme.Secondary);
        }
    }
}
