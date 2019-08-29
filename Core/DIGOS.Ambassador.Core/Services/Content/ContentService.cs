//
//  ContentService.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Async;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Results;
using JetBrains.Annotations;
using Zio;

namespace DIGOS.Ambassador.Core.Services
{
    /// <summary>
    /// Management class for content that comes bundled with the bot. Responsible for loading and providing access to
    /// the content.
    /// </summary>
    public class ContentService
    {
        private List<string> _sass;
        private List<string> _sassNSFW;

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
        private Uri BaseRemoteUri { get; }

        /// <summary>
        /// Gets the base remote content URI.
        /// </summary>
        public Uri BaseRemoteContentUri { get; }

        /// <summary>
        /// Gets the <see cref="Uri"/> pointing to a portrait of Amby.
        /// </summary>
        public Uri AmbyPortraitUri { get; }

        /// <summary>
        /// Gets the <see cref="Uri"/> pointing to a broken Ambybot portrait.
        /// </summary>
        public Uri BrokenAmbyUri { get; }

        /// <summary>
        /// Gets the <see cref="Uri"/> pointing to a templated issue creator on github.
        /// </summary>
        public Uri AutomaticBugReportCreationUri { get; }

        /// <summary>
        /// Gets the <see cref="Uri"/> pointing to to the privacy policy.
        /// </summary>
        public Uri PrivacyPolicyUri { get; }

        /// <summary>
        /// Gets the <see cref="Uri"/> pointing to a proper bweh.
        /// </summary>
        public Uri BwehUri { get; }

        /// <summary>
        /// Gets the Discord bot OAuth token.
        /// </summary>o
        public string BotToken { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentService"/> class.
        /// </summary>
        /// <param name="fileSystem">The filesystem abstraction.</param>
        public ContentService(IFileSystem fileSystem)
        {
            this.FileSystem = fileSystem;

            this.BaseRemoteUri = new Uri("https://raw.githubusercontent.com/Nihlus/digos-ambassador/master/");
            this.BaseRemoteContentUri = new Uri(this.BaseRemoteUri, "digos-ambassador/Content/");

            this.AutomaticBugReportCreationUri = new Uri
            (
                "https://github.com/Nihlus/digos-ambassador/issues/new?template=automated-bug-report.md"
            );

            this.AmbyPortraitUri = new Uri(this.BaseRemoteContentUri, "Portraits/amby-irbynx-3.png");
            this.BrokenAmbyUri = new Uri(this.BaseRemoteContentUri, "Portraits/maintenance.png");
            this.BwehUri = new Uri(this.BaseRemoteContentUri, "Portraits/bweh.png");
            this.PrivacyPolicyUri = new Uri(this.BaseRemoteContentUri, "PrivacyPolicy.pdf");

            this.DatabaseCredentialsPath = UPath.Combine(UPath.Root, "Database", "database.credentials");
        }

        /// <summary>
        /// Loads the default content.
        /// </summary>
        /// <returns>A task wrapping the content load operation.</returns>
        public async Task InitializeAsync()
        {
            await LoadSassAsync();
            await LoadBotTokenAsync();
        }

        /// <summary>
        /// Retrieves the database credentials.
        /// </summary>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public RetrieveEntityResult<Stream> GetDatabaseCredentialStream()
        {
            return OpenLocalStream(this.DatabaseCredentialsPath);
        }

        /// <summary>
        /// Loads the sass from disk.
        /// </summary>
        private async Task LoadSassAsync()
        {
            var sassPath = UPath.Combine(UPath.Root, "Sass", "sass.txt");
            var sassNSFWPath = UPath.Combine(UPath.Root, "Sass", "sass-nsfw.txt");

            if (!this.FileSystem.FileExists(sassPath))
            {
                _sass = new List<string>();
            }

            if (!this.FileSystem.FileExists(sassNSFWPath))
            {
                _sassNSFW = new List<string>();
            }

            var getSassStream = OpenLocalStream(sassPath);
            if (getSassStream.IsSuccess)
            {
                using (var sassStream = getSassStream.Entity)
                {
                    _sass = (await AsyncIO.ReadAllLinesAsync(sassStream)).ToList();
                }
            }

            var getNSFWSassStream = OpenLocalStream(sassNSFWPath);
            if (getNSFWSassStream.IsSuccess)
            {
                using (var nsfwSassStream = getNSFWSassStream.Entity)
                {
                    _sassNSFW = (await AsyncIO.ReadAllLinesAsync(nsfwSassStream)).ToList();
                }
            }
        }

        /// <summary>
        /// Loads the bot token from disk.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown if the bot token file can't be found.</exception>
        /// <exception cref="InvalidDataException">Thrown if no token exists in the file.</exception>
        private async Task LoadBotTokenAsync()
        {
            var tokenPath = UPath.Combine(UPath.Root, "Discord", "bot.token");

            if (!this.FileSystem.FileExists(tokenPath))
            {
                throw new FileNotFoundException("The bot token file could not be found.", tokenPath.ToString());
            }

            var getTokenStream = OpenLocalStream(tokenPath);
            if (!getTokenStream.IsSuccess)
            {
                throw new InvalidDataException("Missing bot token.");
            }

            using (var tokenStream = getTokenStream.Entity)
            {
                var token = await AsyncIO.ReadAllTextAsync(tokenStream);

                if (string.IsNullOrEmpty(token))
                {
                    throw new InvalidDataException("Missing bot token.");
                }

                this.BotToken = token;
            }
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
        public RetrieveEntityResult<Stream> OpenLocalStream
        (
            [PathReference] UPath path,
            FileMode fileMode = FileMode.Open,
            FileAccess fileAccess = FileAccess.Read,
            FileShare fileShare = FileShare.Read
        )
        {
            if (!path.IsAbsolute)
            {
                return RetrieveEntityResult<Stream>.FromError("Content paths must be absolute.");
            }

            try
            {
                var file = this.FileSystem.OpenFile(path, fileMode, fileAccess, fileShare);
                return RetrieveEntityResult<Stream>.FromSuccess(file);
            }
            catch (IOException iex)
            {
                return RetrieveEntityResult<Stream>.FromError(iex);
            }
        }

        /// <summary>
        /// Gets a sassy comment.
        /// </summary>
        /// <param name="includeNSFW">Whether or not to include NSFW sass.</param>
        /// <returns>A sassy comment.</returns>
        [Pure]
        public string GetSass(bool includeNSFW = false)
        {
            if (includeNSFW)
            {
                return _sass.Union(_sassNSFW).ToList().PickRandom();
            }

            return _sass.PickRandom();
        }
    }
}
