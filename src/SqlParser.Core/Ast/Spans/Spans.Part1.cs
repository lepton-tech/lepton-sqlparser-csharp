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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/spans.rs (linhas 109–1003:
// impl Spanned for Parens<T> … impl Spanned for Update).
// Modificado: tradução de Rust para C#.

using static SqlParser.Core.Ast.SpansModule;
using static SqlParser.Core.RustIter;

namespace SqlParser.Core.Ast;

// Cada `impl Spanned for X` de spans.rs vira um `partial` do tipo X, na ordem do arquivo Rust
// (onda-b/B7.md, decisões 1–3). Dentro desses tipos, o nome simples `Span` é o método `Span()`:
// o tipo é escrito `Core.Span` em contexto de expressão (`Core.Span.Empty()`).
// Um `match self` vira uma expressão `this switch` (como `ColumnOptions.AsSlice`), e a
// desestruturação inicial `let X { .. } = self;` fica em comentário, com os campos ignorados e suas
// notas (caso 78; os campos são lidos direto das propriedades).

// impl<T> Spanned for Parens<T> (spans.rs:109)
public sealed partial record Parens<T> : ISpanned
{
    public Span Span() => OpeningToken.Value.Span.Union(ClosingToken.Value.Span);
}

// impl Spanned for Query (spans.rs:115)
public sealed partial record Query : ISpanned
{
    public Span Span()
    {
        // PORT-DEVIATION DIV-005: o Rust não marca este método com #[recursive::recursive], mas aqui cada nível
        // de subconsulta aninhada (`SELECT * FROM (SELECT * FROM (...) x) x`, possível com o limite de recursão
        // elevado) gasta KBs de pilha; recebe a mesma proteção do Span() de Expr (convencoes.md §13, caso 123).
        if (StackGuard.IsStackLow())
        {
            return StackGuard.Grow(this, static query => query.Span());
        }

        // let Query {
        //     with,
        //     body,
        //     order_by,
        //     limit_clause,
        //     fetch,
        //     locks: _,          // todo
        //     for_clause: _,     // todo, mssql specific
        //     settings: _,       // todo, clickhouse specific
        //     format_clause: _,  // todo, clickhouse specific
        //     pipe_operators: _, // todo bigquery specific
        // } = self;

        return UnionSpans(
            OptionIter(With).Select(i => i.Span())
                .Concat(Once(Body.Span()))
                .Concat(OptionIter(OrderBy).Select(i => i.Span()))
                .Concat(OptionIter(LimitClause).Select(i => i.Span()))
                .Concat(OptionIter(Fetch).Select(i => i.Span())));
    }
}

// impl Spanned for LimitClause (spans.rs:141)
public abstract partial record LimitClause : ISpanned
{
    public Span Span() =>
        this switch
        {
            LimitOffset l => UnionSpans(
                OptionIter(l.Limit).Select(i => i.Span())
                    .Concat(OptionIter(l.Offset).Select(i => i.Span()))
                    .Concat(l.LimitBy.Select(i => i.Span()))),
            OffsetCommaLimit o => o.Offset.Span().Union(o.Limit.Span()),
            _ => throw new InvalidOperationException($"Variante de LimitClause não tratada: {GetType().Name}"),
        };
}

// impl Spanned for Offset (spans.rs:160)
public sealed partial record Offset : ISpanned
{
    public Span Span()
    {
        // let Offset {
        //     value,
        //     rows: _, // enum
        // } = self;

        return Value.Span();
    }
}

// impl Spanned for Fetch (spans.rs:171)
public sealed partial record Fetch : ISpanned
{
    public Span Span()
    {
        // let Fetch {
        //     with_ties: _, // bool
        //     percent: _,   // bool
        //     quantity,
        // } = self;

        // quantity.as_ref().map_or(Span::empty(), |i| i.span())
        return Quantity is { } i ? i.Span() : Core.Span.Empty();
    }
}

// impl Spanned for With (spans.rs:183)
public sealed partial record With : ISpanned
{
    public Span Span()
    {
        // let With {
        //     with_token,
        //     recursive: _, // bool
        //     cte_tables,
        // } = self;

        return UnionSpans(
            Once(WithToken.Value.Span).Concat(CteTables.Select(item => item.Span())));
    }
}

// impl Spanned for Cte (spans.rs:197)
public sealed partial record Cte : ISpanned
{
    public Span Span()
    {
        // let Cte {
        //     alias,
        //     query,
        //     from,
        //     materialized: _, // enum
        //     closing_paren_token,
        // } = self;

        return UnionSpans(
            Once(Alias.Span())
                .Concat(Once(Query.Span()))
                .Concat(OptionIter(From).Select(item => item.Span))
                .Concat(Once(ClosingParenToken.Value.Span)));
    }
}

// impl Spanned for SetExpr (spans.rs:219)
public abstract partial record SetExpr : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para><c>SetExpr::Table</c> is not implemented.</para>
    /// </remarks>
    public Span Span()
    {
        // PORT-DEVIATION DIV-005: o Rust não marca este método com #[recursive::recursive], mas aqui cada
        // operação de uma cadeia `SELECT 1 UNION ALL SELECT 2 ...` (não limitada pelo contador de recursão)
        // gasta KBs de pilha; recebe a mesma proteção do Span() de Expr (convencoes.md §13, caso 123).
        if (StackGuard.IsStackLow())
        {
            return StackGuard.Grow(this, static setExpr => setExpr.Span());
        }

        return this switch
        {
            Select select => select.Value.Span(),
            Query query => query.Value.Span(),
            SetOperation s => s.Left.Span().Union(s.Right.Span()),
            Values values => values.Value.Span(),
            Insert statement => statement.Value.Span(),
            Table => Core.Span.Empty(),
            Update statement => statement.Value.Span(),
            Delete statement => statement.Value.Span(),
            Merge statement => statement.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de SetExpr não tratada: {GetType().Name}"),
        };
    }
}

