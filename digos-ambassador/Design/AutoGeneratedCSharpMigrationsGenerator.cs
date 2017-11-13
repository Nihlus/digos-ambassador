﻿//
//  AutoGeneratedCSharpMigrationsGenerator.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace DIGOS.Ambassador.Design
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
			$"// ReSharper disable RedundantUsingDirective{Environment.NewLine}";

		/// <inheritdoc />
		public override string GenerateMigration
		(
			string migrationNamespace,
			string migrationName,
			IReadOnlyList<MigrationOperation> upOperations,
			IReadOnlyList<MigrationOperation> downOperations
		)
		=>
			@"// <auto-generated />"
			+ Environment.NewLine
			+ this.WarningDisablers
			+ base.GenerateMigration(migrationNamespace, migrationName, upOperations, downOperations);

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
			var originalMetadata = base.GenerateMetadata(migrationNamespace, contextType, migrationName, migrationId, targetModel);

			var secondLineIndex = originalMetadata.IndexOf(Environment.NewLine, StringComparison.Ordinal) + 1;

			return originalMetadata.Insert(secondLineIndex, this.WarningDisablers);
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
			var originalMetadata = base.GenerateSnapshot(modelSnapshotNamespace, contextType, modelSnapshotName, model);

			var secondLineIndex = originalMetadata.IndexOf(Environment.NewLine, StringComparison.Ordinal) + 1;

			return originalMetadata.Insert(secondLineIndex, this.WarningDisablers);
		}
	}
}
