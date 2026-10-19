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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/spans.rs (linhas 1005–1723:
// impl Spanned for Merge … impl Spanned for AccessExpr).
// Modificado: tradução de Rust para C#.

using static Lepton.SqlParser.Core.Ast.SpansModule;
using static Lepton.SqlParser.Core.RustIter;

namespace Lepton.SqlParser.Core.Ast;

// Mesmas regras de Spans.Part1.cs (onda-b/B7.md).

// impl Spanned for Merge (spans.rs:1005)
public sealed partial record Merge : ISpanned
{
    public Span Span() =>
        UnionSpans(
            new[] { MergeToken.Value.Span, On.Span() }
                .Concat(Clauses.Select(i => i.Span()))
                .Concat(OptionIter(Output).Select(i => i.Span())));
}

// impl Spanned for FromTable (spans.rs:1016)
public abstract partial record FromTable : ISpanned
{
    public Span Span() =>
        this switch
        {
            WithFromKeyword vec => UnionSpans(vec.Value.Select(i => i.Span())),
            WithoutKeyword vec => UnionSpans(vec.Value.Select(i => i.Span())),
            _ => throw new InvalidOperationException($"Variante de FromTable não tratada: {GetType().Name}"),
        };
}

// impl Spanned for ViewColumnDef (spans.rs:1025)
public sealed partial record ViewColumnDef : ISpanned
{
    public Span Span()
    {
        // let ViewColumnDef {
        //     name,
        //     data_type: _, // todo, DataType
        //     options,
        // } = self;

        return Name.Span.UnionOpt(Options?.Span());
    }
}

// impl Spanned for ColumnOptions (spans.rs:1037)
public abstract partial record ColumnOptions : ISpanned
{
    public Span Span() => UnionSpans(AsSlice().Select(i => i.Span()));
}

// impl Spanned for SqlOption (spans.rs:1043)
public abstract partial record SqlOption : ISpanned
{
    public Span Span() =>
        this switch
        {
            Clustered tableOptionsClustered => tableOptionsClustered.Value.Span(),
            Ident ident => ident.Value.Span,
            // SqlOption::KeyValue { key, value } => key.span.union(&value.span())
            KeyValue keyValue => keyValue.Key.Span.Union(keyValue.Value.Span()),
            // SqlOption::Partition { column_name, range_direction: _, for_values }
            Partition partition => UnionSpans(
                Once(partition.ColumnName.Span).Concat(partition.ForValues.Select(i => i.Span()))),
            TableSpace => Core.Span.Empty(),
            Comment => Core.Span.Empty(),
            // SqlOption::NamedParenthesizedList(NamedParenthesizedList { key: name, name: value, values })
            NamedParenthesizedList { Value: { Key: var name, Name: var value, Values: var values } } =>
                UnionSpans(Once(name.Span).Concat(values.Select(i => i.Span)))
                    .UnionOpt(value?.Span),
            _ => throw new InvalidOperationException($"Variante de SqlOption não tratada: {GetType().Name}"),
        };
}

// impl Spanned for TableOptionsClustered (spans.rs:1072)
public abstract partial record TableOptionsClustered : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>TableOptionsClustered::ColumnstoreIndex</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            ColumnstoreIndex => Core.Span.Empty(),
            ColumnstoreIndexOrder vec => UnionSpans(vec.Value.Select(i => i.Span)),
            Index vec => UnionSpans(vec.Value.Select(i => i.Span())),
            _ => throw new InvalidOperationException($"Variante de TableOptionsClustered não tratada: {GetType().Name}"),
        };
}

// impl Spanned for ClusteredIndex (spans.rs:1084)
public sealed partial record ClusteredIndex : ISpanned
{
    public Span Span()
    {
        // let ClusteredIndex {
        //     name,
        //     asc: _, // bool
        // } = self;

        return Name.Span;
    }
}

