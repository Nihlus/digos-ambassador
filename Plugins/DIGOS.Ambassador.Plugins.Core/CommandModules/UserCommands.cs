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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Plugins.Core.Permissions;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using Humanizer;
using Humanizer.Localisation;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;
using User = DIGOS.Ambassador.Plugins.Core.Model.Users.User;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks
#pragma warning disable SA1118

namespace DIGOS.Ambassador.Plugins.Core.CommandModules
{
    /// <summary>
    /// User-related commands, such as viewing or editing info about a specific user.
    /// </summary>
    [Group("user")]
    [Description("User-related commands, such as viewing or editing info about a specific user.")]
    public class UserCommands : CommandGroup
    {
        private readonly UserService _users;
        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly IDiscordRestGuildAPI _guildAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserCommands"/> class.
        /// </summary>
        /// <param name="users">The user service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="guildAPI">The guild API.</param>
        public UserCommands(UserService users, ICommandContext context, IDiscordRestChannelAPI channelAPI, IDiscordRestGuildAPI guildAPI)
        {
            _users = users;
            _context = context;
            _channelAPI = channelAPI;
            _guildAPI = guildAPI;
        }

        /// <summary>
        /// Shows known information about the mentioned user.
        /// </summary>
        /// <param name="discordUser">The Discord user to show the info of.</param>
        [UsedImplicitly]
        [Command("info")]
        [Description("Shows known information about the target user.")]
        [RequirePermission(typeof(ShowUserInfo), PermissionTarget.Other)]
        public async Task<IResult> ShowInfoAsync(IUser discordUser)
        {
            var getUserResult = await _users.GetOrRegisterUserAsync(discordUser.ID);
            if (!getUserResult.IsSuccess)
            {
                return getUserResult;
            }

            var user = getUserResult.Entity;
            return await ShowUserInfoAsync(discordUser, user);
        }

        /// <summary>
        /// Shows a nicely formatted info block about a user.
        /// </summary>
        /// <param name="discordUser">The Discord user to show the info of.</param>
        /// <param name="user">The stored information about the user.</param>
        private async Task<Result> ShowUserInfoAsync(IUser discordUser, User user)
        {
            var embedFields = new List<IEmbedField>();

            var getUserAvatar = CDN.GetUserAvatarUrl(discordUser);
            var embed = new Embed
            {
                Author = new EmbedAuthor
                (
                    $"{discordUser.Username}#{discordUser.Discriminator}",
                    IconUrl: getUserAvatar.IsSuccess
                        ? getUserAvatar.Entity.ToString()
                        : default(Optional<string>)
                ),
                Thumbnail = new EmbedThumbnail
                (
                    getUserAvatar.IsSuccess
                        ? getUserAvatar.Entity.ToString()
                        : default(Optional<string>)
                ),
                Fields = embedFields
            };

            if (_context.GuildID.HasValue)
            {
                var getMember = await _guildAPI.GetGuildMemberAsync
                (
                    _context.GuildID.Value,
                    discordUser.ID,
                    this.CancellationToken
                );

                if (!getMember.IsSuccess)
                {
                    return Result.FromError(getMember);
                }

                var member = getMember.Entity;
                if (member.Roles.Count > 0)
                {
                    var getRoles = await _guildAPI.GetGuildRolesAsync(_context.GuildID.Value, this.CancellationToken);
                    if (!getRoles.IsSuccess)
                    {
                        return Result.FromError(getRoles);
                    }

                    var roles = getRoles.Entity;
                    var primaryRole = roles.OrderByDescending(r => r.Position).First(r => member.Roles.Contains(r.ID));
                    embed = embed with
                    {
                        Colour = primaryRole.Colour
                    };
                }
            }
            else
            {
                embed = embed with
                {
                    Colour = Color.LightSlateGray
                };
            }

            embedFields.Add(new EmbedField("Name", discordUser.Username));

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

            embedFields.Add(new EmbedField("Timezone", timezoneValue));

            string bioValue = string.IsNullOrEmpty(user.Bio) ? "No bio set." : user.Bio;

            embedFields.Add(new EmbedField("Bio", bioValue));

            var technicalInfo = new StringBuilder();
            technicalInfo.AppendLine($"ID: {discordUser.ID}");

            var span = DateTime.UtcNow - discordUser.ID.Timestamp;

            var humanizedTimeAgo = span > TimeSpan.FromSeconds(60)
                ? span.Humanize(maxUnit: TimeUnit.Year, culture: CultureInfo.InvariantCulture)
                : "a few seconds";

            var created = $"{humanizedTimeAgo} ago ({discordUser.ID.Timestamp.UtcDateTime:yyyy-MM-ddTHH:mm:ssK})\n";

            technicalInfo.AppendLine($"Created: {created}");

            embedFields.Add(new EmbedField("Technical Info", technicalInfo.ToString()));

            var sendEmbed = await _channelAPI.CreateMessageAsync
            (
                _context.ChannelID,
                embeds: new[] { embed },
                ct: this.CancellationToken
            );

            return sendEmbed.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(sendEmbed);
        }

        /// <summary>
        /// User info edit and set commands.
        /// </summary>
        [UsedImplicitly]
        [Group("set")]
        public class SetCommands : CommandGroup
        {
            private readonly UserService _users;

            /// <summary>
            /// Initializes a new instance of the <see cref="SetCommands"/> class.
            /// </summary>
            /// <param name="users">The user service.</param>
            public SetCommands(UserService users)
            {
                _users = users;
            }

            /// <summary>
            /// Sets the target user's bio.
            /// </summary>
            /// <param name="discordUser">The Discord user to change the bio of.</param>
            /// <param name="bio">The user's new bio.</param>
            [Command("bio")]
            [Description("Sets the target user's bio.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditUserInfo), PermissionTarget.Other)]
            public async Task<Result<UserMessage>> SetUserBioAsync(IUser discordUser, string bio)
            {
                // Add the user to the user database if they're not already in it
                var getUserResult = await _users.GetOrRegisterUserAsync(discordUser.ID);
                if (!getUserResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getUserResult);
                }

                var user = getUserResult.Entity;

                var setBioResult = await _users.SetUserBioAsync(user, bio);
                if (!setBioResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(setBioResult);
                }

                return new ConfirmationMessage("Bio updated.");
            }

            /// <summary>
            /// Sets the target user's UTC timezone hour offset.
            /// </summary>
            /// <param name="discordUser">The Discord user to change the timezone of.</param>
            /// <param name="timezone">The user's new timezone hour offset.</param>
            [Command("timezone")]
            [Description("Sets the target user's UTC timezone hour offset.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditUserInfo), PermissionTarget.Other)]
            public async Task<Result<UserMessage>> SetUserTimezoneAsync(IUser discordUser, int timezone)
            {
                // Add the user to the user database if they're not already in it
                var getUserResult = await _users.GetOrRegisterUserAsync(discordUser.ID);
                if (!getUserResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getUserResult);
                }

                var user = getUserResult.Entity;

                var setTimezoneResult = await _users.SetUserTimezoneAsync(user, timezone);
                if (!setTimezoneResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(setTimezoneResult);
                }

                return new ConfirmationMessage("Timezone updated.");
            }
        }
    }
}
