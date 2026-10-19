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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// <c>ColumnOption</c>s are modifiers that follow a column definition in a <c>CREATE
/// TABLE</c> statement.
/// </summary>
public abstract partial record ColumnOption : IDisplay
{
    /// <summary><c>NULL</c></summary>
    public sealed record Null : ColumnOption;

    /// <summary><c>NOT NULL</c></summary>
    public sealed record NotNull : ColumnOption;

    /// <summary><c>DEFAULT &lt;restricted-expr&gt;</c></summary>
    public sealed record Default(Expr Value) : ColumnOption;

    /// <summary>
    /// <c>MATERIALIZED &lt;expr&gt;</c>
    /// Syntax: <c>b INT MATERIALIZED (a + 1)</c>
    /// <para><see href="https://clickhouse.com/docs/en/sql-reference/statements/create/table#default_values">ClickHouse</see></para>
    /// </summary>
    public sealed record Materialized(Expr Value) : ColumnOption;

    /// <summary>
    /// <c>EPHEMERAL [&lt;expr&gt;]</c>
    /// <para><see href="https://clickhouse.com/docs/en/sql-reference/statements/create/table#default_values">ClickHouse</see></para>
    /// </summary>
    public sealed record Ephemeral(Expr? Value) : ColumnOption;

    /// <summary>
    /// <c>ALIAS &lt;expr&gt;</c>
    /// <para><see href="https://clickhouse.com/docs/en/sql-reference/statements/create/table#default_values">ClickHouse</see></para>
    /// </summary>
    public sealed record Alias(Expr Value) : ColumnOption;

    /// <summary><c>PRIMARY KEY [&lt;constraint_characteristics&gt;]</c></summary>
    public sealed record PrimaryKey(PrimaryKeyConstraint Value) : ColumnOption;

    /// <summary><c>UNIQUE [&lt;constraint_characteristics&gt;]</c></summary>
    public sealed record Unique(UniqueConstraint Value) : ColumnOption;

    /// <summary>
    /// A referential integrity constraint (<c>REFERENCES &lt;foreign_table&gt; (&lt;referred_columns&gt;)
    /// [ MATCH { FULL | PARTIAL | SIMPLE } ]
    /// { [ON DELETE &lt;referential_action&gt;] [ON UPDATE &lt;referential_action&gt;] |
    ///   [ON UPDATE &lt;referential_action&gt;] [ON DELETE &lt;referential_action&gt;]
    /// }
    /// [&lt;constraint_characteristics&gt;]
    /// </c>).
    /// </summary>
    public sealed record ForeignKey(ForeignKeyConstraint Value) : ColumnOption;

    /// <summary><c>CHECK (&lt;expr&gt;) [NO INHERIT] [[NOT] ENFORCED]</c></summary>
    public sealed record Check(CheckConstraint Value) : ColumnOption;

    /// <summary>
    /// Dialect-specific options, such as:
    /// - MySQL's <c>AUTO_INCREMENT</c> or SQLite's <c>AUTOINCREMENT</c>
    /// - ...
    /// </summary>
    public sealed record DialectSpecific(Sequence<Token> Value) : ColumnOption;

    /// <summary><c>CHARACTER SET &lt;name&gt;</c> column option</summary>
    public sealed record CharacterSet(ObjectName Value) : ColumnOption;

    /// <summary><c>COLLATE &lt;name&gt;</c> column option</summary>
    public sealed record Collation(ObjectName Value) : ColumnOption;

    /// <summary><c>COMMENT '&lt;text&gt;'</c> column option</summary>
    public sealed record Comment(string Value) : ColumnOption;

    /// <summary><c>ON UPDATE &lt;expr&gt;</c> column option</summary>
    public sealed record OnUpdate(Expr Value) : ColumnOption;

    /// <summary>
    /// <c>Generated</c>s are modifiers that follow a column definition in a <c>CREATE
    /// TABLE</c> statement.
    /// </summary>
    /// <param name="GeneratedAs">How the column is generated (e.g. <c>GENERATED ALWAYS</c>, <c>BY DEFAULT</c>, or expression-stored).</param>
    /// <param name="SequenceOptions">Sequence/identity options when generation is backed by a sequence.</param>
    /// <param name="GenerationExpr">Optional expression used to generate the column value.</param>
    /// <param name="GenerationExprMode">Mode of the generated expression (<c>VIRTUAL</c> or <c>STORED</c>) when <c>generation_expr</c> is present.</param>
    /// <param name="GeneratedKeyword">false if 'GENERATED ALWAYS' is skipped (option starts with AS)</param>
    public sealed record Generated(
        GeneratedAs GeneratedAs,
        Sequence<SequenceOptions>? SequenceOptions,
        Expr? GenerationExpr,
        GeneratedExpressionMode? GenerationExprMode,
        bool GeneratedKeyword) : ColumnOption;

