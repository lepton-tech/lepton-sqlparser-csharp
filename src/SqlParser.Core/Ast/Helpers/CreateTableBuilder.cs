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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/helpers/stmt_create_table.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>Builder for create table statement variant (<c>Statement::CreateTable</c>).</summary>
/// <remarks>
/// This structure helps building and accessing a create table with more ease, without needing to:
/// <list type="bullet">
/// <item>Match the enum itself a lot of times; or</item>
/// <item>Moving a lot of variables around the code.</item>
/// </list>
/// <para>Example</para>
/// <code>
/// use sqlparser::ast::helpers::stmt_create_table::CreateTableBuilder;
/// use sqlparser::ast::{ColumnDef, DataType, Ident, ObjectName};
/// let builder = CreateTableBuilder::new(ObjectName::from(vec![Ident::new("table_name")]))
///    .if_not_exists(true)
///    .columns(vec![ColumnDef {
///        name: Ident::new("c1"),
///        data_type: DataType::Int(None),
///        options: vec![],
/// }]);
/// // You can access internal elements with ease
/// assert!(builder.if_not_exists);
/// // Convert to a statement
/// assert_eq!(
///    builder.build().to_string(),
///    "CREATE TABLE IF NOT EXISTS table_name (c1 INT)"
/// )
/// </code>
/// </remarks>
// Port: `sealed record` com propriedades mutáveis. O record dá a igualdade, o hash e a cópia
// (`builder with { }` = `builder.clone()`) do `#[derive(PartialEq, Eq, Hash, Clone)]`; os
// métodos `fn x(mut self, ..) -> Self` alteram a instância e devolvem `this` (convencoes.md §13,
// caso 24). Métodos com o nome de um campo recebem o prefixo `Set` (CS0102); o campo `clone`
// vira `CloneValue` (CS8859).
public sealed partial record CreateTableBuilder
{
    /// <summary>Create a new <c>CreateTableBuilder</c> for the given table name.</summary>
    public CreateTableBuilder(ObjectName name)
    {
        OrReplace = false;
        Temporary = false;
        Unlogged = false;
        External = false;
        Global = null;
        IfNotExists = false;
        Transient = false;
        Volatile = false;
        Iceberg = false;
        Snapshot = false;
        Dynamic = false;
        Name = name;
        Columns = Sequence<ColumnDef>.Empty;
        Constraints = Sequence<TableConstraint>.Empty;
        HiveDistribution = new HiveDistributionStyle.NONE();
        HiveFormats = null;
        FileFormat = null;
        Location = null;
        Query = null;
        WithoutRowid = false;
        Like = null;
        CloneValue = null;
        Version = null;
        Comment = null;
        OnCommit = null;
        OnCluster = null;
        PrimaryKey = null;
        OrderBy = null;
        PartitionBy = null;
        ClusterBy = null;
        ClusteredBy = null;
        Inherits = null;
        PartitionOf = null;
        ForValues = null;
        Strict = false;
        CopyGrants = false;
        EnableSchemaEvolution = null;
        ChangeTracking = null;
        DataRetentionTimeInDays = null;
        MaxDataExtensionTimeInDays = null;
        DefaultDdlCollation = null;
        WithAggregationPolicy = null;
        WithRowAccessPolicy = null;
        WithStorageLifecyclePolicy = null;
        WithTags = null;
        BaseLocation = null;
        ExternalVolume = null;
        WithConnection = null;
        Catalog = null;
        CatalogSync = null;
        StorageSerializationPolicy = null;
        TableOptions = new CreateTableOptions.None();
        TargetLag = null;
        Warehouse = null;
        RefreshMode = null;
        Initialize = null;
        RequireUser = false;
        Diststyle = null;
        Distkey = null;
        Sortkey = null;
        Backup = null;
        Multiset = null;
        Fallback = null;
        WithData = null;
    }

    /// <summary>Whether the statement uses <c>OR REPLACE</c>.</summary>
    public bool OrReplace { get; set; }

