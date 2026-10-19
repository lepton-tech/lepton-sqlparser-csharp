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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/spans.rs (linhas 1725–2628:
// impl Spanned for ObjectName … impl Spanned for comments::CommentWithSpan).
// Modificado: tradução de Rust para C#.

using static SqlParser.Core.Ast.SpansModule;
using static SqlParser.Core.RustIter;

namespace SqlParser.Core.Ast;

// Mesmas regras de Spans.Part1.cs (onda-b/B7.md).

// impl Spanned for ObjectName (spans.rs:1725)
public sealed partial record ObjectName : ISpanned
{
    public Span Span()
    {
        // let ObjectName(segments) = self;
        var segments = Value;

        return UnionSpans(segments.Select(i => i.Span()));
    }
}

// impl Spanned for ObjectNamePart (spans.rs:1733)
public abstract partial record ObjectNamePart : ISpanned
{
    public Span Span() =>
        this switch
        {
            Identifier ident => ident.Value.Span,
            Function func => func.Value.Name
                .Span
                .Union(UnionSpans(func.Value.Args.Select(i => i.Span()))),
            _ => throw new InvalidOperationException($"Variante de ObjectNamePart não tratada: {GetType().Name}"),
        };
}

// impl Spanned for Array (spans.rs:1745)
public sealed partial record Array : ISpanned
{
    public Span Span()
    {
        // let Array {
        //     elem,
        //     named: _, // bool
        // } = self;

        return UnionSpans(Elem.Select(i => i.Span()));
    }
}

// impl Spanned for Function (spans.rs:1756)
public sealed partial record Function : ISpanned
{
    public Span Span()
    {
        // let Function {
        //     name,
        //     uses_odbc_syntax: _,
        //     parameters,
        //     args,
        //     filter,
        //     null_treatment: _, // enum
        //     over: _,           // todo
        //     within_group,
        // } = self;

        return UnionSpans(
            Name.Value
                .Select(i => i.Span())
                .Concat(Once(Args.Span()))
                .Concat(Once(Parameters.Span()))
                .Concat(OptionIter(Filter).Select(i => i.Span()))
                .Concat(WithinGroup.Select(i => i.Span())));
    }
}

// impl Spanned for FunctionArguments (spans.rs:1784)
public abstract partial record FunctionArguments : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>The span of <c>FunctionArguments::None</c> is empty.</para>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            None => Core.Span.Empty(),
            Subquery query => query.Value.Span(),
            List list => list.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de FunctionArguments não tratada: {GetType().Name}"),
        };
}

// impl Spanned for FunctionArgumentList (spans.rs:1794)
public sealed partial record FunctionArgumentList : ISpanned
{
    public Span Span()
    {
        // let FunctionArgumentList {
        //     duplicate_treatment: _, // enum
        //     args,
        //     clauses,
        // } = self;

        return UnionSpans(
            // # todo: duplicate-treatment span
            Args
                .Select(i => i.Span())
                .Concat(Clauses.Select(i => i.Span())));
    }
}

// impl Spanned for FunctionArgumentClause (spans.rs:1811)
public abstract partial record FunctionArgumentClause : ISpanned
{
    public Span Span() =>
        this switch
        {
            IgnoreOrRespectNulls => Core.Span.Empty(),
            Where expr => expr.Value.Span(),
            OrderBy vec => UnionSpans(vec.Value.Select(i => i.Expr.Span())),
            Limit expr => expr.Value.Span(),
            OnOverflow => Core.Span.Empty(),
            // FunctionArgumentClause::Having(HavingBound(_kind, expr)) => expr.span()
            Having { Value.Expr: var expr } => expr.Span(),
            // FunctionArgumentClause::Separator(value) => value.span(): `ValueWithSpan` implementa
            // ISpanned de forma explícita (tem a propriedade `Span`).
            Separator value => ((ISpanned)value.Value).Span(),
            JsonNullClause => Core.Span.Empty(),
            JsonReturningClause => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de FunctionArgumentClause não tratada: {GetType().Name}"),
        };
}

// impl Spanned for JsonPath (spans.rs:1830)
public sealed partial record JsonPath : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>see Spanned impl for JsonPathElem for more information</para>
    /// </remarks>
    public Span Span()
    {
        // let JsonPath { path } = self;

        return UnionSpans(Path.Select(i => i.Span()));
    }
}

