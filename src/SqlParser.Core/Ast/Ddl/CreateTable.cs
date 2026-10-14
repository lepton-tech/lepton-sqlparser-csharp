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

/// <summary>CREATE TABLE statement.</summary>
/// <param name="OrReplace"><c>OR REPLACE</c> clause</param>
/// <param name="Temporary"><c>TEMP</c> or <c>TEMPORARY</c> clause</param>
/// <param name="Unlogged"><c>UNLOGGED</c> clause</param>
/// <param name="External"><c>EXTERNAL</c> clause</param>
/// <param name="Dynamic"><c>DYNAMIC</c> clause</param>
/// <param name="Global"><c>GLOBAL</c> clause</param>
/// <param name="IfNotExists"><c>IF NOT EXISTS</c> clause</param>
/// <param name="Transient"><c>TRANSIENT</c> clause</param>
/// <param name="Volatile"><c>VOLATILE</c> clause</param>
/// <param name="Iceberg"><c>ICEBERG</c> clause</param>
/// <param name="Snapshot">
/// <c>SNAPSHOT</c> clause
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_snapshot_table_statement"/>
/// </param>
/// <param name="Name">Table name</param>
/// <param name="Columns">Column definitions</param>
/// <param name="Constraints">Table constraints</param>
/// <param name="HiveDistribution">Hive-specific distribution style</param>
/// <param name="HiveFormats">
/// Hive-specific formats like <c>ROW FORMAT DELIMITED</c> or
/// <c>ROW FORMAT SERDE 'serde_class' WITH SERDEPROPERTIES (...)</c>
/// </param>
/// <param name="TableOptions">Table options</param>
/// <param name="FileFormat">General comment for the table</param>
/// <param name="Location">Location of the table data</param>
/// <param name="Query">Query used to populate the table</param>
/// <param name="WithoutRowid">If the table should be created without a rowid (SQLite)</param>
/// <param name="Like"><c>LIKE</c> clause</param>
/// <param name="CloneValue">
/// <c>CLONE</c> clause
/// <para>Rust: campo <c>clone</c>. Renomeado porque records não aceitam membro chamado <c>Clone</c> (CS8859).</para>
/// </param>
/// <param name="Version">Table version (for systems that support versioned tables)</param>
/// <param name="Comment">
/// For Hive dialect, the table comment is after the column definitions without <c>=</c>,
/// so the <c>comment</c> field is optional and different than the comment field in the general options list.
/// <see href="https://cwiki.apache.org/confluence/display/Hive/LanguageManual+DDL#LanguageManualDDL-CreateTable">Hive</see>
/// </param>
/// <param name="OnCommit">
/// ClickHouse "ON COMMIT" clause:
/// <see href="https://clickhouse.com/docs/en/sql-reference/statements/create/table/"/>
/// </param>
/// <param name="OnCluster">
/// ClickHouse "ON CLUSTER" clause:
/// <see href="https://clickhouse.com/docs/en/sql-reference/distributed-ddl/"/>
/// </param>
/// <param name="PrimaryKey">
/// ClickHouse "PRIMARY KEY " clause.
/// <see href="https://clickhouse.com/docs/en/sql-reference/statements/create/table/"/>
/// </param>
/// <param name="OrderBy">
/// ClickHouse "ORDER BY " clause. Note that omitted ORDER BY is different
/// than empty (represented as ()), the latter meaning "no sorting".
/// <see href="https://clickhouse.com/docs/en/sql-reference/statements/create/table/"/>
/// </param>
/// <param name="PartitionBy">
/// BigQuery: A partition expression for the table.
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#partition_expression"/>
/// </param>
/// <param name="ClusterBy">
/// BigQuery: Table clustering column list.
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#table_option_list"/>
/// Snowflake: Table clustering list which contains base column, expressions on base columns.
/// <see href="https://docs.snowflake.com/en/user-guide/tables-clustering-keys#defining-a-clustering-key-for-a-table"/>
/// </param>
/// <param name="ClusteredBy">
/// Hive: Table clustering column list.
/// <see href="https://cwiki.apache.org/confluence/display/Hive/LanguageManual+DDL#LanguageManualDDL-CreateTable"/>
/// </param>
/// <param name="Inherits">
/// Postgres <c>INHERITs</c> clause, which contains the list of tables from which
/// the new table inherits.
/// <see href="https://www.postgresql.org/docs/current/ddl-inherit.html"/>
/// <see href="https://www.postgresql.org/docs/current/sql-createtable.html#SQL-CREATETABLE-PARMS-INHERITS"/>
/// </param>
/// <param name="PartitionOf">
/// PostgreSQL <c>PARTITION OF</c> clause to create a partition of a parent table.
/// Contains the parent table name.
/// <see href="https://www.postgresql.org/docs/current/sql-createtable.html"/>
/// </param>
/// <param name="ForValues">
/// PostgreSQL partition bound specification for PARTITION OF.
/// <see href="https://www.postgresql.org/docs/current/sql-createtable.html"/>
/// </param>
/// <param name="Strict">
/// SQLite "STRICT" clause.
/// if the "STRICT" table-option keyword is added to the end, after the closing ")",
/// then strict typing rules apply to that table.
/// </param>
/// <param name="CopyGrants">
/// Snowflake "COPY GRANTS" clause
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
/// </param>
/// <param name="EnableSchemaEvolution">
/// Snowflake "ENABLE_SCHEMA_EVOLUTION" clause
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
/// </param>
/// <param name="ChangeTracking">
/// Snowflake "CHANGE_TRACKING" clause
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
/// </param>
/// <param name="DataRetentionTimeInDays">
/// Snowflake "DATA_RETENTION_TIME_IN_DAYS" clause
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
/// </param>
/// <param name="MaxDataExtensionTimeInDays">
/// Snowflake "MAX_DATA_EXTENSION_TIME_IN_DAYS" clause
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
/// </param>
/// <param name="DefaultDdlCollation">
/// Snowflake "DEFAULT_DDL_COLLATION" clause
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
/// </param>
/// <param name="WithAggregationPolicy">
/// Snowflake "WITH AGGREGATION POLICY" clause
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
/// </param>
/// <param name="WithRowAccessPolicy">
/// Snowflake "WITH ROW ACCESS POLICY" clause
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
/// </param>
/// <param name="WithStorageLifecyclePolicy">
/// Snowflake <c>WITH STORAGE LIFECYCLE POLICY</c> clause
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
/// </param>
/// <param name="WithTags">
/// Snowflake "WITH TAG" clause
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
/// </param>
/// <param name="ExternalVolume">
/// Snowflake "EXTERNAL_VOLUME" clause for Iceberg tables
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-iceberg-table"/>
/// </param>
/// <param name="WithConnection">
/// <c>WITH CONNECTION</c> clause.
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_external_table_statement">BigQuery</see>
/// </param>
/// <param name="BaseLocation">
/// Snowflake "BASE_LOCATION" clause for Iceberg tables
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-iceberg-table"/>
/// </param>
/// <param name="Catalog">
/// Snowflake "CATALOG" clause for Iceberg tables
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-iceberg-table"/>
/// </param>
/// <param name="CatalogSync">
/// Snowflake "CATALOG_SYNC" clause for Iceberg tables
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-iceberg-table"/>
/// </param>
/// <param name="StorageSerializationPolicy">
/// Snowflake "STORAGE_SERIALIZATION_POLICY" clause for Iceberg tables
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-iceberg-table"/>
/// </param>
/// <param name="TargetLag">
/// Snowflake "TARGET_LAG" clause for dybamic tables
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-dynamic-table"/>
/// </param>
/// <param name="Warehouse">
/// Snowflake "WAREHOUSE" clause for dybamic tables
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-dynamic-table"/>
/// </param>
/// <param name="RefreshMode">
/// Snowflake "REFRESH_MODE" clause for dybamic tables
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-dynamic-table"/>
/// </param>
/// <param name="Initialize">
/// Snowflake "INITIALIZE" clause for dybamic tables
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-dynamic-table"/>
/// </param>
/// <param name="RequireUser">
/// Snowflake "REQUIRE USER" clause for dybamic tables
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-dynamic-table"/>
/// </param>
/// <param name="Diststyle">
/// Redshift <c>DISTSTYLE</c> option
/// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_CREATE_TABLE_NEW.html"/>
/// </param>
/// <param name="Distkey">
/// Redshift <c>DISTKEY</c> option
/// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_CREATE_TABLE_NEW.html"/>
/// </param>
/// <param name="Sortkey">
/// Redshift <c>SORTKEY</c> option
/// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_CREATE_TABLE_NEW.html"/>
/// </param>
/// <param name="Backup">
/// Redshift <c>BACKUP</c> option: <c>BACKUP { YES | NO }</c>
/// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_CREATE_TABLE_NEW.html"/>
/// </param>
/// <param name="Multiset">
/// <c>MULTISET | SET</c> table-kind prefix.
/// <c>Some(true)</c> =&gt; <c>MULTISET</c>, <c>Some(false)</c> =&gt; <c>SET</c>.
/// <para><see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Definition-Language-Syntax-and-Examples/Table-Statements/CREATE-TABLE-and-CREATE-TABLE-AS/Syntax-Elements/MULTISET-or-SET">Teradata</see></para>
/// </param>
/// <param name="Fallback">
/// <c>FALLBACK</c> clause.
/// <c>Some(true)</c> =&gt; <c>FALLBACK</c>, <c>Some(false)</c> =&gt; <c>NO FALLBACK</c>
/// <para><see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Definition-Language-Syntax-and-Examples/Table-Statements/CREATE-TABLE-and-CREATE-TABLE-AS/Syntax-Elements/FALLBACK-or-NO-FALLBACK">Teradata</see></para>
/// </param>
/// <param name="WithData">
/// <c>WITH DATA</c> clause on a <c>CREATE TABLE ... AS</c> statement.
/// <para><see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Definition-Language-Syntax-and-Examples/Table-Statements/CREATE-TABLE-and-CREATE-TABLE-AS/Syntax-Elements/AS_clause/WITH-Clause-Phrase">Teradata</see></para>
/// </param>
/// <remarks>
/// O builder (<c>CreateTableBuilder</c>, <c>ast/helpers/stmt_create_table.rs</c>) é portado em outro arquivo.
/// </remarks>
public sealed partial record CreateTable(
    bool OrReplace,
    bool Temporary,
    bool Unlogged,
    bool External,
    bool Dynamic,
    bool? Global,
    bool IfNotExists,
    bool Transient,
    bool Volatile,
    bool Iceberg,
    bool Snapshot,
    [property: VisitWith("visit_relation")] ObjectName Name,
    Sequence<ColumnDef> Columns,
    Sequence<TableConstraint> Constraints,
    HiveDistributionStyle HiveDistribution,
    HiveFormat? HiveFormats,
    CreateTableOptions TableOptions,
    FileFormat? FileFormat,
    string? Location,
    Query? Query,
    bool WithoutRowid,
    CreateTableLikeKind? Like,
    ObjectName? CloneValue,
    TableVersion? Version,
    CommentDef? Comment,
    OnCommit? OnCommit,
    Ident? OnCluster,
    Expr? PrimaryKey,
    OneOrManyWithParens<Expr>? OrderBy,
    Expr? PartitionBy,
    WrappedCollection<Sequence<Expr>>? ClusterBy,
    ClusteredBy? ClusteredBy,
    Sequence<ObjectName>? Inherits,
    [property: VisitWith("visit_relation")] ObjectName? PartitionOf,
    ForValues? ForValues,
    bool Strict,
    bool CopyGrants,
    bool? EnableSchemaEvolution,
    bool? ChangeTracking,
    ulong? DataRetentionTimeInDays,
    ulong? MaxDataExtensionTimeInDays,
    string? DefaultDdlCollation,
    ObjectName? WithAggregationPolicy,
    RowAccessPolicy? WithRowAccessPolicy,
    StorageLifecyclePolicy? WithStorageLifecyclePolicy,
    Sequence<Tag>? WithTags,
    string? ExternalVolume,
    ObjectName? WithConnection,
    string? BaseLocation,
    string? Catalog,
    string? CatalogSync,
    StorageSerializationPolicy? StorageSerializationPolicy,
    string? TargetLag,
    Ident? Warehouse,
    RefreshModeKind? RefreshMode,
    InitializeKind? Initialize,
    bool RequireUser,
    DistStyle? Diststyle,
    Expr? Distkey,
    Sequence<Expr>? Sortkey,
    bool? Backup,
    bool? Multiset,
    bool? Fallback,
    WithData? WithData) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // We want to allow the following options
        // Empty column list, allowed by PostgreSQL:
        //   `CREATE TABLE t ()`
        // No columns provided for CREATE TABLE AS:
        //   `CREATE TABLE t AS SELECT a from t2`
        // Columns provided for CREATE TABLE AS:
        //   `CREATE TABLE t (a INT) AS SELECT a from t2`
        var orReplace = OrReplace ? "OR REPLACE " : "";
        var external = External ? "EXTERNAL " : "";
        var snapshot = Snapshot ? "SNAPSHOT " : "";
        var global = Global switch
        {
            true => "GLOBAL ",
            false => "LOCAL ",
            null => "",
        };
        var ifNotExists = IfNotExists ? "IF NOT EXISTS " : "";
        var multiset = Multiset switch
        {
            true => "MULTISET ",
            false => "SET ",
            null => "",
        };
        var temporary = Temporary ? "TEMPORARY " : "";
        var unlogged = Unlogged ? "UNLOGGED " : "";
        var transient = Transient ? "TRANSIENT " : "";
        var @volatile = Volatile ? "VOLATILE " : "";
        var iceberg = Iceberg ? "ICEBERG " : "";
        var dynamic = Dynamic ? "DYNAMIC " : "";
        f.Write($"CREATE {orReplace}{external}{global}{multiset}{temporary}{unlogged}{transient}{@volatile}{dynamic}{iceberg}{snapshot}TABLE {ifNotExists}{Name}");
        if (Fallback is { } fallback)
        {
            f.Write($", {(fallback ? "FALLBACK" : "NO FALLBACK")}");
        }
        if (PartitionOf is { } partitionOf)
        {
            f.Write($" PARTITION OF {partitionOf}");
        }
        if (OnCluster is { } onCluster)
        {
            f.Write($" ON CLUSTER {onCluster}");
        }
        if (!Columns.IsEmpty || !Constraints.IsEmpty)
        {
            f.WriteStr(" (");
            NewLine.Instance.Fmt(f);
            new Indent<DisplayCommaSeparated<ColumnDef>>(new DisplayCommaSeparated<ColumnDef>(Columns)).Fmt(f);
            if (!Columns.IsEmpty && !Constraints.IsEmpty)
            {
                f.WriteStr(",");
                SpaceOrNewline.Instance.Fmt(f);
            }
            new Indent<DisplayCommaSeparated<TableConstraint>>(new DisplayCommaSeparated<TableConstraint>(Constraints)).Fmt(f);
            NewLine.Instance.Fmt(f);
            f.WriteStr(")");
        }
        else if (Query is null
            && Like is null
            && CloneValue is null
            && PartitionOf is null)
        {
            // PostgreSQL allows `CREATE TABLE t ();`, but requires empty parens
            f.WriteStr(" ()");
        }
        else if (Like is CreateTableLikeKind.Parenthesized { Value: var likeInColumnsList })
        {
            f.Write($" ({likeInColumnsList})");
        }
        if (ForValues is { } forValues)
        {
            f.Write($" {forValues}");
        }

        // Hive table comment should be after column definitions, please refer to:
        // [Hive](https://cwiki.apache.org/confluence/display/Hive/LanguageManual+DDL#LanguageManualDDL-CreateTable)
        if (Comment is { } comment)
        {
            f.Write($" COMMENT '{comment}'");
        }

        // Only for SQLite
        if (WithoutRowid)
        {
            f.Write($" WITHOUT ROWID");
        }

        if (Like is CreateTableLikeKind.Plain { Value: var like })
        {
            f.Write($" {like}");
        }

        if (CloneValue is { } c)
        {
            f.Write($" CLONE {c}");
        }

        if (Version is { } version)
        {
            f.Write($" {version}");
        }

        switch (HiveDistribution)
        {
            case HiveDistributionStyle.PARTITIONED { Columns: var columns }:
            {
                f.Write($" PARTITIONED BY ({DisplayCommaSeparated(columns)})");
                break;
            }
            case HiveDistributionStyle.SKEWED { Columns: var columns, On: var on, StoredAsDirectories: var storedAsDirectories }:
            {
                f.Write($" SKEWED BY ({DisplayCommaSeparated(columns)})) ON ({DisplayCommaSeparated(on)})");
                if (storedAsDirectories)
                {
                    f.Write($" STORED AS DIRECTORIES");
                }
                break;
            }
            default:
                break;
        }

        if (ClusteredBy is { } clusteredBy)
        {
            f.Write($" {clusteredBy}");
        }

        // Rust: if let Some(HiveFormat { row_format, serde_properties, storage, location }) = &self.hive_formats
        if (HiveFormats is { } hiveFormats)
        {
            switch (hiveFormats.RowFormat)
            {
                case HiveRowFormat.SERDE { Class: var @class }:
                {
                    f.Write($" ROW FORMAT SERDE '{@class}'");
                    break;
                }
                case HiveRowFormat.DELIMITED { Delimiters: var delimiters }:
                {
                    f.Write($" ROW FORMAT DELIMITED");
                    if (!delimiters.IsEmpty)
                    {
                        f.Write($" {DisplaySeparated(delimiters, " ")}");
                    }
                    break;
                }
                case null:
                    break;
                default:
                    throw new InvalidOperationException($"Variante de HiveRowFormat não tratada: {hiveFormats.RowFormat?.GetType().Name}");
            }
            switch (hiveFormats.Storage)
            {
                case HiveIOFormat.IOF { InputFormat: var inputFormat, OutputFormat: var outputFormat }:
                {
                    f.Write($" STORED AS INPUTFORMAT {inputFormat} OUTPUTFORMAT {outputFormat}");
                    break;
                }
                case HiveIOFormat.FileFormat { Format: var format } when !External:
                {
                    f.Write($" STORED AS {format}");
                    break;
                }
                case HiveIOFormat.Using { Format: var format }:
                {
                    f.Write($" USING {format}");
                    break;
                }
                default:
                    break;
            }
            if (hiveFormats.SerdeProperties is { } serdeProperties)
            {
                f.Write($" WITH SERDEPROPERTIES ({DisplayCommaSeparated(serdeProperties)})");
            }
            if (!External)
            {
                if (hiveFormats.Location is { } loc)
                {
                    f.Write($" LOCATION '{loc}'");
                }
            }
        }
        if (External)
        {
            if (FileFormat is { } fileFormat)
            {
                f.Write($" STORED AS {fileFormat}");
            }
            if (Location is { } location)
            {
                f.Write($" LOCATION '{location}'");
            }
        }

        switch (TableOptions)
        {
            // Rust: options @ With(_) | options @ Plain(_) | options @ TableProperties(_) => write!(f, " {options}")
            case CreateTableOptions.With or CreateTableOptions.Plain or CreateTableOptions.TableProperties:
                f.Write($" {TableOptions}");
                break;
            default:
                break;
        }

        if (PrimaryKey is { } primaryKey)
        {
            f.Write($" PRIMARY KEY {primaryKey}");
        }
        if (OrderBy is { } orderBy)
        {
            f.Write($" ORDER BY {orderBy}");
        }
        if (Inherits is { } inherits)
        {
            f.Write($" INHERITS ({DisplayCommaSeparated(inherits)})");
        }
        if (PartitionBy is { } partitionBy)
        {
            f.Write($" PARTITION BY {partitionBy}");
        }
        if (ClusterBy is { } clusterBy)
        {
            f.Write($" CLUSTER BY {clusterBy}");
        }
        if (WithConnection is { } withConnection)
        {
            f.Write($" WITH CONNECTION {withConnection}");
        }
        if (TableOptions is CreateTableOptions.Options)
        {
            f.Write($" {TableOptions}");
        }
        if (ExternalVolume is { } externalVolume)
        {
            f.Write($" EXTERNAL_VOLUME='{externalVolume}'");
        }

        if (Catalog is { } catalog)
        {
            f.Write($" CATALOG='{catalog}'");
        }

        if (Iceberg)
        {
            if (BaseLocation is { } baseLocation)
            {
                f.Write($" BASE_LOCATION='{baseLocation}'");
            }
        }

        if (CatalogSync is { } catalogSync)
        {
            f.Write($" CATALOG_SYNC='{catalogSync}'");
        }

        if (StorageSerializationPolicy is { } storageSerializationPolicy)
        {
            f.Write($" STORAGE_SERIALIZATION_POLICY={storageSerializationPolicy}");
        }

        if (CopyGrants)
        {
            f.Write($" COPY GRANTS");
        }

        if (EnableSchemaEvolution is { } isEnabled)
        {
            f.Write($" ENABLE_SCHEMA_EVOLUTION={(isEnabled ? "TRUE" : "FALSE")}");
        }

        if (ChangeTracking is { } changeTrackingIsEnabled)
        {
            f.Write($" CHANGE_TRACKING={(changeTrackingIsEnabled ? "TRUE" : "FALSE")}");
        }

        if (DataRetentionTimeInDays is { } dataRetentionTimeInDays)
        {
            f.Write($" DATA_RETENTION_TIME_IN_DAYS={dataRetentionTimeInDays}");
        }

        if (MaxDataExtensionTimeInDays is { } maxDataExtensionTimeInDays)
        {
            f.Write($" MAX_DATA_EXTENSION_TIME_IN_DAYS={maxDataExtensionTimeInDays}");
        }

        if (DefaultDdlCollation is { } defaultDdlCollation)
        {
            f.Write($" DEFAULT_DDL_COLLATION='{defaultDdlCollation}'");
        }

        if (WithAggregationPolicy is { } withAggregationPolicy)
        {
            f.Write($" WITH AGGREGATION POLICY {withAggregationPolicy}");
        }

        if (WithRowAccessPolicy is { } rowAccessPolicy)
        {
            f.Write($" {rowAccessPolicy}");
        }

        if (WithStorageLifecyclePolicy is { } storageLifecyclePolicy)
        {
            f.Write($" {storageLifecyclePolicy}");
        }

        if (WithTags is { } tag)
        {
            f.Write($" WITH TAG ({DisplayCommaSeparated(tag)})");
        }

        if (TargetLag is { } targetLag)
        {
            f.Write($" TARGET_LAG='{targetLag}'");
        }

        if (Warehouse is { } warehouse)
        {
            f.Write($" WAREHOUSE={warehouse}");
        }

        if (RefreshMode is { } refreshMode)
        {
            f.Write($" REFRESH_MODE={refreshMode}");
        }

        if (Initialize is { } initialize)
        {
            f.Write($" INITIALIZE={initialize}");
        }

        if (RequireUser)
        {
            f.Write($" REQUIRE USER");
        }

        if (OnCommit is not null)
        {
            // `Ast.` qualificado: dentro do record, `OnCommit` é a propriedade (convencoes.md §13, caso 38).
            var onCommit = OnCommit switch
            {
                Ast.OnCommit.DeleteRows => "ON COMMIT DELETE ROWS",
                Ast.OnCommit.PreserveRows => "ON COMMIT PRESERVE ROWS",
                Ast.OnCommit.Drop => "ON COMMIT DROP",
                null => "",
                _ => throw new InvalidOperationException($"Variante de OnCommit não tratada: {OnCommit}"),
            };
            f.Write($" {onCommit}");
        }
        if (Strict)
        {
            f.Write($" STRICT");
        }
        if (Backup is { } backup)
        {
            f.Write($" BACKUP {(backup ? "YES" : "NO")}");
        }
        if (Diststyle is { } diststyle)
        {
            f.Write($" DISTSTYLE {diststyle}");
        }
        if (Distkey is { } distkey)
        {
            f.Write($" DISTKEY({distkey})");
        }
        if (Sortkey is { } sortkey)
        {
            f.Write($" SORTKEY({DisplayCommaSeparated(sortkey)})");
        }
        if (Query is { } query)
        {
            f.Write($" AS {query}");
        }
        if (WithData is { } withData)
        {
            f.Write($" {withData}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
