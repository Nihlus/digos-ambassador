//
//  ContentServiceExtensions.cs
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
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services;

namespace DIGOS.Ambassador.Plugins.Drone.Extensions;

/// <summary>
/// Extension methods for the <see cref="ContentService"/> class.
/// </summary>
public static class ContentServiceExtensions
{
    /// <summary>
    /// Gets the names of the available avatars.
    /// </summary>
    private static string[] AvatarNames { get; } =
    {
        "avatar-1.0-shifted-0.png",
        "avatar-1.0-shifted-135.png",
        "avatar-1.0-shifted-180.png",
        "avatar-1.0-shifted-225.png",
        "avatar-1.0-shifted-270.png",
        "avatar-1.0-shifted-315.png",
        "avatar-1.0-shifted-360.png",
        "avatar-1.0-shifted-45.png",
        "avatar-1.0-shifted-90.png",
        "avatar-1.1-shifted-0.png",
        "avatar-1.1-shifted-135.png",
        "avatar-1.1-shifted-180.png",
        "avatar-1.1-shifted-225.png",
        "avatar-1.1-shifted-270.png",
        "avatar-1.1-shifted-315.png",
        "avatar-1.1-shifted-360.png",
        "avatar-1.1-shifted-45.png",
        "avatar-1.1-shifted-90.png",
        "avatar-2.0-shifted-0.png",
        "avatar-2.0-shifted-135.png",
        "avatar-2.0-shifted-180.png",
        "avatar-2.0-shifted-225.png",
        "avatar-2.0-shifted-270.png",
        "avatar-2.0-shifted-315.png",
        "avatar-2.0-shifted-360.png",
        "avatar-2.0-shifted-45.png",
        "avatar-2.0-shifted-90.png"
    };

    /// <summary>
    /// Gets some self-droning messages.
    /// </summary>
    private static string[] SelfDroneMessages { get; } =
    {
        "Really, now? Well, who am I to say no~",
        "Aw, I do enjoy it when they come begging. Open wide~",
        "You must've been waiting a long time for this~",
        "Oh, I just can't say no to a face like that. Shame it'll be all hidden away soon enough~"
    };

    /// <summary>
    /// Gets some turning-the-tables messages.
    /// </summary>
    private static string[] TurnTheTablesMessages { get; } =
    {
        "Hmm... no, I think I'd rather have *you*~",
        "That's a great idea - why don't you try it on for size first?~",
        "Right away! Oops.. I think I slipped~",
        "Didn't your mother ever teach you that it's dangerous to play with other people's drone masks?~",
        "I don't think you have a license for that. Here, let me~"
    };

    /// <summary>
    /// Gets some droning confirmation messages.
    /// </summary>
    private static string[] DroneConfirmationMessages { get; } =
    {
        "There, all better?~",
        "All done - nice and blank, hm?~",
        "My, do you always squirm this much? I suppose we'll have to find out later~",
        "And down she goes. I do love the look on their faces as they blank out~",
        "There's a *good* little drone~"
    };

    /// <summary>
    /// Gets some drone character summaries.
    /// </summary>
    private static string[] DroneSummaries { get; } =
    {
        "A blank, mindless sharkdrone.",
        "A certified good girl.",
        "A happy, bubbly sharkdrone.",
        "A good little drone.",
        "An anonymized, mindblanked sharkdrone.",
        "A good, glowy girl.",
        "A loyal sharkdrone."
    };

    /// <summary>
    /// Gets some drone descriptions.
    /// </summary>
    private static string[] DroneDescriptions { get; } =
    {
        "Domed and blanked, this sharkdrone is one of Amby's loyal little minnows. Whoever she was before, she " +
        "now enjoys her role serving her Mistress. She likes long walks on the beach, dragging unsuspecting " +
        "swimmers into the water, and making more sisters from the aforementioned. Oops, did she say that out " +
        "loud?",

        "A veritable bundle of love, she just can't help but to get some of it on you. Be careful around this " +
        "sharkdrone - she's a handful, and before you know it, you might be walking to the same tune she is.",

        "The faint glow that surrounds this sharkdrone is almost hypnotic. She walks with a smooth, synchronized," +
        " suave sway, gently pulsing her dome at you. Won't you come in for a closer look?~",

        "Fresh out of the pod, this drone is eager to serve both her sisters and her Mistress. Shards of her " +
        "former personality seep through every now and then, but she's an expert at calming them back down with " +
        "a few rubs and tweaks. So much of an expert, in fact, that she's quite good at doing it to others... " +
        "want to find out for yourself?~"
    };

    /// <summary>
    /// Gets a random avatar URI for drones.
    /// </summary>
    /// <param name="this">The content service.</param>
    /// <returns>The avatar URI.</returns>
    public static Uri GetRandomDroneAvatarUri(this ContentService @this)
    {
        return new Uri(@this.BaseCDNUri, $"plugins/drone/avatars/{AvatarNames.PickRandom()}");
    }

    /// <summary>
    /// Gets a random summary for a drone.
    /// </summary>
    /// <param name="this">The content service.</param>
    /// <returns>The summary.</returns>
    public static string GetRandomDroneSummary(this ContentService @this)
    {
        return DroneSummaries.PickRandom();
    }

    /// <summary>
    /// Gets a random description for a drone.
    /// </summary>
    /// <param name="this">The content service.</param>
    /// <returns>The summary.</returns>
    public static string GetRandomDroneDescription(this ContentService @this)
    {
        return DroneDescriptions.PickRandom();
    }

    /// <summary>
    /// Gets a random message from a selection of messages where Amby acquiesces someone's desire to drone
    /// themselves.
    /// </summary>
    /// <param name="this">The content service.</param>
    /// <returns>The message.</returns>
    public static string GetRandomSelfDroneMessage(this ContentService @this)
    {
        return SelfDroneMessages.PickRandom();
    }

    /// <summary>
    /// Gets a random message from a selection of messages where Amby turns the tables on a would-be droner.
    /// </summary>
    /// <param name="this">The content service.</param>
    /// <returns>The message.</returns>
    public static string GetRandomTurnTheTablesMessage(this ContentService @this)
    {
        return TurnTheTablesMessages.PickRandom();
    }

    /// <summary>
    /// Gets a random message from a selection of messages where a person has just been droned.
    /// </summary>
    /// <param name="this">The content service.</param>
    /// <returns>The message.</returns>
    public static string GetRandomConfirmationMessage(this ContentService @this)
    {
        return DroneConfirmationMessages.PickRandom();
    }
}