// impl Spanned for Values (spans.rs:240)
public sealed partial record Values : ISpanned
{
    public Span Span()
    {
        // let Values {
        //     explicit_row: _, // bool,
        //     value_keyword: _,
        //     rows,
        // } = self;

        // match &rows[..] { [] => .., [f] => .., [f, .., l] => .. }: padrões de lista sobre a Sequence.
        return Rows switch
        {
            [] => Core.Span.Empty(),
            [var f] => f.Span(),
            [var f, .., var l] => f.Span().Union(l.Span()),
        };
    }
}

// impl Spanned for Statement (spans.rs:322)
public abstract partial record Statement : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>Statement::CopyIntoSnowflake</c></item>
    /// <item><c>Statement::CreateSecret</c></item>
    /// <item><c>Statement::CreateRole</c></item>
    /// <item><c>Statement::AlterType</c></item>
    /// <item><c>Statement::AlterOperator</c></item>
    /// <item><c>Statement::AlterRole</c></item>
    /// <item><c>Statement::AttachDatabase</c></item>
    /// <item><c>Statement::AttachDuckDBDatabase</c></item>
    /// <item><c>Statement::DetachDuckDBDatabase</c></item>
    /// <item><c>Statement::Drop</c></item>
    /// <item><c>Statement::DropFunction</c></item>
    /// <item><c>Statement::DropProcedure</c></item>
    /// <item><c>Statement::DropSecret</c></item>
    /// <item><c>Statement::Declare</c></item>
    /// <item><c>Statement::CreateExtension</c></item>
    /// <item><c>Statement::CreateCollation</c></item>
    /// <item><c>Statement::AlterCollation</c></item>
    /// <item><c>Statement::Fetch</c></item>
    /// <item><c>Statement::Flush</c></item>
    /// <item><c>Statement::Discard</c></item>
    /// <item><c>Statement::Set</c></item>
    /// <item><c>Statement::ShowFunctions</c></item>
    /// <item><c>Statement::ShowVariable</c></item>
    /// <item><c>Statement::ShowStatus</c></item>
    /// <item><c>Statement::ShowVariables</c></item>
    /// <item><c>Statement::ShowCreate</c></item>
    /// <item><c>Statement::ShowColumns</c></item>
    /// <item><c>Statement::ShowTables</c></item>
    /// <item><c>Statement::ShowCollation</c></item>
    /// <item><c>Statement::StartTransaction</c></item>
    /// <item><c>Statement::Comment</c></item>
    /// <item><c>Statement::Commit</c></item>
    /// <item><c>Statement::Rollback</c></item>
    /// <item><c>Statement::CreateSchema</c></item>
    /// <item><c>Statement::CreateDatabase</c></item>
    /// <item><c>Statement::CreateFunction</c></item>
    /// <item><c>Statement::CreateTrigger</c></item>
    /// <item><c>Statement::DropTrigger</c></item>
    /// <item><c>Statement::CreateProcedure</c></item>
    /// <item><c>Statement::CreateMacro</c></item>
    /// <item><c>Statement::CreateStage</c></item>
    /// <item><c>Statement::CreateFileFormat</c></item>
    /// <item><c>Statement::Assert</c></item>
    /// <item><c>Statement::Grant</c></item>
    /// <item><c>Statement::Revoke</c></item>
    /// <item><c>Statement::Deallocate</c></item>
    /// <item><c>Statement::Execute</c></item>
    /// <item><c>Statement::Prepare</c></item>
    /// <item><c>Statement::Kill</c></item>
    /// <item><c>Statement::ExplainTable</c></item>
    /// <item><c>Statement::Explain</c></item>
    /// <item><c>Statement::Savepoint</c></item>
    /// <item><c>Statement::ReleaseSavepoint</c></item>
    /// <item><c>Statement::Cache</c></item>
    /// <item><c>Statement::UNCache</c></item>
    /// <item><c>Statement::CreateSequence</c></item>
    /// <item><c>Statement::CreateType</c></item>
    /// <item><c>Statement::Pragma</c></item>
    /// <item><c>Statement::Lock</c></item>
    /// <item><c>Statement::LockTables</c></item>
    /// <item><c>Statement::UnlockTables</c></item>
    /// <item><c>Statement::Unload</c></item>
    /// <item><c>Statement::OptimizeTable</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            Analyze analyze => analyze.Value.Span(),
            Truncate truncate => truncate.Value.Span(),
            Msck msck => msck.Value.Span(),
            Query query => query.Value.Span(),
            Insert insert => insert.Value.Span(),
            Install install => install.ExtensionName.Span,
            Load load => load.ExtensionName.Span,
            // Statement::Directory { overwrite: _, local: _, path: _, file_format: _, source }
            Directory directory => directory.Source.Span(),
            Case stmt => stmt.Value.Span(),
            If stmt => stmt.Value.Span(),
            While stmt => stmt.Value.Span(),
            Raise stmt => stmt.Value.Span(),
            Call function => function.Value.Span(),
            // Statement::Copy { source, to: _, target: _, options: _, legacy_options: _, values: _ }
            Copy copy => copy.Source.Span(),
            // Statement::CopyIntoSnowflake { into: _, into_columns: _, from_obj: _, from_obj_alias: _,
            //     stage_params: _, from_transformations: _, files: _, pattern: _, file_format: _,
            //     copy_options: _, validation_mode: _, kind: _, from_query: _, partition: _ }
            CopyIntoSnowflake => Core.Span.Empty(),
            Open open => open.Value.Span(),
            Close close => close.Cursor switch
            {
                CloseCursor.All => Core.Span.Empty(),
                CloseCursor.Specific specific => specific.Name.Span,
                _ => throw new InvalidOperationException($"Variante de CloseCursor não tratada: {close.Cursor.GetType().Name}"),
            },
            Update update => update.Value.Span(),
            Delete delete => delete.Value.Span(),
            CreateView createView => createView.Value.Span(),
            CreateTable createTable => createTable.Value.Span(),
            // Statement::CreateVirtualTable { name, if_not_exists: _, module_name, module_args }
            CreateVirtualTable c => UnionSpans(
                Once(c.Name.Span())
                    .Concat(Once(c.ModuleName.Span))
                    .Concat(c.ModuleArgs.Select(i => i.Span))),
            CreateIndex createIndex => createIndex.Value.Span(),
            CreateRole createRole => createRole.Value.Span(),
            CreateExtension createExtension => createExtension.Value.Span(),
            CreateCollation createCollation => createCollation.Value.Span(),
            DropExtension dropExtension => dropExtension.Value.Span(),
            DropOperator dropOperator => dropOperator.Value.Span(),
            DropOperatorFamily dropOperatorFamily => dropOperatorFamily.Value.Span(),
            DropOperatorClass dropOperatorClass => dropOperatorClass.Value.Span(),
            CreateSecret => Core.Span.Empty(),
            CreateServer => Core.Span.Empty(),
            CreateForeignTable stmt => stmt.Value.Span(),
            CreateConnector => Core.Span.Empty(),
            CreateOperator createOperator => createOperator.Value.Span(),
            CreateOperatorFamily createOperatorFamily => createOperatorFamily.Value.Span(),
            CreateOperatorClass createOperatorClass => createOperatorClass.Value.Span(),
            CreateTextSearch createTextSearch => createTextSearch.Value.Span(),
            AlterTable alterTable => alterTable.Value.Span(),
            AlterIndex a => a.Name.Span().Union(a.Operation.Span()),
            // Statement::AlterView { name, columns, query, with_options }
            AlterView v => UnionSpans(
                Once(v.Name.Span())
                    .Concat(v.Columns.Select(i => i.Span))
                    .Concat(Once(v.Query.Span()))
                    .Concat(v.WithOptions.Select(i => i.Span()))),
            // These statements need to be implemented
            AlterFunction => Core.Span.Empty(),
            AlterType => Core.Span.Empty(),
            AlterCollation => Core.Span.Empty(),
            AlterOperator => Core.Span.Empty(),
            AlterOperatorFamily => Core.Span.Empty(),
            AlterOperatorClass => Core.Span.Empty(),
            AlterTextSearch => Core.Span.Empty(),
            AlterRole => Core.Span.Empty(),
            AlterSession => Core.Span.Empty(),
            AttachDatabase => Core.Span.Empty(),
            AttachDuckDBDatabase => Core.Span.Empty(),
            DetachDuckDBDatabase => Core.Span.Empty(),
            Drop => Core.Span.Empty(),
            DropFunction dropFunction => dropFunction.Value.Span(),
            DropDomain => Core.Span.Empty(),
            DropProcedure => Core.Span.Empty(),
            DropSecret => Core.Span.Empty(),
            Declare => Core.Span.Empty(),
            Fetch => Core.Span.Empty(),
            Flush => Core.Span.Empty(),
            Discard => Core.Span.Empty(),
            Set => Core.Span.Empty(),
            ShowFunctions => Core.Span.Empty(),
            ShowVariable => Core.Span.Empty(),
            ShowStatus => Core.Span.Empty(),
            ShowVariables => Core.Span.Empty(),
            ShowCreate => Core.Span.Empty(),
            ShowColumns => Core.Span.Empty(),
            ShowTables => Core.Span.Empty(),
            ShowCollation => Core.Span.Empty(),
            ShowCharset => Core.Span.Empty(),
            Use u => u.Value.Span(),
            StartTransaction => Core.Span.Empty(),
            Comment => Core.Span.Empty(),
            Commit => Core.Span.Empty(),
            Rollback => Core.Span.Empty(),
            CreateSchema => Core.Span.Empty(),
            CreateDatabase => Core.Span.Empty(),
            CreateFunction => Core.Span.Empty(),
            CreateDomain => Core.Span.Empty(),
            CreateTrigger => Core.Span.Empty(),
            DropTrigger => Core.Span.Empty(),
            CreateProcedure => Core.Span.Empty(),
            CreateMacro => Core.Span.Empty(),
            CreateStage => Core.Span.Empty(),
            CreateFileFormat => Core.Span.Empty(),
            CreateWarehouse => Core.Span.Empty(),
            Assert => Core.Span.Empty(),
            Grant => Core.Span.Empty(),
            Deny => Core.Span.Empty(),
            Revoke => Core.Span.Empty(),
            Deallocate => Core.Span.Empty(),
            Execute => Core.Span.Empty(),
            Prepare => Core.Span.Empty(),
            Kill => Core.Span.Empty(),
            ExplainTable => Core.Span.Empty(),
            Explain => Core.Span.Empty(),
            Savepoint => Core.Span.Empty(),
            ReleaseSavepoint => Core.Span.Empty(),
            Merge merge => merge.Value.Span(),
            Cache => Core.Span.Empty(),
            UNCache => Core.Span.Empty(),
            CreateSequence => Core.Span.Empty(),
            CreateType => Core.Span.Empty(),
            Pragma => Core.Span.Empty(),
            Lock => Core.Span.Empty(),
            LockTables => Core.Span.Empty(),
            UnlockTables => Core.Span.Empty(),
            Unload => Core.Span.Empty(),
            OptimizeTable => Core.Span.Empty(),
            CreatePolicy => Core.Span.Empty(),
            AlterPolicy => Core.Span.Empty(),
            AlterConnector => Core.Span.Empty(),
            DropPolicy => Core.Span.Empty(),
            DropConnector => Core.Span.Empty(),
            ShowCatalogs => Core.Span.Empty(),
            ShowDatabases => Core.Span.Empty(),
            ShowProcessList => Core.Span.Empty(),
            ShowSchemas => Core.Span.Empty(),
            ShowObjects => Core.Span.Empty(),
            ShowViews => Core.Span.Empty(),
            LISTEN => Core.Span.Empty(),
            NOTIFY => Core.Span.Empty(),
            LoadData => Core.Span.Empty(),
            UNLISTEN => Core.Span.Empty(),
            RenameTable => Core.Span.Empty(),
            RaisError => Core.Span.Empty(),
            Throw => Core.Span.Empty(),
            Print => Core.Span.Empty(),
            WaitFor => Core.Span.Empty(),
            Return => Core.Span.Empty(),
            List or Put or Remove => Core.Span.Empty(),
            // Statement::ExportData(ExportData { options, query, connection })
            ExportData { Value: { Options: var options, Query: var query, Connection: var connection } } => UnionSpans(
                options.Select(i => i.Span())
                    .Concat(Once(query.Span()))
                    .Concat(OptionIter(connection).Select(i => i.Span()))),
            CreateUser => Core.Span.Empty(),
            AlterSchema s => s.Value.Span(),
            Vacuum => Core.Span.Empty(),
            AlterUser => Core.Span.Empty(),
            Reset => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de Statement não tratada: {GetType().Name}"),
        };
}

