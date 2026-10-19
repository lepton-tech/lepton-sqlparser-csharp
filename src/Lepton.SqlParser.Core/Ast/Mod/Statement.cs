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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>A top-level statement (SELECT, INSERT, CREATE, etc.)</summary>
// `partial`: as variantes e os `impl From` de `mod.rs` 5026–5048 ficam aqui; o
// `impl fmt::Display for Statement` fica em `Statement.Display.cs`. Outros trechos de
// `mod.rs` (os demais `impl From<X> for Statement`) acrescentam membros em arquivos próprios.
// Variantes-tupla cujo tipo tem o nome da variante (`Insert(Insert)`) qualificam o tipo como
// `Ast.X` (convencoes.md §13, caso 38). Campos de variantes-struct com o nome de outra variante
// (`AlterView.Query`, `Explain.Analyze`...) declaram a propriedade explicitamente com `new`:
// sem isso, o membro herdado (o tipo aninhado `Statement.Query`) impede a propriedade sintetizada (CS8866).
[VisitWith("visit_statement")]
public abstract partial record Statement
{
    /// <summary>
    /// <code>
    /// ANALYZE
    /// </code>
    /// Analyze (Hive)
    /// </summary>
    public sealed record Analyze(Ast.Analyze Value) : Statement;

    /// <summary><c>SET</c> statements (session, transaction, timezone, etc.).</summary>
    public sealed record Set(Ast.Set Value) : Statement;

    /// <summary>
    /// <code>
    /// TRUNCATE
    /// </code>
    /// Truncate (Hive)
    /// </summary>
    public sealed record Truncate(Ast.Truncate Value) : Statement;

    /// <summary>
    /// <code>
    /// MSCK
    /// </code>
    /// Msck (Hive)
    /// </summary>
    public sealed record Msck(Ast.Msck Value) : Statement;

    /// <summary>
    /// <code>
    /// SELECT
    /// </code>
    /// </summary>
    public sealed record Query(Ast.Query Value) : Statement;

    /// <summary>
    /// <code>
    /// INSERT
    /// </code>
    /// </summary>
    public sealed record Insert(Ast.Insert Value) : Statement;

    /// <summary>
    /// <code>
    /// INSTALL
    /// </code>
    /// </summary>
    /// <param name="ExtensionName">Only for DuckDB</param>
    public sealed record Install(Ident ExtensionName) : Statement;

    /// <summary>
    /// <code>
    /// LOAD
    /// </code>
    /// </summary>
    /// <param name="ExtensionName">Only for DuckDB</param>
    public sealed record Load(Ident ExtensionName) : Statement;

    // TODO: Support ROW FORMAT
    /// <summary>LOAD DATA from a directory or query source.</summary>
    /// <param name="Overwrite">Whether to overwrite existing files.</param>
    /// <param name="Local">Whether the directory is local to the server.</param>
    /// <param name="Path">Path to the directory or files.</param>
    /// <param name="FileFormat">Optional file format for the data.</param>
    /// <param name="Source">Source query providing data to load.</param>
    public sealed record Directory(
        bool Overwrite,
        bool Local,
        string Path,
        FileFormat? FileFormat,
        Ast.Query Source) : Statement;

    /// <summary>A <c>CASE</c> statement.</summary>
    public sealed record Case(CaseStatement Value) : Statement;

    /// <summary>An <c>IF</c> statement.</summary>
    public sealed record If(IfStatement Value) : Statement;

    /// <summary>A <c>WHILE</c> statement.</summary>
    public sealed record While(WhileStatement Value) : Statement;

    /// <summary>A <c>RAISE</c> statement.</summary>
    public sealed record Raise(RaiseStatement Value) : Statement;

    /// <summary>
    /// <code>
    /// CALL &lt;function&gt;
    /// </code>
    /// </summary>
    public sealed record Call(Function Value) : Statement;

    /// <summary>
    /// <code>
    /// COPY [TO | FROM] ...
    /// </code>
    /// </summary>
    /// <param name="Source">The source of 'COPY TO', or the target of 'COPY FROM'</param>
    /// <param name="To">If true, is a 'COPY TO' statement. If false is a 'COPY FROM'</param>
    /// <param name="Target">The target of 'COPY TO', or the source of 'COPY FROM'</param>
    /// <param name="Options">WITH options (from PostgreSQL version 9.0)</param>
    /// <param name="LegacyOptions">WITH options (before PostgreSQL version 9.0)</param>
    /// <param name="Values">VALUES a vector of values to be copied</param>
    public sealed record Copy(
        CopySource Source,
        bool To,
        CopyTarget Target,
        Sequence<CopyOption> Options,
        Sequence<CopyLegacyOption> LegacyOptions,
        Sequence<string?> Values) : Statement;

    /// <summary>
    /// <code>
    /// COPY INTO &lt;table&gt; | &lt;location&gt;
    /// </code>
    /// See:
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/copy-into-table"/>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/copy-into-location"/>
    /// <para>
    /// Copy Into syntax available for Snowflake is different than the one implemented in
    /// Postgres. Although they share common prefix, it is reasonable to implement them
    /// in different enums. This can be refactored later once custom dialects
    /// are allowed to have custom Statements.
    /// </para>
    /// </summary>
    /// <param name="Kind">Kind of COPY INTO operation (table or location).</param>
    /// <param name="Into">Target object for the COPY INTO operation.</param>
    /// <param name="IntoColumns">Optional list of target columns.</param>
    /// <param name="FromObj">Optional source object name (staged data).</param>
    /// <param name="FromObjAlias">Optional alias for the source object.</param>
    /// <param name="StageParams">Stage-specific parameters (e.g., credentials, path).</param>
    /// <param name="FromTransformations">Optional list of transformations applied when loading.</param>
    /// <param name="FromQuery">Optional source query instead of a staged object.</param>
    /// <param name="Files">Optional list of specific file names to load.</param>
    /// <param name="Pattern">Optional filename matching pattern.</param>
    /// <param name="FileFormat">File format options.</param>
    /// <param name="CopyOptions">Additional copy options.</param>
    /// <param name="ValidationMode">Optional validation mode string.</param>
    /// <param name="Partition">Optional partition expression for loading.</param>
    public sealed record CopyIntoSnowflake(
        CopyIntoSnowflakeKind Kind,
        ObjectName Into,
        Sequence<Ident>? IntoColumns,
        ObjectName? FromObj,
        Ident? FromObjAlias,
        StageParamsObject StageParams,
        Sequence<StageLoadSelectItemKind>? FromTransformations,
        Ast.Query? FromQuery,
        Sequence<string>? Files,
        string? Pattern,
        KeyValueOptions FileFormat,
        KeyValueOptions CopyOptions,
        string? ValidationMode,
        Expr? Partition) : Statement;

    /// <summary>
    /// <code>
    /// OPEN cursor_name
    /// </code>
    /// Opens a cursor.
    /// </summary>
    public sealed record Open(OpenStatement Value) : Statement;

    /// <summary>
    /// <code>
    /// CLOSE
    /// </code>
    /// Closes the portal underlying an open cursor.
    /// </summary>
    /// <param name="Cursor">Cursor name</param>
    public sealed record Close(CloseCursor Cursor) : Statement;

    /// <summary>
    /// <code>
    /// UPDATE
    /// </code>
    /// </summary>
    public sealed record Update(Ast.Update Value) : Statement;