    /// <summary>Whether the table is <c>TEMPORARY</c>.</summary>
    public bool Temporary { get; set; }

    /// <summary>Whether the table is <c>UNLOGGED</c>.</summary>
    public bool Unlogged { get; set; }

    /// <summary>Whether the table is <c>EXTERNAL</c>.</summary>
    public bool External { get; set; }

    /// <summary>Optional <c>GLOBAL</c> flag for dialects that support it.</summary>
    public bool? Global { get; set; }

    /// <summary>Whether <c>IF NOT EXISTS</c> was specified.</summary>
    public bool IfNotExists { get; set; }

    /// <summary>Whether <c>TRANSIENT</c> was specified.</summary>
    public bool Transient { get; set; }

    /// <summary>Whether <c>VOLATILE</c> was specified.</summary>
    public bool Volatile { get; set; }

    /// <summary>Iceberg-specific table flag.</summary>
    public bool Iceberg { get; set; }

    /// <summary><c>SNAPSHOT</c> table flag.</summary>
    public bool Snapshot { get; set; }

    /// <summary>Whether <c>DYNAMIC</c> table option is set.</summary>
    public bool Dynamic { get; set; }

    /// <summary>The table name.</summary>
    public ObjectName Name { get; set; }

    /// <summary>Column definitions for the table.</summary>
    public Sequence<ColumnDef> Columns { get; set; }

    /// <summary>Table-level constraints.</summary>
    public Sequence<TableConstraint> Constraints { get; set; }

    /// <summary>Hive distribution style.</summary>
    public HiveDistributionStyle HiveDistribution { get; set; }

    /// <summary>Optional Hive format settings.</summary>
    public HiveFormat? HiveFormats { get; set; }

    /// <summary>Optional file format for storage.</summary>
    public FileFormat? FileFormat { get; set; }

    /// <summary>Optional storage location.</summary>
    public string? Location { get; set; }

    /// <summary>Optional <c>AS SELECT</c> query for the table.</summary>
    public Query? Query { get; set; }

    /// <summary>Whether <c>WITHOUT ROWID</c> is set.</summary>
    public bool WithoutRowid { get; set; }

    /// <summary>Optional <c>LIKE</c> clause kind.</summary>
    public CreateTableLikeKind? Like { get; set; }

    /// <summary>Optional <c>CLONE</c> source object name.</summary>
    // Rust: campo `clone`; renomeado porque records não aceitam membro chamado `Clone` (CS8859).
    public ObjectName? CloneValue { get; set; }

    /// <summary>Optional table version.</summary>
    public TableVersion? Version { get; set; }

    /// <summary>Optional table comment.</summary>
    public CommentDef? Comment { get; set; }

    /// <summary>Optional <c>ON COMMIT</c> behavior.</summary>
    public OnCommit? OnCommit { get; set; }

    /// <summary>Optional cluster identifier.</summary>
    public Ident? OnCluster { get; set; }

    /// <summary>Optional primary key expression.</summary>
    public Expr? PrimaryKey { get; set; }

    /// <summary>Optional <c>ORDER BY</c> for clustering/sorting.</summary>
    public OneOrManyWithParens<Expr>? OrderBy { get; set; }

    /// <summary>Optional <c>PARTITION BY</c> expression.</summary>
    public Expr? PartitionBy { get; set; }

    /// <summary>Optional <c>CLUSTER BY</c> expressions.</summary>
    public WrappedCollection<Sequence<Expr>>? ClusterBy { get; set; }

    /// <summary>Optional <c>CLUSTERED BY</c> clause.</summary>
    public ClusteredBy? ClusteredBy { get; set; }

    /// <summary>Optional parent tables (<c>INHERITS</c>).</summary>
    public Sequence<ObjectName>? Inherits { get; set; }

    /// <summary>Optional partitioned table (<c>PARTITION OF</c>)</summary>
    public ObjectName? PartitionOf { get; set; }

