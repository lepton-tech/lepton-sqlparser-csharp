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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>An <c>ALTER TABLE</c> (<c>Statement::AlterTable</c>) operation</summary>
public abstract partial record AlterTableOperation : IDisplay
{
    /// <summary><c>ADD &lt;table_constraint&gt; [NOT VALID]</c></summary>
    /// <param name="Constraint">The table constraint to add.</param>
    /// <param name="NotValid">Whether the constraint should be marked <c>NOT VALID</c>.</param>
    public sealed record AddConstraint(TableConstraint Constraint, bool NotValid) : AlterTableOperation;

    /// <summary><c>ADD [COLUMN] [IF NOT EXISTS] &lt;column_def&gt;</c></summary>
    /// <param name="ColumnKeyword"><c>[COLUMN]</c>.</param>
    /// <param name="IfNotExists"><c>[IF NOT EXISTS]</c></param>
    /// <param name="ColumnDef">&lt;column_def&gt;.</param>
    /// <param name="ColumnPosition">MySQL <c>ALTER TABLE</c> only  [FIRST | AFTER column_name]</param>
    public sealed record AddColumn(
        bool ColumnKeyword,
        bool IfNotExists,
        ColumnDef ColumnDef,
        MySQLColumnPosition? ColumnPosition) : AlterTableOperation;

    /// <summary>
    /// <c>ADD PROJECTION [IF NOT EXISTS] name ( SELECT &lt;COLUMN LIST EXPR&gt; [GROUP BY] [ORDER BY])</c>
    /// <para>
    /// Note: this is a ClickHouse-specific operation.
    /// Please refer to <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/projection#add-projection">ClickHouse</see>
    /// </para>
    /// </summary>
    /// <param name="IfNotExists">Whether <c>IF NOT EXISTS</c> was specified.</param>
    /// <param name="Name">Name of the projection to add.</param>
    /// <param name="Select">The projection's select clause.</param>
    public sealed record AddProjection(bool IfNotExists, Ident Name, ProjectionSelect Select) : AlterTableOperation;

    /// <summary>
    /// <c>DROP PROJECTION [IF EXISTS] name</c>
    /// <para>
    /// Note: this is a ClickHouse-specific operation.
    /// Please refer to <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/projection#drop-projection">ClickHouse</see>
    /// </para>
    /// </summary>
    /// <param name="IfExists">Whether <c>IF EXISTS</c> was specified.</param>
    /// <param name="Name">Name of the projection to drop.</param>
    public sealed record DropProjection(bool IfExists, Ident Name) : AlterTableOperation;

    /// <summary>
    /// <c>MATERIALIZE PROJECTION [IF EXISTS] name [IN PARTITION partition_name]</c>
    /// <para>
    ///  Note: this is a ClickHouse-specific operation.
    /// Please refer to <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/projection#materialize-projection">ClickHouse</see>
    /// </para>
    /// </summary>
    /// <param name="IfExists">Whether <c>IF EXISTS</c> was specified.</param>
    /// <param name="Name">Name of the projection to materialize.</param>
    /// <param name="Partition">Optional partition name to operate on.</param>
    public sealed record MaterializeProjection(bool IfExists, Ident Name, Ident? Partition) : AlterTableOperation;

    /// <summary>
    /// <c>CLEAR PROJECTION [IF EXISTS] name [IN PARTITION partition_name]</c>
    /// <para>
    /// Note: this is a ClickHouse-specific operation.
    /// Please refer to <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/projection#clear-projection">ClickHouse</see>
    /// </para>
    /// </summary>
    /// <param name="IfExists">Whether <c>IF EXISTS</c> was specified.</param>
    /// <param name="Name">Name of the projection to clear.</param>
    /// <param name="Partition">Optional partition name to operate on.</param>
    public sealed record ClearProjection(bool IfExists, Ident Name, Ident? Partition) : AlterTableOperation;

    /// <summary>
    /// <c>DISABLE ROW LEVEL SECURITY</c>
    /// <para>
    /// Note: this is a PostgreSQL-specific operation.
    /// Please refer to <see href="https://www.postgresql.org/docs/current/sql-altertable.html">PostgreSQL documentation</see>
    /// </para>
    /// </summary>
    public sealed record DisableRowLevelSecurity : AlterTableOperation;