// impl Spanned for JsonPathElem (spans.rs:1842)
public abstract partial record JsonPathElem : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>JsonPathElem::Dot</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            Dot => Core.Span.Empty(),
            // JsonPathElem::Bracket { key } / JsonPathElem::ColonBracket { key } => key.span()
            Bracket bracket => bracket.Key.Span(),
            ColonBracket colonBracket => colonBracket.Key.Span(),
            _ => throw new InvalidOperationException($"Variante de JsonPathElem não tratada: {GetType().Name}"),
        };
}

// impl Spanned for SelectItemQualifiedWildcardKind (spans.rs:1852)
public abstract partial record SelectItemQualifiedWildcardKind : ISpanned
{
    public Span Span() =>
        this switch
        {
            ObjectName objectName => objectName.Value.Span(),
            Expr expr => expr.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de SelectItemQualifiedWildcardKind não tratada: {GetType().Name}"),
        };
}

// impl Spanned for SelectItem (spans.rs:1861)
public abstract partial record SelectItem : ISpanned
{
    public Span Span() =>
        this switch
        {
            UnnamedExpr expr => expr.Value.Span(),
            // SelectItem::ExprWithAlias { expr, alias } => expr.span().union(&alias.span)
            ExprWithAlias e => e.Expr.Span().Union(e.Alias.Span),
            // SelectItem::ExprWithAliases { expr, aliases }
            ExprWithAliases e => UnionSpans(Once(e.Expr.Span()).Concat(e.Aliases.Select(i => i.Span))),
            // SelectItem::QualifiedWildcard(kind, wildcard_additional_options) (nomes.md: Kind, AdditionalOptions)
            QualifiedWildcard q => UnionSpans(
                new[] { q.Kind.Span() }
                    .Concat(Once(q.AdditionalOptions.Span()))),
            Wildcard wildcardAdditionalOptions => wildcardAdditionalOptions.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de SelectItem não tratada: {GetType().Name}"),
        };
}

// impl Spanned for WildcardAdditionalOptions (spans.rs:1879)
public sealed partial record WildcardAdditionalOptions : ISpanned
{
    public Span Span()
    {
        // let WildcardAdditionalOptions {
        //     wildcard_token,
        //     opt_ilike,
        //     opt_exclude,
        //     opt_except,
        //     opt_replace,
        //     opt_rename,
        //     opt_alias,
        // } = self;

        return UnionSpans(
            Once(WildcardToken.Value.Span)
                .Concat(OptionIter(OptIlike).Select(i => i.Span()))
                .Concat(OptionIter(OptExclude).Select(i => i.Span()))
                .Concat(OptionIter(OptRename).Select(i => i.Span()))
                .Concat(OptionIter(OptReplace).Select(i => i.Span()))
                .Concat(OptionIter(OptExcept).Select(i => i.Span()))
                .Concat(OptionIter(OptAlias).Select(i => i.Span)));
    }
}

// impl Spanned for IlikeSelectItem (spans.rs:1904)
public sealed partial record IlikeSelectItem : ISpanned
{
    /// <remarks><b>missing span</b></remarks>
    public Span Span() => Core.Span.Empty();
}

// impl Spanned for ExcludeSelectItem (spans.rs:1910)
public abstract partial record ExcludeSelectItem : ISpanned
{
    public Span Span() =>
        this switch
        {
            Single name => name.Value.Span(),
            Multiple vec => UnionSpans(vec.Value.Select(i => i.Span())),
            _ => throw new InvalidOperationException($"Variante de ExcludeSelectItem não tratada: {GetType().Name}"),
        };
}

// impl Spanned for RenameSelectItem (spans.rs:1919)
public abstract partial record RenameSelectItem : ISpanned
{
    public Span Span() =>
        this switch
        {
            Single ident => ident.Value.Ident.Span.Union(ident.Value.Alias.Span),
            Multiple vec => UnionSpans(vec.Value.Select(i => i.Ident.Span.Union(i.Alias.Span))),
            _ => throw new InvalidOperationException($"Variante de RenameSelectItem não tratada: {GetType().Name}"),
        };
}

// impl Spanned for ExceptSelectItem (spans.rs:1930)
public sealed partial record ExceptSelectItem : ISpanned
{
    public Span Span()
    {
        // let ExceptSelectItem {
        //     first_element,
        //     additional_elements,
        // } = self;

        return UnionSpans(
            Once(FirstElement.Span).Concat(AdditionalElements.Select(i => i.Span)));
    }
}