    /// <summary>Range of values associated with the partition (<c>FOR VALUES</c>)</summary>
    public ForValues? ForValues { get; set; }

    /// <summary><c>STRICT</c> table flag.</summary>
    public bool Strict { get; set; }

    /// <summary>Whether to copy grants from the source.</summary>
    public bool CopyGrants { get; set; }

    /// <summary>Optional flag for schema evolution support.</summary>
    public bool? EnableSchemaEvolution { get; set; }

    /// <summary>Optional change tracking flag.</summary>
    public bool? ChangeTracking { get; set; }

    /// <summary>Optional data retention time in days.</summary>
    public ulong? DataRetentionTimeInDays { get; set; }

    /// <summary>Optional max data extension time in days.</summary>
    public ulong? MaxDataExtensionTimeInDays { get; set; }

    /// <summary>Optional default DDL collation.</summary>
    public string? DefaultDdlCollation { get; set; }

    /// <summary>Optional aggregation policy object name.</summary>
    public ObjectName? WithAggregationPolicy { get; set; }

    /// <summary>Optional row access policy applied to the table.</summary>
    public RowAccessPolicy? WithRowAccessPolicy { get; set; }

    /// <summary>Optional storage lifecycle policy applied to the table.</summary>
    public StorageLifecyclePolicy? WithStorageLifecyclePolicy { get; set; }

    /// <summary>Optional tags/labels attached to the table metadata.</summary>
    public Sequence<Tag>? WithTags { get; set; }

    /// <summary>Optional base location for staged data.</summary>
    public string? BaseLocation { get; set; }

    /// <summary>Optional external volume identifier.</summary>
    public string? ExternalVolume { get; set; }

    /// <summary>
    /// <c>WITH CONNECTION</c> clause.
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_external_table_statement">BigQuery</see>
    /// </summary>
    public ObjectName? WithConnection { get; set; }

    /// <summary>Optional catalog name.</summary>
    public string? Catalog { get; set; }

    /// <summary>Optional catalog synchronization option.</summary>
    public string? CatalogSync { get; set; }

    /// <summary>Optional storage serialization policy.</summary>
    public StorageSerializationPolicy? StorageSerializationPolicy { get; set; }

    /// <summary>Parsed table options from the statement.</summary>
    public CreateTableOptions TableOptions { get; set; }

    /// <summary>Optional target lag configuration.</summary>
    public string? TargetLag { get; set; }

    /// <summary>Optional warehouse identifier.</summary>
    public Ident? Warehouse { get; set; }

    /// <summary>Optional refresh mode for materialized tables.</summary>
    public RefreshModeKind? RefreshMode { get; set; }

    /// <summary>Optional initialization kind for the table.</summary>
    public InitializeKind? Initialize { get; set; }

    /// <summary>Whether operations require a user identity.</summary>
    public bool RequireUser { get; set; }

    /// <summary>Redshift <c>DISTSTYLE</c> option.</summary>
    public DistStyle? Diststyle { get; set; }

    /// <summary>Redshift <c>DISTKEY</c> option.</summary>
    public Expr? Distkey { get; set; }

    /// <summary>Redshift <c>SORTKEY</c> option.</summary>
    public Sequence<Expr>? Sortkey { get; set; }

    /// <summary>Redshift <c>BACKUP</c> option.</summary>
    public bool? Backup { get; set; }

    /// <summary><c>MULTISET | SET</c> table-kind prefix.</summary>
    public bool? Multiset { get; set; }

    /// <summary><c>FALLBACK</c> clause.</summary>
    public bool? Fallback { get; set; }

    /// <summary><c>WITH DATA</c> clause.</summary>
    public WithData? WithData { get; set; }

    /// <summary>Set <c>OR REPLACE</c> for the CREATE TABLE statement.</summary>
    public CreateTableBuilder SetOrReplace(bool orReplace)
    {
        OrReplace = orReplace;
        return this;
    }