    /// <summary>
    /// BigQuery specific: Explicit column options in a view
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#view_column_option_list">[1]</see>
    /// or table <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#column_option_list">[2]</see>
    /// <para>Syntax</para>
    /// <code>
    /// OPTIONS(description="field desc")
    /// </code>
    /// </summary>
    public sealed record Options(Sequence<SqlOption> Value) : ColumnOption;

    /// <summary>
    /// Creates an identity or an autoincrement column in a table.
    /// <para>Syntax</para>
    /// <code>
    /// { IDENTITY | AUTOINCREMENT } [ (seed , increment) | START num INCREMENT num ] [ ORDER | NOORDER ]
    /// </code>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-table-transact-sql-identity-property">MS SQL Server</see>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
    /// </summary>
    public sealed record Identity(IdentityPropertyKind Value) : ColumnOption;

    /// <summary>
    /// SQLite specific: ON CONFLICT option on column definition
    /// <see href="https://www.sqlite.org/lang_conflict.html"/>
    /// </summary>
    public sealed record OnConflict(Keyword Value) : ColumnOption;

    /// <summary>
    /// Snowflake specific: an option of specifying security masking or projection policy to set on a column.
    /// <para>Syntax:</para>
    /// <code>
    /// [ WITH ] MASKING POLICY &lt;policy_name&gt; [ USING ( &lt;col_name&gt; , &lt;cond_col1&gt; , ... ) ]
    /// [ WITH ] PROJECTION POLICY &lt;policy_name&gt;
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
    /// </summary>
    public sealed record Policy(ColumnPolicy Value) : ColumnOption;

    /// <summary>
    /// Snowflake specific: Specifies the tag name and the tag string value.
    /// <para>Syntax:</para>
    /// <code>
    /// [ WITH ] TAG ( &lt;tag_name&gt; = '&lt;tag_value&gt;' [ , &lt;tag_name&gt; = '&lt;tag_value&gt;' , ... ] )
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
    /// </summary>
    public sealed record Tags(TagsColumnOption Value) : ColumnOption;

    /// <summary>
    /// MySQL specific: Spatial reference identifier
    /// <para>Syntax:</para>
    /// <code>
    /// CREATE TABLE geom (g GEOMETRY NOT NULL SRID 4326);
    /// </code>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/creating-spatial-indexes.html">MySQL</see>
    /// </summary>
    public sealed record Srid(Expr Value) : ColumnOption;

    /// <summary>
    /// MySQL specific: Column is invisible via SELECT *
    /// <para>Syntax:</para>
    /// <code>
    /// CREATE TABLE t (foo INT, bar INT INVISIBLE);
    /// </code>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/invisible-columns.html">MySQL</see>
    /// </summary>
    public sealed record Invisible : ColumnOption;

    // Rust: `impl From<UniqueConstraint> for ColumnOption`.
    public static ColumnOption From(UniqueConstraint c) => new Unique(c);

    // Rust: `impl From<PrimaryKeyConstraint> for ColumnOption`.
    public static ColumnOption From(PrimaryKeyConstraint c) => new PrimaryKey(c);

    // Rust: `impl From<CheckConstraint> for ColumnOption`.
    public static ColumnOption From(CheckConstraint c) => new Check(c);

