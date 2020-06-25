//
//  AbstractTransientStateService.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace DIGOS.Ambassador.Core.Services.TransientState
{
    /// <summary>
    /// Represents an abstract base class for services with transient state.
    /// </summary>
    public abstract class AbstractTransientStateService : ITransientStateService
    {
        /// <summary>
        /// Holds any nested services whose state changes should be managed in sync with this service.
        /// </summary>
        private readonly IReadOnlyCollection<ITransientStateService> _nestedServices;

        /// <summary>
        /// Holds a value indicating whether the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Holds the choice the user has made in regards to saving their state.
        /// </summary>
        private TransientStateChoice? _stateChoice;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractTransientStateService"/> class.
        /// </summary>
        /// <param name="nestedServices">The nested services.</param>
        protected AbstractTransientStateService(params ITransientStateService[] nestedServices)
        {
            _nestedServices = nestedServices;
        }

        /// <summary>
        /// Handles actual save logic. This method runs at the end of a saving scope. By default, this does nothing.
        /// </summary>
        protected virtual void OnSavingChanges()
        {
        }

        /// <summary>
        /// Handles actual save logic. This method runs at the end of a saving scope. By default, this does nothing.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="ValueTask"/> representing the current asynchronous operation.</returns>
        protected virtual ValueTask OnSavingChangesAsync(CancellationToken ct = default)
        {
            return default;
        }

        /// <summary>
        /// Handles actual discard logic. This method runs at the end of a discarding scope. By default, this does
        /// nothing.
        /// </summary>
        protected virtual void OnDiscardingChanges()
        {
        }

        /// <summary>
        /// Handles actual discard logic. This method runs at the end of a discarding scope. By default, this does
        /// nothing.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A <see cref="ValueTask"/> representing the current asynchronous operation.</returns>
        protected virtual ValueTask OnDiscardingChangesAsync(CancellationToken ct = default)
        {
            return default;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            foreach (var nestedService in _nestedServices)
            {
                nestedService.Dispose();
            }

            if (_stateChoice is null)
            {
                throw new InvalidOperationException("No choice as to whether to discard or save changes was made.");
            }

            switch (_stateChoice)
            {
                case TransientStateChoice.Save:
                {
                    OnSavingChanges();
                    break;
                }
                case TransientStateChoice.Discard:
                {
                    OnDiscardingChanges();
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            _isDisposed = true;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            foreach (var nestedService in _nestedServices)
            {
                await nestedService.DisposeAsync();
            }

            if (_stateChoice is null)
            {
                throw new InvalidOperationException("No choice as to whether to discard or save changes was made.");
            }

            switch (_stateChoice)
            {
                case TransientStateChoice.Save:
                {
                    await OnSavingChangesAsync();
                    break;
                }
                case TransientStateChoice.Discard:
                {
                    await OnDiscardingChangesAsync();
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            _isDisposed = true;
        }

        /// <inheritdoc />
        public virtual void SaveChanges()
        {
            foreach (var nestedService in _nestedServices)
            {
                nestedService.SaveChanges();
            }

            _stateChoice = TransientStateChoice.Save;
        }

        /// <inheritdoc />
        public virtual void DiscardChanges()
        {
            foreach (var nestedService in _nestedServices)
            {
                nestedService.DiscardChanges();
            }

            _stateChoice = TransientStateChoice.Discard;
        }
    }
}