    /// <summary>Mark the table as <c>TEMPORARY</c>.</summary>
    public CreateTableBuilder SetTemporary(bool temporary)
    {
        Temporary = temporary;
        return this;
    }

    /// <summary>Mark the table as <c>UNLOGGED</c>.</summary>
    public CreateTableBuilder SetUnlogged(bool unlogged)
    {
        Unlogged = unlogged;
        return this;
    }

    /// <summary>Mark the table as <c>EXTERNAL</c>.</summary>
    public CreateTableBuilder SetExternal(bool external)
    {
        External = external;
        return this;
    }

    /// <summary>Set optional <c>GLOBAL</c> flag (dialect-specific).</summary>
    public CreateTableBuilder SetGlobal(bool? global)
    {
        Global = global;
        return this;
    }

    /// <summary>Set <c>IF NOT EXISTS</c>.</summary>
    public CreateTableBuilder SetIfNotExists(bool ifNotExists)
    {
        IfNotExists = ifNotExists;
        return this;
    }

    /// <summary>Set <c>TRANSIENT</c> flag.</summary>
    public CreateTableBuilder SetTransient(bool transient)
    {
        Transient = transient;
        return this;
    }

    /// <summary>Set <c>VOLATILE</c> flag.</summary>
    public CreateTableBuilder SetVolatile(bool @volatile)
    {
        Volatile = @volatile;
        return this;
    }

    /// <summary>Enable Iceberg table semantics.</summary>
    public CreateTableBuilder SetIceberg(bool iceberg)
    {
        Iceberg = iceberg;
        return this;
    }

    /// <summary>Set <c>SNAPSHOT</c> table flag (BigQuery).</summary>
    public CreateTableBuilder SetSnapshot(bool snapshot)
    {
        Snapshot = snapshot;
        return this;
    }

    /// <summary>Set <c>DYNAMIC</c> table option.</summary>
    public CreateTableBuilder SetDynamic(bool dynamic)
    {
        Dynamic = dynamic;
        return this;
    }

    /// <summary>Set the table column definitions.</summary>
    public CreateTableBuilder SetColumns(Sequence<ColumnDef> columns)
    {
        Columns = columns;
        return this;
    }

    /// <summary>Set table-level constraints.</summary>
    public CreateTableBuilder SetConstraints(Sequence<TableConstraint> constraints)
    {
        Constraints = constraints;
        return this;
    }

    /// <summary>Set Hive distribution style.</summary>
    public CreateTableBuilder SetHiveDistribution(HiveDistributionStyle hiveDistribution)
    {
        HiveDistribution = hiveDistribution;
        return this;
    }

    /// <summary>Set Hive-specific formats.</summary>
    public CreateTableBuilder SetHiveFormats(HiveFormat? hiveFormats)
    {
        HiveFormats = hiveFormats;
        return this;
    }

    /// <summary>Set file format for the table (e.g., PARQUET).</summary>
    public CreateTableBuilder SetFileFormat(FileFormat? fileFormat)
    {
        FileFormat = fileFormat;
        return this;
    }

    /// <summary>Set storage <c>location</c> for the table.</summary>
    public CreateTableBuilder SetLocation(string? location)
    {
        Location = location;
        return this;
    }

    /// <summary>Set an underlying <c>AS SELECT</c> query for the table.</summary>
    public CreateTableBuilder SetQuery(Query? query)
    {
        Query = query;
        return this;
    }

    /// <summary>Set <c>WITHOUT ROWID</c> option.</summary>
    public CreateTableBuilder SetWithoutRowid(bool withoutRowid)
    {
        WithoutRowid = withoutRowid;
        return this;
    }

    /// <summary>Set <c>LIKE</c> clause for the table.</summary>
    public CreateTableBuilder SetLike(CreateTableLikeKind? like)
    {
        Like = like;
        return this;
    }

