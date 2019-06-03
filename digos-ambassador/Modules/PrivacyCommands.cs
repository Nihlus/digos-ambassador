//
//  PrivacyCommands.cs
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
using DIGOS.Ambassador.Attributes;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Modules.Base;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Users;

using Discord.Commands;
using Discord.WebSocket;

using JetBrains.Annotations;
using static Discord.Commands.ContextType;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks
#pragma warning disable SA1118 // Parameter spans multiple lines, big strings

namespace DIGOS.Ambassador.Modules
{
    /// <summary>
    /// Privacy-related commands (data storage, deleting requests, data protection, privacy contacts, etc).
    /// </summary>
    [UsedImplicitly]
    [Group("privacy")]
    [Summary("Privacy-related commands (data storage, deleting requests, data protection, privacy contacts, etc).")]
    public class PrivacyCommands : DatabaseModuleBase
    {
        private readonly DiscordSocketClient Client;

        private readonly PrivacyService Privacy;
        private readonly ContentService Content;

        private readonly UserFeedbackService Feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyCommands"/> class.
        /// </summary>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="client">The Discord client instance.</param>
        /// <param name="database">The database.</param>
        /// <param name="privacy">The privacy service.</param>
        /// <param name="content">The content service.</param>
        public PrivacyCommands
        (
            UserFeedbackService feedback,
            DiscordSocketClient client,
            GlobalInfoContext database,
            PrivacyService privacy,
            ContentService content
        )
            : base(database)
        {
            this.Feedback = feedback;
            this.Client = client;
            this.Privacy = privacy;
            this.Content = content;
        }

        /// <summary>
        /// Requests a copy of the privacy policy.
        /// </summary>
        [UsedImplicitly]
        [Command("policy", RunMode = RunMode.Async)]
        [Summary("Requests a copy of the privacy policy.")]
        [RequireContext(DM)]
        [PrivacyExempt]
        public async Task RequestPolicyAsync()
        {
            await this.Privacy.SendPrivacyPolicyAsync(this.Context.Channel, this.Content, this.Feedback);
        }

        /// <summary>
        /// Grants consent to store user data.
        /// </summary>
        [UsedImplicitly]
        [Command("grant-consent", RunMode = RunMode.Async)]
        [Summary("Grants consent to store user data.")]
        [RequireContext(DM)]
        [PrivacyExempt]
        public async Task GrantConsentAsync()
        {
            await this.Privacy.GrantUserConsentAsync(this.Database, this.Context.User);
            await this.Feedback.SendConfirmationAsync(this.Context, "Thank you! Enjoy using the bot :smiley:");
        }

        /// <summary>
        /// Revokes consent to store user data.
        /// </summary>
        [UsedImplicitly]
        [Command("revoke-consent", RunMode = RunMode.Async)]
        [Summary("Revokes consent to store user data.")]
        [RequireContext(DM)]
        [PrivacyExempt]
        public async Task RevokeConsentAsync()
        {
            await this.Privacy.RevokeUserConsentAsync(this.Database, this.Context.User);
            await this.Feedback.SendConfirmationAsync
            (
                this.Context,
                "Consent revoked - no more information will be stored about you from now on. If you would like to " +
                "delete your existing data, or get a copy of it, please contact the privacy contact individual (use " +
                "!privacy contact to get their contact information)."
            );
        }

        /// <summary>
        /// Displays contact information for the privacy contact person.
        /// </summary>
        [UsedImplicitly]
        [Command("contact", RunMode = RunMode.Async)]
        [Summary("Displays contact information for the privacy contact person.")]
        [RequireContext(DM)]
        [PrivacyExempt]
        public async Task DisplayContactAsync()
        {
            const string avatarURL = "https://i.imgur.com/2E334jS.jpg";
            var discordUser = this.Client.GetUser("Jax", "7487");

            var eb = this.Feedback.CreateEmbedBase();
            eb.WithTitle("Privacy Contact");
            eb.WithAuthor("Jarl Gullberg", avatarURL, "https://github.com/Nihlus/");
            eb.WithThumbnailUrl(avatarURL);

            eb.AddField("Email", "jarl.gullberg@gmail.com");
            eb.AddField("Discord", $"{discordUser.Mention} (Jax#7487)");

            eb.WithFooter("Not your contact person? Edit the source of your instance with the correct information.");

            var embed = eb.Build();

            await this.Context.Channel.SendMessageAsync(string.Empty, embed: embed);
        }
    }
}
