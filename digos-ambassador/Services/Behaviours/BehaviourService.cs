//
//  BehaviourService.cs
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
using System.Reflection;
using System.Threading.Tasks;
using DIGOS.Ambassador.Behaviours;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.Services.Behaviours
{
    /// <summary>
    /// This class manages the access to and lifetime of registered behaviours.
    /// </summary>
    public class BehaviourService
    {
        private readonly ICollection<IBehaviour> RegisteredBehaviours = new List<IBehaviour>();

        /// <summary>
        /// Discovers and adds behaviours defined in the given assembly.
        /// </summary>
        /// <param name="containingAssembly">The assembly where behaviours are defined.</param>
        /// <param name="services">The services available to the application.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AddBehavioursAsync([NotNull] Assembly containingAssembly, IServiceProvider services)
        {
            var definedTypes = containingAssembly.DefinedTypes;
            var behaviourTypes = definedTypes.Where(t => t.ImplementedInterfaces.Contains(typeof(IBehaviour)));

            foreach (var behaviourType in behaviourTypes)
            {
                if (behaviourType.IsAbstract)
                {
                    continue;
                }

                // Since the behaviours run in their own threads, we'll do scoped contexts for them. The behaviours run
                // until they're disposed, so they're responsible for clearing up their own scopes.
                var scope = services.CreateScope();
                var behaviour = (IBehaviour)ActivatorUtilities.CreateInstance
                (
                    scope.ServiceProvider,
                    behaviourType
                );

                behaviour.WithScope(scope);

                // Behaviours are implicitly singletons; there's only ever one instance of a behaviour at any given
                // time.
                if (this.RegisteredBehaviours.Any(b => b.GetType() == behaviourType))
                {
                    var existingBehaviour = this.RegisteredBehaviours.First(b => b.GetType() == behaviourType);
                    this.RegisteredBehaviours.Remove(existingBehaviour);

                    await existingBehaviour.StopAsync();
                    existingBehaviour.Dispose();
                }

                this.RegisteredBehaviours.Add(behaviour);
            }
        }

        /// <summary>
        /// Starts all registered behaviours.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task StartBehavioursAsync()
        {
            foreach (var behaviour in this.RegisteredBehaviours)
            {
                await behaviour.StartAsync();
            }
        }

        /// <summary>
        /// Stops all registered behaviours.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task StopBehavioursAsync()
        {
            foreach (var behaviour in this.RegisteredBehaviours)
            {
                await behaviour.StopAsync();
            }
        }
    }
}