    // Different name to allow the object to be cloned
    /// <summary>Set <c>CLONE</c> source object name.</summary>
    public CreateTableBuilder CloneClause(ObjectName? clone)
    {
        CloneValue = clone;
        return this;
    }

    /// <summary>Set table <c>VERSION</c>.</summary>
    public CreateTableBuilder SetVersion(TableVersion? version)
    {
        Version = version;
        return this;
    }

    /// <summary>Set a comment for the table or following column definitions.</summary>
    public CreateTableBuilder CommentAfterColumnDef(CommentDef? comment)
    {
        Comment = comment;
        return this;
    }

    /// <summary>Set <c>ON COMMIT</c> behavior for temporary tables.</summary>
    public CreateTableBuilder SetOnCommit(OnCommit? onCommit)
    {
        OnCommit = onCommit;
        return this;
    }

    /// <summary>Set cluster identifier for the table.</summary>
    public CreateTableBuilder SetOnCluster(Ident? onCluster)
    {
        OnCluster = onCluster;
        return this;
    }

    /// <summary>Set a primary key expression for the table.</summary>
    public CreateTableBuilder SetPrimaryKey(Expr? primaryKey)
    {
        PrimaryKey = primaryKey;
        return this;
    }

    /// <summary>Set <c>ORDER BY</c> clause for clustered/sorted tables.</summary>
    public CreateTableBuilder SetOrderBy(OneOrManyWithParens<Expr>? orderBy)
    {
        OrderBy = orderBy;
        return this;
    }

    /// <summary>Set <c>PARTITION BY</c> expression.</summary>
    public CreateTableBuilder SetPartitionBy(Expr? partitionBy)
    {
        PartitionBy = partitionBy;
        return this;
    }

    /// <summary>Set <c>CLUSTER BY</c> expression(s).</summary>
    public CreateTableBuilder SetClusterBy(WrappedCollection<Sequence<Expr>>? clusterBy)
    {
        ClusterBy = clusterBy;
        return this;
    }

    /// <summary>Set <c>CLUSTERED BY</c> clause.</summary>
    public CreateTableBuilder SetClusteredBy(ClusteredBy? clusteredBy)
    {
        ClusteredBy = clusteredBy;
        return this;
    }

    /// <summary>Set parent tables via <c>INHERITS</c>.</summary>
    public CreateTableBuilder SetInherits(Sequence<ObjectName>? inherits)
    {
        Inherits = inherits;
        return this;
    }

    /// <summary>Sets the table which is partitioned to create the current table.</summary>
    public CreateTableBuilder SetPartitionOf(ObjectName? partitionOf)
    {
        PartitionOf = partitionOf;
        return this;
    }

    /// <summary>Sets the range of values associated with the partition.</summary>
    public CreateTableBuilder SetForValues(ForValues? forValues)
    {
        ForValues = forValues;
        return this;
    }

    /// <summary>Set <c>STRICT</c> option.</summary>
    public CreateTableBuilder SetStrict(bool strict)
    {
        Strict = strict;
        return this;
    }

    /// <summary>Enable copying grants from source object.</summary>
    public CreateTableBuilder SetCopyGrants(bool copyGrants)
    {
        CopyGrants = copyGrants;
        return this;
    }

    /// <summary>Enable or disable schema evolution features.</summary>
    public CreateTableBuilder SetEnableSchemaEvolution(bool? enableSchemaEvolution)
    {
        EnableSchemaEvolution = enableSchemaEvolution;
        return this;
    }

    /// <summary>Enable or disable change tracking.</summary>
    public CreateTableBuilder SetChangeTracking(bool? changeTracking)
    {
        ChangeTracking = changeTracking;
        return this;
    }

    /// <summary>Set data retention time (in days).</summary>
    public CreateTableBuilder SetDataRetentionTimeInDays(ulong? dataRetentionTimeInDays)
    {
        DataRetentionTimeInDays = dataRetentionTimeInDays;
        return this;
    }