// impl Spanned for Use (spans.rs:530)
public abstract partial record Use : ISpanned
{
    public Span Span() =>
        this switch
        {
            Catalog objectName => objectName.Value.Span(),
            Schema objectName => objectName.Value.Span(),
            Database objectName => objectName.Value.Span(),
            Warehouse objectName => objectName.Value.Span(),
            Role objectName => objectName.Value.Span(),
            // Use::SecondaryRoles(secondary_roles) => {
            //     if let SecondaryRoles::List(roles) = secondary_roles {
            //         return union_spans(roles.iter().map(|i| i.span));
            //     }
            //     Span::empty()
            // }
            SecondaryRoles secondaryRoles => secondaryRoles.Value is Ast.SecondaryRoles.List roles
                ? UnionSpans(roles.Value.Select(i => i.Span))
                : Core.Span.Empty(),
            Object objectName => objectName.Value.Span(),
            Default => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de Use não tratada: {GetType().Name}"),
        };
}

// impl Spanned for CreateTable (spans.rs:550)
public sealed partial record CreateTable : ISpanned
{
    public Span Span()
    {
        // let CreateTable {
        //     or_replace: _,    // bool
        //     temporary: _,     // bool
        //     unlogged: _,      // bool
        //     external: _,      // bool
        //     global: _,        // bool
        //     dynamic: _,       // bool
        //     if_not_exists: _, // bool
        //     transient: _,     // bool
        //     volatile: _,      // bool
        //     iceberg: _,       // bool, Snowflake specific
        //     snapshot: _,      // bool, BigQuery specific
        //     name,
        //     columns,
        //     constraints,
        //     hive_distribution: _, // hive specific
        //     hive_formats: _,      // hive specific
        //     file_format: _,       // enum
        //     location: _,          // string, no span
        //     query,
        //     without_rowid: _, // bool
        //     like: _,
        //     clone,
        //     comment: _, // todo, no span
        //     on_commit: _,
        //     on_cluster: _,   // todo, clickhouse specific
        //     primary_key: _,  // todo, clickhouse specific
        //     order_by: _,     // todo, clickhouse specific
        //     partition_by: _, // todo, BigQuery specific
        //     cluster_by: _,   // todo, BigQuery specific
        //     clustered_by: _, // todo, Hive specific
        //     inherits: _,     // todo, PostgreSQL specific
        //     partition_of,
        //     for_values,
        //     strict: _,                          // bool
        //     copy_grants: _,                     // bool
        //     enable_schema_evolution: _,         // bool
        //     change_tracking: _,                 // bool
        //     data_retention_time_in_days: _,     // u64, no span
        //     max_data_extension_time_in_days: _, // u64, no span
        //     default_ddl_collation: _,           // string, no span
        //     with_aggregation_policy: _,         // todo, Snowflake specific
        //     with_row_access_policy: _,          // todo, Snowflake specific
        //     with_storage_lifecycle_policy: _,   // todo, Snowflake specific
        //     with_tags: _,                       // todo, Snowflake specific
        //     external_volume: _,                 // todo, Snowflake specific
        //     with_connection: _,                 // todo, BigQuery external table connection
        //     base_location: _,                   // todo, Snowflake specific
        //     catalog: _,                         // todo, Snowflake specific
        //     catalog_sync: _,                    // todo, Snowflake specific
        //     storage_serialization_policy: _,
        //     table_options,
        //     target_lag: _,
        //     warehouse: _,
        //     version: _,
        //     refresh_mode: _,
        //     initialize: _,
        //     require_user: _,
        //     diststyle: _,
        //     distkey: _,
        //     sortkey: _,
        //     backup: _,
        //     multiset: _,
        //     fallback: _,
        //     with_data: _,
        // } = self;

        return UnionSpans(
            Once(Name.Span())
                .Concat(Once(TableOptions.Span()))
                .Concat(Columns.Select(i => i.Span()))
                .Concat(Constraints.Select(i => i.Span()))
                .Concat(OptionIter(Query).Select(i => i.Span()))
                .Concat(OptionIter(CloneValue).Select(i => i.Span()))
                .Concat(OptionIter(PartitionOf).Select(i => i.Span()))
                .Concat(OptionIter(ForValues).Select(i => i.Span())));
    }
}

