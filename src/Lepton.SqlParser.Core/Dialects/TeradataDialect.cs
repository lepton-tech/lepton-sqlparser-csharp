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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/teradata.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://docs.teradata.com/">Teradata</see>.
/// </summary>
public class TeradataDialect : Dialect
{
    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Fundamentals/Basic-SQL-Syntax/Object-Names"/>
    /// </summary>
    public override char? IdentifierQuoteStyle(string identifier) => '"';

    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Fundamentals/Basic-SQL-Syntax/Working-with-Unicode-Delimited-Identifiers"/>
    /// </summary>
    public override bool IsDelimitedIdentifierStart(char ch) => ch == '"';

    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/International-Character-Set-Support/Managing-International-Language-Support/Object-Names/Rules-for-Object-Naming"/>
    /// </summary>
    public override bool IsIdentifierStart(char ch) => char.IsLetter(ch) || ch == '_' || ch == '#' || ch == '$';

    // See <https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Fundamentals/Basic-SQL-Syntax/Object-Names>
    public override bool IsIdentifierPart(char ch) => char.IsLetterOrDigit(ch) || IsIdentifierStart(ch);

    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Manipulation-Language/SELECT-Statements/GROUP-BY-Clause/GROUP-BY-Clause-Syntax"/>
    /// </summary>
    public override bool SupportsGroupByExpr => true;

    /// <summary>
    /// Teradata has no native <c>BOOLEAN</c> data type.
    /// <para>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Types-and-Literals"/>
    /// </para>
    /// </summary>
    public override bool SupportsBooleanLiterals => false;

    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Types-and-Literals/Data-Literals/Interval-Literals"/>
    /// </summary>
    public override bool RequireIntervalQualifier => true;

    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Definition-Language-Syntax-and-Examples/Comment-Help-and-Show-Statements/COMMENT-Comment-Placing-Form"/>
    /// </summary>
    public override bool SupportsCommentOn => true;

    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Definition-Language-Syntax-and-Examples/Table-Statements/CREATE-TABLE-and-CREATE-TABLE-AS"/>
    /// </summary>
    public override bool SupportsCreateTableSelect => true;

    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Stored-Procedures-and-Embedded-SQL/Dynamic-Embedded-SQL-Statements/Dynamic-SQL-Statement-Syntax/EXECUTE-IMMEDIATE"/>
    /// </summary>
    public override bool SupportsExecuteImmediate => true;

    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Manipulation-Language/SELECT-Statements/Select-List-Syntax/TOP-Clause"/>
    /// </summary>
    public override bool SupportsTopBeforeDistinct => true;

    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Functions-Expressions-and-Predicates/Ordered-Analytical/Window-Aggregate-Functions"/>
    /// </summary>
    public override bool SupportsWindowFunctionNullTreatmentArg => true;

    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Types-and-Literals/Data-Literals/Character-String-Literals"/>
    /// </summary>
    public override bool SupportsStringLiteralConcatenation => true;

    /// <summary>
    /// See <see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Definition-Language-Syntax-and-Examples/Table-Statements/CREATE-TABLE-and-CREATE-TABLE-AS"/>
    /// </summary>
    public override bool SupportsLeadingCommaBeforeTableOptions => true;
}