// impl Spanned for CreateTableOptions (spans.rs:1095)
public abstract partial record CreateTableOptions : ISpanned
{
    public Span Span() =>
        this switch
        {
            None => Core.Span.Empty(),
            With vec => UnionSpans(vec.Value.Select(i => i.Span())),
            // vec.as_slice().iter(): a Sequence já é a fatia.
            Options vec => UnionSpans(vec.Value.Select(i => i.Span())),
            Plain vec => UnionSpans(vec.Value.Select(i => i.Span())),
            TableProperties vec => UnionSpans(vec.Value.Select(i => i.Span())),
            _ => throw new InvalidOperationException($"Variante de CreateTableOptions não tratada: {GetType().Name}"),
        };
}

// impl Spanned for AlterTableOperation (spans.rs:1113)
public abstract partial record AlterTableOperation : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>AlterTableOperation::OwnerTo</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            // AlterTableOperation::AddConstraint { constraint, not_valid: _ }
            AddConstraint a => a.Constraint.Span(),
            // AlterTableOperation::AddColumn { column_keyword: _, if_not_exists: _, column_def, column_position: _ }
            AddColumn a => a.ColumnDef.Span(),
            // AlterTableOperation::AddProjection { if_not_exists: _, name, select }
            AddProjection a => a.Name.Span.Union(a.Select.Span()),
            DropProjection d => d.Name.Span,
            // AlterTableOperation::MaterializeProjection { if_exists: _, name, partition }
            MaterializeProjection m => m.Name.Span.UnionOpt(m.Partition?.Span),
            ClearProjection c => c.Name.Span.UnionOpt(c.Partition?.Span),
            DisableRowLevelSecurity => Core.Span.Empty(),
            DisableRule d => d.Name.Span,
            DisableTrigger d => d.Name.Span,
            // AlterTableOperation::DropConstraint { if_exists: _, name, drop_behavior: _ }
            DropConstraint d => d.Name.Span,
            // AlterTableOperation::DropColumn { has_column_keyword: _, column_names, if_exists: _, drop_behavior: _ }
            DropColumn d => UnionSpans(d.ColumnNames.Select(i => i.Span)),
            AttachPartition a => a.Partition.Span(),
            DetachPartition d => d.Partition.Span(),
            // AlterTableOperation::FreezePartition { partition, with_name }
            FreezePartition f => f.Partition.Span().UnionOpt(f.WithName?.Span),
            UnfreezePartition u => u.Partition.Span().UnionOpt(u.WithName?.Span),
            DropPrimaryKey => Core.Span.Empty(),
            DropForeignKey d => d.Name.Span,
            DropIndex d => d.Name.Span,
            EnableAlwaysRule e => e.Name.Span,
            EnableAlwaysTrigger e => e.Name.Span,
            EnableReplicaRule e => e.Name.Span,
            EnableReplicaTrigger e => e.Name.Span,
            EnableRowLevelSecurity => Core.Span.Empty(),
            ForceRowLevelSecurity => Core.Span.Empty(),
            NoForceRowLevelSecurity => Core.Span.Empty(),
            EnableRule e => e.Name.Span,
            EnableTrigger e => e.Name.Span,
            // AlterTableOperation::RenamePartitions { old_partitions, new_partitions }
            RenamePartitions r => UnionSpans(
                r.OldPartitions
                    .Select(i => i.Span())
                    .Concat(r.NewPartitions.Select(i => i.Span()))),
            // AlterTableOperation::AddPartitions { if_not_exists: _, new_partitions }
            AddPartitions a => UnionSpans(a.NewPartitions.Select(i => i.Span())),
            // AlterTableOperation::DropPartitions { partitions, if_exists: _ }
            DropPartitions d => UnionSpans(d.Partitions.Select(i => i.Span())),
            // AlterTableOperation::RenameColumn { old_column_name, new_column_name }
            RenameColumn r => r.OldColumnName.Span.Union(r.NewColumnName.Span),
            RenameTable r => r.TableName.Span(),
            // AlterTableOperation::ChangeColumn { old_name, new_name, data_type: _, options, column_position: _ }
            ChangeColumn c => UnionSpans(
                Once(c.OldName.Span)
                    .Concat(Once(c.NewName.Span))
                    .Concat(c.Options.Select(i => i.Span()))),
            // AlterTableOperation::ModifyColumn { col_name, data_type: _, options, column_position: _ }
            ModifyColumn m => UnionSpans(Once(m.ColName.Span).Concat(m.Options.Select(i => i.Span()))),
            // AlterTableOperation::ModifyOrderBy { order_by }
            ModifyOrderBy m => UnionSpans(m.OrderBy.Select(e => e.Span())),
            // AlterTableOperation::RenameConstraint { old_name, new_name }
            RenameConstraint r => r.OldName.Span.Union(r.NewName.Span),
            // AlterTableOperation::AlterColumn { column_name, op }
            AlterColumn a => a.ColumnName.Span.Union(a.Op.Span()),
            SwapWith s => s.TableName.Span(),
            SetTblProperties s => UnionSpans(s.TableProperties.Select(i => i.Span())),
            SetLogged => Core.Span.Empty(),
            SetUnlogged => Core.Span.Empty(),
            OwnerTo => Core.Span.Empty(),
            ClusterBy c => UnionSpans(c.Exprs.Select(e => e.Span())),
            DropClusteringKey => Core.Span.Empty(),
            AlterSortKey => Core.Span.Empty(),
            SuspendRecluster => Core.Span.Empty(),
            ResumeRecluster => Core.Span.Empty(),
            Refresh => Core.Span.Empty(),
            Suspend => Core.Span.Empty(),
            Resume => Core.Span.Empty(),
            Algorithm => Core.Span.Empty(),
            // AlterTableOperation::AutoIncrement { value, .. } => value.span(): `ValueWithSpan`
            // implementa ISpanned de forma explícita (tem a propriedade `Span`).
            AutoIncrement a => ((ISpanned)a.Value).Span(),
            Lock => Core.Span.Empty(),
            ReplicaIdentity => Core.Span.Empty(),
            ValidateConstraint v => v.Name.Span,
            SetOptionsParens s => UnionSpans(s.Options.Select(i => i.Span())),
            _ => throw new InvalidOperationException($"Variante de AlterTableOperation não tratada: {GetType().Name}"),
        };
}