    /// <summary>Set maximum data extension time (in days).</summary>
    public CreateTableBuilder SetMaxDataExtensionTimeInDays(ulong? maxDataExtensionTimeInDays)
    {
        MaxDataExtensionTimeInDays = maxDataExtensionTimeInDays;
        return this;
    }

    /// <summary>Set default DDL collation.</summary>
    public CreateTableBuilder SetDefaultDdlCollation(string? defaultDdlCollation)
    {
        DefaultDdlCollation = defaultDdlCollation;
        return this;
    }

    /// <summary>Set aggregation policy object.</summary>
    public CreateTableBuilder SetWithAggregationPolicy(ObjectName? withAggregationPolicy)
    {
        WithAggregationPolicy = withAggregationPolicy;
        return this;
    }

    /// <summary>Attach a row access policy to the table.</summary>
    public CreateTableBuilder SetWithRowAccessPolicy(RowAccessPolicy? withRowAccessPolicy)
    {
        WithRowAccessPolicy = withRowAccessPolicy;
        return this;
    }

    /// <summary>Attach a storage lifecycle policy to the table.</summary>
    public CreateTableBuilder SetWithStorageLifecyclePolicy(StorageLifecyclePolicy? withStorageLifecyclePolicy)
    {
        WithStorageLifecyclePolicy = withStorageLifecyclePolicy;
        return this;
    }

    /// <summary>Attach tags/labels to the table metadata.</summary>
    public CreateTableBuilder SetWithTags(Sequence<Tag>? withTags)
    {
        WithTags = withTags;
        return this;
    }

    /// <summary>Set a base storage location for staged data.</summary>
    public CreateTableBuilder SetBaseLocation(string? baseLocation)
    {
        BaseLocation = baseLocation;
        return this;
    }

    /// <summary>Set an external volume identifier.</summary>
    public CreateTableBuilder SetExternalVolume(string? externalVolume)
    {
        ExternalVolume = externalVolume;
        return this;
    }

    /// <summary>Set the <c>WITH CONNECTION</c> clause.</summary>
    public CreateTableBuilder SetWithConnection(ObjectName? withConnection)
    {
        WithConnection = withConnection;
        return this;
    }

    /// <summary>Set the catalog name for the table.</summary>
    public CreateTableBuilder SetCatalog(string? catalog)
    {
        Catalog = catalog;
        return this;
    }

    /// <summary>Set catalog synchronization option.</summary>
    public CreateTableBuilder SetCatalogSync(string? catalogSync)
    {
        CatalogSync = catalogSync;
        return this;
    }

    /// <summary>Set a storage serialization policy.</summary>
    public CreateTableBuilder SetStorageSerializationPolicy(StorageSerializationPolicy? storageSerializationPolicy)
    {
        StorageSerializationPolicy = storageSerializationPolicy;
        return this;
    }

    /// <summary>Set arbitrary table options parsed from the statement.</summary>
    public CreateTableBuilder SetTableOptions(CreateTableOptions tableOptions)
    {
        TableOptions = tableOptions;
        return this;
    }

    /// <summary>Set a target lag configuration (dialect-specific).</summary>
    public CreateTableBuilder SetTargetLag(string? targetLag)
    {
        TargetLag = targetLag;
        return this;
    }

    /// <summary>Associate the table with a warehouse identifier.</summary>
    public CreateTableBuilder SetWarehouse(Ident? warehouse)
    {
        Warehouse = warehouse;
        return this;
    }

    /// <summary>Set refresh mode for materialized/managed tables.</summary>
    public CreateTableBuilder SetRefreshMode(RefreshModeKind? refreshMode)
    {
        RefreshMode = refreshMode;
        return this;
    }

    /// <summary>Set initialization mode for the table.</summary>
    public CreateTableBuilder SetInitialize(InitializeKind? initialize)
    {
        Initialize = initialize;
        return this;
    }