// impl Spanned for ColumnDef (spans.rs:632)
public sealed partial record ColumnDef : ISpanned
{
    public Span Span()
    {
        // let ColumnDef {
        //     name,
        //     data_type: _, // enum
        //     options,
        // } = self;

        return UnionSpans(Once(Name.Span).Concat(Options.Select(i => i.Span())));
    }
}

// impl Spanned for ColumnOptionDef (spans.rs:644)
public sealed partial record ColumnOptionDef : ISpanned
{
    public Span Span()
    {
        // let ColumnOptionDef { name, option } = self;

        return Option.Span().UnionOpt(Name?.Span);
    }
}

// impl Spanned for TableConstraint (spans.rs:652)
public abstract partial record TableConstraint : ISpanned
{
    public Span Span() =>
        this switch
        {
            Unique constraint => constraint.Value.Span(),
            PrimaryKey constraint => constraint.Value.Span(),
            ForeignKey constraint => constraint.Value.Span(),
            Check constraint => constraint.Value.Span(),
            Index constraint => constraint.Value.Span(),
            FulltextOrSpatial constraint => constraint.Value.Span(),
            // TableConstraint::PrimaryKeyUsingIndex(constraint) | TableConstraint::UniqueUsingIndex(constraint)
            // => constraint.span(): o C# não liga variáveis em padrões `or` (decisão 5 da B2), então
            // há um braço por variante, com o mesmo corpo.
            PrimaryKeyUsingIndex constraint => constraint.Value.Span(),
            UniqueUsingIndex constraint => constraint.Value.Span(),
            Exclude constraint => constraint.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de TableConstraint não tratada: {GetType().Name}"),
        };
}

