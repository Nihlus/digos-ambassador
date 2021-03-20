﻿//
//  AutoGeneratedCSharpMigrationsGenerator.cs
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
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

#pragma warning disable EF1001 // Internal EF API

namespace DIGOS.Ambassador.Core.Database.Design
{
    /// <inheritdoc />
    public class AutoGeneratedCSharpMigrationsGenerator : CSharpMigrationsGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoGeneratedCSharpMigrationsGenerator"/> class.
        /// </summary>
        /// <param name="dependencies">The base dependencies.</param>
        /// <param name="csharpDependencies">The C# generator dependencies.</param>
        public AutoGeneratedCSharpMigrationsGenerator(MigrationsCodeGeneratorDependencies dependencies, CSharpMigrationsGeneratorDependencies csharpDependencies)
            : base(dependencies, csharpDependencies)
        {
        }

        private string WarningDisablers =>
            $"#pragma warning disable CS1591{Environment.NewLine}" +
            $"// ReSharper disable RedundantArgumentDefaultValue{Environment.NewLine}" +
            $"// ReSharper disable PartialTypeWithSinglePart{Environment.NewLine}" +
            "// ReSharper disable RedundantUsingDirective";

        /// <inheritdoc />
        public override string GenerateMigration
        (
            string migrationNamespace,
            string migrationName,
            IReadOnlyList<MigrationOperation> upOperations,
            IReadOnlyList<MigrationOperation> downOperations
        )
        {
            var originalMigration = base.GenerateMigration
            (
                migrationNamespace,
                migrationName,
                upOperations,
                downOperations
            ).Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

            var builder = new IndentedStringBuilder();

            builder.AppendLine(@"// <auto-generated />");
            builder.AppendLine();
            builder.AppendLine(this.WarningDisablers);
            builder.AppendLine();

            // Find where to insert our modifications
            var firstUsing = originalMigration.First(s => s.Contains("using"));
            var usingIndent = new string(firstUsing.TakeWhile(c => c == ' ' || c == '\t').ToArray());
            var usingInsertionIndex = originalMigration.IndexOf(firstUsing);

            originalMigration.Insert(usingInsertionIndex, $"{usingIndent}using System.Diagnostics.CodeAnalysis;");

            var classDeclaration = originalMigration.First(s => s.Contains("partial class"));
            var attributeInsertionIndex = originalMigration.IndexOf(classDeclaration);
            var classIndent = new string(classDeclaration.TakeWhile(c => c == ' ' || c == '\t').ToArray());

            originalMigration.Insert(attributeInsertionIndex, $"{classIndent}[ExcludeFromCodeCoverage]");

            // The migration
            foreach (var line in originalMigration)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        /// <inheritdoc />
        public override string GenerateMetadata
        (
            string migrationNamespace,
            Type contextType,
            string migrationName,
            string migrationId,
            IModel targetModel
        )
        {
            var originalMigration = base.GenerateMetadata
            (
                migrationNamespace,
                contextType,
                migrationName,
                migrationId,
                targetModel
            ).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();

            var builder = new IndentedStringBuilder();

            builder.AppendLine(@"// <auto-generated />");
            builder.AppendLine(this.WarningDisablers);
            builder.AppendLine();

            // The migration
            foreach (var line in originalMigration)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        /// <inheritdoc />
        public override string GenerateSnapshot
        (
            string modelSnapshotNamespace,
            Type contextType,
            string modelSnapshotName,
            IModel model
        )
        {
            var originalSnapshot = base.GenerateSnapshot
            (
                modelSnapshotNamespace,
                contextType,
                modelSnapshotName,
                model
            ).Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

            var builder = new IndentedStringBuilder();

            builder.AppendLine(@"// <auto-generated />");
            builder.AppendLine();
            builder.AppendLine(this.WarningDisablers);
            builder.AppendLine();

            // Find where to insert our modifications
            var firstUsing = originalSnapshot.First(s => s.Contains("using"));
            var usingIndent = new string(firstUsing.TakeWhile(c => c == ' ' || c == '\t').ToArray());
            var usingInsertionIndex = originalSnapshot.IndexOf(firstUsing);

            originalSnapshot.Insert(usingInsertionIndex, $"{usingIndent}using System.Diagnostics.CodeAnalysis;");

            var classDeclaration = originalSnapshot.First(s => s.Contains("partial class"));
            var attributeInsertionIndex = originalSnapshot.IndexOf(classDeclaration);
            var classIndent = new string(classDeclaration.TakeWhile(c => c == ' ' || c == '\t').ToArray());

            originalSnapshot.Insert(attributeInsertionIndex, $"{classIndent}[ExcludeFromCodeCoverage]");

            // The migration
            foreach (var line in originalSnapshot)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }
    }
}