    /// <summary>
    /// <c>DISABLE RULE rewrite_rule_name</c>
    /// <para>Note: this is a PostgreSQL-specific operation.</para>
    /// </summary>
    /// <param name="Name">Name of the rule to disable.</param>
    public sealed record DisableRule(Ident Name) : AlterTableOperation;

    /// <summary>
    /// <c>DISABLE TRIGGER [ trigger_name | ALL | USER ]</c>
    /// <para>Note: this is a PostgreSQL-specific operation.</para>
    /// </summary>
    /// <param name="Name">Name of the trigger to disable (or ALL/USER).</param>
    public sealed record DisableTrigger(Ident Name) : AlterTableOperation;

    /// <summary><c>DROP CONSTRAINT [ IF EXISTS ] &lt;name&gt;</c></summary>
    /// <param name="IfExists"><c>IF EXISTS</c> flag for dropping the constraint.</param>
    /// <param name="Name">Name of the constraint to drop.</param>
    /// <param name="DropBehavior">Optional drop behavior (<c>CASCADE</c>/<c>RESTRICT</c>).</param>
    public sealed record DropConstraint(bool IfExists, Ident Name, DropBehavior? DropBehavior) : AlterTableOperation;

    /// <summary><c>DROP [ COLUMN ] [ IF EXISTS ] &lt;column_name&gt; [ , &lt;column_name&gt;, ... ] [ CASCADE ]</c></summary>
    /// <param name="HasColumnKeyword">Whether the <c>COLUMN</c> keyword was present.</param>
    /// <param name="ColumnNames">Names of columns to drop.</param>
    /// <param name="IfExists">Whether <c>IF EXISTS</c> was specified for the columns.</param>
    /// <param name="DropBehavior">Optional drop behavior for the column removal.</param>
    public sealed record DropColumn(
        bool HasColumnKeyword,
        Sequence<Ident> ColumnNames,
        bool IfExists,
        DropBehavior? DropBehavior) : AlterTableOperation;

    /// <summary>
    /// <c>ATTACH PART|PARTITION &lt;partition_expr&gt;</c>
    /// Note: this is a ClickHouse-specific operation, please refer to
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/partition#attach-partitionpart">ClickHouse</see>
    /// </summary>
    /// <param name="Partition">Partition expression to attach.</param>
    // PART is not a short form of PARTITION, it's a separate keyword
    // which represents a physical file on disk and partition is a logical entity.
    public sealed record AttachPartition(Partition Partition) : AlterTableOperation;

    /// <summary>
    /// <c>DETACH PART|PARTITION &lt;partition_expr&gt;</c>
    /// Note: this is a ClickHouse-specific operation, please refer to
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/partition#detach-partitionpart">ClickHouse</see>
    /// </summary>
    /// <param name="Partition">Partition expression to detach.</param>
    // See `AttachPartition` for more details
    public sealed record DetachPartition(Partition Partition) : AlterTableOperation;

    /// <summary>
    /// <c>FREEZE PARTITION &lt;partition_expr&gt;</c>
    /// Note: this is a ClickHouse-specific operation, please refer to
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/partition#freeze-partition">ClickHouse</see>
    /// </summary>
    /// <param name="Partition">Partition to freeze.</param>
    /// <param name="WithName">Optional name for the freeze operation.</param>
    public sealed record FreezePartition(Partition Partition, Ident? WithName) : AlterTableOperation;

    /// <summary>
    /// <c>UNFREEZE PARTITION &lt;partition_expr&gt;</c>
    /// Note: this is a ClickHouse-specific operation, please refer to
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/partition#unfreeze-partition">ClickHouse</see>
    /// </summary>
    /// <param name="Partition">Partition to unfreeze.</param>
    /// <param name="WithName">Optional name associated with the unfreeze operation.</param>
    public sealed record UnfreezePartition(Partition Partition, Ident? WithName) : AlterTableOperation;

