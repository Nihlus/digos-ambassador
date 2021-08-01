//
//  TransactionWrappingExecutionEvent.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using DIGOS.Ambassador.Core.Database;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Results;

namespace DIGOS.Ambassador.ExecutionEventServices
{
    /// <summary>
    /// Handles the wrapping of a command inside a transaction scope.
    /// </summary>
    public class TransactionWrappingExecutionEvent : IPreExecutionEvent, IPostExecutionEvent
    {
        private readonly TaskCompletionSource<bool> _scopeCreationSource;
        private TransactionScope? _transaction;

        /// <summary>
        /// Gets a task that completes when the transaction has been created.
        /// </summary>
        public Task ScopeCreated => _scopeCreationSource.Task;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionWrappingExecutionEvent"/> class.
        /// </summary>
        public TransactionWrappingExecutionEvent()
        {
            _scopeCreationSource = new TaskCompletionSource<bool>();
        }

        /// <inheritdoc />
        public Task<Result> BeforeExecutionAsync
        (
            ICommandContext context,
            CancellationToken ct = default
        )
        {
            _transaction = TransactionFactory.Create();
            _scopeCreationSource.SetResult(true);

            return Task.FromResult(Result.FromSuccess());
        }

        /// <inheritdoc />
        public Task<Result> AfterExecutionAsync
        (
            ICommandContext context,
            IResult executionResult,
            CancellationToken ct = default
        )
        {
            if (_transaction is null)
            {
                throw new InvalidOperationException();
            }

            if (executionResult.IsSuccess)
            {
                _transaction.Complete();
            }

            _transaction.Dispose();
            return Task.FromResult(Result.FromSuccess());
        }
    }
}