    /// <summary>
    /// <code>
    /// DELETE
    /// </code>
    /// </summary>
    public sealed record Delete(Ast.Delete Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE VIEW
    /// </code>
    /// </summary>
    public sealed record CreateView(Ast.CreateView Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE TABLE
    /// </code>
    /// </summary>
    public sealed record CreateTable(Ast.CreateTable Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE VIRTUAL TABLE .. USING &lt;module_name&gt; (&lt;module_args&gt;)`
    /// </code>
    /// Sqlite specific statement
    /// </summary>
    /// <param name="Name">Name of the virtual table module instance.</param>
    /// <param name="IfNotExists"><c>true</c> when <c>IF NOT EXISTS</c> was specified.</param>
    /// <param name="ModuleName">Module name used by the virtual table.</param>
    /// <param name="ModuleArgs">Arguments passed to the module.</param>
    public sealed record CreateVirtualTable(
        [property: VisitWith("visit_relation")] ObjectName Name,
        bool IfNotExists,
        Ident ModuleName,
        Sequence<Ident> ModuleArgs) : Statement;

    /// <summary>
    /// <code>
    /// `CREATE INDEX`
    /// </code>
    /// </summary>
    public sealed record CreateIndex(Ast.CreateIndex Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE ROLE
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-createrole.html">PostgreSQL</see>
    /// </summary>
    public sealed record CreateRole(Ast.CreateRole Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE SECRET
    /// </code>
    /// See <see href="https://duckdb.org/docs/sql/statements/create_secret.html">DuckDB</see>
    /// </summary>
    /// <param name="OrReplace"><c>true</c> when <c>OR REPLACE</c> was specified.</param>
    /// <param name="Temporary">Optional <c>TEMPORARY</c> flag.</param>
    /// <param name="IfNotExists"><c>true</c> when <c>IF NOT EXISTS</c> was present.</param>
    /// <param name="Name">Optional secret name.</param>
    /// <param name="StorageSpecifier">Optional storage specifier identifier.</param>
    /// <param name="SecretType">The secret type identifier.</param>
    /// <param name="Options">Additional secret options.</param>
    public sealed record CreateSecret(
        bool OrReplace,
        bool? Temporary,
        bool IfNotExists,
        Ident? Name,
        Ident? StorageSpecifier,
        Ident SecretType,
        Sequence<SecretOption> Options) : Statement;

    /// <summary>A <c>CREATE SERVER</c> statement.</summary>
    public sealed record CreateServer(CreateServerStatement Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE FOREIGN TABLE
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-createforeigntable.html">PostgreSQL</see>
    /// </summary>
    public sealed record CreateForeignTable(Ast.CreateForeignTable Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE POLICY
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-createpolicy.html">PostgreSQL</see>
    /// </summary>
    public sealed record CreatePolicy(Ast.CreatePolicy Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE CONNECTOR
    /// </code>
    /// See <see href="https://cwiki.apache.org/confluence/pages/viewpage.action?pageId=27362034#LanguageManualDDL-CreateDataConnectorCreateConnector">Hive</see>
    /// </summary>
    public sealed record CreateConnector(Ast.CreateConnector Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE OPERATOR
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-createoperator.html">PostgreSQL</see>
    /// </summary>
    public sealed record CreateOperator(Ast.CreateOperator Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE OPERATOR FAMILY
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-createopfamily.html">PostgreSQL</see>
    /// </summary>
    public sealed record CreateOperatorFamily(Ast.CreateOperatorFamily Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE OPERATOR CLASS
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-createopclass.html">PostgreSQL</see>
    /// </summary>
    public sealed record CreateOperatorClass(Ast.CreateOperatorClass Value) : Statement;

    /// <summary>
    /// A <c>CREATE TEXT SEARCH</c> statement.
    /// <para>See <see href="https://www.postgresql.org/docs/current/textsearch-intro.html">PostgreSQL</see></para>
    /// </summary>
    public sealed record CreateTextSearch(Ast.CreateTextSearch Value) : Statement;

    /// <summary>
    /// <code>
    /// ALTER TABLE
    /// </code>
    /// </summary>
    public sealed record AlterTable(Ast.AlterTable Value) : Statement;

    /// <summary>
    /// <code>
    /// ALTER SCHEMA
    /// </code>
    /// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#alter_schema_collate_statement">BigQuery</see>
    /// </summary>
    public sealed record AlterSchema(Ast.AlterSchema Value) : Statement;

    /// <summary>
    /// <code>
    /// ALTER INDEX
    /// </code>
    /// </summary>
    /// <param name="Name">Name of the index to alter.</param>
    /// <param name="Operation">The operation to perform on the index.</param>
    public sealed record AlterIndex(ObjectName Name, AlterIndexOperation Operation) : Statement;

    /// <summary>
    /// <code>
    /// ALTER VIEW
    /// </code>
    /// </summary>
    /// <param name="Name">View name being altered.</param>
    /// <param name="Columns">Optional new column list for the view.</param>
    /// <param name="Query">Replacement query for the view definition.</param>
    /// <param name="WithOptions">Additional WITH options for the view.</param>
    public sealed record AlterView(
        [property: VisitWith("visit_relation")] ObjectName Name,
        Sequence<Ident> Columns,
        Ast.Query Query,
        Sequence<SqlOption> WithOptions) : Statement
    {
        // Esconde a variante herdada Statement.Query (CS8866 sem a declaração explícita).
        public new Ast.Query Query { get; init; } = Query;
    }

    /// <summary>
    /// <code>
    /// ALTER FUNCTION
    /// ALTER AGGREGATE
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-alterfunction.html">PostgreSQL</see>
    /// and <see href="https://www.postgresql.org/docs/current/sql-alteraggregate.html">PostgreSQL</see>
    /// </summary>
    public sealed record AlterFunction(Ast.AlterFunction Value) : Statement;

    /// <summary>
    /// <code>
    /// ALTER TYPE
    /// See [PostgreSQL](https://www.postgresql.org/docs/current/sql-altertype.html)
    /// </code>
    /// </summary>
    public sealed record AlterType(Ast.AlterType Value) : Statement;

    /// <summary>
    /// <code>
    /// ALTER COLLATION
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-altercollation.html">PostgreSQL</see>
    /// </summary>
    public sealed record AlterCollation(Ast.AlterCollation Value) : Statement;

    /// <summary>
    /// <code>
    /// ALTER OPERATOR
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-alteroperator.html">PostgreSQL</see>
    /// </summary>
    public sealed record AlterOperator(Ast.AlterOperator Value) : Statement;

    /// <summary>
    /// <code>
    /// ALTER OPERATOR FAMILY
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-alteropfamily.html">PostgreSQL</see>
    /// </summary>
    public sealed record AlterOperatorFamily(Ast.AlterOperatorFamily Value) : Statement;

    /// <summary>
    /// <code>
    /// ALTER OPERATOR CLASS
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-alteropclass.html">PostgreSQL</see>
    /// </summary>
    public sealed record AlterOperatorClass(Ast.AlterOperatorClass Value) : Statement;

    /// <summary>
    /// An <c>ALTER TEXT SEARCH</c> statement.
    /// <para>See <see href="https://www.postgresql.org/docs/current/textsearch-configuration.html">PostgreSQL</see></para>
    /// </summary>
    public sealed record AlterTextSearch(Ast.AlterTextSearch Value) : Statement;

    /// <summary>
    /// <code>
    /// ALTER ROLE
    /// </code>
    /// </summary>
    /// <param name="Name">Role name being altered.</param>
    /// <param name="Operation">Operation to perform on the role.</param>
    public sealed record AlterRole(Ident Name, AlterRoleOperation Operation) : Statement;

    /// <summary>
    /// <code>
    /// ALTER POLICY &lt;NAME&gt; ON &lt;TABLE NAME&gt; [&lt;OPERATION&gt;]
    /// </code>
    /// (Postgresql-specific)
    /// </summary>
    public sealed record AlterPolicy(Ast.AlterPolicy Value) : Statement;

    /// <summary>
    /// <code>
    /// ALTER CONNECTOR connector_name SET DCPROPERTIES(property_name=property_value, ...);
    /// or
    /// ALTER CONNECTOR connector_name SET URL new_url;
    /// or
    /// ALTER CONNECTOR connector_name SET OWNER [USER|ROLE] user_or_role;
    /// </code>
    /// (Hive-specific)
    /// </summary>
    /// <param name="Name">Name of the connector to alter.</param>
    /// <param name="Properties">Optional connector properties to set.</param>
    /// <param name="Url">Optional new URL for the connector.</param>
    /// <param name="Owner">Optional new owner specification.</param>
    public sealed record AlterConnector(
        Ident Name,
        Sequence<SqlOption>? Properties,
        string? Url,
        AlterConnectorOwner? Owner) : Statement;

    /// <summary>
    /// <code>
    /// ALTER SESSION SET sessionParam
    /// ALTER SESSION UNSET &lt;param_name&gt; [ , &lt;param_name&gt; , ... ]
    /// </code>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-session"/>
    /// </summary>
    /// <param name="Set">true is to set for the session parameters, false is to unset</param>
    /// <param name="SessionParams">The session parameters to set or unset</param>
    public sealed record AlterSession(bool Set, KeyValueOptions SessionParams) : Statement
    {
        // Esconde a variante herdada Statement.Set (CS8866 sem a declaração explícita).
        public new bool Set { get; init; } = Set;
    }

    /// <summary>
    /// <code>
    /// ATTACH DATABASE 'path/to/file' AS alias
    /// </code>
    /// (SQLite-specific)
    /// </summary>
    /// <param name="SchemaName">The name to bind to the newly attached database</param>
    /// <param name="DatabaseFileName">An expression that indicates the path to the database file</param>
    /// <param name="Database">true if the syntax is 'ATTACH DATABASE', false if it's just 'ATTACH'</param>
    public sealed record AttachDatabase(Ident SchemaName, Expr DatabaseFileName, bool Database) : Statement;

    /// <summary>
    /// (DuckDB-specific)
    /// <code>
    /// ATTACH 'sqlite_file.db' AS sqlite_db (READ_ONLY, TYPE SQLITE);
    /// </code>
    /// See <see href="https://duckdb.org/docs/sql/statements/attach.html"/>
    /// </summary>
    /// <param name="IfNotExists"><c>true</c> when <c>IF NOT EXISTS</c> was present.</param>
    /// <param name="Database"><c>true</c> if the syntax used <c>ATTACH DATABASE</c> rather than <c>ATTACH</c>.</param>
    /// <param name="DatabasePath">The path identifier to the database file being attached.</param>
    /// <param name="DatabaseAlias">Optional alias assigned to the attached database.</param>
    /// <param name="AttachOptions">Dialect-specific attach options (e.g., <c>READ_ONLY</c>).</param>
    public sealed record AttachDuckDBDatabase(
        bool IfNotExists,
        bool Database,
        Ident DatabasePath,
        Ident? DatabaseAlias,
        Sequence<AttachDuckDBDatabaseOption> AttachOptions) : Statement;

    /// <summary>
    /// (DuckDB-specific)
    /// <code>
    /// DETACH db_alias;
    /// </code>
    /// See <see href="https://duckdb.org/docs/sql/statements/attach.html"/>
    /// </summary>
    /// <param name="IfExists"><c>true</c> when <c>IF EXISTS</c> was present.</param>
    /// <param name="Database"><c>true</c> if the syntax used <c>DETACH DATABASE</c> rather than <c>DETACH</c>.</param>
    /// <param name="DatabaseAlias">Alias of the database to detach.</param>
    public sealed record DetachDuckDBDatabase(bool IfExists, bool Database, Ident DatabaseAlias) : Statement;

    /// <summary>
    /// <code>
    /// DROP [TABLE, VIEW, ...]
    /// </code>
    /// </summary>
    /// <param name="ObjectType">The type of the object to drop: TABLE, VIEW, etc.</param>
    /// <param name="IfExists">An optional <c>IF EXISTS</c> clause. (Non-standard.)</param>
    /// <param name="Names">One or more objects to drop. (ANSI SQL requires exactly one.)</param>
    /// <param name="Cascade">
    /// Whether <c>CASCADE</c> was specified. This will be <c>false</c> when
    /// <c>RESTRICT</c> or no drop behavior at all was specified.
    /// </param>
    /// <param name="Restrict">
    /// Whether <c>RESTRICT</c> was specified. This will be <c>false</c> when
    /// <c>CASCADE</c> or no drop behavior at all was specified.
    /// </param>
    /// <param name="Purge">
    /// Hive allows you specify whether the table's stored data will be
    /// deleted along with the dropped table
    /// </param>
    /// <param name="Temporary">MySQL-specific "TEMPORARY" keyword</param>
    /// <param name="Table">
    /// MySQL-specific drop index syntax, which requires table specification
    /// See <see href="https://dev.mysql.com/doc/refman/8.4/en/drop-index.html"/>
    /// </param>
    public sealed record Drop(
        ObjectType ObjectType,
        bool IfExists,
        Sequence<ObjectName> Names,
        bool Cascade,
        bool Restrict,
        bool Purge,
        bool Temporary,
        ObjectName? Table) : Statement;

    /// <summary>
    /// <code>
    /// DROP FUNCTION
    /// </code>
    /// </summary>
    public sealed record DropFunction(Ast.DropFunction Value) : Statement;

    /// <summary>
    /// <code>
    /// DROP DOMAIN
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-dropdomain.html">PostgreSQL</see>
    /// <para>DROP DOMAIN [ IF EXISTS ] name [, ...] [ CASCADE | RESTRICT ]</para>
    /// </summary>
    public sealed record DropDomain(Ast.DropDomain Value) : Statement;

    /// <summary>
    /// <code>
    /// DROP PROCEDURE
    /// </code>
    /// </summary>
    /// <param name="IfExists"><c>true</c> when <c>IF EXISTS</c> was present.</param>
    /// <param name="ProcDesc">One or more functions/procedures to drop.</param>
    /// <param name="DropBehavior">Optional drop behavior (<c>CASCADE</c> or <c>RESTRICT</c>).</param>
    public sealed record DropProcedure(
        bool IfExists,
        Sequence<FunctionDesc> ProcDesc,
        DropBehavior? DropBehavior) : Statement;

    /// <summary>
    /// <code>
    /// DROP SECRET
    /// </code>
    /// </summary>
    /// <param name="IfExists"><c>true</c> when <c>IF EXISTS</c> was present.</param>
    /// <param name="Temporary">Optional <c>TEMPORARY</c> marker.</param>
    /// <param name="Name">Name of the secret to drop.</param>
    /// <param name="StorageSpecifier">Optional storage specifier identifier.</param>
    public sealed record DropSecret(
        bool IfExists,
        bool? Temporary,
        Ident Name,
        Ident? StorageSpecifier) : Statement;

    /// <summary>
    /// <code>
    /// DROP POLICY
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-droppolicy.html">PostgreSQL</see>
    /// </summary>
    public sealed record DropPolicy(Ast.DropPolicy Value) : Statement;

    /// <summary>
    /// <code>
    /// DROP CONNECTOR
    /// </code>
    /// See <see href="https://cwiki.apache.org/confluence/pages/viewpage.action?pageId=27362034#LanguageManualDDL-DropConnector">Hive</see>
    /// </summary>
    /// <param name="IfExists"><c>true</c> when <c>IF EXISTS</c> was present.</param>
    /// <param name="Name">Name of the connector to drop.</param>
    public sealed record DropConnector(bool IfExists, Ident Name) : Statement;

    /// <summary>
    /// <code>
    /// DECLARE
    /// </code>
    /// Declare Cursor Variables
    /// <para>
    /// Note: this is a PostgreSQL-specific statement,
    /// but may also compatible with other SQL.
    /// </para>
    /// </summary>
    /// <param name="Stmts">Cursor declaration statements collected by <c>DECLARE</c>.</param>
    public sealed record Declare(Sequence<Ast.Declare> Stmts) : Statement;

    /// <summary>
    /// <code>
    /// CREATE EXTENSION [ IF NOT EXISTS ] extension_name
    ///     [ WITH ] [ SCHEMA schema_name ]
    ///              [ VERSION version ]
    ///              [ CASCADE ]
    /// </code>
    /// <para>Note: this is a PostgreSQL-specific statement,</para>
    /// </summary>
    public sealed record CreateExtension(Ast.CreateExtension Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE COLLATION
    /// </code>
    /// Note: this is a PostgreSQL-specific statement.
    /// <see href="https://www.postgresql.org/docs/current/sql-createcollation.html"/>
    /// </summary>
    public sealed record CreateCollation(Ast.CreateCollation Value) : Statement;

    /// <summary>
    /// <code>
    /// DROP EXTENSION [ IF EXISTS ] name [, ...] [ CASCADE | RESTRICT ]
    /// </code>
    /// Note: this is a PostgreSQL-specific statement.
    /// <see href="https://www.postgresql.org/docs/current/sql-dropextension.html"/>
    /// </summary>
    public sealed record DropExtension(Ast.DropExtension Value) : Statement;

    /// <summary>
    /// <code>
    /// DROP OPERATOR [ IF EXISTS ] name ( { left_type | NONE } , right_type ) [, ...] [ CASCADE | RESTRICT ]
    /// </code>
    /// Note: this is a PostgreSQL-specific statement.
    /// <see href="https://www.postgresql.org/docs/current/sql-dropoperator.html"/>
    /// </summary>
    public sealed record DropOperator(Ast.DropOperator Value) : Statement;

    /// <summary>
    /// <code>
    /// DROP OPERATOR FAMILY [ IF EXISTS ] name USING index_method [ CASCADE | RESTRICT ]
    /// </code>
    /// Note: this is a PostgreSQL-specific statement.
    /// <see href="https://www.postgresql.org/docs/current/sql-dropopfamily.html"/>
    /// </summary>
    public sealed record DropOperatorFamily(Ast.DropOperatorFamily Value) : Statement;

    /// <summary>
    /// <code>
    /// DROP OPERATOR CLASS [ IF EXISTS ] name USING index_method [ CASCADE | RESTRICT ]
    /// </code>
    /// Note: this is a PostgreSQL-specific statement.
    /// <see href="https://www.postgresql.org/docs/current/sql-dropopclass.html"/>
    /// </summary>
    public sealed record DropOperatorClass(Ast.DropOperatorClass Value) : Statement;

    /// <summary>
    /// <code>
    /// FETCH
    /// </code>
    /// Retrieve rows from a query using a cursor
    /// <para>
    /// Note: this is a PostgreSQL-specific statement,
    /// but may also compatible with other SQL.
    /// </para>
    /// </summary>
    /// <param name="Name">Cursor name</param>
    /// <param name="Direction">The fetch direction (e.g., <c>FORWARD</c>, <c>BACKWARD</c>).</param>
    /// <param name="Position">The fetch position (e.g., <c>ALL</c>, <c>NEXT</c>, <c>ABSOLUTE</c>).</param>
    /// <param name="Into">Optional target table to fetch rows into.</param>
    public sealed record Fetch(
        Ident Name,
        FetchDirection Direction,
        FetchPosition Position,
        ObjectName? Into) : Statement;

    /// <summary>
    /// <code>
    /// FLUSH [NO_WRITE_TO_BINLOG | LOCAL] flush_option [, flush_option] ... | tables_option
    /// </code>
    /// <para>
    /// Note: this is a Mysql-specific statement,
    /// but may also compatible with other SQL.
    /// </para>
    /// </summary>
    /// <param name="ObjectType">The specific flush option or object to flush.</param>
    /// <param name="Location">Optional flush location (dialect-specific).</param>
    /// <param name="Channel">Optional channel name used for flush operations.</param>
    /// <param name="ReadLock">Whether a read lock was requested.</param>
    /// <param name="Export">Whether this is an export flush operation.</param>
    /// <param name="Tables">Optional list of tables involved in the flush.</param>
    public sealed record Flush(
        FlushType ObjectType,
        FlushLocation? Location,
        string? Channel,
        bool ReadLock,
        bool Export,
        Sequence<ObjectName> Tables) : Statement;

    /// <summary>
    /// <code>
    /// DISCARD [ ALL | PLANS | SEQUENCES | TEMPORARY | TEMP ]
    /// </code>
    /// <para>
    /// Note: this is a PostgreSQL-specific statement,
    /// but may also compatible with other SQL.
    /// </para>
    /// </summary>
    /// <param name="ObjectType">The kind of object(s) to discard (ALL, PLANS, etc.).</param>
    public sealed record Discard(DiscardObject ObjectType) : Statement;

    /// <summary>
    /// <c>SHOW FUNCTIONS</c>
    /// <para>Note: this is a Presto-specific statement.</para>
    /// </summary>
    /// <param name="Filter">Optional filter for which functions to display.</param>
    public sealed record ShowFunctions(ShowStatementFilter? Filter) : Statement;

    /// <summary>
    /// <code>
    /// SHOW &lt;variable&gt;
    /// </code>
    /// <para>Note: this is a PostgreSQL-specific statement.</para>
    /// </summary>
    /// <param name="Variable">Variable name as one or more identifiers.</param>
    public sealed record ShowVariable(Sequence<Ident> Variable) : Statement;

    /// <summary>
    /// <code>
    /// SHOW [GLOBAL | SESSION] STATUS [LIKE 'pattern' | WHERE expr]
    /// </code>
    /// <para>Note: this is a MySQL-specific statement.</para>
    /// </summary>
    /// <param name="Filter">Optional filter for which status entries to display.</param>
    /// <param name="Global"><c>true</c> when <c>GLOBAL</c> scope was requested.</param>
    /// <param name="Session"><c>true</c> when <c>SESSION</c> scope was requested.</param>
    public sealed record ShowStatus(ShowStatementFilter? Filter, bool Global, bool Session) : Statement;

    /// <summary>
    /// <code>
    /// SHOW VARIABLES
    /// </code>
    /// <para>Note: this is a MySQL-specific statement.</para>
    /// </summary>
    /// <param name="Filter">Optional filter for which variables to display.</param>
    /// <param name="Global"><c>true</c> when <c>GLOBAL</c> scope was requested.</param>
    /// <param name="Session"><c>true</c> when <c>SESSION</c> scope was requested.</param>
    public sealed record ShowVariables(ShowStatementFilter? Filter, bool Global, bool Session) : Statement;

    /// <summary>
    /// <code>
    /// SHOW CREATE TABLE
    /// </code>
    /// <para>Note: this is a MySQL-specific statement.</para>
    /// </summary>
    /// <param name="ObjType">The kind of object being shown (TABLE, VIEW, etc.).</param>
    /// <param name="ObjName">The name of the object to show create statement for.</param>
    public sealed record ShowCreate(ShowCreateObject ObjType, ObjectName ObjName) : Statement;

    /// <summary>
    /// <code>
    /// SHOW COLUMNS
    /// </code>
    /// </summary>
    /// <param name="Extended"><c>true</c> when extended column information was requested.</param>
    /// <param name="Full"><c>true</c> when full column details were requested.</param>
    /// <param name="ShowOptions">Additional options for <c>SHOW COLUMNS</c>.</param>
    public sealed record ShowColumns(bool Extended, bool Full, ShowStatementOptions ShowOptions) : Statement;

    /// <summary>
    /// <code>
    /// SHOW CATALOGS
    /// </code>
    /// </summary>
    /// <param name="Terse"><c>true</c> when terse output format was requested.</param>
    /// <param name="History"><c>true</c> when history information was requested.</param>
    /// <param name="ShowOptions">Additional options for <c>SHOW CATALOGS</c>.</param>
    public sealed record ShowCatalogs(bool Terse, bool History, ShowStatementOptions ShowOptions) : Statement;

    /// <summary>
    /// <code>
    /// SHOW DATABASES
    /// </code>
    /// </summary>
    /// <param name="Terse"><c>true</c> when terse output format was requested.</param>
    /// <param name="History"><c>true</c> when history information was requested.</param>
    /// <param name="ShowOptions">Additional options for <c>SHOW DATABASES</c>.</param>
    public sealed record ShowDatabases(bool Terse, bool History, ShowStatementOptions ShowOptions) : Statement;

    /// <summary>
    /// <code>
    /// SHOW [FULL] PROCESSLIST
    /// </code>
    /// <para>Note: this is a MySQL-specific statement.</para>
    /// </summary>
    /// <param name="Full"><c>true</c> when full process information was requested.</param>
    public sealed record ShowProcessList(bool Full) : Statement;

    /// <summary>
    /// <code>
    /// SHOW SCHEMAS
    /// </code>
    /// </summary>
    /// <param name="Terse"><c>true</c> when terse (compact) output was requested.</param>
    /// <param name="History"><c>true</c> when history information was requested.</param>
    /// <param name="ShowOptions">Additional options for <c>SHOW SCHEMAS</c>.</param>
    public sealed record ShowSchemas(bool Terse, bool History, ShowStatementOptions ShowOptions) : Statement;

    // ```sql
    // SHOW {CHARACTER SET | CHARSET}
    // ```
    // [MySQL]:
    // <https://dev.mysql.com/doc/refman/8.4/en/show.html#:~:text=SHOW%20%7BCHARACTER%20SET%20%7C%20CHARSET%7D%20%5Blike_or_where%5D>
    /// <summary>Show the available character sets (alias <c>CHARSET</c>).</summary>
    public sealed record ShowCharset(Ast.ShowCharset Value) : Statement;

    /// <summary>
    /// <code>
    /// SHOW OBJECTS LIKE 'line%' IN mydb.public
    /// </code>
    /// Snowflake-specific statement
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/show-objects"/>
    /// </summary>
    public sealed record ShowObjects(Ast.ShowObjects Value) : Statement;

    /// <summary>
    /// <code>
    /// SHOW TABLES
    /// </code>
    /// </summary>
    /// <param name="Terse"><c>true</c> when terse output format was requested (compact listing).</param>
    /// <param name="History"><c>true</c> when history rows are requested.</param>
    /// <param name="Extended"><c>true</c> when extended information should be shown.</param>
    /// <param name="Full"><c>true</c> when a full listing was requested.</param>
    /// <param name="External"><c>true</c> when external tables should be included.</param>
    /// <param name="ShowOptions">Additional options for <c>SHOW</c> statements.</param>
    public sealed record ShowTables(
        bool Terse,
        bool History,
        bool Extended,
        bool Full,
        bool External,
        ShowStatementOptions ShowOptions) : Statement;

    /// <summary>
    /// <code>
    /// SHOW VIEWS
    /// </code>
    /// </summary>
    /// <param name="Terse"><c>true</c> when terse output format was requested.</param>
    /// <param name="Materialized"><c>true</c> when materialized views should be included.</param>
    /// <param name="ShowOptions">Additional options for <c>SHOW</c> statements.</param>
    public sealed record ShowViews(bool Terse, bool Materialized, ShowStatementOptions ShowOptions) : Statement;

    /// <summary>
    /// <code>
    /// SHOW COLLATION
    /// </code>
    /// <para>Note: this is a MySQL-specific statement.</para>
    /// </summary>
    /// <param name="Filter">Optional filter for which collations to display.</param>
    public sealed record ShowCollation(ShowStatementFilter? Filter) : Statement;

    /// <summary>
    /// <code>
    /// `USE ...`
    /// </code>
    /// </summary>
    public sealed record Use(Ast.Use Value) : Statement;

    /// <summary>
    /// <code>
    /// START  [ TRANSACTION | WORK ] | START TRANSACTION } ...
    /// </code>
    /// If <c>begin</c> is false.
    /// <code>
    /// `BEGIN  [ TRANSACTION | WORK ] | START TRANSACTION } ...`
    /// </code>
    /// If <c>begin</c> is true
    /// </summary>
    /// <param name="Modes">Transaction modes such as <c>ISOLATION LEVEL</c> or <c>READ WRITE</c>.</param>
    /// <param name="Begin"><c>true</c> when this was parsed as <c>BEGIN</c> instead of <c>START</c>.</param>
    /// <param name="Transaction">Optional specific keyword used: <c>TRANSACTION</c> or <c>WORK</c>.</param>
    /// <param name="Modifier">Optional transaction modifier (e.g., <c>AND NO CHAIN</c>).</param>
    /// <param name="Statements">
    /// List of statements belonging to the <c>BEGIN</c> block.
    /// Example:
    /// <code>
    /// BEGIN
    ///     SELECT 1;
    ///     SELECT 2;
    /// END;
    /// </code>
    /// </param>
    /// <param name="Exception">
    /// Exception handling with exception clauses.
    /// Example:
    /// <code>
    /// EXCEPTION
    ///     WHEN EXCEPTION_1 THEN
    ///         SELECT 2;
    ///     WHEN EXCEPTION_2 OR EXCEPTION_3 THEN
    ///         SELECT 3;
    ///     WHEN OTHER THEN
    ///         SELECT 4;
    /// </code>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#beginexceptionend"/>
    /// <see href="https://docs.snowflake.com/en/sql-reference/snowflake-scripting/exception"/>
    /// </param>
    /// <param name="HasEndKeyword">TRUE if the statement has an <c>END</c> keyword.</param>
    public sealed record StartTransaction(
        Sequence<TransactionMode> Modes,
        bool Begin,
        BeginTransactionKind? Transaction,
        TransactionModifier? Modifier,
        Sequence<Statement> Statements,
        Sequence<ExceptionWhen>? Exception,
        bool HasEndKeyword) : Statement;

    /// <summary>
    /// <code>
    /// COMMENT ON ...
    /// </code>
    /// <para>Note: this is a PostgreSQL-specific statement.</para>
    /// </summary>
    /// <param name="ObjectType">Type of object being commented (table, column, etc.).</param>
    /// <param name="ObjectName">Name of the object the comment applies to.</param>
    /// <param name="CommentValue">
    /// Optional comment text (None to remove comment).
    /// </param>
    /// <param name="IfExists">
    /// An optional <c>IF EXISTS</c> clause. (Non-standard.)
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/comment"/>
    /// </param>
    // CommentValue: Rust: campo `comment`; renomeado porque um membro não pode ter o nome do tipo que o contém
    // (CS0542; convencoes.md §13, caso 41).
    public sealed record Comment(
        CommentObject ObjectType,
        ObjectName ObjectName,
        string? CommentValue,
        bool IfExists) : Statement;

    /// <summary>
    /// <code>
    /// COMMIT [ TRANSACTION | WORK ] [ AND [ NO ] CHAIN ]
    /// </code>
    /// If <c>end</c> is false
    /// <code>
    /// END [ TRY | CATCH ]
    /// </code>
    /// If <c>end</c> is true
    /// </summary>
    /// <param name="Chain"><c>true</c> when <c>AND [ NO ] CHAIN</c> was present.</param>
    /// <param name="End"><c>true</c> when this <c>COMMIT</c> was parsed as an <c>END</c> block terminator.</param>
    /// <param name="Modifier">Optional transaction modifier for commit semantics.</param>
    public sealed record Commit(bool Chain, bool End, TransactionModifier? Modifier) : Statement;

    /// <summary>
    /// <code>
    /// ROLLBACK [ TRANSACTION | WORK ] [ AND [ NO ] CHAIN ] [ TO [ SAVEPOINT ] savepoint_name ]
    /// </code>
    /// </summary>
    /// <param name="Chain"><c>true</c> when <c>AND [ NO ] CHAIN</c> was present.</param>
    /// <param name="Savepoint">Optional savepoint name to roll back to.</param>
    public sealed record Rollback(bool Chain, Ident? Savepoint) : Statement
    {
        // Esconde a variante herdada Statement.Savepoint (CS8866 sem a declaração explícita).
        public new Ident? Savepoint { get; init; } = Savepoint;
    }

    /// <summary>
    /// <code>
    /// CREATE SCHEMA
    /// </code>
    /// </summary>
    /// <param name="SchemaName">
    /// <c>&lt;schema name&gt; | AUTHORIZATION &lt;schema authorization identifier&gt;  | &lt;schema name&gt;  AUTHORIZATION &lt;schema authorization identifier&gt;</c>
    /// </param>
    /// <param name="OrReplace"><c>true</c> when <c>OR REPLACE</c> was present.</param>
    /// <param name="IfNotExists"><c>true</c> when <c>IF NOT EXISTS</c> was present.</param>
    /// <param name="With">
    /// Schema properties.
    /// <code>
    /// CREATE SCHEMA myschema WITH (key1='value1');
    /// </code>
    /// <see href="https://trino.io/docs/current/sql/create-schema.html">Trino</see>
    /// </param>
    /// <param name="Options">
    /// Schema options.
    /// <code>
    /// CREATE SCHEMA myschema OPTIONS(key1='value1');
    /// </code>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_schema_statement">BigQuery</see>
    /// </param>
    /// <param name="DefaultCollateSpec">
    /// Default collation specification for the schema.
    /// <code>
    /// CREATE SCHEMA myschema DEFAULT COLLATE 'und:ci';
    /// </code>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_schema_statement">BigQuery</see>
    /// </param>
    /// <param name="CloneValue">
    /// Clones a schema
    /// <code>
    /// CREATE SCHEMA myschema CLONE otherschema
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-clone#databases-schemas">Snowflake</see>
    /// </param>
    // CloneValue: Rust: campo `clone`; renomeado porque records não aceitam membros chamados `Clone` (CS8859).
    public sealed record CreateSchema(
        SchemaName SchemaName,
        bool OrReplace,
        bool IfNotExists,
        Sequence<SqlOption>? With,
        Sequence<SqlOption>? Options,
        Expr? DefaultCollateSpec,
        ObjectName? CloneValue) : Statement;

    /// <summary>
    /// <code>
    /// CREATE DATABASE
    /// </code>
    /// See:
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-database"/>
    /// </summary>
    /// <param name="DbName">Database name.</param>
    /// <param name="IfNotExists"><c>IF NOT EXISTS</c> flag.</param>
    /// <param name="Location">Optional location URI.</param>
    /// <param name="ManagedLocation">Optional managed location.</param>
    /// <param name="OrReplace"><c>OR REPLACE</c> flag.</param>
    /// <param name="Transient"><c>TRANSIENT</c> flag.</param>
    /// <param name="CloneValue">
    /// Optional clone source.
    /// </param>
    /// <param name="DataRetentionTimeInDays">Optional data retention time in days.</param>
    /// <param name="MaxDataExtensionTimeInDays">Optional maximum data extension time in days.</param>
    /// <param name="ExternalVolume">Optional external volume identifier.</param>
    /// <param name="Catalog">Optional catalog name.</param>
    /// <param name="ReplaceInvalidCharacters">Whether to replace invalid characters.</param>
    /// <param name="DefaultDdlCollation">Default DDL collation string.</param>
    /// <param name="StorageSerializationPolicy">Storage serialization policy.</param>
    /// <param name="Comment">Optional comment.</param>
    /// <param name="DefaultCharset">Optional default character set (MySQL).</param>
    /// <param name="DefaultCollation">Optional default collation (MySQL).</param>
    /// <param name="CatalogSync">Optional catalog sync identifier.</param>
    /// <param name="CatalogSyncNamespaceMode">Catalog sync namespace mode.</param>
    /// <param name="CatalogSyncNamespaceFlattenDelimiter">Optional flatten delimiter for namespace sync.</param>
    /// <param name="WithTags">Optional tags for the database.</param>
    /// <param name="WithContacts">Optional contact entries for the database.</param>
    // CloneValue: Rust: campo `clone`; renomeado porque records não aceitam membros chamados `Clone` (CS8859).
    public sealed record CreateDatabase(
        ObjectName DbName,
        bool IfNotExists,
        string? Location,
        string? ManagedLocation,
        bool OrReplace,
        bool Transient,
        ObjectName? CloneValue,
        ulong? DataRetentionTimeInDays,
        ulong? MaxDataExtensionTimeInDays,
        string? ExternalVolume,
        string? Catalog,
        bool? ReplaceInvalidCharacters,
        string? DefaultDdlCollation,
        StorageSerializationPolicy? StorageSerializationPolicy,
        string? Comment,
        string? DefaultCharset,
        string? DefaultCollation,
        string? CatalogSync,
        CatalogSyncNamespaceMode? CatalogSyncNamespaceMode,
        string? CatalogSyncNamespaceFlattenDelimiter,
        Sequence<Tag>? WithTags,
        Sequence<ContactEntry>? WithContacts) : Statement
    {
        // Esconde a variante herdada Statement.Comment (CS8866 sem a declaração explícita).
        public new string? Comment { get; init; } = Comment;
    }

    /// <summary>
    /// <code>
    /// CREATE FUNCTION
    /// </code>
    /// <para>Supported variants:</para>
    /// <list type="number">
    /// <item><see href="https://cwiki.apache.org/confluence/display/hive/languagemanual+ddl#LanguageManualDDL-Create/Drop/ReloadFunction">Hive</see></item>
    /// <item><see href="https://www.postgresql.org/docs/15/sql-createfunction.html">PostgreSQL</see></item>
    /// <item><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_function_statement">BigQuery</see></item>
    /// <item><see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql">MsSql</see></item>
    /// </list>
    /// </summary>
    public sealed record CreateFunction(Ast.CreateFunction Value) : Statement;

    /// <summary>CREATE TRIGGER statement. See struct [CreateTrigger] for details.</summary>
    public sealed record CreateTrigger(Ast.CreateTrigger Value) : Statement;

    /// <summary>DROP TRIGGER statement. See struct [DropTrigger] for details.</summary>
    public sealed record DropTrigger(Ast.DropTrigger Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE PROCEDURE
    /// </code>
    /// </summary>
    /// <param name="OrAlter"><c>OR ALTER</c> flag.</param>
    /// <param name="Name">Procedure name.</param>
    /// <param name="Params">Optional procedure parameters.</param>
    /// <param name="Language">Optional language identifier.</param>
    /// <param name="Body">Procedure body statements.</param>
    public sealed record CreateProcedure(
        bool OrAlter,
        ObjectName Name,
        Sequence<ProcedureParam>? Params,
        Ident? Language,
        ConditionalStatements Body) : Statement;

    /// <summary>
    /// <code>
    /// CREATE MACRO
    /// </code>
    /// <para>Supported variants:</para>
    /// <list type="number">
    /// <item><see href="https://duckdb.org/docs/sql/statements/create_macro">DuckDB</see></item>
    /// </list>
    /// </summary>
    /// <param name="OrReplace"><c>OR REPLACE</c> flag.</param>
    /// <param name="Temporary">Whether macro is temporary.</param>
    /// <param name="Name">Macro name.</param>
    /// <param name="Args">Optional macro arguments.</param>
    /// <param name="Definition">Macro definition body.</param>
    public sealed record CreateMacro(
        bool OrReplace,
        bool Temporary,
        ObjectName Name,
        Sequence<MacroArg>? Args,
        MacroDefinition Definition) : Statement;

    /// <summary>
    /// <code>
    /// CREATE STAGE
    /// </code>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/create-stage"/>
    /// </summary>
    /// <param name="OrReplace"><c>OR REPLACE</c> flag for stage.</param>
    /// <param name="Temporary">Whether stage is temporary.</param>
    /// <param name="IfNotExists"><c>IF NOT EXISTS</c> flag.</param>
    /// <param name="Name">Stage name.</param>
    /// <param name="StageParams">Stage parameters.</param>
    /// <param name="DirectoryTableParams">Directory table parameters.</param>
    /// <param name="FileFormat">File format options.</param>
    /// <param name="CopyOptions">Copy options for stage.</param>
    /// <param name="Comment">Optional comment.</param>
    public sealed record CreateStage(
        bool OrReplace,
        bool Temporary,
        bool IfNotExists,
        ObjectName Name,
        StageParamsObject StageParams,
        KeyValueOptions DirectoryTableParams,
        KeyValueOptions FileFormat,
        KeyValueOptions CopyOptions,
        string? Comment) : Statement
    {
        // Esconde a variante herdada Statement.Comment (CS8866 sem a declaração explícita).
        public new string? Comment { get; init; } = Comment;
    }

    /// <summary>
    /// <code>
    /// CREATE [ OR REPLACE ] [ { TEMP | TEMPORARY | VOLATILE } ] FILE FORMAT [ IF NOT EXISTS ] &lt;name&gt;
    ///   [ TYPE = { CSV | JSON | AVRO | ORC | PARQUET | XML } [ formatTypeOptions ] ]
    ///   [ COMMENT = '&lt;string_literal&gt;' ]
    /// </code>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/create-file-format"/>
    /// </summary>
    /// <param name="OrReplace"><c>OR REPLACE</c> flag.</param>
    /// <param name="Temporary">Whether file format is temporary.</param>
    /// <param name="Volatile">Whether file format is volatile.</param>
    /// <param name="IfNotExists"><c>IF NOT EXISTS</c> flag.</param>
    /// <param name="Name">File format name.</param>
    /// <param name="Options">Format type options (e.g. <c>TYPE</c>, <c>FIELD_DELIMITER</c>, <c>COMPRESSION</c>, ...).</param>
    /// <param name="Comment">Optional comment.</param>
    public sealed record CreateFileFormat(
        bool OrReplace,
        bool Temporary,
        bool Volatile,
        bool IfNotExists,
        ObjectName Name,
        KeyValueOptions Options,
        string? Comment) : Statement
    {
        // Esconde a variante herdada Statement.Comment (CS8866 sem a declaração explícita).
        public new string? Comment { get; init; } = Comment;
    }

    /// <summary>
    /// <code>
    /// CREATE [ OR REPLACE ] WAREHOUSE [ IF NOT EXISTS ] &lt;name&gt;
    ///   [ [ WITH ] &lt;property&gt; = &lt;value&gt; [ ... ] ]
    /// </code>
    /// Snowflake-specific statement to create a virtual warehouse.
    /// <para>See <see href="https://docs.snowflake.com/en/sql-reference/sql/create-warehouse"/></para>
    /// </summary>
    public sealed record CreateWarehouse(Ast.CreateWarehouse Value) : Statement;

    /// <summary>
    /// <code>
    /// ASSERT &lt;condition&gt; [AS &lt;message&gt;]
    /// </code>
    /// </summary>
    /// <param name="Condition">Assertion condition expression.</param>
    /// <param name="Message">Optional message expression.</param>
    public sealed record Assert(Expr Condition, Expr? Message) : Statement;

    /// <summary>
    /// <code>
    /// GRANT privileges ON objects TO grantees
    /// </code>
    /// </summary>
    public sealed record Grant(Ast.Grant Value) : Statement;

    /// <summary>
    /// <code>
    /// DENY privileges ON object TO grantees
    /// </code>
    /// </summary>
    public sealed record Deny(DenyStatement Value) : Statement;

    /// <summary>
    /// <code>
    /// REVOKE privileges ON objects FROM grantees
    /// </code>
    /// </summary>
    public sealed record Revoke(Ast.Revoke Value) : Statement;

    /// <summary>
    /// <code>
    /// DEALLOCATE [ PREPARE ] { name | ALL }
    /// </code>
    /// <para>Note: this is a PostgreSQL-specific statement.</para>
    /// </summary>
    /// <param name="Name">Name to deallocate (or <c>ALL</c>).</param>
    /// <param name="Prepare">Whether <c>PREPARE</c> keyword was present.</param>
    public sealed record Deallocate(Ident Name, bool Prepare) : Statement
    {
        // Esconde a variante herdada Statement.Prepare (CS8866 sem a declaração explícita).
        public new bool Prepare { get; init; } = Prepare;
    }

    /// <summary>
    /// <code>
    /// An `EXECUTE` statement
    /// </code>
    /// <para>
    /// Postgres: <see href="https://www.postgresql.org/docs/current/sql-execute.html"/>
    /// MSSQL: <see href="https://learn.microsoft.com/en-us/sql/relational-databases/stored-procedures/execute-a-stored-procedure"/>
    /// BigQuery: <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#execute_immediate"/>
    /// Snowflake: <see href="https://docs.snowflake.com/en/sql-reference/sql/execute-immediate"/>
    /// </para>
    /// </summary>
    /// <param name="Name">Optional function/procedure name.</param>
    /// <param name="Parameters">Parameter expressions passed to execute.</param>
    /// <param name="HasParentheses">Whether parentheses were present around <c>parameters</c>.</param>
    /// <param name="Immediate">Is this an <c>EXECUTE IMMEDIATE</c>.</param>
    /// <param name="Into">Identifiers to capture results into.</param>
    /// <param name="Using"><c>USING</c> expressions with optional aliases.</param>
    /// <param name="Output">
    /// Whether the last parameter is the return value of the procedure
    /// MSSQL: <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/execute-transact-sql?view=sql-server-ver17#output"/>
    /// </param>
    /// <param name="Default">
    /// Whether to invoke the procedure with the default parameter values
    /// MSSQL: <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/execute-transact-sql?view=sql-server-ver17#default"/>
    /// </param>
    public sealed record Execute(
        ObjectName? Name,
        Sequence<Expr> Parameters,
        bool HasParentheses,
        bool Immediate,
        Sequence<Ident> Into,
        Sequence<ExprWithAlias> Using,
        bool Output,
        bool Default) : Statement;

    /// <summary>
    /// <code>
    /// PREPARE name [ ( data_type [, ...] ) ] AS statement
    /// </code>
    /// <para>Note: this is a PostgreSQL-specific statement.</para>
    /// </summary>
    /// <param name="Name">Name of the prepared statement.</param>
    /// <param name="DataTypes">Optional data types for parameters.</param>
    /// <param name="Statement">Statement being prepared.</param>
    public sealed record Prepare(Ident Name, Sequence<DataType> DataTypes, Statement Statement) : Statement;

    /// <summary>
    /// <code>
    /// KILL [CONNECTION | QUERY | MUTATION]
    /// </code>
    /// <para>
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/statements/kill/"/>
    /// See <see href="https://dev.mysql.com/doc/refman/8.0/en/kill.html"/>
    /// </para>
    /// </summary>
    /// <param name="Modifier">Optional kill modifier (CONNECTION, QUERY, MUTATION).</param>
    /// <param name="Id">The id of the process to kill.</param>
    public sealed record Kill(
        KillType? Modifier,
        // processlist_id
        ulong Id) : Statement;

    /// <summary>
    /// <code>
    /// [EXPLAIN | DESC | DESCRIBE] TABLE
    /// </code>
    /// Note: this is a MySQL-specific statement. See <see href="https://dev.mysql.com/doc/refman/8.0/en/explain.html"/>
    /// </summary>
    /// <param name="DescribeAlias"><c>EXPLAIN | DESC | DESCRIBE</c></param>
    /// <param name="HiveFormat">Hive style <c>FORMATTED | EXTENDED</c></param>
    /// <param name="HasTableKeyword">
    /// Snowflake and ClickHouse support <c>DESC|DESCRIBE TABLE &lt;table_name&gt;</c> syntax
    /// <para>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/desc-table.html">Snowflake</see>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/describe-table">ClickHouse</see>
    /// </para>
    /// </param>
    /// <param name="TableName">Table name</param>
    public sealed record ExplainTable(
        DescribeAlias DescribeAlias,
        HiveDescribeFormat? HiveFormat,
        bool HasTableKeyword,
        [property: VisitWith("visit_relation")] ObjectName TableName) : Statement;

    /// <summary>
    /// <code>
    /// [EXPLAIN | DESC | DESCRIBE]  &lt;statement&gt;
    /// </code>
    /// </summary>
    /// <param name="DescribeAlias"><c>EXPLAIN | DESC | DESCRIBE</c></param>
    /// <param name="Analyze">Carry out the command and show actual run times and other statistics.</param>
    /// <param name="Verbose">Display additional information regarding the plan.</param>
    /// <param name="QueryPlan">
    /// <c>EXPLAIN QUERY PLAN</c>
    /// Display the query plan without running the query.
    /// <para><see href="https://sqlite.org/lang_explain.html">SQLite</see></para>
    /// </param>
    /// <param name="Estimate">
    /// <c>EXPLAIN ESTIMATE</c>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/explain#explain-estimate">Clickhouse</see>
    /// </param>
    /// <param name="Statement">A SQL query that specifies what to explain</param>
    /// <param name="Format">Optional output format of explain</param>
    /// <param name="Options">Postgres style utility options, <c>(analyze, verbose true)</c></param>
    public sealed record Explain(
        DescribeAlias DescribeAlias,
        bool Analyze,
        bool Verbose,
        bool QueryPlan,
        bool Estimate,
        Statement Statement,
        AnalyzeFormatKind? Format,
        Sequence<UtilityOption>? Options) : Statement
    {
        // Esconde a variante herdada Statement.Analyze (CS8866 sem a declaração explícita).
        public new bool Analyze { get; init; } = Analyze;
    }

    /// <summary>
    /// <code>
    /// SAVEPOINT
    /// </code>
    /// Define a new savepoint within the current transaction
    /// </summary>
    /// <param name="Name">Name of the savepoint being defined.</param>
    public sealed record Savepoint(Ident Name) : Statement;

    /// <summary>
    /// <code>
    /// RELEASE [ SAVEPOINT ] savepoint_name
    /// </code>
    /// </summary>
    /// <param name="Name">Name of the savepoint to release.</param>
    public sealed record ReleaseSavepoint(Ident Name) : Statement;

    /// <summary>
    /// A <c>MERGE</c> statement.
    /// <code>
    /// MERGE INTO &lt;target_table&gt; USING &lt;source&gt; ON &lt;join_expr&gt; { matchedClause | notMatchedClause } [ ... ]
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/merge">Snowflake</see>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dml-syntax#merge_statement">BigQuery</see>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/merge-transact-sql?view=sql-server-ver16">MSSQL</see>
    /// </summary>
    public sealed record Merge(Ast.Merge Value) : Statement;

    /// <summary>
    /// <code>
    /// CACHE [ FLAG ] TABLE &lt;table_name&gt; [ OPTIONS('K1' = 'V1', 'K2' = V2) ] [ AS ] [ &lt;query&gt; ]
    /// </code>
    /// <para>
    /// See <see href="https://docs.databricks.com/spark/latest/spark-sql/language-manual/sql-ref-syntax-aux-cache-cache-table.html">Spark SQL docs</see> for more details.
    /// </para>
    /// </summary>
    /// <param name="TableFlag">Table flag</param>
    /// <param name="TableName">Table name</param>
    /// <param name="HasAs"><c>true</c> if <c>AS</c> keyword was present before the query.</param>
    /// <param name="Options">Table confs</param>
    /// <param name="Query">Cache table as a Query</param>
    public sealed record Cache(
        ObjectName? TableFlag,
        [property: VisitWith("visit_relation")] ObjectName TableName,
        bool HasAs,
        Sequence<SqlOption> Options,
        Ast.Query? Query) : Statement
    {
        // Esconde a variante herdada Statement.Query (CS8866 sem a declaração explícita).
        public new Ast.Query? Query { get; init; } = Query;
    }

    /// <summary>
    /// <code>
    /// UNCACHE TABLE [ IF EXISTS ]  &lt;table_name&gt;
    /// </code>
    /// </summary>
    /// <param name="TableName">Table name</param>
    /// <param name="IfExists"><c>true</c> when <c>IF EXISTS</c> was present.</param>
    public sealed record UNCache(
        [property: VisitWith("visit_relation")] ObjectName TableName,
        bool IfExists) : Statement;

    /// <summary>
    /// <code>
    /// CREATE [ { TEMPORARY | TEMP } ] SEQUENCE [ IF NOT EXISTS ] &lt;sequence_name&gt;
    /// </code>
    /// Define a new sequence:
    /// </summary>
    /// <param name="Temporary">Whether the sequence is temporary.</param>
    /// <param name="IfNotExists"><c>IF NOT EXISTS</c> flag.</param>
    /// <param name="Name">Sequence name.</param>
    /// <param name="DataType">Optional data type for the sequence.</param>
    /// <param name="SequenceOptions">Sequence options (INCREMENT, MINVALUE, etc.).</param>
    /// <param name="OwnedBy">Optional <c>OWNED BY</c> target.</param>
    public sealed record CreateSequence(
        bool Temporary,
        bool IfNotExists,
        ObjectName Name,
        DataType? DataType,
        Sequence<SequenceOptions> SequenceOptions,
        ObjectName? OwnedBy) : Statement;

    /// <summary>A <c>CREATE DOMAIN</c> statement.</summary>
    public sealed record CreateDomain(Ast.CreateDomain Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE TYPE &lt;name&gt;
    /// </code>
    /// </summary>
    /// <param name="Name">Type name to create.</param>
    /// <param name="Representation">Optional type representation details.</param>
    public sealed record CreateType(ObjectName Name, UserDefinedTypeRepresentation? Representation) : Statement;

    /// <summary>
    /// <code>
    /// PRAGMA &lt;schema-name&gt;.&lt;pragma-name&gt; = &lt;pragma-value&gt;
    /// </code>
    /// </summary>
    /// <param name="Name">Pragma name (possibly qualified).</param>
    /// <param name="Value">Optional pragma value.</param>
    /// <param name="IsEq">Whether the pragma used <c>=</c>.</param>
    public sealed record Pragma(ObjectName Name, ValueWithSpan? Value, bool IsEq) : Statement;

    /// <summary>
    /// <code>
    /// LOCK [ TABLE ] [ ONLY ] name [ * ] [, ...] [ IN lockmode MODE ] [ NOWAIT ]
    /// </code>
    /// <para>See <see href="https://www.postgresql.org/docs/current/sql-lock.html"/></para>
    /// </summary>
    public sealed record Lock(Ast.Lock Value) : Statement;

    /// <summary>
    /// <code>
    /// LOCK TABLES &lt;table_name&gt; [READ [LOCAL] | [LOW_PRIORITY] WRITE]
    /// </code>
    /// Note: this is a MySQL-specific statement. See <see href="https://dev.mysql.com/doc/refman/8.0/en/lock-tables.html"/>
    /// </summary>
    /// <param name="Tables">List of tables to lock with modes.</param>
    public sealed record LockTables(Sequence<LockTable> Tables) : Statement;

    /// <summary>
    /// <code>
    /// UNLOCK TABLES
    /// </code>
    /// Note: this is a MySQL-specific statement. See <see href="https://dev.mysql.com/doc/refman/8.0/en/lock-tables.html"/>
    /// </summary>
    public sealed record UnlockTables : Statement;

    /// <summary>
    /// Unloads the result of a query to file
    /// <para><see href="https://docs.aws.amazon.com/athena/latest/ug/unload.html">Athena</see>:</para>
    /// <code>
    /// UNLOAD(statement) TO &lt;destination&gt; [ WITH options ]
    /// </code>
    /// <para><see href="https://docs.aws.amazon.com/redshift/latest/dg/r_UNLOAD.html">Redshift</see>:</para>
    /// <code>
    /// UNLOAD('statement') TO &lt;destination&gt; [ OPTIONS ]
    /// </code>
    /// </summary>
    /// <param name="Query">Optional query AST to unload.</param>
    /// <param name="QueryText">Optional original query text.</param>
    /// <param name="To">Destination identifier.</param>
    /// <param name="Auth">Optional IAM role/auth information.</param>
    /// <param name="With">Additional <c>WITH</c> options.</param>
    /// <param name="Options">Legacy copy-style options.</param>
    public sealed record Unload(
        Ast.Query? Query,
        string? QueryText,
        Ident To,
        IamRoleKind? Auth,
        Sequence<SqlOption> With,
        Sequence<CopyLegacyOption> Options) : Statement
    {
        // Esconde a variante herdada Statement.Query (CS8866 sem a declaração explícita).
        public new Ast.Query? Query { get; init; } = Query;
    }

    /// <summary>
    /// ClickHouse:
    /// <code>
    /// OPTIMIZE TABLE [db.]name [ON CLUSTER cluster] [PARTITION partition | PARTITION ID 'partition_id'] [FINAL] [DEDUPLICATE [BY expression]]
    /// </code>
    /// See ClickHouse <see href="https://clickhouse.com/docs/en/sql-reference/statements/optimize"/>
    /// <para>Databricks:</para>
    /// <code>
    /// OPTIMIZE table_name [WHERE predicate] [ZORDER BY (col_name1 [, ...])]
    /// </code>
    /// See Databricks <see href="https://docs.databricks.com/en/sql/language-manual/delta-optimize.html"/>
    /// </summary>
    /// <param name="Name">Table name to optimize.</param>
    /// <param name="HasTableKeyword">Whether the <c>TABLE</c> keyword was present (ClickHouse uses <c>OPTIMIZE TABLE</c>, Databricks uses <c>OPTIMIZE</c>).</param>
    /// <param name="OnCluster">
    /// Optional cluster identifier.
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/optimize">ClickHouse</see>
    /// </param>
    /// <param name="Partition">
    /// Optional partition spec.
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/optimize">ClickHouse</see>
    /// </param>
    /// <param name="IncludeFinal">
    /// Whether <c>FINAL</c> was specified.
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/optimize">ClickHouse</see>
    /// </param>
    /// <param name="Deduplicate">
    /// Optional deduplication settings.
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/optimize">ClickHouse</see>
    /// </param>
    /// <param name="Predicate">
    /// Optional WHERE predicate.
    /// <see href="https://docs.databricks.com/en/sql/language-manual/delta-optimize.html">Databricks</see>
    /// </param>
    /// <param name="Zorder">
    /// Optional ZORDER BY columns.
    /// <see href="https://docs.databricks.com/en/sql/language-manual/delta-optimize.html">Databricks</see>
    /// </param>
    public sealed record OptimizeTable(
        ObjectName Name,
        bool HasTableKeyword,
        Ident? OnCluster,
        Partition? Partition,
        bool IncludeFinal,
        Deduplicate? Deduplicate,
        Expr? Predicate,
        Sequence<Expr>? Zorder) : Statement;

    /// <summary>
    /// <code>
    /// LISTEN
    /// </code>
    /// listen for a notification channel
    /// <para>See Postgres <see href="https://www.postgresql.org/docs/current/sql-listen.html"/></para>
    /// </summary>
    /// <param name="Channel">Notification channel identifier.</param>
    public sealed record LISTEN(Ident Channel) : Statement;

    /// <summary>
    /// <code>
    /// UNLISTEN
    /// </code>
    /// stop listening for a notification
    /// <para>See Postgres <see href="https://www.postgresql.org/docs/current/sql-unlisten.html"/></para>
    /// </summary>
    /// <param name="Channel">Notification channel identifier.</param>
    public sealed record UNLISTEN(Ident Channel) : Statement;

    /// <summary>
    /// <code>
    /// NOTIFY channel [ , payload ]
    /// </code>
    /// send a notification event together with an optional "payload" string to channel
    /// <para>See Postgres <see href="https://www.postgresql.org/docs/current/sql-notify.html"/></para>
    /// </summary>
    /// <param name="Channel">Notification channel identifier.</param>
    /// <param name="Payload">Optional payload string.</param>
    public sealed record NOTIFY(Ident Channel, string? Payload) : Statement;

    /// <summary>
    /// <code>
    /// LOAD DATA [LOCAL] INPATH 'filepath' [OVERWRITE] INTO TABLE tablename
    /// [PARTITION (partcol1=val1, partcol2=val2 ...)]
    /// [INPUTFORMAT 'inputformat' SERDE 'serde']
    /// </code>
    /// Loading files into tables
    /// <para>See Hive <see href="https://cwiki.apache.org/confluence/pages/viewpage.action?pageId=27362036#LanguageManualDML-Loadingfilesintotables"/></para>
    /// </summary>
    /// <param name="Local">Whether <c>LOCAL</c> is present.</param>
    /// <param name="Inpath">Input path for files to load.</param>
    /// <param name="Overwrite">Whether <c>OVERWRITE</c> was specified.</param>
    /// <param name="TableName">Target table name to load into.</param>
    /// <param name="Partitioned">Optional partition specification.</param>
    /// <param name="TableFormat">Optional table format information.</param>
    public sealed record LoadData(
        bool Local,
        string Inpath,
        bool Overwrite,
        ObjectName TableName,
        Sequence<Expr>? Partitioned,
        HiveLoadDataFormat? TableFormat) : Statement;

    /// <summary>
    /// <code>
    /// Rename TABLE tbl_name TO new_tbl_name[, tbl_name2 TO new_tbl_name2] ...
    /// </code>
    /// Renames one or more tables
    /// <para>See Mysql <see href="https://dev.mysql.com/doc/refman/9.1/en/rename-table.html"/></para>
    /// </summary>
    public sealed record RenameTable(Sequence<Ast.RenameTable> Value) : Statement;

    /// <summary>
    /// Snowflake <c>LIST</c>
    /// See: <see href="https://docs.snowflake.com/en/sql-reference/sql/list"/>
    /// </summary>
    public sealed record List(FileStagingCommand Value) : Statement;

    /// <summary>
    /// Snowflake <c>PUT</c>
    /// <code>
    /// PUT 'file://&lt;path&gt;' &lt;internalStage&gt; [ &lt;option&gt; = &lt;value&gt; ... ]
    /// </code>
    /// Options include <c>PARALLEL</c>, <c>AUTO_COMPRESS</c>, <c>SOURCE_COMPRESSION</c>, <c>OVERWRITE</c>.
    /// See: <see href="https://docs.snowflake.com/en/sql-reference/sql/put"/>
    /// </summary>
    /// <param name="Source">Local source URI as written in the statement, e.g. <c>file:///tmp/data.csv</c>.</param>
    /// <param name="Stage">Target internal stage (e.g. <c>@mystage</c>, <c>@~</c>, <c>@%table</c>).</param>
    /// <param name="Options">Trailing options (<c>PARALLEL=4</c>, <c>AUTO_COMPRESS=TRUE</c>, ...).</param>
    public sealed record Put(string Source, ObjectName Stage, KeyValueOptions Options) : Statement;

    /// <summary>
    /// Snowflake <c>REMOVE</c>
    /// See: <see href="https://docs.snowflake.com/en/sql-reference/sql/remove"/>
    /// </summary>
    public sealed record Remove(FileStagingCommand Value) : Statement;

    /// <summary>
    /// RaiseError (MSSQL)
    /// RAISERROR ( { msg_id | msg_str | @local_variable }
    /// { , severity , state }
    /// [ , argument [ , ...n ] ] )
    /// [ WITH option [ , ...n ] ]
    /// See <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/raiserror-transact-sql?view=sql-server-ver16"/>
    /// </summary>
    /// <param name="Message">Error message expression or identifier.</param>
    /// <param name="Severity">Severity expression.</param>
    /// <param name="State">State expression.</param>
    /// <param name="Arguments">Substitution arguments for the message.</param>
    /// <param name="Options">Additional <c>WITH</c> options for RAISERROR.</param>
    public sealed record RaisError(
        Expr Message,
        Expr Severity,
        Expr State,
        Sequence<Expr> Arguments,
        Sequence<RaisErrorOption> Options) : Statement;

    /// <summary>A MSSQL <c>THROW</c> statement.</summary>
    public sealed record Throw(ThrowStatement Value) : Statement;

    /// <summary>
    /// <code>
    /// PRINT msg_str | @local_variable | string_expr
    /// </code>
    /// <para>See: <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/print-transact-sql"/></para>
    /// </summary>
    public sealed record Print(PrintStatement Value) : Statement;

    /// <summary>
    /// MSSQL <c>WAITFOR</c> statement.
    /// <para>See: <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/waitfor-transact-sql"/></para>
    /// </summary>
    public sealed record WaitFor(WaitForStatement Value) : Statement;

    /// <summary>
    /// <code>
    /// RETURN [ expression ]
    /// </code>
    /// <para>See [ReturnStatement]</para>
    /// </summary>
    public sealed record Return(ReturnStatement Value) : Statement;

    /// <summary>
    /// Export data statement
    /// <para>Example:</para>
    /// <code>
    /// EXPORT DATA OPTIONS(uri='gs://bucket/folder/*', format='PARQUET', overwrite=true) AS
    /// SELECT field1, field2 FROM mydataset.table1 ORDER BY field1 LIMIT 10
    /// </code>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/export-statements">BigQuery</see>
    /// </summary>
    public sealed record ExportData(Ast.ExportData Value) : Statement;

    /// <summary>
    /// <code>
    /// CREATE [OR REPLACE] USER &lt;user&gt; [IF NOT EXISTS]
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-user">Snowflake</see>
    /// </summary>
    public sealed record CreateUser(Ast.CreateUser Value) : Statement;

    /// <summary>
    /// <code>
    /// ALTER USER \[ IF EXISTS \] \[ &lt;name&gt; \]
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-user">Snowflake</see>
    /// </summary>
    public sealed record AlterUser(Ast.AlterUser Value) : Statement;

    /// <summary>
    /// Re-sorts rows and reclaims space in either a specified table or all tables in the current database
    /// <code>
    /// VACUUM tbl
    /// </code>
    /// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_VACUUM_command.html">Redshift</see>
    /// </summary>
    public sealed record Vacuum(VacuumStatement Value) : Statement;

    /// <summary>
    /// Restore the value of a run-time parameter to the default value.
    /// <code>
    /// RESET configuration_parameter;
    /// RESET ALL;
    /// </code>
    /// <see href="https://www.postgresql.org/docs/current/sql-reset.html">PostgreSQL</see>
    /// </summary>
    public sealed record Reset(ResetStatement Value) : Statement;

    // Rust: `impl From<Analyze> for Statement`.
    public static Statement From(Ast.Analyze analyze) => new Analyze(analyze);

    // Rust: `impl From<ddl::Truncate> for Statement`.
    public static Statement From(Ast.Truncate truncate) => new Truncate(truncate);

    // Rust: `impl From<Lock> for Statement`.
    public static Statement From(Ast.Lock @lock) => new Lock(@lock);

    // Rust: `impl From<ddl::Msck> for Statement`.
    public static Statement From(Ast.Msck msck) => new Msck(msck);
}