// impl Spanned for ReplaceSelectItem (spans.rs:1943)
public sealed partial record ReplaceSelectItem : ISpanned
{
    public Span Span()
    {
        // let ReplaceSelectItem { items } = self;

        return UnionSpans(Items.Select(i => i.Span()));
    }
}

// impl Spanned for ReplaceSelectElement (spans.rs:1951)
public sealed partial record ReplaceSelectElement : ISpanned
{
    public Span Span()
    {
        // let ReplaceSelectElement {
        //     expr,
        //     column_name,
        //     as_keyword: _, // bool
        // } = self;

        return Expr.Span().Union(ColumnName.Span);
    }
}

// impl Spanned for TableFactor (spans.rs:1967)
public abstract partial record TableFactor : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>TableFactor::JsonTable</c></item>
    /// </list>
    /// </remarks>
    public Span Span()
    {
        // PORT-DEVIATION DIV-005: o Rust não marca este método com #[recursive::recursive], mas aqui cada nível
        // de junções entre parênteses ou de tabelas derivadas aninhadas (possível com o limite de recursão
        // elevado) gasta KBs de pilha; recebe a mesma proteção do Span() de Expr (convencoes.md §13, caso 123).
        if (StackGuard.IsStackLow())
        {
            return StackGuard.Grow(this, static tableFactor => tableFactor.Span());
        }

        return this switch
        {
            // TableFactor::Table { name, alias, args: _, with_hints: _, version: _, with_ordinality: _,
            //     partitions: _, json_path: _, sample: _, index_hints: _ }
            Table table => UnionSpans(
                table.Name.Value
                    .Select(i => i.Span())
                    .Concat(OptionIter(table.Alias).Select(alias =>
                        UnionSpans(
                            Once(alias.Name.Span)
                                .Concat(alias.Columns.Select(i => i.Span())))))),
            // TableFactor::Derived { lateral: _, subquery, alias, sample: _ }
            Derived derived => derived.Subquery
                .Span()
                .UnionOpt(derived.Alias?.Span()),
            // TableFactor::TableFunction { expr, alias }
            TableFunction tableFunction => tableFunction.Expr
                .Span()
                .UnionOpt(tableFunction.Alias?.Span()),
            // TableFactor::UNNEST { alias, with_offset: _, with_offset_alias, array_exprs, with_ordinality: _ }
            UNNEST unnest => UnionSpans(
                OptionIter(unnest.Alias)
                    .Select(i => i.Span())
                    .Concat(unnest.ArrayExprs.Select(i => i.Span()))
                    .Concat(OptionIter(unnest.WithOffsetAlias).Select(i => i.Span))),
            // TableFactor::NestedJoin { table_with_joins, alias }
            NestedJoin nestedJoin => nestedJoin.TableWithJoins
                .Span()
                .UnionOpt(nestedJoin.Alias?.Span()),
            // TableFactor::Function { lateral: _, name, args, with_ordinality: _, alias }
            Function function => UnionSpans(
                function.Name.Value
                    .Select(i => i.Span())
                    .Concat(function.Args.Select(i => i.Span()))
                    .Concat(OptionIter(function.Alias).Select(alias => alias.Span()))),
            JsonTable => Core.Span.Empty(),
            XmlTable => Core.Span.Empty(),
            // TableFactor::Pivot { table, aggregate_functions, value_column, value_source, default_on_null, alias }
            Pivot pivot => UnionSpans(
                Once(pivot.Table.Span())
                    .Concat(pivot.AggregateFunctions.Select(i => i.Span()))
                    .Concat(pivot.ValueColumn.Select(i => i.Span()))
                    .Concat(Once(pivot.ValueSource.Span()))
                    .Concat(OptionIter(pivot.DefaultOnNull).Select(i => i.Span()))
                    .Concat(OptionIter(pivot.Alias).Select(i => i.Span()))),
            // TableFactor::Unpivot { table, value, null_inclusion: _, name, columns, alias }
            Unpivot unpivot => UnionSpans(
                Once(unpivot.Table.Span())
                    .Concat(Once(unpivot.Value.Span()))
                    .Concat(Once(unpivot.Name.Span))
                    .Concat(unpivot.Columns.Select(ilist => ilist.Span()))
                    .Concat(OptionIter(unpivot.Alias).Select(alias => alias.Span()))),
            // TableFactor::UnpivotExpr { expression, value_alias, attribute_alias }
            UnpivotExpr unpivotExpr => UnionSpans(
                Once(unpivotExpr.Expression.Span())
                    .Concat(Once(unpivotExpr.ValueAlias.Span))
                    .Concat(OptionIter(unpivotExpr.AttributeAlias).Select(alias => alias.Span))),
            // TableFactor::MatchRecognize { table, partition_by, order_by, measures, rows_per_match: _,
            //     after_match_skip: _, pattern, symbols, alias }
            MatchRecognize matchRecognize => UnionSpans(
                Once(matchRecognize.Table.Span())
                    .Concat(matchRecognize.PartitionBy.Select(i => i.Span()))
                    .Concat(matchRecognize.OrderBy.Select(i => i.Span()))
                    .Concat(matchRecognize.Measures.Select(i => i.Span()))
                    .Concat(Once(matchRecognize.Pattern.Span()))
                    .Concat(matchRecognize.Symbols.Select(i => i.Span()))
                    .Concat(OptionIter(matchRecognize.Alias).Select(i => i.Span()))),
            // TableFactor::SemanticView { name, dimensions, metrics, facts, where_clause, alias }
            SemanticView semanticView => UnionSpans(
                semanticView.Name.Value
                    .Select(i => i.Span())
                    .Concat(semanticView.Dimensions.Select(d => d.Span()))
                    .Concat(semanticView.Metrics.Select(m => m.Span()))
                    .Concat(semanticView.Facts.Select(f => f.Span()))
                    .Concat(OptionIter(semanticView.WhereClause).Select(e => e.Span()))
                    .Concat(OptionIter(semanticView.Alias).Select(a => a.Span()))),
            OpenJsonTable => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de TableFactor não tratada: {GetType().Name}"),
        };
    }
}

