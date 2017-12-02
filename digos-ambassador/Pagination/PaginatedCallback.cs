//
//  PaginatedCallback.cs
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

// Originally licensed under the ISC license; modified from https://github.com/foxbot/Discord.Addons.Interactive
using System;
using System.Threading.Tasks;

using DIGOS.Ambassador.Services;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

// ReSharper disable AssignmentIsFullyDiscarded
namespace DIGOS.Ambassador.Pagination
{
	/// <summary>
	/// A page building class for paginated galleries.
	/// </summary>
	/// <typeparam name="T1">The type of content in the pager.</typeparam>
	/// <typeparam name="T2">The type of the pager.</typeparam>
	public sealed class PaginatedCallback<T1, T2> : IReactionCallback where T2 : IPager<T1, T2>
	{
		/// <inheritdoc />
		public SocketCommandContext Context { get; }

		private IMessageChannel Channel { get; }

		/// <summary>
		/// Gets the interaction service associated with this gallery callback.
		/// </summary>
		private InteractiveService Interactive { get; }

		/// <summary>
		/// Gets the user interaction service.
		/// </summary>
		private UserFeedbackService Feedback { get; }

		/// <summary>
		/// Gets or sets the message associated with this gallery callback.
		/// </summary>
		public IUserMessage Message { get; set; }

		/// <inheritdoc />
		public RunMode RunMode => RunMode.Sync;

		/// <inheritdoc />
		public ICriterion<SocketReaction> Criterion { get; }

		/// <inheritdoc />
		public TimeSpan? Timeout => this.Options.Timeout;

		private readonly IPager<T1, T2> Pager;

		private PaginatedAppearanceOptions Options => this.Pager.Options;

		private readonly int PageCount;

		private int CurrentPage = 1;

		/// <summary>
		/// Initializes a new instance of the <see cref="PaginatedCallback{T1, T2}"/> class.
		/// </summary>
		/// <param name="interactive">The interaction service.</param>
		/// <param name="feedbackService">The user feedback service.</param>
		/// <param name="sourceContext">The context to which the gallery belongs.</param>
		/// <param name="pager">The pages in the gallery.</param>
		/// <param name="targetChannel">The channel in which the gallery should be posted.</param>
		/// <param name="criterion">The criterion for reactions.</param>
		public PaginatedCallback
		(
			InteractiveService interactive,
			UserFeedbackService feedbackService,
			SocketCommandContext sourceContext,
			IPager<T1, T2> pager,
			IMessageChannel targetChannel = null,
			ICriterion<SocketReaction> criterion = null
		)
		{
			this.Interactive = interactive;
			this.Feedback = feedbackService;
			this.Context = sourceContext;
			this.Criterion = criterion ?? new EmptyCriterion<SocketReaction>();
			this.Pager = pager;
			this.Channel = targetChannel ?? this.Context.Channel;
			this.PageCount = this.Pager.Pages.Count;
		}

		/// <summary>
		/// Displays the gallery.
		/// </summary>
		/// <returns>A task which must be awaited.</returns>
		public async Task DisplayAsync()
		{
			var embed = this.Pager.BuildEmbed(this.CurrentPage - 1);
			var message = await this.Channel.SendMessageAsync(string.Empty, embed: embed).ConfigureAwait(false);
			this.Message = message;
			this.Interactive.AddReactionCallback(message, this);

			// Reactions take a while to add, don't wait for them
			_ = Task.Run(async () =>
			{
				await message.AddReactionAsync(this.Options.First);
				await message.AddReactionAsync(this.Options.Back);
				await message.AddReactionAsync(this.Options.Next);
				await message.AddReactionAsync(this.Options.Last);

				var manageMessages =
					this.Channel is IGuildChannel guildChannel &&
					this.Context.Guild?.GetUser(this.Context.Client.CurrentUser.Id) is IGuildUser guildUser &&
					guildUser.GetPermissions(guildChannel).ManageMessages;

				var canJump =
					this.Options.JumpDisplayOptions == JumpDisplayOptions.Always ||
					(this.Options.JumpDisplayOptions == JumpDisplayOptions.WithManageMessages && manageMessages);

				if (canJump)
				{
					await message.AddReactionAsync(this.Options.Jump);
				}

				await message.AddReactionAsync(this.Options.Stop);

				if (this.Options.DisplayInformationIcon)
				{
					await message.AddReactionAsync(this.Options.Info);
				}
			});

			// TODO: (Next major version) timeouts need to be handled at the service-level!
			if (this.Timeout.HasValue)
			{
				_ = Task.Delay(this.Timeout.Value).ContinueWith(_ =>
				{
					this.Interactive.RemoveReactionCallback(message);
					this.Message.DeleteAsync();
				});
			}
		}

		/// <inheritdoc />
		public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
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
					return false;
				}
				++this.CurrentPage;
			}
			else if (emote.Equals(this.Options.Back))
			{
				if (this.CurrentPage <= 1)
				{
					return false;
				}
				--this.CurrentPage;
			}
			else if (emote.Equals(this.Options.Last))
			{
				this.CurrentPage = this.PageCount;
			}
			else if (emote.Equals(this.Options.Stop))
			{
				await this.Message.DeleteAsync().ConfigureAwait(false);
				return true;
			}
			else if (emote.Equals(this.Options.Jump))
			{
				_ = Task.Run
				(
					async () =>
					{
						var criteria = new Criteria<SocketMessage>()
							.AddCriterion(new EnsureSourceChannelCriterion())
							.AddCriterion(new EnsureFromUserCriterion(reaction.UserId));

						var response = await this.Interactive.NextMessageAsync(this.Context, criteria, TimeSpan.FromSeconds(15));
						if (response is null)
						{
							return;
						}

						if (!int.TryParse(response.Content, out int request) || request < 1 || request > this.PageCount)
						{
							_ = response.DeleteAsync().ConfigureAwait(false);

							var eb = this.Feedback.CreateFeedbackEmbed(response.Author, Color.DarkPurple, "Please specify a page to jump to.");

							await this.Interactive.ReplyAndDeleteAsync(this.Context, string.Empty, embed: eb);
							return;
						}

						this.CurrentPage = request;
						_ = response.DeleteAsync().ConfigureAwait(false);
						await RenderAsync().ConfigureAwait(false);
					}
				);
			}
			else if (emote.Equals(this.Options.Info))
			{
				var user = this.Context.Client.GetUser(reaction.UserId);
				var eb = this.Feedback.CreateFeedbackEmbed(user, Color.DarkPurple, this.Options.InformationText);

				await this.Interactive.ReplyAndDeleteAsync(this.Context, string.Empty, embed: eb, timeout: this.Options.InfoTimeout);
				return false;
			}

			var manageMessages =
				this.Channel is IGuildChannel guildChannel &&
				this.Context.Guild?.GetUser(this.Context.Client.CurrentUser.Id) is IGuildUser guildUser &&
				guildUser.GetPermissions(guildChannel).ManageMessages;

			if (manageMessages)
			{
				_ = this.Message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
			}

			await RenderAsync().ConfigureAwait(false);
			return false;
		}

		private async Task RenderAsync()
		{
			var embed = this.Pager.BuildEmbed(this.CurrentPage - 1);
			await this.Message.ModifyAsync(m => m.Embed = embed).ConfigureAwait(false);
		}
	}
}