    /// <summary>
    /// <c>DROP PRIMARY KEY</c>
    /// <para>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/alter-table.html">MySQL</see>
    /// <see href="https://docs.snowflake.com/en/sql-reference/constraints-drop">Snowflake</see>
    /// </para>
    /// </summary>
    /// <param name="DropBehavior">Optional drop behavior for the primary key (<c>CASCADE</c>/<c>RESTRICT</c>).</param>
    public sealed record DropPrimaryKey(DropBehavior? DropBehavior) : AlterTableOperation;

    /// <summary>
    /// <c>DROP FOREIGN KEY &lt;fk_symbol&gt;</c>
    /// <para>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/alter-table.html">MySQL</see>
    /// <see href="https://docs.snowflake.com/en/sql-reference/constraints-drop">Snowflake</see>
    /// </para>
    /// </summary>
    /// <param name="Name">Foreign key symbol/name to drop.</param>
    /// <param name="DropBehavior">Optional drop behavior for the foreign key.</param>
    public sealed record DropForeignKey(Ident Name, DropBehavior? DropBehavior) : AlterTableOperation;

    /// <summary>
    /// <c>DROP INDEX &lt;index_name&gt;</c>
    /// <para><see href="https://dev.mysql.com/doc/refman/8.4/en/alter-table.html">MySQL</see></para>
    /// </summary>
    /// <param name="Name">Name of the index to drop.</param>
    public sealed record DropIndex(Ident Name) : AlterTableOperation;

    /// <summary>
    /// <c>ENABLE ALWAYS RULE rewrite_rule_name</c>
    /// <para>Note: this is a PostgreSQL-specific operation.</para>
    /// </summary>
    /// <param name="Name">Name of the rule to enable.</param>
    public sealed record EnableAlwaysRule(Ident Name) : AlterTableOperation;

    /// <summary>
    /// <c>ENABLE ALWAYS TRIGGER trigger_name</c>
    /// <para>Note: this is a PostgreSQL-specific operation.</para>
    /// </summary>
    /// <param name="Name">Name of the trigger to enable.</param>
    public sealed record EnableAlwaysTrigger(Ident Name) : AlterTableOperation;

    /// <summary>
    /// <c>ENABLE REPLICA RULE rewrite_rule_name</c>
    /// <para>Note: this is a PostgreSQL-specific operation.</para>
    /// </summary>
    /// <param name="Name">Name of the replica rule to enable.</param>
    public sealed record EnableReplicaRule(Ident Name) : AlterTableOperation;

    /// <summary>
    /// <c>ENABLE REPLICA TRIGGER trigger_name</c>
    /// <para>Note: this is a PostgreSQL-specific operation.</para>
    /// </summary>
    /// <param name="Name">Name of the replica trigger to enable.</param>
    public sealed record EnableReplicaTrigger(Ident Name) : AlterTableOperation;

    /// <summary>
    /// <c>ENABLE ROW LEVEL SECURITY</c>
    /// <para>
    /// Note: this is a PostgreSQL-specific operation.
    /// Please refer to <see href="https://www.postgresql.org/docs/current/sql-altertable.html">PostgreSQL documentation</see>
    /// </para>
    /// </summary>
    public sealed record EnableRowLevelSecurity : AlterTableOperation;

    /// <summary>
    /// <c>FORCE ROW LEVEL SECURITY</c>
    /// <para>
    /// Note: this is a PostgreSQL-specific operation.
    /// Please refer to <see href="https://www.postgresql.org/docs/current/sql-altertable.html">PostgreSQL documentation</see>
    /// </para>
    /// </summary>
    public sealed record ForceRowLevelSecurity : AlterTableOperation;

    /// <summary>
    /// <c>NO FORCE ROW LEVEL SECURITY</c>
    /// <para>
    /// Note: this is a PostgreSQL-specific operation.
    /// Please refer to <see href="https://www.postgresql.org/docs/current/sql-altertable.html">PostgreSQL documentation</see>
    /// </para>
    /// </summary>
    public sealed record NoForceRowLevelSecurity : AlterTableOperation;

    /// <summary>
    /// <c>ENABLE RULE rewrite_rule_name</c>
    /// <para>Note: this is a PostgreSQL-specific operation.</para>
    /// </summary>
    /// <param name="Name">Name of the rule to enable.</param>
    public sealed record EnableRule(Ident Name) : AlterTableOperation;

