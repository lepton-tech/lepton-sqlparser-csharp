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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/oracle.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/index.html">Oracle Databases</see>
/// </summary>
public class OracleDialect : Dialect
{
    // Constante de módulo no Rust (antes do struct).
    private static readonly Keyword[] ReservedKeywordsForSelectItemOperator = { Keyword.CONNECT_BY_ROOT };

    // ~ appears not to be called anywhere
    public override char? IdentifierQuoteStyle(string identifier) => '"';

    public override bool IsDelimitedIdentifierStart(char ch) => ch == '"';

    public override bool IsIdentifierStart(char ch) => char.IsLetter(ch);

    public override bool IsIdentifierPart(char ch) => char.IsLetterOrDigit(ch) || ch == '_' || ch == '$' || ch == '#' || ch == '@';

    public override bool SupportsOuterJoinOperator => true;

    public override bool SupportsConnectBy => true;

    public override bool SupportsExecuteImmediate => true;

    public override bool SupportsMatchRecognize => true;

    public override bool SupportsWindowFunctionNullTreatmentArg => true;

    public override bool SupportsBooleanLiterals => false;

    public override bool SupportsCommentOn => true;

    public override bool SupportsCreateTableSelect => true;

    public override bool SupportsSetStmtWithoutOperator => true;

    // PENDENTE (Fase 2, depende de Parser/AST): get_next_precedence (oracle.rs:88).

    public override bool SupportsGroupByExpr => true;

    public override IReadOnlyList<Keyword> GetReservedKeywordsForSelectItemOperator => ReservedKeywordsForSelectItemOperator;

    public override bool SupportsQuoteDelimitedString => true;

    public override bool SupportsCommentOptimizerHint => true;

    public override bool SupportsInsertTableAlias => true;

    /// <summary>
    /// See <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/INSERT.html#GUID-903F8043-0254-4EE9-ACC1-CB8AC0AF3423__I2126242"/>
    /// </summary>
    public override bool SupportsInsertTableQuery => true;
}