// impl Spanned for PivotValueSource (spans.rs:2116)
public abstract partial record PivotValueSource : ISpanned
{
    public Span Span() =>
        this switch
        {
            List vec => UnionSpans(vec.Value.Select(i => i.Span())),
            Any vec => UnionSpans(vec.Value.Select(i => i.Span())),
            Subquery query => query.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de PivotValueSource não tratada: {GetType().Name}"),
        };
}

// impl Spanned for ExprWithAlias (spans.rs:2126)
public sealed partial record ExprWithAlias : ISpanned
{
    public Span Span()
    {
        // let ExprWithAlias { expr, alias } = self;

        return Expr.Span().UnionOpt(Alias?.Span);
    }
}

// impl Spanned for MatchRecognizePattern (spans.rs:2135)
public abstract partial record MatchRecognizePattern : ISpanned
{
    /// <remarks><b>missing span</b></remarks>
    public Span Span() => Core.Span.Empty();
}

// impl Spanned for SymbolDefinition (spans.rs:2141)
public sealed partial record SymbolDefinition : ISpanned
{
    public Span Span()
    {
        // let SymbolDefinition { symbol, definition } = self;

        return Symbol.Span.Union(Definition.Span());
    }
}

// impl Spanned for Measure (spans.rs:2149)
public sealed partial record Measure : ISpanned
{
    public Span Span()
    {
        // let Measure { expr, alias } = self;

        return Expr.Span().Union(Alias.Span);
    }
}

// impl Spanned for OrderByExpr (spans.rs:2157)
public sealed partial record OrderByExpr : ISpanned
{
    public Span Span()
    {
        // let OrderByExpr {
        //     expr,
        //     options: _,
        //     with_fill,
        // } = self;

        return Expr.Span().UnionOpt(WithFill?.Span());
    }
}

// impl Spanned for WithFill (spans.rs:2169)
public sealed partial record WithFill : ISpanned
{
    public Span Span()
    {
        // let WithFill { from, to, step } = self;

        return UnionSpans(
            OptionIter(From)
                .Select(f => f.Span())
                .Concat(OptionIter(To).Select(t => t.Span()))
                .Concat(OptionIter(Step).Select(s => s.Span())));
    }
}

