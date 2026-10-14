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
/// Acesso público às listas de keywords geradas a partir de <c>keywords.rs</c>.
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

    /// <summary>All SQL keywords as strings, sorted ordinally (Rust: <c>ALL_KEYWORDS</c>).</summary>
    public static IReadOnlyList<string> AllKeywords => _allKeywords ??= Array.AsReadOnly(AllKeywordsArray);

    /// <summary>All <see cref="Keyword"/> values in declaration order (Rust: <c>ALL_KEYWORDS_INDEX</c>).</summary>
    public static IReadOnlyList<Keyword> AllKeywordsIndex => _allKeywordsIndex ??= Array.AsReadOnly(AllKeywordsIndexArray);

    /// <summary>
    /// These keywords can't be used as a table alias, so that <c>FROM table_name alias</c>
    /// can be parsed unambiguously without looking ahead (Rust: <c>RESERVED_FOR_TABLE_ALIAS</c>).
    /// </summary>
    public static IReadOnlyList<Keyword> ReservedForTableAlias =>
        _reservedForTableAlias ??= Array.AsReadOnly(ReservedForTableAliasArray);

    /// <summary>
    /// Can't be used as a column alias, so that <c>SELECT &lt;expr&gt; alias</c>
    /// can be parsed unambiguously without looking ahead (Rust: <c>RESERVED_FOR_COLUMN_ALIAS</c>).
    /// </summary>
    public static IReadOnlyList<Keyword> ReservedForColumnAlias =>
        _reservedForColumnAlias ??= Array.AsReadOnly(ReservedForColumnAliasArray);

    /// <summary>Global list of reserved keywords allowed after FROM (Rust: <c>RESERVED_FOR_TABLE_FACTOR</c>).</summary>
    public static IReadOnlyList<Keyword> ReservedForTableFactor =>
        _reservedForTableFactor ??= Array.AsReadOnly(ReservedForTableFactorArray);

    /// <summary>
    /// Global list of reserved keywords that cannot be parsed as identifiers
    /// without special handling like quoting (Rust: <c>RESERVED_FOR_IDENTIFIER</c>).
    /// </summary>
    public static IReadOnlyList<Keyword> ReservedForIdentifier =>
        _reservedForIdentifier ??= Array.AsReadOnly(ReservedForIdentifierArray);

    /// <summary>
    /// Procura a keyword correspondente a uma palavra já convertida para maiúsculas.
    /// Equivale a <c>ALL_KEYWORDS.binary_search(..)</c> + <c>ALL_KEYWORDS_INDEX[i]</c> no tokenizer do Rust
    /// (comparação ordinal, como a ordenação por bytes do Rust).
    /// </summary>
    /// <returns>A keyword, ou <see cref="Keyword.NoKeyword"/> se a palavra não for keyword.</returns>
    public static Keyword Lookup(string upperCaseWord)
    {
        var index = Array.BinarySearch(AllKeywordsArray, upperCaseWord, StringComparer.Ordinal);
        return index >= 0 ? AllKeywordsIndexArray[index] : Keyword.NoKeyword;
    }

    /// <summary>
    /// Texto SQL da keyword (Rust: <c>impl Display for Keyword</c>). Difere de <c>ToString()</c>
    /// em casos como <see cref="Keyword.END_EXEC"/>, cujo texto é <c>END-EXEC</c>.
    /// </summary>
    public static string ToSql(this Keyword keyword) => KeywordTexts[(int)keyword];
}
