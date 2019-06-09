//
//  DossierService.cs
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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Dossiers;
using DIGOS.Ambassador.Extensions;

using Discord.Commands;

using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Services
{
    /// <summary>
    /// Handles dossier management.
    /// </summary>
    public class DossierService
    {
        private readonly ContentService _content;

        /// <summary>
        /// Initializes a new instance of the <see cref="DossierService"/> class.
        /// </summary>
        /// <param name="contentService">The content service.</param>
        public DossierService([NotNull] ContentService contentService)
        {
            _content = contentService;
        }

        /// <summary>
        /// Creates a new dossier with the given title, summary, and data.
        /// </summary>
        /// <param name="db">The database where the dossier should be stored.</param>
        /// <param name="title">The title of the dossier.</param>
        /// <param name="summary">The summary of the dossier.</param>
        /// <returns>A creation task which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<Dossier>> CreateDossierAsync
        (
            [NotNull] AmbyDatabaseContext db,
            [NotNull] string title,
            [NotNull] string summary
        )
        {
            var dossier = new Dossier();
            var setTitleResult = await SetDossierTitleAsync(db, dossier, title);
            if (!setTitleResult.IsSuccess)
            {
                return CreateEntityResult<Dossier>.FromError(setTitleResult);
            }

            var setSummary = await SetDossierSummaryAsync(db, dossier, summary);
            if (!setSummary.IsSuccess)
            {
                return CreateEntityResult<Dossier>.FromError(setSummary);
            }

            await db.Dossiers.AddAsync(dossier);
            await db.SaveChangesAsync();
            return CreateEntityResult<Dossier>.FromSuccess((await GetDossierByTitleAsync(db, title)).Entity);
        }

        /// <summary>
        /// Deletes a dossier from the database.
        /// </summary>
        /// <param name="db">The database where dossier metadata is stored.</param>
        /// <param name="dossier">The dossier to delete.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteDossierAsync
        (
            [NotNull] AmbyDatabaseContext db,
            [NotNull] Dossier dossier
        )
        {
            var deleteContentResult = await _content.DeleteDossierDataAsync(dossier);
            if (!deleteContentResult.IsSuccess)
            {
                return deleteContentResult;
            }

            db.Dossiers.Remove(dossier);
            await db.SaveChangesAsync();

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Determines whether or not the given dossier title is unique. This method is case-insensitive.
        /// </summary>
        /// <param name="db">The database where dossier metadata is stored.</param>
        /// <param name="dossierTitle">The title of the dossier.</param>
        /// <returns><value>true</value> if the title is unique; otherwise,<value>false</value>.</returns>
        [Pure]
        public async Task<bool> IsDossierTitleUniqueAsync
        (
            [NotNull] AmbyDatabaseContext db,
            [NotNull] string dossierTitle
        )
        {
            return await db.Dossiers.Select(d => d.Title)
            .AllAsync
            (
                p => !p.Equals(dossierTitle, StringComparison.OrdinalIgnoreCase)
            );
        }

        /// <summary>
        /// Gets a dossier by its title.
        /// </summary>
        /// <param name="db">The database containing the dossiers.</param>
        /// <param name="title">The title of the dossier.</param>
        /// <returns>A retrieval task that may or may not have succeeded.</returns>
        [Pure]
        public async Task<RetrieveEntityResult<Dossier>> GetDossierByTitleAsync
        (
            [NotNull] AmbyDatabaseContext db,
            [NotNull] string title
        )
        {
            var dossier = await db.Dossiers.FirstOrDefaultAsync(d => string.Equals(d.Title, title, StringComparison.OrdinalIgnoreCase));
            if (dossier is null)
            {
                return RetrieveEntityResult<Dossier>.FromError(CommandError.ObjectNotFound, "No dossier with that title found.");
            }

            return RetrieveEntityResult<Dossier>.FromSuccess(dossier);
        }

        /// <summary>
        /// Sets the title of the dossier.
        /// </summary>
        /// <param name="db">The database containing the dossier metadata.</param>
        /// <param name="dossier">The dossier to modify.</param>
        /// <param name="newTitle">The new title.</param>
        /// <returns>An entity modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDossierTitleAsync
        (
            [NotNull] AmbyDatabaseContext db,
            [NotNull] Dossier dossier,
            [NotNull] string newTitle
        )
        {
            var isNewNameUnique = await IsDossierTitleUniqueAsync(db, newTitle);

            // If the only thing that has changed is casing, let it through
            if (!isNewNameUnique)
            {
                bool isOnlyCaseChange = false;
                if (!(dossier.Title is null))
                {
                    isOnlyCaseChange = string.Equals(dossier.Title, newTitle, StringComparison.OrdinalIgnoreCase);
                }

                if (!isOnlyCaseChange)
                {
                    return ModifyEntityResult.FromError(CommandError.MultipleMatches, "A dossier with that title already exists.");
                }
            }

            if (newTitle.Contains("\""))
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The title may not contain double quotes.");
            }

            if (newTitle.IndexOfAny(Path.GetInvalidPathChars()) > -1)
            {
                return ModifyEntityResult.FromError(CommandError.Unsuccessful, $"The title contains one or more of invalid characters ({Path.GetInvalidPathChars().Humanize("or")})");
            }

            dossier.Title = newTitle;

            var updateDataResult = await UpdateDossierDataLocationAsync(db, dossier);
            if (!updateDataResult.IsSuccess)
            {
                return updateDataResult;
            }

            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the summary of the dossier.
        /// </summary>
        /// <param name="db">The database containing the dossier metadata.</param>
        /// <param name="dossier">The dossier to modify.</param>
        /// /// <param name="newSummary">The new summary.</param>
        /// <returns>An entity modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDossierSummaryAsync
        (
            [NotNull] AmbyDatabaseContext db,
            [NotNull] Dossier dossier,
            [NotNull] string newSummary
        )
        {
            if (newSummary.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError(CommandError.ObjectNotFound, "You need to provide a summary.");
            }

            dossier.Summary = newSummary;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Updates the location of the dossier data, matching it to the dossier's name.
        /// </summary>
        /// <param name="db">The database containing the dossier metadata.</param>
        /// <param name="dossier">The dossier to update.</param>
        /// <returns>An entity modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> UpdateDossierDataLocationAsync
        (
            [NotNull] AmbyDatabaseContext db,
            [NotNull] Dossier dossier
        )
        {
            var originalDossierPath = dossier.Path;
            var newDossierPath = Path.GetFullPath(Path.Combine(_content.BaseDossierPath, $"{dossier.Title}.pdf"));
            if (Directory.GetParent(newDossierPath).FullName != _content.BaseDossierPath)
            {
                return ModifyEntityResult.FromError(CommandError.Exception, "Invalid data path.");
            }

            if (originalDossierPath.IsNullOrWhitespace() || !File.Exists(originalDossierPath) || originalDossierPath == newDossierPath)
            {
                return ModifyEntityResult.FromSuccess();
            }

            try
            {
                File.Move(originalDossierPath, newDossierPath);
            }
            catch (Exception e)
            {
                return ModifyEntityResult.FromError(CommandError.Exception, e.Message);
            }

            dossier.Path = newDossierPath;
            await db.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the PDF data of a given dossier. This overwrites existing data.
        /// </summary>
        /// <param name="db">The database containing the dossier metadata.</param>
        /// <param name="dossier">The dosser for which to set the data.</param>
        /// <param name="context">The stream containing the PDF data.</param>
        /// <returns>An entity modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetDossierDataAsync
        (
            [NotNull] AmbyDatabaseContext db,
            [NotNull] Dossier dossier,
            [NotNull] SocketCommandContext context
        )
        {
            var dossierPath = Path.GetFullPath(Path.Combine(_content.BaseContentPath, "Dossiers", $"{dossier.Title}.pdf"));

            if (Directory.GetParent(dossierPath).FullName != _content.BaseDossierPath)
            {
                return ModifyEntityResult.FromError(CommandError.Exception, "Invalid data path.");
            }

            if (context.Message.Attachments.Count <= 0)
            {
                return ModifyEntityResult.FromError(CommandError.ObjectNotFound, "No file provided. Please attach a PDF with the dossier data.");
            }

            var dossierAttachment = context.Message.Attachments.First();
            if (!dossierAttachment.Filename.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return ModifyEntityResult.FromError(CommandError.UnmetPrecondition, "Invalid dossier format. PDF files are accepted.");
            }

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(2);

                try
                {
                    using (var dataStream = await client.GetStreamAsync(dossierAttachment.Url))
                    {
                        try
                        {
                            using (var dataFile = File.Create(dossierPath))
                            {
                                await dataStream.CopyToAsync(dataFile);

                                if (!await dataFile.HasSignatureAsync(FileSignatures.PDF))
                                {
                                    return ModifyEntityResult.FromError(CommandError.Exception, "Invalid dossier format. PDF files are accepted.");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            switch (e)
                            {
                                case UnauthorizedAccessException _:
                                case PathTooLongException _:
                                case DirectoryNotFoundException _:
                                case IOException _:
                                {
                                    if (File.Exists(dossierPath))
                                    {
                                        File.Delete(dossierPath);
                                    }

                                    return ModifyEntityResult.FromError(CommandError.Exception, $"Failed to set the dossier data: {e.Message}");
                                }
                            }

                            throw;
                        }

                        dossier.Path = dossierPath;

                        await db.SaveChangesAsync();
                    }
                }
                catch (TaskCanceledException)
                {
                    return ModifyEntityResult.FromError(CommandError.Unsuccessful, "The download operation timed out. The data file was not added.");
                }
            }

            return ModifyEntityResult.FromSuccess();
        }
    }
}
