// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//
// Portado para C# a partir de sqlparser-rs v0.63.0, src/keywords.rs.
// Modificado: tradução de Rust para C#. As listas em si são geradas em Keywords.g.cs.

namespace SqlParser.Core;

/// <summary>
/// Public access to the keyword lists generated from <c>keywords.rs</c>.
/// </summary>
public static partial class Keywords
{
    // Os arrays gerados (Keywords.g.cs) ficam internos; as propriedades públicas devolvem um wrapper
    // somente leitura em cache, porque um array exposto como IReadOnlyList<T> poderia ser alterado por cast
    // para T[] (convencoes.md §13, caso 10). Criação preguiçosa: a ordem de inicialização dos campos
    // estáticos entre os arquivos de uma classe partial não é definida.
    private static IReadOnlyList<string>? _allKeywords;
    private static IReadOnlyList<Keyword>? _allKeywordsIndex;
    private static IReadOnlyList<Keyword>? _reservedForTableAlias;
    private static IReadOnlyList<Keyword>? _reservedForColumnAlias;
    private static IReadOnlyList<Keyword>? _reservedForTableFactor;
    private static IReadOnlyList<Keyword>? _reservedForIdentifier;

    /// <summary>Array of all SQL keywords as string constants.</summary>
    // Rust: `ALL_KEYWORDS`. Em ordem ordinal, como exige a busca binária de Lookup.
    public static IReadOnlyList<string> AllKeywords => _allKeywords ??= Array.AsReadOnly(AllKeywordsArray);

    /// <summary>Array of all <c>Keyword</c> enum values in declaration order.</summary>
    // Rust: `ALL_KEYWORDS_INDEX`.
    public static IReadOnlyList<Keyword> AllKeywordsIndex => _allKeywordsIndex ??= Array.AsReadOnly(AllKeywordsIndexArray);

    /// <summary>
    /// These keywords can't be used as a table alias, so that <c>FROM table_name alias</c>
    /// can be parsed unambiguously without looking ahead.
    /// </summary>
    // Rust: `RESERVED_FOR_TABLE_ALIAS`.
    public static IReadOnlyList<Keyword> ReservedForTableAlias =>
        _reservedForTableAlias ??= Array.AsReadOnly(ReservedForTableAliasArray);

    /// <summary>
    /// Can't be used as a column alias, so that <c>SELECT &lt;expr&gt; alias</c>
    /// can be parsed unambiguously without looking ahead.
    /// </summary>
    // Rust: `RESERVED_FOR_COLUMN_ALIAS`.
    public static IReadOnlyList<Keyword> ReservedForColumnAlias =>
        _reservedForColumnAlias ??= Array.AsReadOnly(ReservedForColumnAliasArray);

    /// <summary>
    /// Global list of reserved keywords allowed after FROM.
    /// Parser should call Dialect::get_reserved_keyword_after_from
    /// to allow for each dialect to customize the list.
    /// </summary>
    // Rust: `RESERVED_FOR_TABLE_FACTOR`.
    public static IReadOnlyList<Keyword> ReservedForTableFactor =>
        _reservedForTableFactor ??= Array.AsReadOnly(ReservedForTableFactorArray);

    /// <summary>
    /// Global list of reserved keywords that cannot be parsed as identifiers
    /// without special handling like quoting. Parser should call <c>Dialect::is_reserved_for_identifier</c>
    /// to allow for each dialect to customize the list.
    /// </summary>
    // Rust: `RESERVED_FOR_IDENTIFIER`.
    public static IReadOnlyList<Keyword> ReservedForIdentifier =>
        _reservedForIdentifier ??= Array.AsReadOnly(ReservedForIdentifierArray);

    /// <summary>Looks up the keyword matching a word that is already in uppercase.</summary>
    /// <returns>The keyword, or <see cref="Keyword.NoKeyword"/> if the word is not a keyword.</returns>
    // Equivale a `ALL_KEYWORDS.binary_search(..)` + `ALL_KEYWORDS_INDEX[i]` no tokenizer do Rust
    // (comparação ordinal, como a ordenação por bytes do Rust).
    public static Keyword Lookup(string upperCaseWord)
    {
        var index = Array.BinarySearch(AllKeywordsArray, upperCaseWord, StringComparer.Ordinal);
        return index >= 0 ? AllKeywordsIndexArray[index] : Keyword.NoKeyword;
    }

    /// <summary>
    /// The SQL text of the keyword. It differs from <c>ToString()</c> in cases such as
    /// <see cref="Keyword.END_EXEC"/>, whose text is <c>END-EXEC</c>.
    /// </summary>
    // Rust: `impl Display for Keyword` (convencoes.md §13, caso 3).
    public static string ToSql(this Keyword keyword) => KeywordTexts[(int)keyword];
}
