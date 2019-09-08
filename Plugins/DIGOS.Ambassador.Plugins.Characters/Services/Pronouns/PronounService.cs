//
//  PronounService.cs
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
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Core.Utility;
using DIGOS.Ambassador.Plugins.Characters.Model;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Characters.Services.Pronouns
{
    /// <summary>
    /// Provides access to pronouns.
    /// </summary>
    [PublicAPI]
    public sealed class PronounService
    {
        [NotNull] private readonly Dictionary<string, IPronounProvider> _pronounProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="PronounService"/> class.
        /// </summary>
        public PronounService()
        {
            _pronounProviders = new Dictionary<string, IPronounProvider>(new CaseInsensitiveStringEqualityComparer());
        }

        /// <summary>
        /// Discovers available pronoun providers in the assembly, adding them to the available providers.
        /// </summary>
        public void DiscoverPronounProviders()
        {
            _pronounProviders.Clear();

            var assembly = Assembly.GetExecutingAssembly();
            var pronounProviderTypes = assembly.DefinedTypes.Where
            (
                t => t.ImplementedInterfaces.Contains(typeof(IPronounProvider))
                && t.IsClass
                && !t.IsAbstract
            );

            foreach (var type in pronounProviderTypes)
            {
                var pronounProvider = Activator.CreateInstance(type) as IPronounProvider;
                if (pronounProvider is null)
                {
                    continue;
                }

                WithPronounProvider(pronounProvider);
            }
        }

        /// <summary>
        /// Adds the given pronoun provider to the service.
        /// </summary>
        /// <param name="pronounProvider">The pronoun provider to add.</param>
        /// <returns>The service with the provider.</returns>
        [NotNull]
        public PronounService WithPronounProvider([NotNull] IPronounProvider pronounProvider)
        {
            _pronounProviders.Add(pronounProvider.Family, pronounProvider);
            return this;
        }

        /// <summary>
        /// Gets the pronoun provider for the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>A pronoun provider.</returns>
        /// <exception cref="ArgumentException">Thrown if no pronoun provider exists for the character's preference.</exception>
        [NotNull]
        public IPronounProvider GetPronounProvider([NotNull] Character character)
        {
            if (_pronounProviders.ContainsKey(character.PronounProviderFamily))
            {
                return _pronounProviders[character.PronounProviderFamily];
            }

            throw new KeyNotFoundException("No pronoun provider for that family found.");
        }

        /// <summary>
        /// Gets the available pronoun providers.
        /// </summary>
        /// <returns>An enumerator over the available pronouns.</returns>
        [NotNull, ItemNotNull]
        public IEnumerable<IPronounProvider> GetAvailablePronounProviders()
        {
            return _pronounProviders.Values;
        }

        /// <summary>
        /// Gets the pronoun provider with the given family.
        /// </summary>
        /// <param name="pronounFamily">The family.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [NotNull]
        public RetrieveEntityResult<IPronounProvider> GetPronounProvider([NotNull] string pronounFamily)
        {
            if (!_pronounProviders.TryGetValue(pronounFamily, out var provider))
            {
                return RetrieveEntityResult<IPronounProvider>.FromError
                (
                    "Could not find a pronoun provider for that family."
                );
            }

            return RetrieveEntityResult<IPronounProvider>.FromSuccess(provider);
        }
    }
}
