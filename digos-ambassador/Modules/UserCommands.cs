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
using System.Text;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Modules.Base;
using DIGOS.Ambassador.Permissions;
using DIGOS.Ambassador.Permissions.Preconditions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.TypeReaders;

using Discord;
using Discord.Commands;

using Humanizer;
using Humanizer.Localisation;
using JetBrains.Annotations;
using PermissionTarget = DIGOS.Ambassador.Permissions.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
    /// <summary>
    /// User-related commands, such as viewing or editing info about a specific user.
    /// </summary>
    [UsedImplicitly]
    [Group("user")]
    [Summary("User-related commands, such as viewing or editing info about a specific user.")]
    public class UserCommands : DatabaseModuleBase
    {
        private readonly UserFeedbackService Feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserCommands"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="feedback">The user feedback service.</param>
        public UserCommands(GlobalInfoContext database, UserFeedbackService feedback)
            : base(database)
        {
            this.Feedback = feedback;
        }

        /// <summary>
        /// Shows known information about the invoking user.
        /// </summary>
        [UsedImplicitly]
        [Command("info", RunMode = RunMode.Async)]
        [Summary("Shows known information about the invoking user.")]
        public async Task ShowInfoAsync()
        {
            var getUserResult = await this.Database.GetOrRegisterUserAsync(this.Context.Message.Author);
            if (!getUserResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                return;
            }

            var user = getUserResult.Entity;
            await ShowUserInfoAsync(this.Context.Message.Author, user);
        }

        /// <summary>
        /// Shows known information about the mentioned user.
        /// </summary>
        /// <param name="discordUser">The Discord user to show the info of.</param>
        [UsedImplicitly]
        [Command("info", RunMode = RunMode.Async)]
        [Summary("Shows known information about the target user.")]
        public async Task ShowInfoAsync([NotNull] IUser discordUser)
        {
            var getUserResult = await this.Database.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
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

            switch (user.Class)
            {
                case UserClass.Other:
                {
                    eb.WithColor(1.0f, 1.0f, 1.0f); // White
                    break;
                }
                case UserClass.DIGOSInfrastructure:
                {
                    eb.WithColor(Color.Purple);
                    break;
                }
                case UserClass.DIGOSDronie:
                {
                    eb.WithColor(Color.DarkOrange);
                    break;
                }
                case UserClass.DIGOSUnit:
                {
                    eb.WithColor(Color.DarkPurple);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            eb.AddField("Name", discordUser.Username);
            eb.AddField("Class", user.Class.Humanize().Transform(To.TitleCase));

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

            await this.Feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
        }

        /// <summary>
        /// User info edit and set commands.
        /// </summary>
        [UsedImplicitly]
        [Group("set")]
        public class SetCommands : DatabaseModuleBase
        {
            private readonly UserFeedbackService Feedback;

            /// <summary>
            /// Initializes a new instance of the <see cref="SetCommands"/> class.
            /// </summary>
            /// <param name="database">A database context from the context pool.</param>
            /// <param name="feedback">The user feedback service.</param>
            public SetCommands(GlobalInfoContext database, UserFeedbackService feedback)
                : base(database)
            {
                this.Feedback = feedback;
            }

            /// <summary>
            /// Sets the invoking user's class.
            /// </summary>
            /// <param name="userClass">The user's new class.</param>
            [UsedImplicitly]
            [Command("class", RunMode = RunMode.Async)]
            [Summary("Sets the invoking user's class.")]
            [RequirePermission(Permission.SetClass)]
            public async Task SetUserClassAsync
            (
                [OverrideTypeReader(typeof(HumanizerEnumTypeReader<UserClass>))]
                UserClass userClass
            )
            {
                // Add the user to the user database if they're not already in it
                var getUserResult = await this.Database.GetOrRegisterUserAsync(this.Context.Message.Author);
                if (!getUserResult.IsSuccess)
                {
                    await this.Feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                    return;
                }

                var user = getUserResult.Entity;

                user.Class = userClass;

                await this.Database.SaveChangesAsync();

                await this.Feedback.SendConfirmationAsync(this.Context, "Class updated.");
            }

            /// <summary>
            /// Sets the target user's class.
            /// </summary>
            /// <param name="discordUser">The Discord user to change the class of.</param>
            /// <param name="userClass">The user's new class.</param>
            [UsedImplicitly]
            [Command("class", RunMode = RunMode.Async)]
            [Summary("Sets the target user's class.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(Permission.SetClass, PermissionTarget.Other)]
            public async Task SetUserClassAsync
            (
                [NotNull]
                IUser discordUser,
                [OverrideTypeReader(typeof(HumanizerEnumTypeReader<UserClass>))]
                UserClass userClass
            )
            {
                // Add the user to the user database if they're not already in it
                var getUserResult = await this.Database.GetOrRegisterUserAsync(discordUser);
                if (!getUserResult.IsSuccess)
                {
                    await this.Feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                    return;
                }

                var user = getUserResult.Entity;

                user.Class = userClass;

                await this.Database.SaveChangesAsync();

                await this.Feedback.SendConfirmationAsync(this.Context, $"Class of {discordUser.Mention} updated.");
            }

            /// <summary>
            /// Sets the invoking user's bio.
            /// </summary>
            /// <param name="bio">The user's new bio.</param>
            [UsedImplicitly]
            [Command("bio", RunMode = RunMode.Async)]
            [Summary("Sets the invoking user's bio.")]
            [RequirePermission(Permission.EditUser)]
            public async Task SetUserBioAsync([NotNull] string bio)
            {
                // Add the user to the user database if they're not already in it
                var getUserResult = await this.Database.GetOrRegisterUserAsync(this.Context.User);
                if (!getUserResult.IsSuccess)
                {
                    await this.Feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                    return;
                }

                var user = getUserResult.Entity;

                user.Bio = bio;

                await this.Database.SaveChangesAsync();

                await this.Feedback.SendConfirmationAsync(this.Context, "Bio updated.");
            }

            /// <summary>
            /// Sets the target user's bio.
            /// </summary>
            /// <param name="discordUser">The Discord user to change the bio of.</param>
            /// <param name="bio">The user's new bio.</param>
            [UsedImplicitly]
            [Command("bio", RunMode = RunMode.Async)]
            [Summary("Sets the target user's bio.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(Permission.EditUser, PermissionTarget.Other)]
            public async Task SetUserBioAsync([NotNull] IUser discordUser, [NotNull] string bio)
            {
                // Add the user to the user database if they're not already in it
                var getUserResult = await this.Database.GetOrRegisterUserAsync(discordUser);
                if (!getUserResult.IsSuccess)
                {
                    await this.Feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                    return;
                }

                var user = getUserResult.Entity;

                user.Bio = bio;

                await this.Database.SaveChangesAsync();

                await this.Feedback.SendConfirmationAsync(this.Context, $"Bio of {discordUser.Mention} updated.");
            }

            /// <summary>
            /// Sets the invoking user's UTC timezone hour offset.
            /// </summary>
            /// <param name="timezone">The user's new timezone hour offset.</param>
            [UsedImplicitly]
            [Command("timezone", RunMode = RunMode.Async)]
            [Summary("Sets the invoking user's UTC timezone hour offset.")]
            [RequirePermission(Permission.EditUser)]
            public async Task SetUserTimezoneAsync(int timezone)
            {
                // Add the user to the user database if they're not already in it
                var getUserResult = await this.Database.GetOrRegisterUserAsync(this.Context.User);
                if (!getUserResult.IsSuccess)
                {
                    await this.Feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                    return;
                }

                var user = getUserResult.Entity;

                user.Timezone = timezone;

                await this.Database.SaveChangesAsync();

                await this.Feedback.SendConfirmationAsync(this.Context, "Timezone updated.");
            }

            /// <summary>
            /// Sets the target user's UTC timezone hour offset.
            /// </summary>
            /// <param name="discordUser">The Discord user to change the timezone of.</param>
            /// <param name="timezone">The user's new timezone hour offset.</param>
            [UsedImplicitly]
            [Command("timezone", RunMode = RunMode.Async)]
            [Summary("Sets the target user's UTC timezone hour offset.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(Permission.EditUser, PermissionTarget.Other)]
            public async Task SetUserTimezoneAsync([NotNull] IUser discordUser, int timezone)
            {
                // Add the user to the user database if they're not already in it
                var getUserResult = await this.Database.GetOrRegisterUserAsync(discordUser);
                if (!getUserResult.IsSuccess)
                {
                    await this.Feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                    return;
                }

                var user = getUserResult.Entity;

                user.Timezone = timezone;

                await this.Database.SaveChangesAsync();

                await this.Feedback.SendConfirmationAsync(this.Context, $"Timezone of {discordUser.Mention} updated.");
            }
        }
    }
}