// impl Spanned for FunctionArg (spans.rs:2182)
public abstract partial record FunctionArg : ISpanned
{
    public Span Span() =>
        this switch
        {
            // FunctionArg::Named { name, arg, operator: _ } => name.span.union(&arg.span())
            Named named => named.Name.Span.Union(named.Arg.Span()),
            Unnamed arg => arg.Value.Span(),
            // FunctionArg::ExprNamed { name, arg, operator: _ } => name.span().union(&arg.span())
            ExprNamed exprNamed => exprNamed.Name.Span().Union(exprNamed.Arg.Span()),
            _ => throw new InvalidOperationException($"Variante de FunctionArg não tratada: {GetType().Name}"),
        };
}

// impl Spanned for FunctionArgExpr (spans.rs:2205)
public abstract partial record FunctionArgExpr : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>FunctionArgExpr::Wildcard</c></item>
    /// <item><c>FunctionArgExpr::WildcardWithOptions</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            Expr expr => expr.Value.Span(),
            QualifiedWildcard objectName => UnionSpans(objectName.Value.Value.Select(i => i.Span())),
            Wildcard => Core.Span.Empty(),
            WildcardWithOptions => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de FunctionArgExpr não tratada: {GetType().Name}"),
        };
}

// impl Spanned for TableAlias (spans.rs:2218)
public sealed partial record TableAlias : ISpanned
{
    public Span Span()
    {
        // let TableAlias {
        //     explicit: _,
        //     name,
        //     columns,
        //     at,
        // } = self;
        return UnionSpans(
            Once(Name.Span)
                .Concat(Columns.Select(i => i.Span()))
                .Concat(OptionIter(At).Select(at => at.Span)));
    }
}

// impl Spanned for TableAliasColumnDef (spans.rs:2234)
public sealed partial record TableAliasColumnDef : ISpanned
{
    public Span Span()
    {
        // let TableAliasColumnDef { name, data_type: _ } = self;

        return Name.Span;
    }
}

// impl Spanned for ValueWithSpan (spans.rs:2242)
// O record já tem a propriedade `Span` (campo `span`): implementação explícita (onda-b/B7.md).
public sealed partial record ValueWithSpan : ISpanned
{
    Span ISpanned.Span() => Span;
}

// impl Spanned for Join (spans.rs:2248)
public sealed partial record Join : ISpanned
{
    public Span Span()
    {
        // let Join {
        //     relation,
        //     global: _, // bool
        //     join_operator,
        // } = self;

        return Relation.Span().Union(JoinOperator.Span());
    }
}

// impl Spanned for JoinOperator (spans.rs:2266)
public abstract partial record JoinOperator : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>JoinOperator::CrossJoin</c></item>
    /// <item><c>JoinOperator::CrossApply</c></item>
    /// <item><c>JoinOperator::OuterApply</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            Join joinConstraint => joinConstraint.Value.Span(),
            Inner joinConstraint => joinConstraint.Value.Span(),
            Left joinConstraint => joinConstraint.Value.Span(),
            LeftOuter joinConstraint => joinConstraint.Value.Span(),
            Right joinConstraint => joinConstraint.Value.Span(),
            RightOuter joinConstraint => joinConstraint.Value.Span(),
            FullOuter joinConstraint => joinConstraint.Value.Span(),
            CrossJoin joinConstraint => joinConstraint.Value.Span(),
            LeftSemi joinConstraint => joinConstraint.Value.Span(),
            RightSemi joinConstraint => joinConstraint.Value.Span(),
            LeftAnti joinConstraint => joinConstraint.Value.Span(),
            RightAnti joinConstraint => joinConstraint.Value.Span(),
            CrossApply => Core.Span.Empty(),
            OuterApply => Core.Span.Empty(),
            // JoinOperator::AsOf { match_condition, constraint }
            AsOf asOf => asOf.MatchCondition.Span().Union(asOf.Constraint.Span()),
            Anti joinConstraint => joinConstraint.Value.Span(),
            Semi joinConstraint => joinConstraint.Value.Span(),
            StraightJoin joinConstraint => joinConstraint.Value.Span(),
            ArrayJoin => Core.Span.Empty(),
            LeftArrayJoin => Core.Span.Empty(),
            InnerArrayJoin => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de JoinOperator não tratada: {GetType().Name}"),
        };
}

