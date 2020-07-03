//
//  PrivacyService.cs
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

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Discord;
using Discord.Net;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Results;
using Zio;

namespace DIGOS.Ambassador.Plugins.Core.Services.Users
{
    /// <summary>
    /// Handles privacy-related logic.
    /// </summary>
    [PublicAPI]
    public sealed class PrivacyService
    {
        private readonly CoreDatabaseContext _database;

        private readonly UserFeedbackService _feedback;

        private readonly ContentService _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyService"/> class.
        /// </summary>
        /// <param name="database">The core database.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="content">The content service.</param>
        public PrivacyService
        (
            CoreDatabaseContext database,
            UserFeedbackService feedback,
            ContentService content
        )
        {
            _database = database;
            _feedback = feedback;
            _content = content;
        }

        /// <summary>
        /// Sends a consent request to the given DM channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>An execution result.</returns>
        public async Task<DetermineConditionResult> RequestConsentAsync(IDMChannel channel)
        {
            try
            {
                var consentBuilder = _feedback.CreateEmbedBase(Color.Orange);
                consentBuilder.WithDescription
                (
                    "Hello there! This appears to be the first time you're using the bot (or you've not granted your " +
                    "consent for it to store potentially sensitive or identifiable data about you).\n" +
                    "\n" +
                    "In order to use Amby and her commands, you need to give her your consent to store various data " +
                    "about you. We need this consent in order to be compliant with data regulations in the European " +
                    "Union (and it'd be rude not to ask!).\n" +
                    "\n" +
                    "In short, if you use the bot, we're going to be storing " +
                    "stuff like your Discord ID, some messages, server IDs, etc. You can - and should! - read the " +
                    "full privacy policy before you agree to anything. It's not very long (3 pages) and shouldn't take " +
                    "more than five minutes to read through.\n" +
                    "\n" +
                    "Once you've read it, you can grant consent by running the `!privacy grant-consent` command over DM. If you " +
                    "don't want to consent to anything, just don't use the bot :smiley:"
                );

                await channel.SendMessageAsync(string.Empty, embed: consentBuilder.Build());

                await SendPrivacyPolicyAsync(channel);
            }
            catch (HttpException hex) when (hex.HttpCode == HttpStatusCode.Forbidden)
            {
                return DetermineConditionResult.FromError("Could not send the privacy message over DM.");
            }

            return DetermineConditionResult.FromSuccess();
        }

        /// <summary>
        /// Sends the privacy policy to the given channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>A task that must be awaited.</returns>
        public async Task SendPrivacyPolicyAsync(IMessageChannel channel)
        {
            var result = _content.OpenLocalStream(UPath.Combine(UPath.Root, "Privacy", "PrivacyPolicy.pdf"));
            if (!result.IsSuccess)
            {
                var errorBuilder = _feedback.CreateEmbedBase(Color.Red);
                errorBuilder.WithDescription
                (
                    "Oops. Something went wrong, and I couldn't grab the privacy policy. Please report this to the " +
                    "developer, don't agree to anything, and read it online instead."
                );

                errorBuilder.AddField("Privacy Policy", _content.PrivacyPolicyUri);

                await channel.SendMessageAsync(string.Empty, embed: errorBuilder.Build());
            }

            await using var privacyPolicy = result.Entity;
            await channel.SendFileAsync(privacyPolicy, "PrivacyPolicy.pdf");
        }

        /// <summary>
        /// Determines whether or not the given user has granted consent to store user data.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        /// <returns>true if the user has granted consent; Otherwise, false.</returns>
        [Pure]
        public async Task<bool> HasUserConsentedAsync(IUser discordUser)
        {
            var consent = await _database.UserConsents.ServersideQueryAsync
            (
                q => q
                    .Where(uc => uc.DiscordID == (long)discordUser.Id && uc.HasConsented)
                    .SingleOrDefaultAsync()
            );

            return !(consent is null) && consent.HasConsented;
        }

        /// <summary>
        /// Gets a consent entity for the given user.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<UserConsent>> GetUserConsentAsync(IUser discordUser)
        {
            var consent = await _database.UserConsents.ServersideQueryAsync
            (
                q => q
                    .Where(uc => uc.DiscordID == (long)discordUser.Id)
                    .SingleOrDefaultAsync()
            );

            if (!(consent is null))
            {
                return consent;
            }

            return RetrieveEntityResult<UserConsent>.FromError("The given user doesn't have a consent entity.");
        }

        /// <summary>
        /// Grants consent to store user data for a given user.
        /// </summary>
        /// <param name="discordUser">The user that has granted consent.</param>
        /// <returns>A task that must be awaited.</returns>
        public async Task GrantUserConsentAsync(IUser discordUser)
        {
            var getConsent = await GetUserConsentAsync(discordUser);
            if (!getConsent.IsSuccess)
            {
                var userConsent = _database.CreateProxy<UserConsent>((long)discordUser.Id);
                _database.UserConsents.Update(userConsent);

                userConsent.HasConsented = true;
            }
            else
            {
                var userConsent = getConsent.Entity;
                userConsent.HasConsented = true;
            }

            await _database.SaveChangesAsync();
        }

        /// <summary>
        /// Revokes consent to store user data for a given user.
        /// </summary>
        /// <param name="discordUser">The user that has revoked consent.</param>
        /// <returns>A task that must be awaited.</returns>
        public async Task<ModifyEntityResult> RevokeUserConsentAsync(IUser discordUser)
        {
            var getConsent = await GetUserConsentAsync(discordUser);
            if (!getConsent.IsSuccess)
            {
                return ModifyEntityResult.FromError("The user has not consented.");
            }

            var userConsent = getConsent.Entity;
            userConsent.HasConsented = false;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }
    }
}
