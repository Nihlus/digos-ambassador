//
//  AmbyCommands.cs
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

using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Plugins.Amby.Services;
using DIGOS.Ambassador.Plugins.Core.Services;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Amby.CommandModules
{
    /// <summary>
    /// Assorted commands that don't really fit anywhere - just for fun, testing, etc.
    /// </summary>
    [UsedImplicitly]
    [Description("Assorted commands that don't really fit anywhere - just for fun, testing, etc.")]
    public class AmbyCommands : CommandGroup
    {
        private readonly PortraitService _portraits;
        private readonly SassService _sass;
        private readonly UserFeedbackService _feedback;
        private readonly ICommandContext _context;
        private readonly IdentityInformationService _identityInformation;
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbyCommands"/> class.
        /// </summary>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="sass">The sass service.</param>
        /// <param name="portraits">The portrait service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="identityInformation">The identity information service.</param>
        /// <param name="channelAPI">The channel API.</param>
        public AmbyCommands
        (
            UserFeedbackService feedback,
            SassService sass,
            PortraitService portraits,
            ICommandContext context,
            IdentityInformationService identityInformation,
            IDiscordRestChannelAPI channelAPI
        )
        {
            _feedback = feedback;
            _sass = sass;
            _portraits = portraits;
            _context = context;
            _identityInformation = identityInformation;
            _channelAPI = channelAPI;
        }

        /// <summary>
        /// Instructs Amby to contact you over DM.
        /// </summary>
        [UsedImplicitly]
        [Command("contact")]
        [Description("Instructs Amby to contact you over DM.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> ContactSelfAsync() => await ContactUserAsync(_context.User);

        /// <summary>
        /// Instructs Amby to contact a user over DM.
        /// </summary>
        /// <param name="discordUser">The user to contact.</param>
        [UsedImplicitly]
        [Command("contact-user")]
        [Description("Instructs Amby to contact a user over DM.")]
        [RequireContext(ChannelContext.Guild)]
        [RequireUserGuildPermission(DiscordPermission.MentionEveryone)]
        public async Task<Result<UserMessage>> ContactUserAsync(IUser discordUser)
        {
            if (discordUser.ID == _identityInformation.ID)
            {
                return new UserError
                (
                    "That's a splendid idea - at least then, I'd get an intelligent reply."
                );
            }

            if (discordUser.IsBot.HasValue && discordUser.IsBot.Value)
            {
                return new UserError("I could do that, but I doubt I'd get a reply.");
            }

            var contactMessage = $"Hello there, <@{discordUser.ID}>. I've been instructed to initiate... " +
                                 $"negotiations... with you. \nA good place to start would be the \"!help <topic>\" " +
                                 $"command.";

            var eb = _feedback.CreateFeedbackEmbed
            (
                discordUser.ID,
                Color.MediumPurple,
                contactMessage
            );

            var sendPrivate = await _feedback.SendPrivateEmbedAsync(discordUser.ID, eb);
            if (!sendPrivate.IsSuccess)
            {
                return Result<UserMessage>.FromError(sendPrivate);
            }

            return new ConfirmationMessage("User contacted.");
        }

        /// <summary>
        /// Sasses the user in a DIGOS fashion.
        /// </summary>
        [UsedImplicitly]
        [Command("sass")]
        [Description("Sasses the user in a DIGOS fashion.")]
        public async Task<Result<UserMessage>> SassAsync()
        {
            var getChannel = await _channelAPI.GetChannelAsync(_context.ChannelID, this.CancellationToken);
            if (!getChannel.IsSuccess)
            {
                return Result<UserMessage>.FromError(getChannel);
            }

            var channel = getChannel.Entity;

            var isNsfwChannel = channel.IsNsfw.HasValue && channel.IsNsfw.Value;
            var getSassResult = await _sass.GetSassAsync(isNsfwChannel);
            if (!getSassResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(getSassResult);
            }

            var sass = getSassResult.Entity;
            return new ConfirmationMessage(sass);
        }

        /// <summary>
        /// Mow! A silly command.
        /// </summary>
        [UsedImplicitly]
        [Command("mow")]
        [Description("Mow!")]
        public async Task<IResult> MowAsync()
        {
            var eb = _feedback.CreateEmbedBase() with
            {
                Image = new EmbedImage(_portraits.MowUri.ToString())
            };

            return await _feedback.SendEmbedAsync(_context.ChannelID, eb);
        }

        /// <summary>
        /// Bweh! A silly command.
        /// </summary>
        [UsedImplicitly]
        [Command("bweh")]
        [Description("Bweh!")]
        public async Task<IResult> BwehAsync()
        {
            var eb = _feedback.CreateEmbedBase() with
            {
                Image = new EmbedImage(_portraits.BwehUri.ToString())
            };

            return await _feedback.SendEmbedAsync(_context.ChannelID, eb);
        }

        /// <summary>
        /// Boops the invoking user.
        /// </summary>
        [UsedImplicitly]
        [Command("boop")]
        [Description("Boops you.")]
        public Task<Result<UserMessage>> BoopAsync()
        {
            return Task.FromResult<Result<UserMessage>>(new ConfirmationMessage("*boop*"));
        }

        /// <summary>
        /// Baps the invoking user.
        /// </summary>
        [UsedImplicitly]
        [Command("bap")]
        [Description("Baps you.")]
        public Task<Result<UserMessage>> BapAsync()
        {
            return Task.FromResult<Result<UserMessage>>(new ConfirmationMessage("**baps**"));
        }

        /// <summary>
        /// Boops the target user.
        /// </summary>
        /// <param name="target">The target.</param>
        [UsedImplicitly]
        [Command("boop-user")]
        [Description("Boops the user.")]
        public async Task<Result<UserMessage>> BoopAsync(IUser target)
        {
            if (target.ID != _identityInformation.ID)
            {
                return new ConfirmationMessage($"*boops <@{target.ID}>*");
            }

            var sendAnnoyed = await _feedback.SendConfirmationAsync
            (
                _context.ChannelID,
                _context.User.ID,
                "...seriously?",
                this.CancellationToken
            );

            return sendAnnoyed.Any(r => !r.IsSuccess)
                ? Result<UserMessage>.FromError(sendAnnoyed.First(r => !r.IsSuccess))
                : new ConfirmationMessage($"*boops <@{_context.User.ID}>*");
        }

        /// <summary>
        /// Baps the target user.
        /// </summary>
        /// <param name="target">The target.</param>
        [UsedImplicitly]
        [Command("bap-user")]
        [Description("Baps the user.")]
        public async Task<Result<UserMessage>> BapAsync(IUser target)
        {
            if (target.ID != _identityInformation.ID)
            {
                return new ConfirmationMessage($"**baps <@{target.ID}>**");
            }

            var sendAnnoyed = await _feedback.SendConfirmationAsync
            (
                _context.ChannelID,
                _context.User.ID,
                "...seriously?",
                this.CancellationToken
            );

            return sendAnnoyed.Any(r => !r.IsSuccess)
                ? Result<UserMessage>.FromError(sendAnnoyed.First(r => !r.IsSuccess))
                : new ConfirmationMessage($"**baps <@{_context.User.ID}>**");
        }

        /// <summary>
        /// Shows some information about Amby's metaworkings.
        /// </summary>
        [UsedImplicitly]
        [Command("bot-info")]
        [Description("Shows some information about Amby's metaworkings.")]
        public async Task<IResult> InfoAsync()
        {
            var eb = _feedback.CreateEmbedBase() with
            {
                Author = new EmbedAuthor
                {
                    Name = "DIGOS Ambassador",
                    IconUrl = _portraits.AmbyPortraitUri.ToString()
                },
                Title = "The DIGOS Ambassador (\"Amby\")",
                Image = new EmbedImage
                {
                    Url = _portraits.AmbyPortraitUri.ToString()
                },
                Description =
                "Amby is a Discord bot written in C# using the Discord.Net and EF Core frameworks. As an ambassador " +
                "for the DIGOS community, she provides a number of useful services for communities with similar " +
                "interests - namely, roleplaying, transformation, weird and wonderful sexual kinks, and much more.\n" +
                "\n" +
                "Amby is free and open source software, licensed under the AGPLv3. All of her source code can be " +
                "freely viewed and improved on Github at https://github.com/Nihlus/DIGOS.Ambassador. You are free to " +
                "run your own instance of Amby, redistribute her code, and modify it to your heart's content. If " +
                "you're not familiar with the AGPL, an excellent summary is available here: " +
                "https://choosealicense.com/licenses/agpl-3.0/.\n" +
                "\n" +
                "Any bugs you encounter should be reported on Github, following the issue template provided there. " +
                "The same holds for feature requests, for which a separate template is provided. Contributions in " +
                "the form of code, artwork, bug triaging, or quality control testing is always greatly appreciated!\n" +
                "\n" +
                "Stay sharky~\n" +
                "- Amby"
            };

            return await _feedback.SendPrivateEmbedAsync(_context.User.ID, eb);
        }
    }
}