    /// <summary>
    /// <c>ENABLE TRIGGER [ trigger_name | ALL | USER ]</c>
    /// <para>Note: this is a PostgreSQL-specific operation.</para>
    /// </summary>
    /// <param name="Name">Name of the trigger to enable (or ALL/USER).</param>
    public sealed record EnableTrigger(Ident Name) : AlterTableOperation;

    /// <summary><c>RENAME TO PARTITION (partition=val)</c></summary>
    /// <param name="OldPartitions">Old partition expressions to be renamed.</param>
    /// <param name="NewPartitions">New partition expressions corresponding to the old ones.</param>
    public sealed record RenamePartitions(Sequence<Expr> OldPartitions, Sequence<Expr> NewPartitions) : AlterTableOperation;

    /// <summary>
    /// REPLICA IDENTITY { DEFAULT | USING INDEX index_name | FULL | NOTHING }
    /// <para>
    /// Note: this is a PostgreSQL-specific operation.
    /// Please refer to <see href="https://www.postgresql.org/docs/current/sql-altertable.html">PostgreSQL documentation</see>
    /// </para>
    /// </summary>
    /// <param name="Identity">Replica identity setting to apply.</param>
    // A variante esconde o tipo de topo homônimo: tipo qualificado (convencoes.md §13, caso 38).
    public sealed record ReplicaIdentity(Ast.ReplicaIdentity Identity) : AlterTableOperation;

    /// <summary>Add Partitions</summary>
    /// <param name="IfNotExists">Whether <c>IF NOT EXISTS</c> was present when adding partitions.</param>
    /// <param name="NewPartitions">New partitions to add.</param>
    public sealed record AddPartitions(bool IfNotExists, Sequence<Partition> NewPartitions) : AlterTableOperation;

    /// <summary><c>DROP PARTITIONS ...</c> / drop partitions from the table.</summary>
    /// <param name="Partitions">Partitions to drop (expressions).</param>
    /// <param name="IfExists">Whether <c>IF EXISTS</c> was specified for dropping partitions.</param>
    public sealed record DropPartitions(Sequence<Expr> Partitions, bool IfExists) : AlterTableOperation;

    /// <summary><c>RENAME [ COLUMN ] &lt;old_column_name&gt; TO &lt;new_column_name&gt;</c></summary>
    /// <param name="OldColumnName">Existing column name to rename.</param>
    /// <param name="NewColumnName">New column name.</param>
    public sealed record RenameColumn(Ident OldColumnName, Ident NewColumnName) : AlterTableOperation;

    /// <summary><c>RENAME TO &lt;table_name&gt;</c></summary>
    /// <param name="TableName">The new table name or renaming kind.</param>
    public sealed record RenameTable(RenameTableNameKind TableName) : AlterTableOperation;

    // CHANGE [ COLUMN ] <old_name> <new_name> <data_type> [ <options> ]
    /// <summary>Change an existing column's name, type, and options.</summary>
    /// <param name="OldName">Old column name.</param>
    /// <param name="NewName">New column name.</param>
    /// <param name="DataType">New data type for the column.</param>
    /// <param name="Options">Column options to apply after the change.</param>
    /// <param name="ColumnPosition">MySQL-specific column position (<c>FIRST</c>/<c>AFTER</c>).</param>
    public sealed record ChangeColumn(
        Ident OldName,
        Ident NewName,
        DataType DataType,
        Sequence<ColumnOption> Options,
        MySQLColumnPosition? ColumnPosition) : AlterTableOperation;

    // CHANGE [ COLUMN ] <col_name> <data_type> [ <options> ]
    /// <summary>Modify an existing column's type and options.</summary>
    /// <param name="ColName">Column name to modify.</param>
    /// <param name="DataType">New data type for the column.</param>
    /// <param name="Options">Column options to set.</param>
    /// <param name="ColumnPosition">MySQL-specific column position (<c>FIRST</c>/<c>AFTER</c>).</param>
    public sealed record ModifyColumn(
        Ident ColName,
        DataType DataType,
        Sequence<ColumnOption> Options,
        MySQLColumnPosition? ColumnPosition) : AlterTableOperation;