// impl Spanned for Partition (spans.rs:1256)
public abstract partial record Partition : ISpanned
{
    public Span Span() =>
        this switch
        {
            Identifier ident => ident.Value.Span,
            Expr expr => expr.Value.Span(),
            Part expr => expr.Value.Span(),
            Partitions vec => UnionSpans(vec.Value.Select(i => i.Span())),
            _ => throw new InvalidOperationException($"Variante de Partition não tratada: {GetType().Name}"),
        };
}

// impl Spanned for ProjectionSelect (spans.rs:1267)
public sealed partial record ProjectionSelect : ISpanned
{
    public Span Span()
    {
        // let ProjectionSelect {
        //     projection,
        //     order_by,
        //     group_by,
        // } = self;

        return UnionSpans(
            Projection
                .Select(i => i.Span())
                .Concat(OptionIter(OrderBy).Select(i => i.Span()))
                .Concat(OptionIter(GroupBy).Select(i => i.Span())));
    }
}

// impl Spanned for OrderBy (spans.rs:1289)
public sealed partial record OrderBy : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>OrderByKind::All</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        Kind switch
        {
            OrderByKind.All => Core.Span.Empty(),
            OrderByKind.Expressions exprs => UnionSpans(
                exprs.Value
                    .Select(i => i.Span())
                    .Concat(OptionIter(Interpolate).Select(i => i.Span()))),
            _ => throw new InvalidOperationException($"Variante de OrderByKind não tratada: {Kind.GetType().Name}"),
        };
}

// impl Spanned for GroupByExpr (spans.rs:1307)
public abstract partial record GroupByExpr : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>GroupByExpr::All</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            All => Core.Span.Empty(),
            // GroupByExpr::Expressions(exprs, _modifiers)
            Expressions exprs => UnionSpans(exprs.Exprs.Select(i => i.Span())),
            _ => throw new InvalidOperationException($"Variante de GroupByExpr não tratada: {GetType().Name}"),
        };
}