// impl Spanned for PartitionBoundValue (spans.rs:668)
public abstract partial record PartitionBoundValue : ISpanned
{
    public Span Span() =>
        this switch
        {
            Expr expr => expr.Value.Span(),
            // MINVALUE and MAXVALUE are keywords without tracked spans
            MinValue => Core.Span.Empty(),
            MaxValue => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de PartitionBoundValue não tratada: {GetType().Name}"),
        };
}

// impl Spanned for ForValues (spans.rs:679)
public abstract partial record ForValues : ISpanned
{
    public Span Span() =>
        this switch
        {
            In exprs => UnionSpans(exprs.Value.Select(e => e.Span())),
            // ForValues::From { from, to }
            From f => UnionSpans(
                f.FromValue.Select(v => v.Span())
                    .Concat(f.To.Select(v => v.Span()))),
            // WITH (MODULUS n, REMAINDER r) - u64 values have no spans
            With => Core.Span.Empty(),
            Default => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de ForValues não tratada: {GetType().Name}"),
        };
}

// impl Spanned for CreateIndex (spans.rs:695)
public sealed partial record CreateIndex : ISpanned
{
    public Span Span()
    {
        // let CreateIndex {
        //     name,
        //     table_name,
        //     using: _,
        //     columns,
        //     unique: _,        // bool
        //     concurrently: _,  // bool
        //     r#async: _,       // bool
        //     if_not_exists: _, // bool
        //     include,
        //     nulls_distinct: _, // bool
        //     with,
        //     predicate,
        //     index_options: _,
        //     alter_options,
        // } = self;

        return UnionSpans(
            OptionIter(Name).Select(i => i.Span())
                .Concat(Once(TableName.Span()))
                .Concat(Columns.Select(i => i.Column.Span()))
                .Concat(Include.Select(i => i.Span))
                .Concat(With.Select(i => i.Span()))
                .Concat(OptionIter(Predicate).Select(i => i.Span()))
                .Concat(AlterOptions.Select(i => i.Span())));
    }
}