    /// <summary>
    /// <c>MODIFY ORDER BY &lt;expr&gt;</c>
    /// <para>
    /// Note: this is a ClickHouse-specific operation.
    /// Please refer to <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/order-by">ClickHouse</see>
    /// </para>
    /// </summary>
    /// <param name="OrderBy">The new sorting key.</param>
    public sealed record ModifyOrderBy(OneOrManyWithParens<Expr> OrderBy) : AlterTableOperation;

    /// <summary>
    /// <c>RENAME CONSTRAINT &lt;old_constraint_name&gt; TO &lt;new_constraint_name&gt;</c>
    /// <para>
    /// Note: this is a PostgreSQL-specific operation.
    /// Rename a constraint on the table.
    /// </para>
    /// </summary>
    /// <param name="OldName">Existing constraint name.</param>
    /// <param name="NewName">New constraint name.</param>
    public sealed record RenameConstraint(Ident OldName, Ident NewName) : AlterTableOperation;

    /// <summary>
    /// <c>ALTER [ COLUMN ]</c>
    /// Alter a specific column with the provided operation.
    /// </summary>
    /// <param name="ColumnName">The column to alter.</param>
    /// <param name="Op">Operation to apply to the column.</param>
    public sealed record AlterColumn(Ident ColumnName, AlterColumnOperation Op) : AlterTableOperation;

    /// <summary>
    /// 'SWAP WITH &lt;table_name&gt;'
    /// <para>Note: this is Snowflake specific <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-table"/></para>
    /// </summary>
    /// <param name="TableName">Table name to swap with.</param>
    public sealed record SwapWith(ObjectName TableName) : AlterTableOperation;

    /// <summary>'SET TBLPROPERTIES ( { property_key [ = ] property_val } [, ...] )'</summary>
    /// <param name="TableProperties">Table properties specified as SQL options.</param>
    public sealed record SetTblProperties(Sequence<SqlOption> TableProperties) : AlterTableOperation;

    /// <summary>
    /// <c>SET LOGGED</c>
    /// <para>Note: this is PostgreSQL-specific.</para>
    /// </summary>
    public sealed record SetLogged : AlterTableOperation;

    /// <summary>
    /// <c>SET UNLOGGED</c>
    /// <para>Note: this is PostgreSQL-specific.</para>
    /// </summary>
    public sealed record SetUnlogged : AlterTableOperation;

    /// <summary>
    /// <c>OWNER TO { &lt;new_owner&gt; | CURRENT_ROLE | CURRENT_USER | SESSION_USER }</c>
    /// <para>Note: this is PostgreSQL-specific <see href="https://www.postgresql.org/docs/current/sql-altertable.html"/></para>
    /// </summary>
    /// <param name="NewOwner">The new owner to assign to the table.</param>
    public sealed record OwnerTo(Owner NewOwner) : AlterTableOperation;

    /// <summary>
    /// Snowflake table clustering options
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-table#clustering-actions-clusteringaction"/>
    /// </summary>
    /// <param name="Exprs">Expressions used for clustering the table.</param>
    public sealed record ClusterBy(Sequence<Expr> Exprs) : AlterTableOperation;

    /// <summary>Remove the clustering key from the table.</summary>
    public sealed record DropClusteringKey : AlterTableOperation;

    /// <summary>
    /// Redshift <c>ALTER SORTKEY (column_list)</c>
    /// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_ALTER_TABLE.html"/>
    /// </summary>
    /// <param name="Columns">Column references in the sort key.</param>
    public sealed record AlterSortKey(Sequence<Expr> Columns) : AlterTableOperation;

    /// <summary>Suspend background reclustering operations.</summary>
    public sealed record SuspendRecluster : AlterTableOperation;

    /// <summary>Resume background reclustering operations.</summary>
    public sealed record ResumeRecluster : AlterTableOperation;

    /// <summary>
    /// <c>REFRESH [ '&lt;subpath&gt;' ]</c>
    /// <para>
    /// Note: this is Snowflake specific for dynamic/external tables
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-dynamic-table"/>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-external-table"/>
    /// </para>
    /// </summary>
    /// <param name="Subpath">Optional subpath for external table refresh</param>
    public sealed record Refresh(string? Subpath) : AlterTableOperation;