// impl Spanned for Interpolate (spans.rs:1318)
public sealed partial record Interpolate : ISpanned
{
    public Span Span()
    {
        // let Interpolate { exprs } = self;

        return UnionSpans(OptionIter(Exprs).SelectMany(i => i.Select(e => e.Span())));
    }
}

// impl Spanned for InterpolateExpr (spans.rs:1326)
public sealed partial record InterpolateExpr : ISpanned
{
    public Span Span()
    {
        // let InterpolateExpr { column, expr } = self;

        return Column.Span.UnionOpt(Expr?.Span());
    }
}

// impl Spanned for AlterIndexOperation (spans.rs:1334)
public abstract partial record AlterIndexOperation : ISpanned
{
    public Span Span() =>
        this switch
        {
            // AlterIndexOperation::RenameIndex { index_name } => index_name.span()
            RenameIndex r => r.IndexName.Span(),
            _ => throw new InvalidOperationException($"Variante de AlterIndexOperation não tratada: {GetType().Name}"),
        };
}

// impl Spanned for Insert (spans.rs:1346)
public sealed partial record Insert : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:ever</para>
    /// <list type="bullet">
    /// <item><c>Insert::insert_alias</c></item>
    /// </list>
    /// </remarks>
    public Span Span()
    {
        // let Insert {
        //     insert_token,
        //     optimizer_hints: _,
        //     or: _,     // enum, sqlite specific
        //     ignore: _, // bool
        //     into: _,   // bool
        //     table,
        //     table_alias,
        //     columns,
        //     by_name: _,   // bool
        //     overwrite: _, // bool
        //     source,
        //     partitioned,
        //     after_columns,
        //     has_table_keyword: _, // bool
        //     on,
        //     returning,
        //     output,
        //     replace_into: _, // bool
        //     priority: _,     // todo, mysql specific
        //     insert_alias: _, // todo, mysql specific
        //     assignments,
        //     settings: _,                 // todo, clickhouse specific
        //     format_clause: _,            // todo, clickhouse specific
        //     multi_table_insert_type: _,  // snowflake multi-table insert
        //     multi_table_into_clauses: _, // snowflake multi-table insert
        //     multi_table_when_clauses: _, // snowflake multi-table insert
        //     multi_table_else_clause: _,  // snowflake multi-table insert
        // } = self;

        return UnionSpans(
            Once(InsertToken.Value.Span)
                .Concat(Once(Table.Span()))
                .Concat(OptionIter(TableAlias).Select(k => k.Alias.Span))
                .Concat(Columns.Select(i => i.Span()))
                .Concat(OptionIter(Source).Select(q => q.Span()))
                .Concat(Assignments.Select(i => i.Span()))
                .Concat(OptionIter(Partitioned).SelectMany(i => i.Select(k => k.Span())))
                .Concat(AfterColumns.Select(i => i.Span))
                .Concat(OptionIter(On).Select(i => i.Span()))
                .Concat(OptionIter(Returning).SelectMany(i => i.Select(k => k.Span())))
                .Concat(OptionIter(Output).Select(i => i.Span())));
    }
}

// impl Spanned for OnInsert (spans.rs:1393)
public abstract partial record OnInsert : ISpanned
{
    public Span Span() =>
        this switch
        {
            DuplicateKeyUpdate vec => UnionSpans(vec.Value.Select(i => i.Span())),
            OnConflict onConflict => onConflict.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de OnInsert não tratada: {GetType().Name}"),
        };
}

// impl Spanned for OnConflict (spans.rs:1402)
public sealed partial record OnConflict : ISpanned
{
    public Span Span()
    {
        // let OnConflict {
        //     conflict_target,
        //     action,
        // } = self;

        return Action
            .Span()
            .UnionOpt(ConflictTarget?.Span());
    }
}

// impl Spanned for ConflictTarget (spans.rs:1415)
public abstract partial record ConflictTarget : ISpanned
{
    public Span Span() =>
        this switch
        {
            Columns vec => UnionSpans(vec.Value.Select(i => i.Span)),
            OnConstraint objectName => objectName.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de ConflictTarget não tratada: {GetType().Name}"),
        };
}

