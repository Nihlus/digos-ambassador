//
//  ICharacterEditor.cs
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

using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Model.Data;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Characters.Services.Interfaces;

/// <summary>
/// Represents the public interface of a type that's capable of editing a character instance.
/// </summary>
public interface ICharacterEditor
{
    /// <summary>
    /// Sets the name of the given character.
    /// </summary>
    /// <param name="character">The character to set the name of.</param>
    /// <param name="name">The new name.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    Task<Result> SetCharacterNameAsync
    (
        Character character,
        string name,
        CancellationToken ct = default
    );

    /// <summary>
    /// Sets the avatar of the given character.
    /// </summary>
    /// <param name="character">The character to set the avatar of.</param>
    /// <param name="avatarUrl">The new avatar.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    Task<Result> SetCharacterAvatarAsync
    (
        Character character,
        string avatarUrl,
        CancellationToken ct = default
    );

    /// <summary>
    /// Sets the nickname of the given character.
    /// </summary>
    /// <param name="character">The character to set the nickname of.</param>
    /// <param name="nickname">The new nickname.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    Task<Result> SetCharacterNicknameAsync
    (
        Character character,
        string nickname,
        CancellationToken ct = default
    );

    /// <summary>
    /// Sets the summary of the given character.
    /// </summary>
    /// <param name="character">The character to set the summary of.</param>
    /// <param name="summary">The new summary.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    Task<Result> SetCharacterSummaryAsync
    (
        Character character,
        string summary,
        CancellationToken ct = default
    );

    /// <summary>
    /// Sets the description of the given character.
    /// </summary>
    /// <param name="character">The character to set the description of.</param>
    /// <param name="description">The new description.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    Task<Result> SetCharacterDescriptionAsync
    (
        Character character,
        string description,
        CancellationToken ct = default
    );

    /// <summary>
    /// Sets the preferred pronoun for the given character.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="pronounFamily">The pronoun family.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    Task<Result> SetCharacterPronounsAsync
    (
        Character character,
        string pronounFamily,
        CancellationToken ct = default
    );

    /// <summary>
    /// Sets whether or not a character is NSFW.
    /// </summary>
    /// <param name="character">The character to edit.</param>
    /// <param name="isNSFW">Whether or not the character is NSFW.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A task that must be awaited.</returns>
    Task<Result> SetCharacterIsNSFWAsync
    (
        Character character,
        bool isNSFW,
        CancellationToken ct = default
    );

    /// <summary>
    /// Adds the given image with the given metadata to the given character.
    /// </summary>
    /// <param name="character">The character to add the image to.</param>
    /// <param name="imageName">The name of the image.</param>
    /// <param name="imageUrl">The url of the image.</param>
    /// <param name="imageCaption">The caption of the image.</param>
    /// <param name="isNSFW">Whether or not the image is NSFW.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>An execution result which may or may not have succeeded.</returns>
    Task<Result<Image>> AddImageToCharacterAsync
    (
        Character character,
        string imageName,
        string imageUrl,
        string? imageCaption = null,
        bool isNSFW = false,
        CancellationToken ct = default
    );

    /// <summary>
    /// Removes the named image from the given character.
    /// </summary>
    /// <param name="character">The character to remove the image from.</param>
    /// <param name="image">The image.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>An execution result which may or may not have succeeded.</returns>
    Task<Result> RemoveImageFromCharacterAsync
    (
        Character character,
        Image image,
        CancellationToken ct = default
    );
}
