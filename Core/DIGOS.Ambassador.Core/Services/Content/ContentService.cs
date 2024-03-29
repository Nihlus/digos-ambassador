﻿//
//  ContentService.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using System.IO;
using JetBrains.Annotations;
using Remora.Results;
using Zio;

namespace DIGOS.Ambassador.Core.Services;

/// <summary>
/// Management class for content that comes bundled with the bot. Responsible for loading and providing access to
/// the content.
/// </summary>
public class ContentService
{
    /// <summary>
    /// Gets the virtual filesystem that encapsulates the content.
    /// </summary>
    public IFileSystem FileSystem { get; }

    /// <summary>
    /// Gets the path to the database credentials.
    /// </summary>
    private UPath DatabaseCredentialsPath { get; }

    /// <summary>
    /// Gets the base remote content URI.
    /// </summary>
    public Uri BaseCDNUri { get; }

    /// <summary>
    /// Gets the <see cref="Uri"/> pointing to a templated issue creator on github.
    /// </summary>
    public Uri AutomaticBugReportCreationUri { get; }

    /// <summary>
    /// Gets the <see cref="Uri"/> pointing to to the privacy policy.
    /// </summary>
    public Uri PrivacyPolicyUri { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentService"/> class.
    /// </summary>
    /// <param name="fileSystem">The filesystem abstraction.</param>
    public ContentService(IFileSystem fileSystem)
    {
        this.FileSystem = fileSystem;

        this.BaseCDNUri = new Uri("https://cdn.algiz.nu/amby/");

        this.AutomaticBugReportCreationUri = new Uri
        (
            "https://github.com/Nihlus/digos-ambassador/issues/new?template=automated-bug-report.md"
        );
        this.PrivacyPolicyUri = new Uri(this.BaseCDNUri, "privacy/PrivacyPolicy.pdf");

        this.DatabaseCredentialsPath = UPath.Combine(UPath.Root, "Database", "database.credentials");
    }

    /// <summary>
    /// Gets the stream of a local content file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="fileMode">The mode with which to open the stream.</param>
    /// <param name="fileAccess">The access rights with which to open the stream.</param>
    /// <param name="fileShare">The sharing rights with which to open the stream.</param>
    /// <returns>A <see cref="FileStream"/> with the file data.</returns>
    [Pure]
    [MustUseReturnValue("The resulting file stream must be disposed.")]
    public Result<Stream> OpenLocalStream
    (
        [PathReference] UPath path,
        FileMode fileMode = FileMode.Open,
        FileAccess fileAccess = FileAccess.Read,
        FileShare fileShare = FileShare.Read
    )
    {
        if (!path.IsAbsolute)
        {
            return new InvalidOperationError("Content paths must be absolute.");
        }

        try
        {
            var file = this.FileSystem.OpenFile(path, fileMode, fileAccess, fileShare);
            return Result<Stream>.FromSuccess(file);
        }
        catch (IOException iex)
        {
            return Result<Stream>.FromError(iex);
        }
    }
}
