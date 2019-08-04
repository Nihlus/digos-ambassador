//
//  ServerCommands.cs
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using static Discord.Commands.ContextType;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Core.CommandModules
{
    /// <summary>
    /// Server-related commands, such as viewing or editing info about a specific server.
    /// </summary>
    [UsedImplicitly]
    [Group("server")]
    [Alias("server", "guild")]
    [Summary("Server-related commands, such as viewing or editing info about a specific server.")]
    public class ServerCommands : ModuleBase<SocketCommandContext>
    {
        private readonly UserFeedbackService _feedback;
        private readonly ServerService _servers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerCommands"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="servers">The servers service.</param>
        public ServerCommands(CoreDatabaseContext database, UserFeedbackService feedback, ServerService servers)
        {
            _feedback = feedback;
            _servers = servers;
        }

        /// <summary>
        /// Shows general information about the current server.
        /// </summary>
        [UsedImplicitly]
        [Command("show")]
        [Alias("show", "info")]
        [Summary("Shows general information about the current server.")]
        [RequireContext(Guild)]
        public async Task ShowServerAsync()
        {
            var eb = _feedback.CreateEmbedBase();

            var guild = this.Context.Guild;
            var server = await _servers.GetOrRegisterServerAsync(this.Context.Guild);

            eb.WithTitle(guild.Name);

            if (!(guild.SplashUrl is null))
            {
                eb.WithThumbnailUrl(guild.SplashUrl);
            }
            else if (!(guild.IconUrl is null))
            {
                eb.WithThumbnailUrl(guild.IconUrl);
            }

            var getDescriptionResult = _servers.GetDescription(server);
            if (getDescriptionResult.IsSuccess)
            {
                eb.WithDescription(getDescriptionResult.Entity);
            }
            else
            {
                eb.WithDescription("The server doesn't have a description set.");
            }

            eb.AddField("Permission Warnings", server.SuppressPermissonWarnings ? "On" : "Off", true);
            eb.AddField("NSFW", server.IsNSFW ? "Yes" : "No");

            string content;
            if (server.SendJoinMessage)
            {
                content = server.JoinMessage is null ? "Yes (no join message set)" : "Yes";
            }
            else
            {
                content = server.JoinMessage is null ? "No" : "No (join message set)";
            }

            eb.AddField("First-join Message", content);

            await _feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
        }

        /// <summary>
        /// Shows the server's join message.
        /// </summary>
        [UsedImplicitly]
        [Command("join-message")]
        [Summary("Shows the server's join message.")]
        [RequireContext(Guild)]
        public async Task ShowJoinMessageAsync()
        {
            var server = await _servers.GetOrRegisterServerAsync(this.Context.Guild);

            var getJoinMessageResult = _servers.GetJoinMessage(server);
            if (!getJoinMessageResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getJoinMessageResult.ErrorReason);
                return;
            }

            var eb = _feedback.CreateEmbedBase();

            eb.WithTitle("Welcome!");
            eb.WithDescription(getJoinMessageResult.Entity);

            await _feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
        }

        /// <summary>
        /// Clears the channel category to use for dedicated roleplays.
        /// </summary>
        [UsedImplicitly]
        [Command("clear-roleplay-category")]
        [Summary("Clears the channel category to use for dedicated roleplays.")]
        [RequireContext(Guild)]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task ClearDedicatedRoleplayChannelCategory()
        {
            var server = await _servers.GetOrRegisterServerAsync(this.Context.Guild);
            var result = await _servers.SetDedicatedRoleplayChannelCategoryAsync(server, null);

            if (!result.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Dedicated channel category cleared.");
        }

        /// <summary>
        /// Server info setter commands.
        /// </summary>
        [UsedImplicitly]
        [Group("set")]
        public class SetCommands : ModuleBase<SocketCommandContext>
        {
            private readonly CoreDatabaseContext _database;
            private readonly UserFeedbackService _feedback;
            private readonly ServerService _servers;

            /// <summary>
            /// Initializes a new instance of the <see cref="SetCommands"/> class.
            /// </summary>
            /// <param name="database">A database context from the context pool.</param>
            /// <param name="feedback">The user feedback service.</param>
            /// <param name="servers">The servers service.</param>
            public SetCommands(CoreDatabaseContext database, UserFeedbackService feedback, ServerService servers)
            {
                _database = database;
                _feedback = feedback;
                _servers = servers;
            }

            /// <summary>
            /// Sets the server's description.
            /// </summary>
            /// <param name="newDescription">The new description.</param>
            [UsedImplicitly]
            [Command("description")]
            [Summary("Sets the server's description.")]
            [RequireContext(Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task SetDescriptionAsync([NotNull] string newDescription)
            {
                var server = await _servers.GetOrRegisterServerAsync(this.Context.Guild);
                var result = await _servers.SetDescriptionAsync(server, newDescription);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Server description set.");
            }

            /// <summary>
            /// Sets the server's first-join message.
            /// </summary>
            /// <param name="newJoinMessage">The new join message.</param>
            [UsedImplicitly]
            [Command("join-message")]
            [Summary("Sets the server's first-join message.")]
            [RequireContext(Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task SetJoinMessageAsync([NotNull] string newJoinMessage)
            {
                var server = await _servers.GetOrRegisterServerAsync(this.Context.Guild);
                var result = await _servers.SetJoinMessageAsync(server, newJoinMessage);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Server first-join message set.");
            }

            /// <summary>
            /// Sets whether the server is NSFW.
            /// </summary>
            /// <param name="isNsfw">Whether the server is NSFW.</param>
            [UsedImplicitly]
            [Command("is-nsfw")]
            [Summary("Sets whether the server is NSFW.")]
            [RequireContext(Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task SetIsNSFWAsync(bool isNsfw)
            {
                var server = await _servers.GetOrRegisterServerAsync(this.Context.Guild);
                var result = await _servers.SetIsNSFWAsync(server, isNsfw);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync
                (
                    this.Context,
                    $"The server is {(isNsfw ? "now set as NSFW" : "no longer NSFW")}."
                );
            }

            /// <summary>
            /// Sets whether the bot sends join messages to new users.
            /// </summary>
            /// <param name="sendJoinMessage">Whether the bot sends join messages to new users.</param>
            [UsedImplicitly]
            [Command("send-join-messages")]
            [Summary("Sets whether the bot sends join messages to new users.")]
            [RequireContext(Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task SetSendJoinMessagesAsync(bool sendJoinMessage)
            {
                var server = await _servers.GetOrRegisterServerAsync(this.Context.Guild);
                var result = await _servers.SetSendJoinMessageAsync(server, sendJoinMessage);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                var willDo = sendJoinMessage
                    ? "will now send first-join messages to new users"
                    : "no longer sends first-join messages";

                await _feedback.SendConfirmationAsync
                (
                    this.Context,
                    $"The server {willDo}."
                );
            }

            /// <summary>
            /// Sets the channel category to use for dedicated roleplays.
            /// </summary>
            /// <param name="category">The category to use.</param>
            [UsedImplicitly]
            [Command("roleplay-category")]
            [Summary("Sets the channel category to use for dedicated roleplays.")]
            [RequireContext(Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task SetDedicatedRoleplayChannelCategory(ICategoryChannel category)
            {
                var server = await _servers.GetOrRegisterServerAsync(this.Context.Guild);
                var result = await _servers.SetDedicatedRoleplayChannelCategoryAsync
                (
                    server,
                    category
                );

                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Dedicated channel category set.");
            }
        }
    }
}
