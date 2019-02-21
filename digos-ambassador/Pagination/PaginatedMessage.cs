//
//  PaginatedMessage.cs
//
//  Author:
//        Jarl Gullberg <jarl.gullberg@gmail.com>
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Interactivity.Messages;

using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;

// ReSharper disable AssignmentIsFullyDiscarded
namespace DIGOS.Ambassador.Pagination
{
    /// <summary>
    /// A page building class for paginated galleries.
    /// </summary>
    /// <typeparam name="TContent">The type of content in the pager.</typeparam>
    /// <typeparam name="TType">The type of the pager.</typeparam>
    public sealed class PaginatedMessage<TContent, TType> : InteractiveMessage where TType : IPager<TContent, TType>
    {
        /// <summary>
        /// Gets the user interaction service.
        /// </summary>
        private UserFeedbackService Feedback { get; }

        private readonly IPager<TContent, TType> Pager;

        private PaginatedAppearanceOptions Options => this.Pager.Options;

        private readonly int PageCount;

        private int CurrentPage = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedMessage{T1,T2}"/> class.
        /// </summary>
        /// <param name="feedbackService">The user feedback service.</param>
        /// <param name="pager">The pages in the gallery.</param>
        public PaginatedMessage
        (
            UserFeedbackService feedbackService,
            IPager<TContent, TType> pager
        )
        {
            this.Feedback = feedbackService;
            this.Pager = pager;
            this.PageCount = this.Pager.Pages.Count;
        }

        /// <inheritdoc/>
        protected override async Task<IUserMessage> DisplayAsync([NotNull] IMessageChannel channel)
        {
            var embed = this.Pager.BuildEmbed(this.CurrentPage - 1);

            var message = await channel.SendMessageAsync(string.Empty, embed: embed).ConfigureAwait(false);

            // Reactions take a while to add, don't wait for them
            _ = Task.Run(async () =>
            {
                await message.AddReactionAsync(this.Options.First);
                await message.AddReactionAsync(this.Options.Back);
                await message.AddReactionAsync(this.Options.Next);
                await message.AddReactionAsync(this.Options.Last);

                var manageMessages = await CanManageMessages();

                var canJump =
                    this.Options.JumpDisplayCondition == JumpDisplayCondition.Always ||
                    (this.Options.JumpDisplayCondition == JumpDisplayCondition.WithManageMessages && manageMessages);

                if (canJump)
                {
                    await message.AddReactionAsync(this.Options.Jump);
                }

                await message.AddReactionAsync(this.Options.Stop);

                if (this.Options.DisplayInformationIcon)
                {
                    await message.AddReactionAsync(this.Options.Help);
                }
            });

            return message;
        }

        /// <remarks>
        /// This override forwards to the added handler, letting removed reactions act the same as added reactions.
        /// </remarks>
        /// <inheritdoc/>
        public override Task HandleRemovedInteractionAsync(SocketReaction reaction) =>
            HandleAddedInteractionAsync(reaction);

        /// <inheritdoc/>
        public override async Task HandleAddedInteractionAsync(SocketReaction reaction)
        {
            var emote = reaction.Emote;

            if (emote.Equals(this.Options.First))
            {
                this.CurrentPage = 1;
            }
            else if (emote.Equals(this.Options.Next))
            {
                if (this.CurrentPage >= this.PageCount)
                {
                    return;
                }

                ++this.CurrentPage;
            }
            else if (emote.Equals(this.Options.Back))
            {
                if (this.CurrentPage <= 1)
                {
                    return;
                }

                --this.CurrentPage;
            }
            else if (emote.Equals(this.Options.Last))
            {
                this.CurrentPage = this.PageCount;
            }
            else if (emote.Equals(this.Options.Stop))
            {
                await this.Interactivity.DeleteInteractiveMessageAsync(this);
                return;
            }
            else if (emote.Equals(this.Options.Jump))
            {
                _ = Task.Run
                (
                    async () =>
                    {
                        bool Filter(IUserMessage m) => m.Author.Id == reaction.UserId;

                        var responseResult = await this.Interactivity.GetNextMessageAsync(this.MessageContext.Channel, Filter, TimeSpan.FromSeconds(15));
                        if (!responseResult.IsSuccess)
                        {
                            return;
                        }

                        var response = responseResult.Entity;

                        if (!int.TryParse(response.Content, out int request) || request < 1 || request > this.PageCount)
                        {
                            _ = response.DeleteAsync().ConfigureAwait(false);

                            var eb = this.Feedback.CreateFeedbackEmbed(response.Author, Color.DarkPurple, "Please specify a page to jump to.");

                            await this.Feedback.SendEmbedAndDeleteAsync(this.MessageContext.Channel, eb);
                            return;
                        }

                        this.CurrentPage = request;
                        _ = response.DeleteAsync().ConfigureAwait(false);
                        await UpdateAsync().ConfigureAwait(false);
                    }
                );
            }
            else if (emote.Equals(this.Options.Help))
            {
                var user = this.Interactivity.Client.GetUser(reaction.UserId);
                var eb = this.Feedback.CreateFeedbackEmbed(user, Color.DarkPurple, this.Options.HelpText);

                await this.Feedback.SendEmbedAndDeleteAsync(this.MessageContext.Channel, eb, this.Options.InfoTimeout);
                return;
            }

            if (await CanManageMessages())
            {
                _ = this.Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            }

            await UpdateAsync().ConfigureAwait(false);
        }

        private async Task<bool> CanManageMessages()
        {
            if (this.MessageContext.Channel is IGuildChannel guildChannel)
            {
                var botUser = this.Interactivity.Client.CurrentUser;
                var botGuildUser = await guildChannel.Guild.GetUserAsync(botUser.Id);

                return botGuildUser.GetPermissions(guildChannel).ManageMessages;
            }

            return false;
        }

        /// <inheritdoc/>
        protected override async Task UpdateAsync()
        {
            var embed = this.Pager.BuildEmbed(this.CurrentPage - 1);

            await this.Message.ModifyAsync(m => m.Embed = embed).ConfigureAwait(false);
        }
    }
}
