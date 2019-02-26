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

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Servers;

using Discord;
using Discord.Commands;

using JetBrains.Annotations;
using static Discord.Commands.ContextType;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
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
        private readonly GlobalInfoContext Database;
        private readonly UserFeedbackService Feedback;
        private readonly ServerService Servers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerCommands"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="servers">The servers service.</param>
        public ServerCommands(GlobalInfoContext database, UserFeedbackService feedback, ServerService servers)
        {
            this.Database = database;
            this.Feedback = feedback;
            this.Servers = servers;
        }

        /// <summary>
        /// Shows general information about the current server.
        /// </summary>
        [UsedImplicitly]
        [Command("show", RunMode = RunMode.Async)]
        [Alias("show", "info")]
        [Summary("Shows general information about the current server.")]
        [RequireContext(Guild)]
        public async Task ShowServerAsync()
        {
            var eb = this.Feedback.CreateEmbedBase();

            var guild = this.Context.Guild;
            var server = await this.Database.GetOrRegisterServerAsync(this.Context.Guild);

            eb.WithTitle(guild.Name);

            if (!(guild.SplashUrl is null))
            {
                eb.WithImageUrl(guild.SplashUrl);
            }
            else if (!(guild.IconUrl is null))
            {
                eb.WithImageUrl(guild.IconUrl);
            }

            var getDescriptionResult = this.Servers.GetDescription(server);
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

            await this.Feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
        }

        /// <summary>
        /// Shows the server's join message.
        /// </summary>
        [UsedImplicitly]
        [Command("join-message", RunMode = RunMode.Async)]
        [Summary("Shows the server's join message.")]
        [RequireContext(Guild)]
        public async Task ShowJoinMessageAsync()
        {
            var server = await this.Database.GetOrRegisterServerAsync(this.Context.Guild);

            var getJoinMessageResult = this.Servers.GetJoinMessage(server);
            if (!getJoinMessageResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getJoinMessageResult.ErrorReason);
                return;
            }

            var eb = this.Feedback.CreateEmbedBase();

            eb.WithTitle("Welcome!");
            eb.WithDescription(getJoinMessageResult.Entity);

            await this.Feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
        }

        /// <summary>
        /// Server info setter commands.
        /// </summary>
        [UsedImplicitly]
        [Group("set")]
        public class SetCommands : ModuleBase<SocketCommandContext>
        {
            [ProvidesContext]
            private readonly GlobalInfoContext Database;
            private readonly UserFeedbackService Feedback;
            private readonly ServerService Servers;

            /// <summary>
            /// Initializes a new instance of the <see cref="SetCommands"/> class.
            /// </summary>
            /// <param name="database">A database context from the context pool.</param>
            /// <param name="feedback">The user feedback service.</param>
            /// <param name="servers">The servers service.</param>
            public SetCommands(GlobalInfoContext database, UserFeedbackService feedback, ServerService servers)
            {
                this.Database = database;
                this.Feedback = feedback;
                this.Servers = servers;
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
                var server = await this.Database.GetOrRegisterServerAsync(this.Context.Guild);
                var result = await this.Servers.SetDescriptionAsync(this.Database, server, newDescription);
                if (!result.IsSuccess)
                {
                    await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await this.Feedback.SendConfirmationAsync(this.Context, "Server description set.");
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
                var server = await this.Database.GetOrRegisterServerAsync(this.Context.Guild);
                var result = await this.Servers.SetJoinMessageAsync(this.Database, server, newJoinMessage);
                if (!result.IsSuccess)
                {
                    await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await this.Feedback.SendConfirmationAsync(this.Context, "Server first-join message set.");
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
                var server = await this.Database.GetOrRegisterServerAsync(this.Context.Guild);
                var result = await this.Servers.SetIsNSFWAsync(this.Database, server, isNsfw);
                if (!result.IsSuccess)
                {
                    await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await this.Feedback.SendConfirmationAsync
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
                var server = await this.Database.GetOrRegisterServerAsync(this.Context.Guild);
                var result = await this.Servers.SetSendJoinMessageAsync(this.Database, server, sendJoinMessage);
                if (!result.IsSuccess)
                {
                    await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                var willDo = sendJoinMessage
                    ? "will now send first-join messages to new users"
                    : "no longer sends first-join messages";

                await this.Feedback.SendConfirmationAsync
                (
                    this.Context,
                    $"The server {willDo}."
                );
            }
        }
    }
}
