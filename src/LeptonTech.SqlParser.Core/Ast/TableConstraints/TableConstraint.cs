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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/table_constraints.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// A table-level constraint, specified in a <c>CREATE TABLE</c> or an
/// <c>ALTER TABLE ADD &lt;constraint&gt;</c> statement.
/// </summary>
public abstract partial record TableConstraint : IDisplay
{
    /// <summary>
    /// MySQL <see href="https://dev.mysql.com/doc/refman/8.3/en/create-table.html">definition</see> for <c>UNIQUE</c> constraints statements:
    /// <c>[CONSTRAINT [&lt;name&gt;]] UNIQUE &lt;index_type_display&gt; [&lt;index_name&gt;] [index_type] (&lt;columns&gt;) &lt;index_options&gt;</c>
    /// </summary>
    /// <remarks>
    /// where:
    /// <list type="bullet">
    /// <item>index_type (<c>IndexType</c>) is <c>USING {BTREE | HASH}</c></item>
    /// <item>index_options (<c>IndexOption</c>) is <c>{index_type | COMMENT 'string' | ... %currently unsupported stmts% } ...</c></item>
    /// <item>index_type_display (<c>KeyOrIndexDisplay</c>) is <c>[INDEX | KEY]</c></item>
    /// </list>
    /// </remarks>
    public sealed record Unique(UniqueConstraint Value) : TableConstraint;

    /// <summary>
    /// MySQL <see href="https://dev.mysql.com/doc/refman/8.3/en/create-table.html">definition</see> for <c>PRIMARY KEY</c> constraints statements:
    /// <c>[CONSTRAINT [&lt;name&gt;]] PRIMARY KEY [index_name] [index_type] (&lt;columns&gt;) &lt;index_options&gt;</c>
    /// </summary>
    /// <remarks>
    /// Actually the specification have no <c>[index_name]</c> but the next query will complete successfully:
    /// <code>
    /// CREATE TABLE unspec_table (
    ///   xid INT NOT NULL,
    ///   CONSTRAINT p_name PRIMARY KEY index_name USING BTREE (xid)
    /// );
    /// </code>
    /// where:
    /// <list type="bullet">
    /// <item>index_type (<c>IndexType</c>) is <c>USING {BTREE | HASH}</c></item>
    /// <item>index_options (<c>IndexOption</c>) is <c>{index_type | COMMENT 'string' | ... %currently unsupported stmts% } ...</c></item>
    /// </list>
    /// </remarks>
    public sealed record PrimaryKey(PrimaryKeyConstraint Value) : TableConstraint;

    /// <summary>
    /// A referential integrity constraint (<c>[ CONSTRAINT &lt;name&gt; ] FOREIGN KEY (&lt;columns&gt;)
    /// REFERENCES &lt;foreign_table&gt; (&lt;referred_columns&gt;)
    /// { [ON DELETE &lt;referential_action&gt;] [ON UPDATE &lt;referential_action&gt;] |
    ///   [ON UPDATE &lt;referential_action&gt;] [ON DELETE &lt;referential_action&gt;]
    /// }</c>).
    /// </summary>
    public sealed record ForeignKey(ForeignKeyConstraint Value) : TableConstraint;

    /// <summary><c>[ CONSTRAINT &lt;name&gt; ] CHECK (&lt;expr&gt;) [NO INHERIT] [[NOT] ENFORCED]</c></summary>
    public sealed record Check(CheckConstraint Value) : TableConstraint;

    /// <summary>
    /// MySQLs <see href="https://dev.mysql.com/doc/refman/8.0/en/create-table.html">index definition</see> for index creation.
    /// Not present on ANSI so, for now, the usage
    /// is restricted to MySQL, as no other dialects that support this syntax were found.
    /// </summary>
    /// <remarks><c>{INDEX | KEY} [index_name] [index_type] (key_part,...) [index_option]...</c></remarks>
    public sealed record Index(IndexConstraint Value) : TableConstraint;

    /// <summary>
    /// MySQLs <see href="https://dev.mysql.com/doc/refman/8.0/en/fulltext-natural-language.html">fulltext</see> definition. Since the
    /// <see href="https://dev.mysql.com/doc/refman/8.0/en/spatial-types.html"><c>SPATIAL</c></see> definition is exactly the same,
    /// and MySQL displays both the same way, it is part of this definition as well.
    /// </summary>
    /// <remarks>
    /// Supported syntax:
    /// <code>
    /// {FULLTEXT | SPATIAL} [INDEX | KEY] [index_name] (key_part,...)
    ///
    /// key_part: col_name
    /// </code>
    /// </remarks>
    public sealed record FulltextOrSpatial(FullTextOrSpatialConstraint Value) : TableConstraint;

