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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/sqlite.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://www.sqlite.org">SQLite</see>
/// <para>
/// This dialect allows columns in a
/// <see href="https://sqlite.org/lang_createtable.html"><c>CREATE TABLE</c></see> statement with no
/// type specified, as in <c>CREATE TABLE t1 (a)</c>. In the AST, these columns will
/// have the data type <c>Unspecified</c> (<c>crate::ast::DataType::Unspecified</c>).
/// </para>
/// </summary>
public class SQLiteDialect : Dialect
{
    // see https://www.sqlite.org/lang_keywords.html
    // parse `...`, [...] and "..." as identifier
    // TODO: support depending on the context tread '...' as identifier too.
    public override bool IsDelimitedIdentifierStart(char ch) => ch == '`' || ch == '"' || ch == '[';

    public override char? IdentifierQuoteStyle(string identifier) => '`';

    public override bool IsIdentifierStart(char ch) =>
        // See https://www.sqlite.org/draft/tokenreq.html
        (ch >= 'a' && ch <= 'z')
            || (ch >= 'A' && ch <= 'Z')
            || ch == '_'
            // Port: um char Rust nunca é substituto; `!char.IsSurrogate` mantém o intervalo do Rust
            // (fora do BMP não é aceito). Ver convencoes.md §13.
            || (ch >= '\u007f' && ch <= '\uffff' && !char.IsSurrogate(ch));

    public override bool SupportsFilterDuringAggregation => true;

    public override bool SupportsStartTransactionModifier => true;

    public override bool IsIdentifierPart(char ch) => IsIdentifierStart(ch) || (ch >= '0' && ch <= '9');

    // PENDENTE (Fase 2, depende de Parser/AST): parse_statement (sqlite.rs:69).

    // PENDENTE (Fase 2, depende de Parser/AST): parse_infix (sqlite.rs:78).

    public override bool SupportsInEmptyList => true;

    public override bool SupportsLimitComma => true;

    public override bool SupportsAscDescInColumnDefinition => true;

    public override bool SupportsDollarPlaceholder => true;

    /// <summary>
    /// SQLite supports <c>NOTNULL</c> as aliases for <c>IS NOT NULL</c>
    /// See: <see href="https://sqlite.org/syntax/expr.html"/>
    /// </summary>
    public override bool SupportsNotnullOperator => true;

    public override bool SupportsCommaSeparatedTrim => true;

    public override bool SupportsNumericLiteralUnderscores => true;
}