// impl Spanned for IndexColumn (spans.rs:727)
public sealed partial record IndexColumn : ISpanned
{
    public Span Span() => Column.Span();
}

// impl Spanned for ExcludeConstraintElement (spans.rs:733)
public sealed partial record ExcludeConstraintElement : ISpanned
{
    public Span Span() => Column.Span();
}

// impl Spanned for CaseStatement (spans.rs:739)
public sealed partial record CaseStatement : ISpanned
{
    public Span Span()
    {
        // let CaseStatement {
        //     case_token: AttachedToken(start),
        //     match_expr: _,
        //     when_blocks: _,
        //     else_block: _,
        //     end_case_token: AttachedToken(end),
        // } = self;
        var start = CaseToken.Value;
        var end = EndCaseToken.Value;

        return UnionSpans(new[] { start.Span, end.Span });
    }
}

// impl Spanned for IfStatement (spans.rs:753)
public sealed partial record IfStatement : ISpanned
{
    public Span Span()
    {
        // let IfStatement {
        //     if_block,
        //     elseif_blocks,
        //     else_block,
        //     end_token,
        // } = self;

        return UnionSpans(
            Once(IfBlock.Span())
                .Concat(ElseifBlocks.Select(b => b.Span()))
                .Concat(OptionIter(ElseBlock).Select(b => b.Span()))
                // end_token.as_ref().map(|AttachedToken(t)| t.span)
                .Concat(OptionIter(EndToken).Select(t => t.Value.Span)));
    }
}

