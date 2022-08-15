//
//  TransactionFactory.cs
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

using System.Transactions;

namespace DIGOS.Ambassador.Core.Database;

/// <summary>
/// Creates transactions.
/// </summary>
public static class TransactionFactory
{
    /// <summary>
    /// Creates a new ambient transaction with asynchronous flow enabled.
    /// </summary>
    /// <param name="isolationLevel">The isolation level to use.</param>
    /// <returns>The created scope.</returns>
    public static TransactionScope Create(IsolationLevel isolationLevel = IsolationLevel.Serializable)
    {
        var options = new TransactionOptions
        {
            IsolationLevel = isolationLevel,
            Timeout = TransactionManager.DefaultTimeout
        };

        return new TransactionScope
        (
            TransactionScopeOption.Required,
            options,
            TransactionScopeAsyncFlowOption.Enabled
        );
    }
}
