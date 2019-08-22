//
//  UserCommands.cs
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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Core.Permissions;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using JetBrains.Annotations;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Core.CommandModules
{
    /// <summary>
    /// User-related commands, such as viewing or editing info about a specific user.
    /// </summary>
    [UsedImplicitly]
    [Group("user")]
    [Summary("User-related commands, such as viewing or editing info about a specific user.")]
    public class UserCommands : ModuleBase<SocketCommandContext>
    {
        private readonly UserService _users;
        private readonly UserFeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserCommands"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="users">The user service.</param>
        public UserCommands(CoreDatabaseContext database, UserFeedbackService feedback, UserService users)
        {
            _feedback = feedback;
            _users = users;
        }

        /// <summary>
        /// Shows known information about the invoking user.
        /// </summary>
        [UsedImplicitly]
        [Command("info")]
        [Summary("Shows known information about the invoking user.")]
        [RequirePermission(typeof(ShowUserInfo), PermissionTarget.Self)]
        public async Task ShowInfoAsync() => await ShowInfoAsync(this.Context.User);

        /// <summary>
        /// Shows known information about the mentioned user.
        /// </summary>
        /// <param name="discordUser">The Discord user to show the info of.</param>
        [UsedImplicitly]
        [Command("info")]
        [Summary("Shows known information about the target user.")]
        [RequirePermission(typeof(ShowUserInfo), PermissionTarget.Other)]
        public async Task ShowInfoAsync([NotNull] IUser discordUser)
        {
            var getUserResult = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                return;
            }

            var user = getUserResult.Entity;
            await ShowUserInfoAsync(discordUser, user);
        }

        /// <summary>
        /// Shows a nicely formatted info block about a user.
        /// </summary>
        /// <param name="discordUser">The Discord user to show the info of.</param>
        /// <param name="user">The stored information about the user.</param>
        private async Task ShowUserInfoAsync([NotNull] IUser discordUser, [NotNull] User user)
        {
            var eb = new EmbedBuilder();

            eb.WithAuthor(discordUser);
            eb.WithThumbnailUrl(discordUser.GetAvatarUrl());

            if (discordUser is SocketGuildUser guildUser)
            {
                var primaryRole = guildUser.Roles.OrderByDescending(r => r.Position).FirstOrDefault();
                if (!(primaryRole is null) && !primaryRole.IsEveryone)
                {
                    eb.WithColor(primaryRole.Color);
                }
                else
                {
                    eb.WithColor(Color.LighterGrey);
                }
            }

            eb.AddField("Name", discordUser.Username);

            string timezoneValue;
            if (user.Timezone is null)
            {
                timezoneValue = "No timezone set.";
            }
            else
            {
                timezoneValue = "UTC";
                if (user.Timezone >= 0)
                {
                    timezoneValue += "+";
                }

                timezoneValue += user.Timezone.Value;
            }

            eb.AddField("Timezone", timezoneValue);

            string bioValue;
            if (string.IsNullOrEmpty(user.Bio))
            {
                bioValue = "No bio set.";
            }
            else
            {
                bioValue = user.Bio;
            }

            eb.AddField("Bio", bioValue);

            var technicalInfo = new StringBuilder();
            technicalInfo.AppendLine($"ID: {discordUser.Id}");

            var span = DateTime.UtcNow - discordUser.CreatedAt;

            var humanizedTimeAgo = span > TimeSpan.FromSeconds(60)
                ? span.Humanize(maxUnit: TimeUnit.Year, culture: CultureInfo.InvariantCulture)
                : "a few seconds";

            var created = $"{humanizedTimeAgo} ago ({discordUser.CreatedAt.UtcDateTime:yyyy-MM-ddTHH:mm:ssK})\n";

            technicalInfo.AppendLine($"Created: {created}");

            eb.AddField("Technical Info", technicalInfo.ToString());

            await _feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
        }

        /// <summary>
        /// User info edit and set commands.
        /// </summary>
        [UsedImplicitly]
        [Group("set")]
        public class SetCommands : ModuleBase<SocketCommandContext>
        {
            private readonly CoreDatabaseContext _database;
            private readonly UserService _users;
            private readonly UserFeedbackService _feedback;

            /// <summary>
            /// Initializes a new instance of the <see cref="SetCommands"/> class.
            /// </summary>
            /// <param name="database">A database context from the context pool.</param>
            /// <param name="feedback">The user feedback service.</param>
            /// <param name="users">The user service.</param>
            public SetCommands(CoreDatabaseContext database, UserFeedbackService feedback, UserService users)
            {
                _database = database;
                _feedback = feedback;
                _users = users;
            }

            /// <summary>
            /// Sets the invoking user's bio.
            /// </summary>
            /// <param name="bio">The user's new bio.</param>
            [UsedImplicitly]
            [Command("bio")]
            [Summary("Sets the invoking user's bio.")]
            [RequirePermission(typeof(EditUserInfo), PermissionTarget.Self)]
            public async Task SetUserBioAsync([NotNull] string bio) => await SetUserBioAsync(this.Context.User, bio);

            /// <summary>
            /// Sets the target user's bio.
            /// </summary>
            /// <param name="discordUser">The Discord user to change the bio of.</param>
            /// <param name="bio">The user's new bio.</param>
            [UsedImplicitly]
            [Command("bio")]
            [Summary("Sets the target user's bio.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditUserInfo), PermissionTarget.Other)]
            public async Task SetUserBioAsync([NotNull] IUser discordUser, [NotNull] string bio)
            {
                // Add the user to the user database if they're not already in it
                var getUserResult = await _users.GetOrRegisterUserAsync(discordUser);
                if (!getUserResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                    return;
                }

                var user = getUserResult.Entity;

                user.Bio = bio;

                await _database.SaveChangesAsync();

                await _feedback.SendConfirmationAsync(this.Context, $"Bio of {discordUser.Mention} updated.");
            }

            /// <summary>
            /// Sets the invoking user's UTC timezone hour offset.
            /// </summary>
            /// <param name="timezone">The user's new timezone hour offset.</param>
            [UsedImplicitly]
            [Command("timezone")]
            [Summary("Sets the invoking user's UTC timezone hour offset.")]
            [RequirePermission(typeof(EditUserInfo), PermissionTarget.Self)]
            public async Task SetUserTimezoneAsync(int timezone)
                => await SetUserTimezoneAsync(this.Context.User, timezone);

            /// <summary>
            /// Sets the target user's UTC timezone hour offset.
            /// </summary>
            /// <param name="discordUser">The Discord user to change the timezone of.</param>
            /// <param name="timezone">The user's new timezone hour offset.</param>
            [UsedImplicitly]
            [Command("timezone")]
            [Summary("Sets the target user's UTC timezone hour offset.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditUserInfo), PermissionTarget.Other)]
            public async Task SetUserTimezoneAsync([NotNull] IUser discordUser, int timezone)
            {
                // Add the user to the user database if they're not already in it
                var getUserResult = await _users.GetOrRegisterUserAsync(discordUser);
                if (!getUserResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                    return;
                }

                var user = getUserResult.Entity;

                user.Timezone = timezone;

                await _database.SaveChangesAsync();

                await _feedback.SendConfirmationAsync(this.Context, $"Timezone of {discordUser.Mention} updated.");
            }
        }
    }
}