    /// <summary>
    /// PostgreSQL <see href="https://www.postgresql.org/docs/current/sql-altertable.html">definition</see> for promoting an existing
    /// unique index to a <c>PRIMARY KEY</c> constraint:
    /// </summary>
    /// <remarks>
    /// <c>[ CONSTRAINT constraint_name ] PRIMARY KEY USING INDEX index_name
    ///   [ DEFERRABLE | NOT DEFERRABLE ] [ INITIALLY DEFERRED | INITIALLY IMMEDIATE ]</c>
    /// </remarks>
    public sealed record PrimaryKeyUsingIndex(ConstraintUsingIndex Value) : TableConstraint;

    /// <summary>
    /// PostgreSQL <see href="https://www.postgresql.org/docs/current/sql-altertable.html">definition</see> for promoting an existing
    /// unique index to a <c>UNIQUE</c> constraint:
    /// </summary>
    /// <remarks>
    /// <c>[ CONSTRAINT constraint_name ] UNIQUE USING INDEX index_name
    ///   [ DEFERRABLE | NOT DEFERRABLE ] [ INITIALLY DEFERRED | INITIALLY IMMEDIATE ]</c>
    /// </remarks>
    public sealed record UniqueUsingIndex(ConstraintUsingIndex Value) : TableConstraint;

    /// <summary><c>EXCLUDE</c> constraint.</summary>
    /// <remarks>
    /// <c>[ CONSTRAINT &lt;name&gt; ] EXCLUDE [ USING &lt;index_method&gt; ] ( &lt;element&gt; WITH &lt;operator&gt; [, ...] ) [ INCLUDE (&lt;cols&gt;) ] [ WHERE (&lt;predicate&gt;) ]</c>
    /// <para><see href="https://www.postgresql.org/docs/current/sql-createtable.html#SQL-CREATETABLE-EXCLUDE">PostgreSQL</see></para>
    /// </remarks>
    public sealed record Exclude(ExcludeConstraint Value) : TableConstraint;

    // Rust: `impl From<UniqueConstraint> for TableConstraint`.
    public static TableConstraint From(UniqueConstraint constraint) => new Unique(constraint);

    // Rust: `impl From<PrimaryKeyConstraint> for TableConstraint`.
    public static TableConstraint From(PrimaryKeyConstraint constraint) => new PrimaryKey(constraint);

    // Rust: `impl From<ForeignKeyConstraint> for TableConstraint`.
    public static TableConstraint From(ForeignKeyConstraint constraint) => new ForeignKey(constraint);

    // Rust: `impl From<CheckConstraint> for TableConstraint`.
    public static TableConstraint From(CheckConstraint constraint) => new Check(constraint);

    // Rust: `impl From<IndexConstraint> for TableConstraint`.
    public static TableConstraint From(IndexConstraint constraint) => new Index(constraint);

    // Rust: `impl From<FullTextOrSpatialConstraint> for TableConstraint`.
    public static TableConstraint From(FullTextOrSpatialConstraint constraint) => new FulltextOrSpatial(constraint);

    // Rust: `impl From<ExcludeConstraint> for TableConstraint`.
    public static TableConstraint From(ExcludeConstraint constraint) => new Exclude(constraint);

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Unique constraint:
                constraint.Value.Fmt(f);
                break;
            case PrimaryKey constraint:
                constraint.Value.Fmt(f);
                break;
            case ForeignKey constraint:
                constraint.Value.Fmt(f);
                break;
            case Check constraint:
                constraint.Value.Fmt(f);
                break;
            case Index constraint:
                constraint.Value.Fmt(f);
                break;
            case FulltextOrSpatial constraint:
                constraint.Value.Fmt(f);
                break;
            case PrimaryKeyUsingIndex c:
                c.Value.FmtWithKeyword(f, "PRIMARY KEY");
                break;
            case UniqueUsingIndex c:
                c.Value.FmtWithKeyword(f, "UNIQUE");
                break;
            case Exclude constraint:
                constraint.Value.Fmt(f);
                break;
            default:
                throw new InvalidOperationException($"Variante de TableConstraint não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