    // Rust: `impl From<ForeignKeyConstraint> for ColumnOption`.
    public static ColumnOption From(ForeignKeyConstraint fk) => new ForeignKey(fk);

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Null:
                f.Write($"NULL");
                break;
            case NotNull:
                f.Write($"NOT NULL");
                break;
            case Default { Value: var expr }:
                f.Write($"DEFAULT {expr}");
                break;
            case Materialized { Value: var expr }:
                f.Write($"MATERIALIZED {expr}");
                break;
            case Ephemeral { Value: var expr }:
            {
                if (expr is { } e)
                {
                    f.Write($"EPHEMERAL {e}");
                }
                else
                {
                    f.Write($"EPHEMERAL");
                }
                break;
            }
            case Alias { Value: var expr }:
                f.Write($"ALIAS {expr}");
                break;
            case PrimaryKey { Value: var constraint }:
            {
                f.Write($"PRIMARY KEY");
                if (constraint.Characteristics is { } characteristics)
                {
                    f.Write($" {characteristics}");
                }
                break;
            }
            case Unique { Value: var constraint }:
            {
                // write!(f, "UNIQUE{:>}", constraint.index_type_display): a lacuna `{:>}` (alinhamento à
                // direita) não existe no Formatter; KeyOrIndexDisplay.FmtAlignRight a reproduz.
                f.Write($"UNIQUE");
                constraint.IndexTypeDisplay.FmtAlignRight(f);
                if (constraint.Characteristics is { } characteristics)
                {
                    f.Write($" {characteristics}");
                }
                break;
            }
            case ForeignKey { Value: var constraint }:
            {
                f.Write($"REFERENCES {constraint.ForeignTable}");
                if (!constraint.ReferredColumns.IsEmpty)
                {
                    f.Write($" ({DisplayCommaSeparated(constraint.ReferredColumns)})");
                }
                if (constraint.MatchKind is { } matchKind)
                {
                    f.Write($" {matchKind}");
                }
                // No Rust, as duas ligações se chamam `action`; em C# os nomes precisam ser distintos.
                if (constraint.OnDelete is { } onDeleteAction)
                {
                    f.Write($" ON DELETE {onDeleteAction}");
                }
                if (constraint.OnUpdate is { } onUpdateAction)
                {
                    f.Write($" ON UPDATE {onUpdateAction}");
                }
                if (constraint.Characteristics is { } characteristics)
                {
                    f.Write($" {characteristics}");
                }
                break;
            }
            case Check { Value: var constraint }:
                f.Write($"{constraint}");
                break;
            case DialectSpecific { Value: var val }:
                f.Write($"{DisplaySeparated(val, " ")}");
                break;
            case CharacterSet { Value: var n }:
                f.Write($"CHARACTER SET {n}");
                break;
            case Collation { Value: var n }:
                f.Write($"COLLATE {n}");
                break;
            case Comment { Value: var v }:
                f.Write($"COMMENT '{Ast.Value.EscapeSingleQuoteString(v)}'");
                break;
            case OnUpdate { Value: var expr }:
                f.Write($"ON UPDATE {expr}");
                break;
            case Generated generated:
            {
                if (generated.GenerationExpr is { } expr)
                {
                    var modifier = generated.GenerationExprMode switch
                    {
                        null => "",
                        GeneratedExpressionMode.Virtual => " VIRTUAL",
                        GeneratedExpressionMode.Stored => " STORED",
                        _ => throw new InvalidOperationException(
                            $"Variante de GeneratedExpressionMode não tratada: {generated.GenerationExprMode}"),
                    };
                    if (generated.GeneratedKeyword)
                    {
                        f.Write($"GENERATED ALWAYS AS ({expr}){modifier}");
                    }
                    else
                    {
                        f.Write($"AS ({expr}){modifier}");
                    }
                }
                else
                {
                    // Like Postgres - generated from sequence
                    var when = generated.GeneratedAs switch
                    {
                        GeneratedAs.Always => "ALWAYS",
                        GeneratedAs.ByDefault => "BY DEFAULT",
                        // ExpStored goes with an expression, handled above
                        GeneratedAs.ExpStored => "",
                        _ => throw new InvalidOperationException(
                            $"Variante de GeneratedAs não tratada: {generated.GeneratedAs}"),
                    };
                    f.Write($"GENERATED {when} AS IDENTITY");
                    if (generated.SequenceOptions is { } so)
                    {
                        if (!so.IsEmpty)
                        {
                            f.Write($" (");
                        }
                        foreach (var sequenceOption in so)
                        {
                            f.Write($"{sequenceOption}");
                        }
                        if (!so.IsEmpty)
                        {
                            f.Write($" )");
                        }
                    }
                }
                break;
            }
            case Options { Value: var options }:
                f.Write($"OPTIONS({DisplayCommaSeparated(options)})");
                break;
            case Identity { Value: var parameters }:
                f.Write($"{parameters}");
                break;
            case OnConflict { Value: var keyword }:
                // write!(f, "ON CONFLICT {keyword:?}"): o Debug de Keyword é o nome da variante, que é o
                // nome do membro do enum C# (ToString()), e não o texto SQL (ToSql()).
                f.Write($"ON CONFLICT {keyword.ToString()}");
                break;
            case Policy { Value: var parameters }:
                f.Write($"{parameters}");
                break;
            case Tags { Value: var tags }:
                f.Write($"{tags}");
                break;
            case Srid { Value: var srid }:
                f.Write($"SRID {srid}");
                break;
            case Invisible:
                f.Write($"INVISIBLE");
                break;
            default:
                throw new InvalidOperationException($"Variante de ColumnOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