// impl Spanned for OnConflictAction (spans.rs:1428)
public abstract partial record OnConflictAction : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>OnConflictAction::DoNothing</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            DoNothing => Core.Span.Empty(),
            DoUpdate doUpdate => doUpdate.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de OnConflictAction não tratada: {GetType().Name}"),
        };
}

// impl Spanned for DoUpdate (spans.rs:1437)
public sealed partial record DoUpdate : ISpanned
{
    public Span Span()
    {
        // let DoUpdate {
        //     assignments,
        //     selection,
        // } = self;

        return UnionSpans(
            Assignments
                .Select(i => i.Span())
                .Concat(OptionIter(Selection).Select(i => i.Span())));
    }
}

// impl Spanned for Assignment (spans.rs:1453)
public sealed partial record Assignment : ISpanned
{
    public Span Span()
    {
        // let Assignment { target, value } = self;

        return Target.Span().Union(Value.Span());
    }
}

// impl Spanned for AssignmentTarget (spans.rs:1461)
public abstract partial record AssignmentTarget : ISpanned
{
    public Span Span() =>
        this switch
        {
            ColumnName objectName => objectName.Value.Span(),
            Tuple vec => UnionSpans(vec.Value.Select(i => i.Span())),
            _ => throw new InvalidOperationException($"Variante de AssignmentTarget não tratada: {GetType().Name}"),
        };
}