// impl Spanned for JoinConstraint (spans.rs:2302)
public abstract partial record JoinConstraint : ISpanned
{
    /// <remarks>
    /// <para><b>partial span</b></para>
    /// <para>Missing spans:</para>
    /// <list type="bullet">
    /// <item><c>JoinConstraint::Natural</c></item>
    /// <item><c>JoinConstraint::None</c></item>
    /// </list>
    /// </remarks>
    public Span Span() =>
        this switch
        {
            On expr => expr.Value.Span(),
            Using vec => UnionSpans(vec.Value.Select(i => i.Span())),
            Natural => Core.Span.Empty(),
            None => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de JoinConstraint não tratada: {GetType().Name}"),
        };
}

// impl Spanned for TableWithJoins (spans.rs:2313)
public sealed partial record TableWithJoins : ISpanned
{
    public Span Span()
    {
        // let TableWithJoins { relation, joins } = self;

        return UnionSpans(Once(Relation.Span()).Concat(Joins.Select(item => item.Span())));
    }
}

// impl Spanned for Select (spans.rs:2321)
public sealed partial record Select : ISpanned
{
    public Span Span()
    {
        // let Select {
        //     select_token,
        //     optimizer_hints: _,
        //     distinct: _, // todo
        //     select_modifiers: _,
        //     top: _, // todo, mysql specific
        //     projection,
        //     exclude: _,
        //     into,
        //     from,
        //     lateral_views,
        //     prewhere,
        //     selection,
        //     group_by,
        //     cluster_by,
        //     distribute_by,
        //     sort_by,
        //     having,
        //     named_window,
        //     qualify,
        //     window_before_qualify: _, // bool
        //     value_table_mode: _,      // todo, BigQuery specific
        //     connect_by,
        //     top_before_distinct: _,
        //     flavor: _,
        // } = self;

        return UnionSpans(
            Once(SelectToken.Value.Span)
                .Concat(Projection.Select(item => item.Span()))
                .Concat(OptionIter(Into).Select(item => item.Span()))
                .Concat(From.Select(item => item.Span()))
                .Concat(LateralViews.Select(item => item.Span()))
                .Concat(OptionIter(Prewhere).Select(item => item.Span()))
                .Concat(OptionIter(Selection).Select(item => item.Span()))
                .Concat(ConnectBy.Select(item => item.Span()))
                .Concat(Once(GroupBy.Span()))
                .Concat(ClusterBy.Select(item => item.Span()))
                .Concat(DistributeBy.Select(item => item.Span()))
                .Concat(SortBy.Select(item => item.Span()))
                .Concat(OptionIter(Having).Select(item => item.Span()))
                .Concat(NamedWindow.Select(item => item.Span()))
                .Concat(OptionIter(Qualify).Select(item => item.Span())));
    }
}

// impl Spanned for ConnectByKind (spans.rs:2370)
public abstract partial record ConnectByKind : ISpanned
{
    public Span Span() =>
        this switch
        {
            // ConnectByKind::ConnectBy { connect_token, nocycle: _, relationships }
            // connect_token.0.span(): `TokenWithSpan` implementa ISpanned de forma explícita.
            ConnectBy connectBy => UnionSpans(
                Once(((ISpanned)connectBy.ConnectToken.Value).Span())
                    .Concat(OptionIter(connectBy.Relationships.Last).Select(item => item.Span()))),
            // ConnectByKind::StartWith { start_token, condition }
            StartWith startWith => UnionSpans(
                new[] { ((ISpanned)startWith.StartToken.Value).Span(), startWith.Condition.Span() }),
            _ => throw new InvalidOperationException($"Variante de ConnectByKind não tratada: {GetType().Name}"),
        };
}

// impl Spanned for NamedWindowDefinition (spans.rs:2389)
public sealed partial record NamedWindowDefinition : ISpanned
{
    public Span Span()
    {
        // let NamedWindowDefinition(
        //     ident,
        //     _, // todo: NamedWindowExpr
        // ) = self;
        var ident = Name;

        return ident.Span;
    }
}

// impl Spanned for LateralView (spans.rs:2400)
public sealed partial record LateralView : ISpanned
{
    public Span Span()
    {
        // let LateralView {
        //     lateral_view,
        //     lateral_view_name,
        //     lateral_col_alias,
        //     outer: _, // bool
        // } = self;

        return UnionSpans(
            Once(LateralViewValue.Span())
                .Concat(Once(LateralViewName.Span()))
                .Concat(LateralColAlias.Select(i => i.Span)));
    }
}

