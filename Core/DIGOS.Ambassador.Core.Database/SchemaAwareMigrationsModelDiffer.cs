//
//  SchemaAwareMigrationsModelDiffer.cs
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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;

namespace DIGOS.Ambassador.Core.Database
{
    /// <summary>
    /// Generates differences between migrations, taking the schema of the model entities into account.
    /// </summary>
    public class SchemaAwareMigrationsModelDiffer : MigrationsModelDiffer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaAwareMigrationsModelDiffer"/> class.
        /// </summary>
        /// <param name="typeMappingSource">The type mapping source to use.</param>
        /// <param name="migrationsAnnotations">The migration annotation provider.</param>
        /// <param name="changeDetector">The change detector.</param>
        /// <param name="stateManagerDependencies">The state manager dependencies.</param>
        /// <param name="commandBatchPreparerDependencies">The command batch preparer dependencies.</param>
        public SchemaAwareMigrationsModelDiffer
        (
            IRelationalTypeMappingSource typeMappingSource,
            IMigrationsAnnotationProvider migrationsAnnotations,
            IChangeDetector changeDetector,
            StateManagerDependencies stateManagerDependencies,
            CommandBatchPreparerDependencies commandBatchPreparerDependencies
        )
            : base
            (
                typeMappingSource,
                migrationsAnnotations,
                changeDetector,
                stateManagerDependencies,
                commandBatchPreparerDependencies
            )
        {
        }

        /// <inheritdoc/>
        public override bool HasDifferences(IModel source, IModel target)
            => Diff(source, target, new SchemaAwareDiffContext(source, target)).Any();

        /// <inheritdoc/>
        public override IReadOnlyList<MigrationOperation> GetDifferences
        (
            IModel source,
            IModel target
        )
        {
            var diffContext = new SchemaAwareDiffContext(source, target);
            return Sort(Diff(source, target, diffContext), diffContext);
        }

        private static ReferentialAction ToReferentialAction(DeleteBehavior deleteBehavior)
            => deleteBehavior == DeleteBehavior.Cascade
                ? ReferentialAction.Cascade
                : deleteBehavior == DeleteBehavior.SetNull
                    ? ReferentialAction.SetNull
                    : ReferentialAction.Restrict;

        /// <inheritdoc/>
        protected override IEnumerable<MigrationOperation> Diff(
            IEnumerable<IForeignKey> source,
            IEnumerable<IForeignKey> target,
            DiffContext diffContext)
        {
            return DiffCollection
            (
                source,
                target,
                diffContext,
                Diff,
                Add,
                Remove,
                (s, t, c) =>
                {
                    if (s.Relational().Name != t.Relational().Name)
                    {
                        return false;
                    }

                    if (!s.Properties.Select(p => p.Relational().ColumnName).SequenceEqual(
                        t.Properties.Select(p => c.FindSource(p)?.Relational().ColumnName)))
                    {
                        return false;
                    }

                    var schemaToInclude = ((SchemaAwareDiffContext)diffContext).Source.Relational().DefaultSchema;

                    if (c.FindSourceTable(s.PrincipalEntityType).Schema == schemaToInclude &&
                        c.FindSourceTable(s.PrincipalEntityType) !=
                        c.FindSource(c.FindTargetTable(t.PrincipalEntityType)))
                    {
                        return false;
                    }

                    if (t.PrincipalKey.Properties.Select(p => c.FindSource(p)?.Relational().ColumnName)
                            .First() != null && !s.PrincipalKey.Properties
                            .Select(p => p.Relational().ColumnName).SequenceEqual(
                                t.PrincipalKey.Properties.Select(p =>
                                    c.FindSource(p)?.Relational().ColumnName)))
                    {
                        return false;
                    }

                    if (ToReferentialAction(s.DeleteBehavior) != ToReferentialAction(t.DeleteBehavior))
                    {
                        return false;
                    }

                    return !HasDifferences
                    (
                        this.MigrationsAnnotations.For(s),
                        this.MigrationsAnnotations.For(t)
                    );
                }
            );
        }

        /// <summary>
        /// Provides a schema aware diffing context for the migration differ.
        /// </summary>
        protected class SchemaAwareDiffContext : DiffContext
        {
            /// <summary>
            /// Gets the source model.
            /// </summary>
            public IModel Source { get; }

            /// <summary>
            /// Gets the target model.
            /// </summary>
            public IModel Target { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="SchemaAwareDiffContext"/> class.
            /// </summary>
            /// <param name="source">The source model.</param>
            /// <param name="target">The target model.</param>
            public SchemaAwareDiffContext(IModel source, IModel target)
                : base(source, target)
            {
                this.Source = source;
                this.Target = target;
            }

            /// <inheritdoc/>
            public override IEnumerable<TableMapping> GetSourceTables()
            {
                var schemaToInclude = this.Source.Relational().DefaultSchema;
                var tables = base.GetSourceTables();

                return tables.Where(x => x.Schema == schemaToInclude);
            }

            /// <inheritdoc/>
            public override IEnumerable<TableMapping> GetTargetTables()
            {
                var schemaToInclude = this.Target.Relational().DefaultSchema;
                var tables = base.GetTargetTables();

                return tables.Where(x => x.Schema == schemaToInclude);
            }
        }
    }
}