// impl Spanned for Expr (spans.rs:1483)
public abstract partial record Expr : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>
    /// Most expressions are missing keywords in their spans.
    /// f.e. <c>IS NULL &lt;expr&gt;</c> reports as <c>&lt;expr&gt;::span</c>.
    /// </para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>Expr::MatchAgainst</c> # MySQL specific</item>
    /// <item><c>Expr::RLike</c> # MySQL specific</item>
    /// <item><c>Expr::Struct</c> # BigQuery specific</item>
    /// <item><c>Expr::Named</c> # BigQuery specific</item>
    /// <item><c>Expr::Dictionary</c> # DuckDB specific</item>
    /// <item><c>Expr::Map</c> # DuckDB specific</item>
    /// <item><c>Expr::Lambda</c></item>
    /// </list>
    /// </remarks>
    public Span Span()
    {
        // PORT-DEVIATION DIV-005: o Rust não marca este método com #[recursive::recursive], mas aqui cada
        // nível de uma cadeia `a + b + c ...` (não limitada pelo contador de recursão) gasta KBs de pilha;
        // recebe a mesma proteção do Display de Expr (convencoes.md §13, caso 123).
        if (StackGuard.IsStackLow())
        {
            return StackGuard.Grow(this, static expr => expr.Span());
        }

        return this switch
        {
            Identifier ident => ident.Value.Span,
            CompoundIdentifier vec => UnionSpans(vec.Value.Select(i => i.Span)),
            // Expr::CompoundFieldAccess { root, access_chain }
            CompoundFieldAccess c => UnionSpans(
                Once(c.Root.Span()).Concat(c.AccessChain.Select(i => i.Span()))),
            IsFalse expr => expr.Value.Span(),
            IsNotFalse expr => expr.Value.Span(),
            IsTrue expr => expr.Value.Span(),
            IsNotTrue expr => expr.Value.Span(),
            IsNull expr => expr.Value.Span(),
            IsNotNull expr => expr.Value.Span(),
            IsUnknown expr => expr.Value.Span(),
            IsNotUnknown expr => expr.Value.Span(),
            // Expr::IsJson { expr, kind: _, unique_keys: _, negated: _ }
            IsJson isJson => isJson.Expr.Span(),
            // Expr::IsDistinctFrom(lhs, rhs) / Expr::IsNotDistinctFrom(lhs, rhs) (nomes.md: Left/Right)
            IsDistinctFrom d => d.Left.Span().Union(d.Right.Span()),
            IsNotDistinctFrom d => d.Left.Span().Union(d.Right.Span()),
            // Expr::InList { expr, list, negated: _, global: _ }
            InList inList => UnionSpans(
                Once(inList.Expr.Span()).Concat(inList.List.Select(item => item.Span()))),
            // Expr::InSubquery { expr, subquery, negated: _, global: _ }
            InSubquery inSubquery => inSubquery.Expr.Span().Union(inSubquery.Subquery.Span()),
            // Expr::InUnnest { expr, array_expr, negated: _ }
            InUnnest inUnnest => inUnnest.Expr.Span().Union(inUnnest.ArrayExpr.Span()),
            // Expr::Between { expr, negated: _, low, high }
            Between between => between.Expr.Span().Union(between.Low.Span()).Union(between.High.Span()),

            // Expr::BinaryOp { left, op: _, right }
            BinaryOp binaryOp => binaryOp.Left.Span().Union(binaryOp.Right.Span()),
            // Expr::Like { negated: _, expr, pattern, escape_char: _, any: _ }
            Like like => like.Expr.Span().Union(like.Pattern.Span()),
            // Expr::ILike { negated: _, expr, pattern, escape_char: _, any: _ }
            ILike ilike => ilike.Expr.Span().Union(ilike.Pattern.Span()),
            RLike => Core.Span.Empty(),
            // Expr::IsNormalized { expr, form: _, negated: _ }
            IsNormalized isNormalized => isNormalized.Expr.Span(),
            // Expr::SimilarTo { negated: _, expr, pattern, escape_char: _ }
            SimilarTo similarTo => similarTo.Expr.Span().Union(similarTo.Pattern.Span()),
            // Expr::Ceil { expr, field: _ } / Expr::Floor { expr, field: _ }
            Ceil ceil => ceil.Expr.Span(),
            Floor floor => floor.Expr.Span(),
            // Expr::Position { expr, r#in }
            Position position => position.Expr.Span().Union(position.In.Span()),
            // Expr::Overlay { expr, overlay_what, overlay_from, overlay_for }
            Overlay overlay => overlay.Expr
                .Span()
                .Union(overlay.OverlayWhat.Span())
                .Union(overlay.OverlayFrom.Span())
                .UnionOpt(overlay.OverlayFor?.Span()),
            // Expr::Collate { expr, collation }
            Collate collate => collate.Expr
                .Span()
                .Union(UnionSpans(collate.Collation.Value.Select(i => i.Span()))),
            Nested expr => expr.Value.Span(),
            // Expr::Value(value) => value.span(): `ValueWithSpan` implementa ISpanned de forma
            // explícita (tem a propriedade `Span`).
            Value value => ((ISpanned)value.Inner).Span(),
            // Expr::TypedString(TypedString { value, .. }) => value.span()
            TypedString { Value.Value: var value } => ((ISpanned)value).Span(),
            Function function => function.Value.Span(),
            GroupingSets vec => UnionSpans(vec.Value.SelectMany(i => i.Select(k => k.Span()))),
            Cube vec => UnionSpans(vec.Value.SelectMany(i => i.Select(k => k.Span()))),
            Rollup vec => UnionSpans(vec.Value.SelectMany(i => i.Select(k => k.Span()))),
            Tuple vec => UnionSpans(vec.Value.Select(i => i.Span())),
            Array array => array.Value.Span(),
            MatchAgainst => Core.Span.Empty(),
            // Expr::JsonAccess { value, path }
            JsonAccess jsonAccess => jsonAccess.Value.Span().Union(jsonAccess.Path.Span()),
            // Expr::AnyOp { left, compare_op: _, right, is_some: _ }
            AnyOp anyOp => anyOp.Left.Span().Union(anyOp.Right.Span()),
            // Expr::AllOp { left, compare_op: _, right }
            AllOp allOp => allOp.Left.Span().Union(allOp.Right.Span()),
            // Expr::UnaryOp { op: _, expr }
            UnaryOp unaryOp => unaryOp.Expr.Span(),
            // Expr::Convert { expr, data_type: _, charset, target_before_value: _, styles, is_try: _ }
            Convert convert => UnionSpans(
                Once(convert.Expr.Span())
                    .Concat(OptionIter(convert.Charset).Select(i => i.Span()))
                    .Concat(convert.Styles.Select(i => i.Span()))),
            // Expr::Cast { kind: _, expr, data_type: _, format: _ }
            Cast cast => cast.Expr.Span(),
            // Expr::AtTimeZone { timestamp, time_zone }
            AtTimeZone atTimeZone => atTimeZone.Timestamp.Span().Union(atTimeZone.TimeZone.Span()),
            // Expr::Extract { field: _, syntax: _, expr }
            Extract extract => extract.Expr.Span(),
            // Expr::Substring { expr, substring_from, substring_for, special: _, shorthand: _ }
            Substring substring => UnionSpans(
                Once(substring.Expr.Span())
                    .Concat(OptionIter(substring.SubstringFrom).Select(i => i.Span()))
                    .Concat(OptionIter(substring.SubstringFor).Select(i => i.Span()))),
            // Expr::Trim { expr, trim_where: _, trim_what, trim_characters }
            Trim trim => UnionSpans(
                Once(trim.Expr.Span())
                    .Concat(OptionIter(trim.TrimWhat).Select(i => i.Span()))
                    .Concat(
                        OptionIter(trim.TrimCharacters)
                            .Select(items => UnionSpans(items.Select(i => i.Span()))))),
            // Expr::Prefixed { value, .. } => value.span()
            Prefixed prefixed => prefixed.Value.Span(),
            // Expr::Case { case_token, end_token, operand, conditions, else_result }
            Case c => UnionSpans(
                Once(c.CaseToken.Value.Span)
                    .Concat(
                        OptionIter(c.Operand)
                            .Select(i => i.Span())
                            .Concat(c.Conditions.SelectMany(caseWhen =>
                                new[] { caseWhen.Condition.Span(), caseWhen.Result.Span() }))
                            .Concat(OptionIter(c.ElseResult).Select(i => i.Span())))
                    .Concat(Once(c.EndToken.Value.Span))),
            // Expr::Exists { subquery, .. } => subquery.span()
            Exists exists => exists.Subquery.Span(),
            Subquery query => query.Value.Span(),
            Struct => Core.Span.Empty(),
            Named => Core.Span.Empty(),
            Dictionary => Core.Span.Empty(),
            Map => Core.Span.Empty(),
            Interval interval => interval.Value.Value.Span(),
            Wildcard token => token.Value.Value.Span,
            // Expr::QualifiedWildcard(object_name, token) (nomes.md: Prefix, Token)
            QualifiedWildcard q => UnionSpans(
                q.Prefix.Value
                    .Select(i => i.Span())
                    .Concat(Once(q.Token.Value.Span))),
            OuterJoin expr => expr.Value.Span(),
            Prior expr => expr.Value.Span(),
            Lambda => Core.Span.Empty(),
            MemberOf memberOf => memberOf.Value.Value.Span().Union(memberOf.Value.Array.Span()),
            _ => throw new InvalidOperationException($"Variante de Expr não tratada: {GetType().Name}"),
        };
    }
}

// impl Spanned for Subscript (spans.rs:1695)
public abstract partial record Subscript : ISpanned
{
    public Span Span() =>
        this switch
        {
            // Subscript::Index { index } => index.span() (nomes.md: IndexValue)
            Index index => index.IndexValue.Span(),
            // Subscript::Slice { lower_bound, upper_bound, stride }
            // [..].into_iter().flatten() sobre Option<Span> → OfType<Span>() (decisão 16 da B4)
            Slice slice => UnionSpans(
                new Span?[]
                {
                    slice.LowerBound?.Span(),
                    slice.UpperBound?.Span(),
                    slice.Stride?.Span(),
                }
                .OfType<Span>()),
            _ => throw new InvalidOperationException($"Variante de Subscript não tratada: {GetType().Name}"),
        };
}

// impl Spanned for AccessExpr (spans.rs:1716)
public abstract partial record AccessExpr : ISpanned
{
    public Span Span() =>
        this switch
        {
            Dot ident => ident.Value.Span(),
            Subscript subscript => subscript.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de AccessExpr não tratada: {GetType().Name}"),
        };
}
