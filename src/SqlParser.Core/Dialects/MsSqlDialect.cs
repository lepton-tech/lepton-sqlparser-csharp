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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/mssql.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://www.microsoft.com/en-us/sql-server/">Microsoft SQL Server</see>
/// </summary>
public class MsSqlDialect : Dialect
{
    public override bool IsDelimitedIdentifierStart(char ch) => ch == '"' || ch == '[';

    public override bool IsIdentifierStart(char ch) =>
        // See https://docs.microsoft.com/en-us/sql/relational-databases/databases/database-identifiers?view=sql-server-2017#rules-for-regular-identifiers
        char.IsLetter(ch) || ch == '_' || ch == '#' || ch == '@';

    public override bool IsIdentifierPart(char ch) =>
        char.IsLetter(ch)
            || (ch >= '0' && ch <= '9')
            || ch == '@'
            || ch == '$'
            || ch == '#'
            || ch == '_';

    public override char? IdentifierQuoteStyle(string identifier) => '[';

    /// <summary>
    /// SQL Server has <c>CONVERT(type, value)</c> instead of <c>CONVERT(value, type)</c>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql?view=sql-server-ver16"/>
    /// </summary>
    public override bool ConvertTypeBeforeValue => true;

    public override bool SupportsOuterJoinOperator => true;

    /// <summary>
    /// SQL Server supports <c>$</c> as a prefix for money literals
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/constants-transact-sql?view=sql-server-ver17#money-constants"/>
    /// </summary>
    public override bool SupportsDollarAsMoneyPrefix => true;

    public override bool SupportsConnectBy => true;

    public override bool SupportsEqAliasAssignment => true;

    public override bool SupportsTryConvert => true;

    /// <summary>
    /// In MSSQL, there is no boolean type, and <c>true</c> and <c>false</c> are valid column names
    /// </summary>
    public override bool SupportsBooleanLiterals => false;

    public override bool SupportsNamedFnArgsWithColonOperator => true;

    public override bool SupportsNamedFnArgsWithExprName => true;

    public override bool SupportsNamedFnArgsWithRarrowOperator => false;

    public override bool SupportsStartTransactionModifier => true;

    public override bool SupportsEndTransactionModifier => true;

    /// <summary>
    /// See: <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/set-statements-transact-sql"/>
    /// </summary>
    public override bool SupportsSetStmtWithoutOperator => true;

    /// <summary>
    /// See: <see href="https://learn.microsoft.com/en-us/sql/relational-databases/tables/querying-data-in-a-system-versioned-temporal-table"/>
    /// </summary>
    public override bool SupportsTableVersioning => true;

    /// <summary>
    /// See <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/slash-star-comment-transact-sql?view=sql-server-ver16"/>
    /// </summary>
    public override bool SupportsNestedComments => true;

    /// <summary>
    /// See <see href="https://learn.microsoft.com/en-us/sql/t-sql/queries/from-transact-sql"/>
    /// </summary>
    public override bool SupportsObjectNameDoubleDotNotation => true;

    // PENDENTE (Fase 2, depende da AST): get_reserved_grantees_types (mssql.rs:132).

    // PENDENTE (Fase 2, depende de Parser/AST): is_select_item_alias (mssql.rs:136).

    // PENDENTE (Fase 2, depende de Parser/AST): is_table_factor_alias (mssql.rs:166).

    // PENDENTE (Fase 2, depende de Parser/AST): parse_statement (mssql.rs:200).

    // PENDENTE (Fase 2, depende de Parser/AST): get_next_precedence (mssql.rs:243).

    // PENDENTE (Fase 2, depende de Parser/AST): bloco `impl MsSqlDialect` (mssql.rs:253): parse_if_stmt
    // (mssql.rs:260), parse_create_trigger (mssql.rs:341) e parse_statement_list (mssql.rs:380).
}
