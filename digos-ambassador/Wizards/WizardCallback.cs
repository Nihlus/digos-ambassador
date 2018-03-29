//
//  WizardCallback.cs
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
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Wizards
{
	/// <summary>
	/// Represents a reaction callback for wizards.
	/// </summary>
	public class WizardCallback : IReactionCallback
	{
		/// <inheritdoc />
		public RunMode RunMode { get; }

		/// <inheritdoc />
		public ICriterion<SocketReaction> Criterion { get; }

		/// <inheritdoc />
		public TimeSpan? Timeout { get; }

		/// <inheritdoc />
		public SocketCommandContext Context { get; }

		/// <summary>
		/// Gets the wizard associated with the callback
		/// </summary>
		public IWizard Wizard { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="WizardCallback"/> class.
		/// </summary>
		/// <param name="context">The command context.</param>
		/// <param name="wizard">The associated wizard.</param>
		/// <param name="timeout">The callback timeout length.</param>
		/// <param name="runMode">The mode with which to run the callback.</param>
		/// <param name="criterion">The reaction criteria.</param>
		public WizardCallback
		(
			SocketCommandContext context,
			IWizard wizard,
			[CanBeNull] ICriterion<SocketReaction> criterion = null,
			TimeSpan? timeout = null,
			RunMode runMode = RunMode.Async)
		{
			this.Context = context;
			this.Wizard = wizard;
			this.Criterion = criterion ?? new EmptyCriterion<SocketReaction>();
			this.Timeout = timeout;
			this.RunMode = runMode;
		}

		/// <inheritdoc />
		public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
		{
			bool matchesCriterion = await this.Criterion.JudgeAsync(this.Context, reaction);
			if (!matchesCriterion || !this.Wizard.AcceptedEmotes.Contains(reaction.Emote))
			{
				return true;
			}

			return await this.Wizard.ConsumeEmoteAsync(reaction.Emote).ConfigureAwait(false);
		}
	}
}