// impl Spanned for SelectInto (spans.rs:2417)
public sealed partial record SelectInto : ISpanned
{
    public Span Span()
    {
        // let SelectInto {
        //     temporary: _, // bool
        //     unlogged: _,  // bool
        //     table: _,     // bool
        //     targets,
        // } = self;

        return UnionSpans(Targets.Select(t => t.Span()));
    }
}

// impl Spanned for UpdateTableFromKind (spans.rs:2430)
public abstract partial record UpdateTableFromKind : ISpanned
{
    public Span Span()
    {
        var @from = this switch
        {
            BeforeSet beforeSet => beforeSet.Value,
            AfterSet afterSet => afterSet.Value,
            _ => throw new InvalidOperationException($"Variante de UpdateTableFromKind não tratada: {GetType().Name}"),
        };
        return UnionSpans(@from.Select(t => t.Span()));
    }
}

// impl Spanned for TableObject (spans.rs:2440)
public abstract partial record TableObject : ISpanned
{
    public Span Span() =>
        this switch
        {
            // TableObject::TableName(ObjectName(segments)) => union_spans(segments.iter().map(|i| i.span()))
            TableName { Value.Value: var segments } => UnionSpans(segments.Select(i => i.Span())),
            TableFunction func => func.Value.Span(),
            TableQuery query => query.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de TableObject não tratada: {GetType().Name}"),
        };
}

// impl Spanned for BeginEndStatements (spans.rs:2452)
public sealed partial record BeginEndStatements : ISpanned
{
    public Span Span()
    {
        // let BeginEndStatements {
        //     begin_token,
        //     statements,
        //     end_token,
        // } = self;
        return UnionSpans(
            Once(BeginToken.Value.Span)
                .Concat(Statements.Select(i => i.Span()))
                .Concat(Once(EndToken.Value.Span)));
    }
}

// impl Spanned for OpenStatement (spans.rs:2467)
public sealed partial record OpenStatement : ISpanned
{
    public Span Span()
    {
        // let OpenStatement { cursor_name } = self;
        return CursorName.Span;
    }
}

// impl Spanned for AlterSchemaOperation (spans.rs:2474)
public abstract partial record AlterSchemaOperation : ISpanned
{
    public Span Span() =>
        this switch
        {
            // AlterSchemaOperation::SetDefaultCollate { collate } => collate.span()
            SetDefaultCollate setDefaultCollate => setDefaultCollate.Collate.Span(),
            // AlterSchemaOperation::AddReplica { replica, options }
            AddReplica addReplica => UnionSpans(
                Once(addReplica.Replica.Span)
                    .Concat(OptionIter(addReplica.Options).SelectMany(i => i.Select(i => i.Span())))),
            // AlterSchemaOperation::DropReplica { replica } => replica.span
            DropReplica dropReplica => dropReplica.Replica.Span,
            // AlterSchemaOperation::SetOptionsParens { options }
            SetOptionsParens setOptionsParens => UnionSpans(setOptionsParens.Options.Select(i => i.Span())),
            // AlterSchemaOperation::Rename { name } => name.span()
            Rename rename => rename.Name.Span(),
            // AlterSchemaOperation::OwnerTo { owner } => {
            //     if let Owner::Ident(ident) = owner { ident.span } else { Span::empty() }
            // }
            OwnerTo ownerTo => ownerTo.Owner is Owner.Ident ident
                ? ident.Value.Span
                : Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de AlterSchemaOperation não tratada: {GetType().Name}"),
        };
}

// impl Spanned for AlterSchema (spans.rs:2498)
public sealed partial record AlterSchema : ISpanned
{
    public Span Span() =>
        UnionSpans(
            Once(Name.Span()).Concat(Operations.Select(i => i.Span())));
}

// impl Spanned for CreateView (spans.rs:2506)
public sealed partial record CreateView : ISpanned
{
    public Span Span() =>
        UnionSpans(
            Once(Name.Span())
                .Concat(Columns.Select(i => i.Span()))
                .Concat(Once(Query.Span()))
                .Concat(Once(Options.Span()))
                .Concat(ClusterBy.Select(i => i.Span))
                .Concat(OptionIter(To).Select(i => i.Span())));
}

