//
//  SassService.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Async;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services;
using JetBrains.Annotations;
using Remora.Results;
using Zio;

namespace DIGOS.Ambassador.Plugins.Amby.Services;

/// <summary>
/// Serves access to various sass-related functionality.
/// </summary>
public class SassService
{
    private readonly ContentService _content;

    private List<string> _sass;
    private List<string> _sassNSFW;

    private bool _isSassLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="SassService"/> class.
    /// </summary>
    /// <param name="content">The content service.</param>
    public SassService(ContentService content)
    {
        _content = content;

        _sass = new List<string>();
        _sassNSFW = new List<string>();

        _isSassLoaded = false;
    }

    /// <summary>
    /// Loads the sass from disk.
    /// </summary>
    private async Task<Result> LoadSassAsync()
    {
        var sassPath = UPath.Combine(UPath.Root, "Sass", "sass.txt");
        var sassNSFWPath = UPath.Combine(UPath.Root, "Sass", "sass-nsfw.txt");

        var getSassStream = _content.OpenLocalStream(sassPath);
        if (getSassStream.IsSuccess)
        {
            await using var sassStream = getSassStream.Entity;
            _sass = (await AsyncIO.ReadAllLinesAsync(sassStream)).ToList();
        }
        else
        {
            return Result.FromError(getSassStream);
        }

        var getNSFWSassStream = _content.OpenLocalStream(sassNSFWPath);
        if (getNSFWSassStream.IsSuccess)
        {
            await using var nsfwSassStream = getNSFWSassStream.Entity;
            _sassNSFW = (await AsyncIO.ReadAllLinesAsync(nsfwSassStream)).ToList();
        }
        else
        {
            return Result.FromError(getNSFWSassStream);
        }

        _isSassLoaded = true;
        return Result.FromSuccess();
    }

    /// <summary>
    /// Gets a sassy comment.
    /// </summary>
    /// <param name="includeNSFW">Whether or not to include NSFW sass.</param>
    /// <returns>A sassy comment.</returns>
    [Pure]
    public async Task<Result<string>> GetSassAsync(bool includeNSFW = false)
    {
        if (!_isSassLoaded)
        {
            var loadSassAsync = await LoadSassAsync();
            if (!loadSassAsync.IsSuccess)
            {
                return Result<string>.FromError(loadSassAsync);
            }
        }

        var availableSass = includeNSFW
            ? _sass.Union(_sassNSFW).ToList()
            : _sass;

        return availableSass.Count == 0
            ? "There's no available sass. You'll just have to provide your own."
            : Result<string>.FromSuccess(availableSass.PickRandom());
    }
}