    /// <summary>Require a user identity for table operations.</summary>
    public CreateTableBuilder SetRequireUser(bool requireUser)
    {
        RequireUser = requireUser;
        return this;
    }

    /// <summary>Set Redshift <c>DISTSTYLE</c> option.</summary>
    public CreateTableBuilder SetDiststyle(DistStyle? diststyle)
    {
        Diststyle = diststyle;
        return this;
    }

    /// <summary>Set Redshift <c>DISTKEY</c> option.</summary>
    public CreateTableBuilder SetDistkey(Expr? distkey)
    {
        Distkey = distkey;
        return this;
    }

    /// <summary>Set Redshift <c>SORTKEY</c> option.</summary>
    public CreateTableBuilder SetSortkey(Sequence<Expr>? sortkey)
    {
        Sortkey = sortkey;
        return this;
    }

    /// <summary>Set the Redshift <c>BACKUP</c> option.</summary>
    public CreateTableBuilder SetBackup(bool? backup)
    {
        Backup = backup;
        return this;
    }

    /// <summary>
    /// Set <c>MULTISET | SET</c> table-kind prefix.
    /// Some(true) =&gt; <c>MULTISET</c>, Some(false) =&gt; <c>SET</c>.
    /// </summary>
    public CreateTableBuilder SetMultiset(bool? multiset)
    {
        Multiset = multiset;
        return this;
    }

    /// <summary>Set <c>FALLBACK</c> / <c>NO FALLBACK</c> flag.</summary>
    public CreateTableBuilder SetFallback(bool? fallback)
    {
        Fallback = fallback;
        return this;
    }

    /// <summary>Set <c>WITH DATA</c> clause.</summary>
    public CreateTableBuilder SetWithData(WithData? withData)
    {
        WithData = withData;
        return this;
    }

    /// <summary>Consume the builder and produce a <c>CreateTable</c>.</summary>
    public CreateTable Build() =>
        new CreateTable(
            OrReplace: OrReplace,
            Temporary: Temporary,
            Unlogged: Unlogged,
            External: External,
            Global: Global,
            IfNotExists: IfNotExists,
            Transient: Transient,
            Volatile: Volatile,
            Iceberg: Iceberg,
            Snapshot: Snapshot,
            Dynamic: Dynamic,
            Name: Name,
            Columns: Columns,
            Constraints: Constraints,
            HiveDistribution: HiveDistribution,
            HiveFormats: HiveFormats,
            FileFormat: FileFormat,
            Location: Location,
            Query: Query,
            WithoutRowid: WithoutRowid,
            Like: Like,
            CloneValue: CloneValue,
            Version: Version,
            Comment: Comment,
            OnCommit: OnCommit,
            OnCluster: OnCluster,
            PrimaryKey: PrimaryKey,
            OrderBy: OrderBy,
            PartitionBy: PartitionBy,
            ClusterBy: ClusterBy,
            ClusteredBy: ClusteredBy,
            Inherits: Inherits,
            PartitionOf: PartitionOf,
            ForValues: ForValues,
            Strict: Strict,
            CopyGrants: CopyGrants,
            EnableSchemaEvolution: EnableSchemaEvolution,
            ChangeTracking: ChangeTracking,
            DataRetentionTimeInDays: DataRetentionTimeInDays,
            MaxDataExtensionTimeInDays: MaxDataExtensionTimeInDays,
            DefaultDdlCollation: DefaultDdlCollation,
            WithAggregationPolicy: WithAggregationPolicy,
            WithRowAccessPolicy: WithRowAccessPolicy,
            WithStorageLifecyclePolicy: WithStorageLifecyclePolicy,
            WithTags: WithTags,
            BaseLocation: BaseLocation,
            ExternalVolume: ExternalVolume,
            WithConnection: WithConnection,
            Catalog: Catalog,
            CatalogSync: CatalogSync,
            StorageSerializationPolicy: StorageSerializationPolicy,
            TableOptions: TableOptions,
            TargetLag: TargetLag,
            Warehouse: Warehouse,
            RefreshMode: RefreshMode,
            Initialize: Initialize,
            RequireUser: RequireUser,
            Diststyle: Diststyle,
            Distkey: Distkey,
            Sortkey: Sortkey,
            Backup: Backup,
            Multiset: Multiset,
            Fallback: Fallback,
            WithData: WithData);

