//
//  TransformationTextTokenizer.cs
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
using System.Text;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Transformations
{
    /// <summary>
    /// Tokenizer for transformation text. This class should be instantiated as a singleton.
    /// </summary>
    public sealed class TransformationTextTokenizer
    {
        private readonly Dictionary<string, Type> _availableTokens = new Dictionary<string, Type>();

        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationTextTokenizer"/> class.
        /// </summary>
        /// <param name="services">The services to make available to tokens via dependency injection.</param>
        public TransformationTextTokenizer(IServiceProvider services)
        {
            _services = services;
        }

        /// <summary>
        /// Discovers available tokens in the given assembly, adding them to the tokenizer.
        /// </summary>
        /// <param name="assembly">The assembly to scan. Defaults to the executing assembly.</param>
        public void DiscoverAvailableTokens(Assembly assembly = null)
        {
            assembly = assembly ?? Assembly.GetExecutingAssembly();
            var tokenTypes = assembly.DefinedTypes.Where
            (
                t =>
                    t.ImplementedInterfaces.Contains(typeof(IReplaceableTextToken))
                    && !t.IsInterface
                    && !(t.BaseType is null)
                    && t.BaseType.IsGenericType
                    &&
                        (t.BaseType.GetGenericTypeDefinition() == typeof(ReplacableTextToken<>)
                        || t.BaseType.GetGenericTypeDefinition().IsSubclassOf(typeof(ReplacableTextToken<>)))
            );

            foreach (var tokenType in tokenTypes)
            {
                AddAvailableTokenType(tokenType);
            }
        }

        private void AddAvailableTokenType<T>() where T : ReplacableTextToken<T>
        {
            AddAvailableTokenType(typeof(T));
        }

        private void AddAvailableTokenType([NotNull] Type tokenType)
        {
            var tokenIdentifier = tokenType.GetCustomAttribute<TokenIdentifierAttribute>();
            if (tokenIdentifier is null)
            {
                throw new ArgumentException($"The token type \"{tokenType.Name}\" does not have an identifier attribute.");
            }

            foreach (var identifier in tokenIdentifier.Identifiers)
            {
                if (_availableTokens.ContainsKey(identifier))
                {
                    throw new ArgumentException($"A token with the identifier \"{identifier}\"has already been registered.");
                }

                _availableTokens.Add(identifier, tokenType);
            }
        }

        /// <summary>
        /// Adds an available token type to the tokenizer.
        /// </summary>
        /// <typeparam name="T">A valid token type.</typeparam>
        /// <returns>The tokenizer.</returns>
        [NotNull]
        public TransformationTextTokenizer WithTokenType<T>() where T : ReplacableTextToken<T>
        {
            AddAvailableTokenType<T>();
            return this;
        }

        /// <summary>
        /// Gets a list of the recognized tokens in a piece of text.
        /// </summary>
        /// <param name="text">The text to tokenize.</param>
        /// <returns>A list of recognized tokens in the text.</returns>
        [NotNull]
        [ItemNotNull]
        public IReadOnlyList<IReplaceableTextToken> GetTokens([NotNull] string text)
        {
            var tokens = new List<IReplaceableTextToken>();
            int position = 0;
            while (position < text.Length - 1)
            {
                // Scan ahead to the next token
                while (position < text.Length - 1 && text[position] != '{')
                {
                    position++;
                }

                var sb = new StringBuilder();

                int start = position;

                // Read the token text
                while (position + 1 < text.Length - 1 && text[position + 1] != '}')
                {
                    position++;
                    sb.Append(text[position]);
                }

                // Try to create a matching token
                var tokenText = sb.ToString();
                var token = ParseToken(start, tokenText);
                if (token is null)
                {
                    continue;
                }

                tokens.Add(token);
            }

            return tokens;
        }

        /// <summary>
        /// Parses a token from the given text and starting position.
        /// </summary>
        /// <param name="start">The start index of the token.</param>
        /// <param name="tokenText">The raw text of the token.</param>
        /// <returns>A token object.</returns>
        [CanBeNull]
        public IReplaceableTextToken ParseToken(int start, string tokenText)
        {
            // Tokens are of the format @<tag>|<data>
            if (tokenText.Length <= 1)
            {
                return null;
            }

            if (tokenText[0] != '@')
            {
                return null;
            }

            tokenText = new string(tokenText.Skip(1).ToArray());

            string identifier;
            string data = null;
            if (tokenText.Contains('|'))
            {
                int splitter = tokenText.LastIndexOf('|');
                identifier = tokenText.Substring(0, splitter);
                data = tokenText.Substring(splitter + 1);
            }
            else
            {
                identifier = tokenText;
            }

            // Look up the token type
            if (!_availableTokens.ContainsKey(identifier))
            {
                return null;
            }

            var tokenType = _availableTokens[identifier];

            // TargetToken is only used here for compile-time resolution of the CreateFrom method name.
            var creationMethod = tokenType.GetMethod
            (
                nameof(ReplacableTextToken<TargetToken>.CreateFrom),
                BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public
            );

            // The +3 here includes the surrounding braces and the @
            var tokenObject = creationMethod?.Invoke(null, new object[] { start, tokenText.Length + 3, data, _services } );

            return (IReplaceableTextToken)tokenObject;
        }
    }
}