    /// <summary>
    /// <c>SUSPEND</c>
    /// <para>Note: this is Snowflake specific for dynamic tables <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-table"/></para>
    /// </summary>
    public sealed record Suspend : AlterTableOperation;

    /// <summary>
    /// <c>RESUME</c>
    /// <para>Note: this is Snowflake specific for dynamic tables <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-table"/></para>
    /// </summary>
    public sealed record Resume : AlterTableOperation;

    /// <summary>
    /// <c>ALGORITHM [=] { DEFAULT | INSTANT | INPLACE | COPY }</c>
    /// <para><see href="https://dev.mysql.com/doc/refman/8.4/en/alter-table.html">MySQL</see>-specific table alter algorithm.</para>
    /// </summary>
    /// <param name="EqualsValue">Whether the <c>=</c> sign was used (<c>ALGORITHM = ...</c>).</param>
    /// <param name="AlgorithmValue">The algorithm to use for the alter operation (MySQL-specific).</param>
    // Rust: campos `equals` e `algorithm`. `Equals` colidiria com os métodos Equals do record (CS0102) e
    // `Algorithm` com o nome do próprio record (CS0542): sufixo `Value` (convencoes.md §13, caso 41).
    public sealed record Algorithm(bool EqualsValue, AlterTableAlgorithm AlgorithmValue) : AlterTableOperation;

    /// <summary>
    /// <c>LOCK [=] { DEFAULT | NONE | SHARED | EXCLUSIVE }</c>
    /// <para><see href="https://dev.mysql.com/doc/refman/8.4/en/alter-table.html">MySQL</see>-specific table alter lock.</para>
    /// </summary>
    /// <param name="EqualsValue">Whether the <c>=</c> sign was used (<c>LOCK = ...</c>).</param>
    /// <param name="LockValue">The locking behavior to apply (MySQL-specific).</param>
    // Rust: campos `equals` e `lock` (CS0102/CS0542: sufixo `Value`, como em `Algorithm`).
    public sealed record Lock(bool EqualsValue, AlterTableLock LockValue) : AlterTableOperation;

    /// <summary>
    /// <c>AUTO_INCREMENT [=] &lt;value&gt;</c>
    /// <para><see href="https://dev.mysql.com/doc/refman/8.4/en/alter-table.html">MySQL</see>-specific table option for raising current auto increment value.</para>
    /// </summary>
    /// <param name="EqualsValue">Whether the <c>=</c> sign was used (<c>AUTO_INCREMENT = ...</c>).</param>
    /// <param name="Value">Value to set for the auto-increment counter.</param>
    // Rust: campo `equals` (CS0102: sufixo `Value`, como em `Algorithm`).
    public sealed record AutoIncrement(bool EqualsValue, ValueWithSpan Value) : AlterTableOperation;

    /// <summary><c>VALIDATE CONSTRAINT &lt;name&gt;</c></summary>
    /// <param name="Name">Name of the constraint to validate.</param>
    public sealed record ValidateConstraint(Ident Name) : AlterTableOperation;

    /// <summary>
    /// Arbitrary parenthesized <c>SET</c> options.
    /// <para>Example:</para>
    /// <code>SET (scale_factor = 0.01, threshold = 500)`</code>
    /// <see href="https://www.postgresql.org/docs/current/sql-altertable.html">PostgreSQL</see>
    /// </summary>
    /// <param name="Options">Parenthesized options supplied to <c>SET (...)</c>.</param>
    public sealed record SetOptionsParens(Sequence<SqlOption> Options) : AlterTableOperation;