// impl Spanned for WhileStatement (spans.rs:771)
public sealed partial record WhileStatement : ISpanned
{
    public Span Span()
    {
        // let WhileStatement { while_block } = self;

        return WhileBlock.Span();
    }
}

// impl Spanned for ConditionalStatements (spans.rs:779)
public abstract partial record ConditionalStatements : ISpanned
{
    public Span Span() =>
        this switch
        {
            // ConditionalStatements::Sequence { statements } => union_spans(statements.iter().map(|s| s.span()))
            Sequence sequence => UnionSpans(sequence.Statements.Select(s => s.Span())),
            BeginEnd bes => bes.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de ConditionalStatements não tratada: {GetType().Name}"),
        };
}

// impl Spanned for ConditionalStatementBlock (spans.rs:790)
public sealed partial record ConditionalStatementBlock : ISpanned
{
    public Span Span()
    {
        // let ConditionalStatementBlock {
        //     start_token: AttachedToken(start_token),
        //     condition,
        //     then_token,
        //     conditional_statements,
        // } = self;
        var startToken = StartToken.Value;

        return UnionSpans(
            Once(startToken.Span)
                .Concat(OptionIter(Condition).Select(c => c.Span()))
                // then_token.as_ref().map(|AttachedToken(t)| t.span)
                .Concat(OptionIter(ThenToken).Select(t => t.Value.Span))
                .Concat(Once(ConditionalStatements.Span())));
    }
}

// impl Spanned for RaiseStatement (spans.rs:808)
public sealed partial record RaiseStatement : ISpanned
{
    public Span Span()
    {
        // let RaiseStatement { value } = self;

        return UnionSpans(OptionIter(Value).Select(value => value.Span()));
    }
}

// impl Spanned for RaiseStatementValue (spans.rs:816)
public abstract partial record RaiseStatementValue : ISpanned
{
    public Span Span() =>
        this switch
        {
            UsingMessage expr => expr.Value.Span(),
            Expr expr => expr.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de RaiseStatementValue não tratada: {GetType().Name}"),
        };
}

// impl Spanned for ColumnOption (spans.rs:835)
public abstract partial record ColumnOption : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>ColumnOption::Null</c></item>
    /// <item><c>ColumnOption::NotNull</c></item>
    /// <item><c>ColumnOption::Comment</c></item>
    /// <item><c>ColumnOption::PrimaryKey</c></item>
    /// <item><c>ColumnOption::Unique</c></item>
    /// <item><c>ColumnOption::DialectSpecific</c></item>
    /// <item><c>ColumnOption::Generated</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            Null => Core.Span.Empty(),
            NotNull => Core.Span.Empty(),
            Default expr => expr.Value.Span(),
            Materialized expr => expr.Value.Span(),
            // expr.as_ref().map_or(Span::empty(), |e| e.span())
            Ephemeral expr => expr.Value is { } e ? e.Span() : Core.Span.Empty(),
            Alias expr => expr.Value.Span(),
            PrimaryKey constraint => constraint.Value.Span(),
            Unique constraint => constraint.Value.Span(),
            Check constraint => constraint.Value.Span(),
            ForeignKey constraint => constraint.Value.Span(),
            DialectSpecific => Core.Span.Empty(),
            CharacterSet objectName => objectName.Value.Span(),
            Collation objectName => objectName.Value.Span(),
            Comment => Core.Span.Empty(),
            OnUpdate expr => expr.Value.Span(),
            Generated => Core.Span.Empty(),
            Options vec => UnionSpans(vec.Value.Select(i => i.Span())),
            Identity => Core.Span.Empty(),
            OnConflict => Core.Span.Empty(),
            Policy => Core.Span.Empty(),
            Tags => Core.Span.Empty(),
            Srid => Core.Span.Empty(),
            Invisible => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de ColumnOption não tratada: {GetType().Name}"),
        };
}