    /// <exception cref="ParserException">The statement is not a <c>Statement::CreateTable</c>.</exception>
    // Rust: `impl TryFrom<Statement> for CreateTableBuilder`. O `Err(ParserError)` devolvido quando a
    // instrução não é `Statement::CreateTable` vira ParserException (convencoes.md §8).
    public static CreateTableBuilder TryFrom(Statement stmt)
    {
        // As the builder can be transformed back to a statement, it shouldn't be a problem to take the
        // ownership.
        switch (stmt)
        {
            case Statement.CreateTable createTable:
                return From(createTable.Value);
            default:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Expected create table statement, but received: {stmt}"));
        }
    }

    // Rust: `impl From<CreateTable> for CreateTableBuilder`.
    public static CreateTableBuilder From(CreateTable table) =>
        new CreateTableBuilder(table.Name)
        {
            OrReplace = table.OrReplace,
            Temporary = table.Temporary,
            Unlogged = table.Unlogged,
            External = table.External,
            Global = table.Global,
            IfNotExists = table.IfNotExists,
            Transient = table.Transient,
            Volatile = table.Volatile,
            Iceberg = table.Iceberg,
            Snapshot = table.Snapshot,
            Dynamic = table.Dynamic,
            Name = table.Name,
            Columns = table.Columns,
            Constraints = table.Constraints,
            HiveDistribution = table.HiveDistribution,
            HiveFormats = table.HiveFormats,
            FileFormat = table.FileFormat,
            Location = table.Location,
            Query = table.Query,
            WithoutRowid = table.WithoutRowid,
            Like = table.Like,
            CloneValue = table.CloneValue,
            Version = table.Version,
            Comment = table.Comment,
            OnCommit = table.OnCommit,
            OnCluster = table.OnCluster,
            PrimaryKey = table.PrimaryKey,
            OrderBy = table.OrderBy,
            PartitionBy = table.PartitionBy,
            ClusterBy = table.ClusterBy,
            ClusteredBy = table.ClusteredBy,
            Inherits = table.Inherits,
            PartitionOf = table.PartitionOf,
            ForValues = table.ForValues,
            Strict = table.Strict,
            CopyGrants = table.CopyGrants,
            EnableSchemaEvolution = table.EnableSchemaEvolution,
            ChangeTracking = table.ChangeTracking,
            DataRetentionTimeInDays = table.DataRetentionTimeInDays,
            MaxDataExtensionTimeInDays = table.MaxDataExtensionTimeInDays,
            DefaultDdlCollation = table.DefaultDdlCollation,
            WithAggregationPolicy = table.WithAggregationPolicy,
            WithRowAccessPolicy = table.WithRowAccessPolicy,
            WithStorageLifecyclePolicy = table.WithStorageLifecyclePolicy,
            WithTags = table.WithTags,
            BaseLocation = table.BaseLocation,
            ExternalVolume = table.ExternalVolume,
            WithConnection = table.WithConnection,
            Catalog = table.Catalog,
            CatalogSync = table.CatalogSync,
            StorageSerializationPolicy = table.StorageSerializationPolicy,
            TableOptions = table.TableOptions,
            TargetLag = table.TargetLag,
            Warehouse = table.Warehouse,
            RefreshMode = table.RefreshMode,
            Initialize = table.Initialize,
            RequireUser = table.RequireUser,
            Diststyle = table.Diststyle,
            Distkey = table.Distkey,
            Sortkey = table.Sortkey,
            Backup = table.Backup,
            Multiset = table.Multiset,
            Fallback = table.Fallback,
            WithData = table.WithData,
        };
}