// impl Spanned for AlterTable (spans.rs:2519)
public sealed partial record AlterTable : ISpanned
{
    public Span Span() =>
        UnionSpans(
            Once(Name.Span())
                .Concat(Operations.Select(i => i.Span()))
                .Concat(OptionIter(OnCluster).Select(i => i.Span))
                .Concat(Once(EndToken.Value.Span)));
}

// impl Spanned for CreateOperator (spans.rs:2530)
public sealed partial record CreateOperator : ISpanned
{
    public Span Span() => Core.Span.Empty();
}

// impl Spanned for CreateOperatorFamily (spans.rs:2536)
public sealed partial record CreateOperatorFamily : ISpanned
{
    public Span Span() => Core.Span.Empty();
}

// impl Spanned for CreateOperatorClass (spans.rs:2542)
public sealed partial record CreateOperatorClass : ISpanned
{
    public Span Span() => Core.Span.Empty();
}

// impl Spanned for MergeClause (spans.rs:2548)
public sealed partial record MergeClause : ISpanned
{
    public Span Span() => UnionSpans(new[] { WhenToken.Value.Span, Action.Span() });
}

// impl Spanned for MergeAction (spans.rs:2554)
public abstract partial record MergeAction : ISpanned
{
    public Span Span() =>
        this switch
        {
            Insert expr => expr.Value.Span(),
            Update expr => expr.Value.Span(),
            // MergeAction::Delete { delete_token } => delete_token.0.span
            Delete delete => delete.DeleteToken.Value.Span,
            // MergeAction::DoNothing { do_token, nothing_token } => do_token.0.span.union(&nothing_token.0.span)
            DoNothing doNothing => doNothing.DoToken.Value.Span.Union(doNothing.NothingToken.Value.Span),
            _ => throw new InvalidOperationException($"Variante de MergeAction não tratada: {GetType().Name}"),
        };
}

// impl Spanned for MergeInsertExpr (spans.rs:2568)
public sealed partial record MergeInsertExpr : ISpanned
{
    public Span Span() =>
        UnionSpans(
            new[]
            {
                InsertToken.Value.Span,
                KindToken.Value.Span,
                Kind switch
                {
                    MergeInsertKind.Values values => values.Value.Span(),
                    MergeInsertKind.Row or MergeInsertKind.Wildcard => Core.Span.Empty(),
                    _ => throw new InvalidOperationException($"Variante de MergeInsertKind não tratada: {Kind.GetType().Name}"),
                },
            }
            .Concat(OptionIter(InsertPredicate).Select(i => i.Span()))
            .Concat(Columns.Select(i => i.Span())));
}

// impl Spanned for MergeUpdateExpr (spans.rs:2586)
public sealed partial record MergeUpdateExpr : ISpanned
{
    public Span Span()
    {
        var kindSpan = Kind switch
        {
            MergeUpdateKind.Set assignments => UnionSpans(assignments.Value.Select(i => i.Span())),
            MergeUpdateKind.Wildcard => Core.Span.Empty(),
            _ => throw new InvalidOperationException($"Variante de MergeUpdateKind não tratada: {Kind.GetType().Name}"),
        };
        return UnionSpans(
            Once(UpdateToken.Value.Span)
                .Concat(Once(kindSpan))
                .Concat(OptionIter(UpdatePredicate).Select(i => i.Span()))
                .Concat(OptionIter(DeletePredicate).Select(i => i.Span())));
    }
}

// impl Spanned for OutputClause (spans.rs:2601)
public abstract partial record OutputClause : ISpanned
{
    public Span Span() =>
        this switch
        {
            // OutputClause::Output { output_token, select_items, into_table }
            Output output => UnionSpans(
                Once(output.OutputToken.Value.Span)
                    .Concat(OptionIter(output.IntoTable).Select(i => i.Span()))
                    .Concat(output.SelectItems.Select(i => i.Span()))),
            // OutputClause::Returning { returning_token, select_items }
            Returning returning => UnionSpans(
                Once(returning.ReturningToken.Value.Span)
                    .Concat(returning.SelectItems.Select(i => i.Span()))),
            _ => throw new InvalidOperationException($"Variante de OutputClause não tratada: {GetType().Name}"),
        };
}

// impl Spanned for comments::CommentWithSpan (spans.rs:2624)
// O record já tem a propriedade `Span` (campo `span`): implementação explícita (onda-b/B7.md).
public sealed partial record CommentWithSpan : ISpanned
{
    Span ISpanned.Span() => Span;
}