// impl Spanned for ReferentialAction (spans.rs:866)
public abstract partial record ReferentialAction : ISpanned
{
    /// <remarks><b>missing span</b></remarks>
    public Span Span() => Core.Span.Empty();
}

// impl Spanned for ConstraintCharacteristics (spans.rs:873)
public sealed partial record ConstraintCharacteristics : ISpanned
{
    /// <remarks><b>missing span</b></remarks>
    public Span Span()
    {
        // let ConstraintCharacteristics {
        //     deferrable: _, // bool
        //     initially: _,  // enum
        //     enforced: _,   // bool
        // } = self;

        return Core.Span.Empty();
    }
}

// impl Spanned for Analyze (spans.rs:885)
public sealed partial record Analyze : ISpanned
{
    public Span Span() =>
        UnionSpans(
            OptionIter(TableName)
                .Select(t => t.Span())
                .Concat(OptionIter(Partitions).SelectMany(i => i.Select(k => k.Span())))
                .Concat(Columns.Select(i => i.Span)));
}

// impl Spanned for AlterColumnOperation (spans.rs:908)
public abstract partial record AlterColumnOperation : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>AlterColumnOperation::SetNotNull</c></item>
    /// <item><c>AlterColumnOperation::DropNotNull</c></item>
    /// <item><c>AlterColumnOperation::DropDefault</c></item>
    /// <item><c>AlterColumnOperation::AddGenerated</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            SetNotNull => Core.Span.Empty(),
            DropNotNull => Core.Span.Empty(),
            // AlterColumnOperation::SetDefault { value } => value.span()
            SetDefault s => s.Value.Span(),
            DropDefault => Core.Span.Empty(),
            // AlterColumnOperation::SetDataType { data_type: _, using, had_set: _ }
            //     => using.as_ref().map_or(Span::empty(), |u| u.span())
            SetDataType s => s.Using is { } u ? u.Span() : Core.Span.Empty(),
            AddGenerated => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de AlterColumnOperation não tratada: {GetType().Name}"),
        };
}

// impl Spanned for CopySource (spans.rs:925)
public abstract partial record CopySource : ISpanned
{
    public Span Span() =>
        this switch
        {
            // CopySource::Table { table_name, columns }
            Table t => UnionSpans(
                Once(t.TableName.Span()).Concat(t.Columns.Select(i => i.Span))),
            Query query => query.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de CopySource não tratada: {GetType().Name}"),
        };
}

// impl Spanned for Delete (spans.rs:939)
public sealed partial record Delete : ISpanned
{
    public Span Span()
    {
        // let Delete {
        //     delete_token,
        //     optimizer_hints: _,
        //     tables,
        //     from,
        //     using,
        //     selection,
        //     returning,
        //     output,
        //     order_by,
        //     limit,
        // } = self;

        return UnionSpans(
            Once(DeleteToken.Value.Span).Concat(
                Tables
                    .Select(i => i.Span())
                    .Concat(Once(From.Span()))
                    .Concat(
                        OptionIter(Using)
                            .Select(u => UnionSpans(u.Select(i => i.Span()))))
                    .Concat(OptionIter(Selection).Select(i => i.Span()))
                    .Concat(OptionIter(Returning).SelectMany(i => i.Select(k => k.Span())))
                    .Concat(OptionIter(Output).Select(i => i.Span()))
                    .Concat(OrderBy.Select(i => i.Span()))
                    .Concat(OptionIter(Limit).Select(i => i.Span()))));
    }
}

// impl Spanned for Update (spans.rs:975)
public sealed partial record Update : ISpanned
{
    public Span Span()
    {
        // let Update {
        //     update_token,
        //     optimizer_hints: _,
        //     table,
        //     assignments,
        //     from,
        //     selection,
        //     returning,
        //     output,
        //     or: _,
        //     order_by,
        //     limit,
        // } = self;

        return UnionSpans(
            Once(Table.Span())
                .Concat(Once(UpdateToken.Value.Span))
                .Concat(Assignments.Select(i => i.Span()))
                .Concat(OptionIter(From).Select(i => i.Span()))
                .Concat(OptionIter(Selection).Select(i => i.Span()))
                .Concat(OptionIter(Returning).SelectMany(i => i.Select(k => k.Span())))
                .Concat(OptionIter(Output).Select(i => i.Span()))
                .Concat(OrderBy.Select(i => i.Span()))
                .Concat(OptionIter(Limit).Select(i => i.Span())));
    }
}
