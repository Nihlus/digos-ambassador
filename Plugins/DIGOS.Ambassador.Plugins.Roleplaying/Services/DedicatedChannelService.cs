//
//  DedicatedChannelService.cs
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

using System.Numerics;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Plugins.Core.Services;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Discord.Rest.Results;
using Remora.Results;
using static Remora.Discord.API.Abstractions.Objects.DiscordPermission;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Services
{
    /// <summary>
    /// Business logic for managing dedicated roleplay channels.
    /// </summary>
    public class DedicatedChannelService
    {
        private readonly RoleplayingDatabaseContext _database;
        private readonly RoleplayServerSettingsService _serverSettings;
        private readonly IdentityInformationService _identityInformation;
        private readonly IDiscordRestGuildAPI _guildAPI;
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="DedicatedChannelService"/> class.
        /// </summary>
        /// <param name="serverSettings">The server settings service.</param>
        /// <param name="database">The database context.</param>
        /// <param name="identityInformation">The identity information service.</param>
        /// <param name="guildAPI">The guild API.</param>
        /// <param name="channelAPI">The channel API.</param>
        public DedicatedChannelService
        (
            RoleplayServerSettingsService serverSettings,
            RoleplayingDatabaseContext database,
            IdentityInformationService identityInformation,
            IDiscordRestGuildAPI guildAPI,
            IDiscordRestChannelAPI channelAPI
        )
        {
            _serverSettings = serverSettings;
            _database = database;
            _identityInformation = identityInformation;
            _guildAPI = guildAPI;
            _channelAPI = channelAPI;
        }

        /// <summary>
        /// Creates a dedicated channel for the roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to create the channel for.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result<IChannel>> CreateDedicatedChannelAsync(Roleplay roleplay)
        {
            var getExistingChannelResult = GetDedicatedChannel(roleplay);
            if (getExistingChannelResult.IsSuccess)
            {
                return new UserError
                (
                    "The roleplay already has a dedicated channel."
                );
            }

            var getSettingsResult = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync
            (
                roleplay.Server.DiscordID
            );

            if (!getSettingsResult.IsSuccess)
            {
                return Result<IChannel>.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            if (settings.DedicatedRoleplayChannelsCategory is null)
            {
                return new UserError
                (
                    "No dedicated channel category has been configured."
                );
            }

            var createChannel = await _guildAPI.CreateGuildChannelAsync
            (
                roleplay.Server.DiscordID,
                $"{roleplay.Name}-rp",
                ChannelType.GuildText,
                parentID: settings.DedicatedRoleplayChannelsCategory.Value,
                isNsfw: roleplay.IsNSFW,
                topic: $"Dedicated roleplay channel for {roleplay.Name}. {roleplay.Summary}"
            );

            if (!createChannel.IsSuccess)
            {
                if (createChannel.Unwrap() is not DiscordRestResultError rre)
                {
                    return Result<IChannel>.FromError(createChannel);
                }

                switch (rre.DiscordError.Code)
                {
                    case DiscordError.MissingPermission:
                    {
                        return new UserError
                        (
                            "I don't have permission to manage channels, so I can't create dedicated RP channels."
                        );
                    }
                    /*
                    case DiscordError.MaxChannelsInCategory:
                    {
                        return new UserError
                        (
                            "The server's roleplaying category has reached its maximum number of channels. Try " +
                            "contacting the server's owners and either removing some old roleplays or setting up " +
                            "a new category."
                        );
                    }
                    */
                }

                return Result<IChannel>.FromError(createChannel);
            }

            var dedicatedChannel = createChannel.Entity;
            roleplay.DedicatedChannelID = dedicatedChannel.ID;

            // This can fail in all manner of ways because of Discord.NET. Try, catch, etc...
            var resetPermissions = await ResetChannelPermissionsAsync(roleplay);
            if (!resetPermissions.IsSuccess)
            {
                // Clean up - it's not nice to leave channels laying around
                var deleteChannel = await _channelAPI.DeleteChannelAsync(dedicatedChannel.ID);
                if (!deleteChannel.IsSuccess)
                {
                    return Result<IChannel>.FromError(deleteChannel);
                }

                return new UserError
                (
                    "Failed to update channel permissions. Does the bot have permissions to manage permissions on " +
                    "new channels?"
                );
            }

            await _database.SaveChangesAsync();

            return Result<IChannel>.FromSuccess(dedicatedChannel);
        }

        /// <summary>
        /// Sets the writability of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="channelID">The roleplay's dedicated channel.</param>
        /// <param name="userID">The participant to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be writable.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetChannelWritabilityForUserAsync
        (
            Snowflake channelID,
            Snowflake userID,
            bool isVisible
        )
        {
            var userPermissions = new DiscordPermissionSet(SendMessages, AddReactions);
            return await _channelAPI.EditChannelPermissionsAsync
            (
                channelID,
                userID,
                isVisible ? userPermissions : default,
                isVisible ? default : userPermissions
            );
        }

        /// <summary>
        /// Sets the visibility of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="channelID">The roleplay's dedicated channel.</param>
        /// <param name="userID">The participant to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be visible.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetChannelVisibilityForUserAsync
        (
            Snowflake channelID,
            Snowflake userID,
            bool isVisible
        )
        {
            var userPermissions = new DiscordPermissionSet(ReadMessageHistory, ViewChannel);
            return await _channelAPI.EditChannelPermissionsAsync
            (
                channelID,
                userID,
                isVisible ? userPermissions : default,
                isVisible ? default : userPermissions
            );
        }

        /// <summary>
        /// Deletes the dedicated channel for the roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay to delete the channel of.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> DeleteChannelAsync(Roleplay roleplay)
        {
            var getChannel = GetDedicatedChannel(roleplay);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            var deleteChannel = await _channelAPI.DeleteChannelAsync(channel);
            if (!deleteChannel.IsSuccess)
            {
                if (deleteChannel.Unwrap() is not DiscordRestResultError)
                {
                    return deleteChannel;
                }
            }

            roleplay.DedicatedChannelID = null;
            await _database.SaveChangesAsync();

            return Result.FromSuccess();
        }

        /// <summary>
        /// Gets the channel dedicated to the given roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public Result<Snowflake> GetDedicatedChannel(Roleplay roleplay)
        {
            if (roleplay.DedicatedChannelID is null)
            {
                return new UserError
                (
                    "The roleplay doesn't have a dedicated channel."
                );
            }

            return roleplay.DedicatedChannelID.Value;
        }

        /// <summary>
        /// Sets the visibility of the given dedicated channel for the given user.
        /// </summary>
        /// <param name="channelID">The roleplay's dedicated channel.</param>
        /// <param name="roleID">The role to grant access to.</param>
        /// <param name="isVisible">Whether or not the channel should be visible.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetChannelVisibilityForRoleAsync
        (
            Snowflake channelID,
            Snowflake roleID,
            bool isVisible
        )
        {
            var userPermissions = new DiscordPermissionSet(ReadMessageHistory, ViewChannel);
            return await _channelAPI.EditChannelPermissionsAsync
            (
                channelID,
                roleID,
                isVisible ? userPermissions : default,
                isVisible ? default : userPermissions
            );
        }

        /// <summary>
        /// Revokes the given roleplay participant access to the given roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <param name="userID">The participant to grant access to.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> RevokeUserAccessAsync(Roleplay roleplay, Snowflake userID)
        {
            var getChannel = GetDedicatedChannel(roleplay);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            return await _channelAPI.DeleteChannelPermissionAsync(channel, userID);
        }

        /// <summary>
        /// Retrieves the channel category that's set for the given server as the roleplay category.
        /// </summary>
        /// <param name="guildID">The server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<Snowflake>> GetDedicatedChannelCategoryAsync(Snowflake guildID)
        {
            var getSettingsResult = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync(guildID);
            if (!getSettingsResult.IsSuccess)
            {
                return Result<Snowflake>.FromError(getSettingsResult);
            }

            var settings = getSettingsResult.Entity;

            if (!settings.DedicatedRoleplayChannelsCategory.HasValue)
            {
                return new UserError("Failed to retrieve a valid category.");
            }

            return settings.DedicatedRoleplayChannelsCategory.Value;
        }

        /// <summary>
        /// Resets the channel permissions for the given roleplay to their default values.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> ResetChannelPermissionsAsync(Roleplay roleplay)
        {
            var getChannel = GetDedicatedChannel(roleplay);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            // First, clear all overwrites from the channel
            var clear = await ClearChannelPermissionOverwrites(channel);
            if (!clear.IsSuccess)
            {
                return clear;
            }

            // Next, apply default role settings
            var configureDefault = await ConfigureDefaultUserRolePermissions
            (
                roleplay.Server.DiscordID,
                channel
            );

            if (!configureDefault.IsSuccess)
            {
                return configureDefault;
            }

            // Finally, set up permission overrides for participants
            var updateParticipants = await UpdateParticipantPermissionsAsync(roleplay);
            if (!updateParticipants.IsSuccess)
            {
                return updateParticipants;
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Updates the permissions for the participants of the roleplay.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> UpdateParticipantPermissionsAsync(Roleplay roleplay)
        {
            var getChannel = GetDedicatedChannel(roleplay);
            if (!getChannel.IsSuccess)
            {
                return Result.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            foreach (var participant in roleplay.ParticipatingUsers)
            {
                var setWritability = await SetChannelWritabilityForUserAsync
                (
                    channel,
                    participant.User.DiscordID,
                    roleplay.IsActive
                );

                if (!setWritability.IsSuccess)
                {
                    return setWritability;
                }

                var setVisibility = await SetChannelVisibilityForUserAsync
                (
                    channel,
                    participant.User.DiscordID,
                    roleplay.IsActive
                );

                if (!setVisibility.IsSuccess)
                {
                    return setWritability;
                }
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Updates the name of the roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> UpdateChannelNameAsync(Roleplay roleplay)
        {
            if (roleplay.DedicatedChannelID is null)
            {
                return new UserError("The roleplay doesn't have a dedicated channel.");
            }

            var setName = await _channelAPI.ModifyChannelAsync
            (
                roleplay.DedicatedChannelID.Value,
                $"{roleplay.Name}-rp"
            );

            return setName.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(setName);
        }

        /// <summary>
        /// Updates the summary of the roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> UpdateChannelSummaryAsync(Roleplay roleplay)
        {
            if (roleplay.DedicatedChannelID is null)
            {
                return new UserError("The roleplay doesn't have a dedicated channel.");
            }

            var setTopic = await _channelAPI.ModifyChannelAsync
            (
                roleplay.DedicatedChannelID.Value,
                topic: $"Dedicated roleplay channel for {roleplay.Name}. {roleplay.Summary}"
            );

            return setTopic.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(setTopic);
        }

        /// <summary>
        /// Updates the NSFW status of the roleplay channel.
        /// </summary>
        /// <param name="roleplay">The roleplay.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> UpdateChannelNSFWStatus(Roleplay roleplay)
        {
            if (roleplay.DedicatedChannelID is null)
            {
                return new UserError("The roleplay doesn't have a dedicated channel.");
            }

            var setNsfwStatus = await _channelAPI.ModifyChannelAsync
            (
                roleplay.DedicatedChannelID.Value,
                isNsfw: roleplay.IsNSFW
            );

            return setNsfwStatus.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(setNsfwStatus);
        }

        /// <summary>
        /// Clears all channel permission overwrites from the given channel.
        /// </summary>
        /// <param name="channelID">The channel.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        private async Task<Result> ClearChannelPermissionOverwrites(Snowflake channelID)
        {
            var botPermissions = new DiscordPermissionSet
            (
                ViewChannel,
                ReadMessageHistory,
                AddReactions,
                ManageRoles,
                SendMessages
            );

            var botOverwrite = new PermissionOverwrite
            (
                _identityInformation.ID,
                PermissionOverwriteType.Member,
                botPermissions,
                new DiscordPermissionSet(BigInteger.Zero)
            );

            var deleteOverwrites = await _channelAPI.ModifyChannelAsync
            (
                channelID,
                permissionOverwrites: new[] { botOverwrite }
            );

            return deleteOverwrites.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(deleteOverwrites);
        }

        private async Task<Result> ConfigureDefaultUserRolePermissions
        (
            Snowflake guildID,
            Snowflake channelID
        )
        {
            var getSettings = await _serverSettings.GetOrCreateServerRoleplaySettingsAsync(guildID);
            if (!getSettings.IsSuccess)
            {
                return Result.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            // Configure visibility for everyone
            // viewChannel starts off as deny, since starting or stopping the RP will set the correct permissions.
            var deny = new DiscordPermissionSet(ViewChannel, SendMessages, AddReactions);
            var defaultUserRole = settings.DefaultUserRole ?? guildID;

            var setDefaultRolePermissions = await _channelAPI.EditChannelPermissionsAsync
            (
                channelID,
                defaultUserRole,
                deny: deny
            );

            if (!setDefaultRolePermissions.IsSuccess)
            {
                return setDefaultRolePermissions;
            }

            if (defaultUserRole == guildID)
            {
                return Result.FromSuccess();
            }

            // Also override @everyone so it can't see anything
            var setEveryoneRolePermissions = await _channelAPI.EditChannelPermissionsAsync
            (
                channelID,
                guildID,
                deny: deny
            );

            if (!setEveryoneRolePermissions.IsSuccess)
            {
                return setEveryoneRolePermissions;
            }

            return Result.FromSuccess();
        }
    }
}