    // Rust: impl fmt::Display for AlterTableOperation (declarado depois de AlterIndexOperation em ddl.rs).
    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case AddPartitions op:
                f.Write($"ADD{(op.IfNotExists ? " IF NOT EXISTS" : "")} {DisplaySeparated(op.NewPartitions, " ")}");
                break;
            case AddConstraint op:
                f.Write($"ADD {op.Constraint}");
                if (op.NotValid)
                {
                    f.Write($" NOT VALID");
                }
                break;
            case AddColumn op:
            {
                f.Write($"ADD");
                if (op.ColumnKeyword)
                {
                    f.Write($" COLUMN");
                }
                if (op.IfNotExists)
                {
                    f.Write($" IF NOT EXISTS");
                }
                f.Write($" {op.ColumnDef}");

                if (op.ColumnPosition is { } position)
                {
                    f.Write($" {position}");
                }
                break;
            }
            case AddProjection op:
                f.Write($"ADD PROJECTION");
                if (op.IfNotExists)
                {
                    f.Write($" IF NOT EXISTS");
                }
                f.Write($" {op.Name} ({op.Select})");
                break;
            case Algorithm op:
                f.Write($"ALGORITHM {(op.EqualsValue ? "= " : "")}{op.AlgorithmValue}");
                break;
            case DropProjection op:
                f.Write($"DROP PROJECTION");
                if (op.IfExists)
                {
                    f.Write($" IF EXISTS");
                }
                f.Write($" {op.Name}");
                break;
            case MaterializeProjection op:
            {
                f.Write($"MATERIALIZE PROJECTION");
                if (op.IfExists)
                {
                    f.Write($" IF EXISTS");
                }
                f.Write($" {op.Name}");
                if (op.Partition is { } partition)
                {
                    f.Write($" IN PARTITION {partition}");
                }
                break;
            }
            case ClearProjection op:
            {
                f.Write($"CLEAR PROJECTION");
                if (op.IfExists)
                {
                    f.Write($" IF EXISTS");
                }
                f.Write($" {op.Name}");
                if (op.Partition is { } partition)
                {
                    f.Write($" IN PARTITION {partition}");
                }
                break;
            }
            case AlterColumn op:
                f.Write($"ALTER COLUMN {op.ColumnName} {op.Op}");
                break;
            case DisableRowLevelSecurity:
                f.Write($"DISABLE ROW LEVEL SECURITY");
                break;
            case DisableRule op:
                f.Write($"DISABLE RULE {op.Name}");
                break;
            case DisableTrigger op:
                f.Write($"DISABLE TRIGGER {op.Name}");
                break;
            case DropPartitions op:
                f.Write($"DROP{(op.IfExists ? " IF EXISTS" : "")} PARTITION ({DisplayCommaSeparated(op.Partitions)})");
                break;
            case DropConstraint op:
            {
                f.Write($"DROP CONSTRAINT {(op.IfExists ? "IF EXISTS " : "")}{op.Name}");
                if (op.DropBehavior is { } dropBehavior)
                {
                    f.Write($" {dropBehavior}");
                }
                break;
            }
            case DropPrimaryKey op:
            {
                f.Write($"DROP PRIMARY KEY");
                if (op.DropBehavior is { } dropBehavior)
                {
                    f.Write($" {dropBehavior}");
                }
                break;
            }
            case DropForeignKey op:
            {
                f.Write($"DROP FOREIGN KEY {op.Name}");
                if (op.DropBehavior is { } dropBehavior)
                {
                    f.Write($" {dropBehavior}");
                }
                break;
            }
            case DropIndex op:
                f.Write($"DROP INDEX {op.Name}");
                break;
            case DropColumn op:
            {
                f.Write($"DROP {(op.HasColumnKeyword ? "COLUMN " : "")}{(op.IfExists ? "IF EXISTS " : "")}{DisplayCommaSeparated(op.ColumnNames)}");
                if (op.DropBehavior is { } dropBehavior)
                {
                    f.Write($" {dropBehavior}");
                }
                break;
            }
            case AttachPartition op:
                f.Write($"ATTACH {op.Partition}");
                break;
            case DetachPartition op:
                f.Write($"DETACH {op.Partition}");
                break;
            case EnableAlwaysRule op:
                f.Write($"ENABLE ALWAYS RULE {op.Name}");
                break;
            case EnableAlwaysTrigger op:
                f.Write($"ENABLE ALWAYS TRIGGER {op.Name}");
                break;
            case EnableReplicaRule op:
                f.Write($"ENABLE REPLICA RULE {op.Name}");
                break;
            case EnableReplicaTrigger op:
                f.Write($"ENABLE REPLICA TRIGGER {op.Name}");
                break;
            case EnableRowLevelSecurity:
                f.Write($"ENABLE ROW LEVEL SECURITY");
                break;
            case ForceRowLevelSecurity:
                f.Write($"FORCE ROW LEVEL SECURITY");
                break;
            case NoForceRowLevelSecurity:
                f.Write($"NO FORCE ROW LEVEL SECURITY");
                break;
            case EnableRule op:
                f.Write($"ENABLE RULE {op.Name}");
                break;
            case EnableTrigger op:
                f.Write($"ENABLE TRIGGER {op.Name}");
                break;
            case RenamePartitions op:
                f.Write($"PARTITION ({DisplayCommaSeparated(op.OldPartitions)}) RENAME TO PARTITION ({DisplayCommaSeparated(op.NewPartitions)})");
                break;
            case RenameColumn op:
                f.Write($"RENAME COLUMN {op.OldColumnName} TO {op.NewColumnName}");
                break;
            case RenameTable op:
                f.Write($"RENAME {op.TableName}");
                break;
            case ChangeColumn op:
            {
                f.Write($"CHANGE COLUMN {op.OldName} {op.NewName} {op.DataType}");
                if (!op.Options.IsEmpty)
                {
                    f.Write($" {DisplaySeparated(op.Options, " ")}");
                }
                if (op.ColumnPosition is { } position)
                {
                    f.Write($" {position}");
                }

                break;
            }
            case ModifyColumn op:
            {
                f.Write($"MODIFY COLUMN {op.ColName} {op.DataType}");
                if (!op.Options.IsEmpty)
                {
                    f.Write($" {DisplaySeparated(op.Options, " ")}");
                }
                if (op.ColumnPosition is { } position)
                {
                    f.Write($" {position}");
                }

                break;
            }
            case ModifyOrderBy op:
                f.Write($"MODIFY ORDER BY {op.OrderBy}");
                break;
            case RenameConstraint op:
                f.Write($"RENAME CONSTRAINT {op.OldName} TO {op.NewName}");
                break;
            case SwapWith op:
                f.Write($"SWAP WITH {op.TableName}");
                break;
            case OwnerTo op:
                f.Write($"OWNER TO {op.NewOwner}");
                break;
            case SetTblProperties op:
                f.Write($"SET TBLPROPERTIES({DisplayCommaSeparated(op.TableProperties)})");
                break;
            case SetLogged:
                f.Write($"SET LOGGED");
                break;
            case SetUnlogged:
                f.Write($"SET UNLOGGED");
                break;
            case FreezePartition op:
            {
                f.Write($"FREEZE {op.Partition}");
                if (op.WithName is { } name)
                {
                    f.Write($" WITH NAME {name}");
                }
                break;
            }
            case UnfreezePartition op:
            {
                f.Write($"UNFREEZE {op.Partition}");
                if (op.WithName is { } name)
                {
                    f.Write($" WITH NAME {name}");
                }
                break;
            }
            case ClusterBy op:
                f.Write($"CLUSTER BY ({DisplayCommaSeparated(op.Exprs)})");
                break;
            case DropClusteringKey:
                f.Write($"DROP CLUSTERING KEY");
                break;
            case AlterSortKey op:
                f.Write($"ALTER SORTKEY({DisplayCommaSeparated(op.Columns)})");
                break;
            case SuspendRecluster:
                f.Write($"SUSPEND RECLUSTER");
                break;
            case ResumeRecluster:
                f.Write($"RESUME RECLUSTER");
                break;
            case Refresh op:
            {
                f.Write($"REFRESH");
                if (op.Subpath is { } path)
                {
                    f.Write($" '{path}'");
                }
                break;
            }
            case Suspend:
                f.Write($"SUSPEND");
                break;
            case Resume:
                f.Write($"RESUME");
                break;
            case AutoIncrement op:
                f.Write($"AUTO_INCREMENT {(op.EqualsValue ? "= " : "")}{op.Value}");
                break;
            case Lock op:
                f.Write($"LOCK {(op.EqualsValue ? "= " : "")}{op.LockValue}");
                break;
            case ReplicaIdentity op:
                f.Write($"REPLICA IDENTITY {op.Identity}");
                break;
            case ValidateConstraint op:
                f.Write($"VALIDATE CONSTRAINT {op.Name}");
                break;
            case SetOptionsParens op:
                f.Write($"SET ({DisplayCommaSeparated(op.Options)})");
                break;
            default:
                throw new InvalidOperationException($"Variante de AlterTableOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
