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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 16460–17265:
// parse_table_factor … parse_match_recognize).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>A table name or a parenthesized subquery, followed by optional <c>[AS] alias</c></summary>
    public TableFactor ParseTableFactor()
    {
        // Rust: #[cfg_attr(feature = "recursive-protection", recursive::recursive)] (convencoes.md §13, caso 123).
        if (StackGuard.IsStackLow())
        {
            return StackGuard.Grow(this, static parser => parser.ParseTableFactor());
        }

        using var _guard = _recursionCounter.TryDecrease();
        if (ParseKeyword(Keyword.LATERAL))
        {
            // LATERAL must always be followed by a subquery or table function.
            if (ConsumeToken(new Token.LParen()))
            {
                return ParseDerivedTableFactor(IsLateral.Lateral);
            }
            else
            {
                var name = ParseObjectName(false);
                ExpectToken(new Token.LParen());
                var args = ParseOptionalArgs();
                var withOrdinality = ParseKeywords(Keyword.WITH, Keyword.ORDINALITY);
                var alias = MaybeParseTableAlias();
                return new TableFactor.Function(
                    Lateral: true,
                    Name: name,
                    Args: args,
                    WithOrdinality: withOrdinality,
                    Alias: alias);
            }
        }
        else if (ParseKeyword(Keyword.TABLE))
        {
            // parse table function (SELECT * FROM TABLE (<expr>) [ AS <alias> ])
            ExpectToken(new Token.LParen());
            var expr = ParseExpr();
            ExpectToken(new Token.RParen());
            var alias = MaybeParseTableAlias();
            return new TableFactor.TableFunction(Expr: expr, Alias: alias);
        }
        else if (ConsumeToken(new Token.LParen()))
        {
            // A left paren introduces either a derived table (i.e., a subquery)
            // or a nested join. It's nearly impossible to determine ahead of
            // time which it is... so we just try to parse both.
            //
            // Here's an example that demonstrates the complexity:
            //                     /-------------------------------------------------------\
            //                     | /-----------------------------------\                 |
            //     SELECT * FROM ( ( ( (SELECT 1) UNION (SELECT 2) ) AS t1 NATURAL JOIN t2 ) )
            //                   ^ ^ ^ ^
            //                   | | | |
            //                   | | | |
            //                   | | | (4) belongs to a SetExpr::Query inside the subquery
            //                   | | (3) starts a derived table (subquery)
            //                   | (2) starts a nested join
            //                   (1) an additional set of parens around a nested join
            //

            // If the recently consumed '(' starts a derived table, the call to
            // `parse_derived_table_factor` below will return success after parsing the
            // subquery, followed by the closing ')', and the alias of the derived table.
            // In the example above this is case (3).
            //
            // Memoize failures to break the 2^N work on inputs like
            // `FROM ((((...`, where the nested-join fallback recurses back into
            // `parse_table_factor` and re-attempts the same speculative parse.
            var derivedPos = _index;
            TableFactor? derived;
            if (_failedDerivedTableFactorPositions.Contains(derivedPos))
            {
                derived = null;
            }
            else
            {
                switch (MaybeParse(parser => parser.ParseDerivedTableFactor(IsLateral.NotLateral)))
                {
                    case { } t:
                        derived = t;
                        break;
                    case null:
                        _failedDerivedTableFactorPositions.Add(derivedPos);
                        derived = null;
                        break;
                }
            }
            if (derived is { } derivedTable)
            {
                // Rust: `if let Some(mut table) = derived`.
                var table = derivedTable;
                while (ParseOneOfKeywords(Keyword.PIVOT, Keyword.UNPIVOT) is { } kw)
                {
                    switch (kw)
                    {
                        case Keyword.PIVOT:
                            table = ParsePivotTableFactor(table);
                            break;
                        case Keyword.UNPIVOT:
                            table = ParseUnpivotTableFactor(table);
                            break;
                        case var unexpectedKeyword:
                            throw new ParserException(new ParserError.ParserErrorValue(
                                $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in pivot/unpivot"));
                    }
                }
                return table;
            }

            // A parsing error from `parse_derived_table_factor` indicates that the '(' we've
            // recently consumed does not start a derived table (cases 1, 2, or 4).
            // `maybe_parse` will ignore such an error and rewind to be after the opening '('.

            // Inside the parentheses we expect to find an (A) table factor
            // followed by some joins or (B) another level of nesting.
            var tableAndJoins = ParseTableAndJoins();

            // #[allow(clippy::if_same_then_else)]: não traduzido.
            if (!tableAndJoins.Joins.IsEmpty)
            {
                ExpectToken(new Token.RParen());
                var alias = MaybeParseTableAlias();
                return new TableFactor.NestedJoin(
                    TableWithJoins: tableAndJoins,
                    Alias: alias); // (A)
            }
            else if (tableAndJoins.Relation is TableFactor.NestedJoin)
            {
                // (B): `table_and_joins` (what we found inside the parentheses)
                // is a nested join `(foo JOIN bar)`, not followed by other joins.
                ExpectToken(new Token.RParen());
                var alias = MaybeParseTableAlias();
                return new TableFactor.NestedJoin(
                    TableWithJoins: tableAndJoins,
                    Alias: alias);
            }
            else if (_dialect.SupportsParensAroundTableFactor)
            {
                // Dialect-specific behavior: Snowflake diverges from the
                // standard and from most of the other implementations by
                // allowing extra parentheses not only around a join (B), but
                // around lone table names (e.g. `FROM (mytable [AS alias])`)
                // and around derived tables (e.g. `FROM ((SELECT ...)
                // [AS alias])`) as well.
                ExpectToken(new Token.RParen());

                if (MaybeParseTableAlias() is { } outerAlias)
                {
                    // Snowflake also allows specifying an alias *after* parens
                    // e.g. `FROM (mytable) AS alias`
                    //
                    // Rust: `match &mut table_and_joins.relation { X { alias, .. } | .. => { corpo } }`
                    // altera o campo no lugar. Os records são imutáveis: o corpo do braço vira a
                    // função local `ReplaceAlias` (recebe o `alias` atual e devolve o novo) e cada
                    // `case` reconstrói a variante com `with`.
                    static TableAlias ReplaceAlias(TableAlias? alias, TableAlias outerAlias)
                    {
                        // but not `FROM (mytable AS alias1) AS alias2`.
                        if (alias is { } innerAlias)
                        {
                            throw new ParserException(new ParserError.ParserErrorValue(
                                $"duplicate alias {innerAlias}"));
                        }
                        // Act as if the alias was specified normally next
                        // to the table name: `(mytable) AS alias` ->
                        // `(mytable AS alias)`
                        return outerAlias;
                    }

                    switch (tableAndJoins.Relation)
                    {
                        case TableFactor.Derived t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.Table t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.Function t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.UNNEST t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.JsonTable t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.XmlTable t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.OpenJsonTable t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.TableFunction t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.Pivot t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.Unpivot t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.MatchRecognize t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.SemanticView t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.NestedJoin t:
                            tableAndJoins = tableAndJoins with { Relation = t with { Alias = ReplaceAlias(t.Alias, outerAlias) } };
                            break;
                        case TableFactor.UnpivotExpr:
                            throw new ParserException(new ParserError.ParserErrorValue(
                                "alias after parenthesized UNPIVOT expression is not supported"));
                        default:
                            throw new InvalidOperationException(
                                $"Variante de TableFactor não tratada: {tableAndJoins.Relation.GetType().Name}");
                    }
                }
                // Do not store the extra set of parens in the AST
                return tableAndJoins.Relation;
            }
            else
            {
                // The SQL spec prohibits derived tables and bare tables from
                // appearing alone in parentheses (e.g. `FROM (mytable)`)
                return ExpectedRef<TableFactor>("joined table", PeekTokenRef());
            }
        }
        // Rust: `matches!(self.peek_tokens(), [Token::Word(Word { keyword: VALUES, .. }), Token::LParen])`
        // dentro da cadeia de `else if`: padrão de lista sobre o array de `PeekTokens(2)`.
        else if (_dialect.SupportsValuesAsTableFactor
            && PeekTokens(2) is [Token.Word { Value.Keyword: Keyword.VALUES }, Token.LParen])
        {
            ExpectKeywordIs(Keyword.VALUES);

            // Snowflake and Databricks allow syntax like below:
            // SELECT * FROM VALUES (1, 'a'), (2, 'b') AS t (col1, col2)
            // where there are no parentheses around the VALUES clause.
            var values = new SetExpr.Values(ParseValues(false, false));
            var alias = MaybeParseTableAlias();
            return new TableFactor.Derived(
                Lateral: false,
                Subquery: new Query(
                    With: null,
                    Body: values,
                    OrderBy: null,
                    LimitClause: null,
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty),
                Alias: alias,
                Sample: null);
        }
        else if ((_dialect is BigQueryDialect or PostgreSqlDialect or GenericDialect)
            && ParseKeyword(Keyword.UNNEST))
        {
            ExpectToken(new Token.LParen());
            var arrayExprs = ParseCommaSeparated(p => p.ParseExpr());
            ExpectToken(new Token.RParen());

            var withOrdinality = ParseKeywords(Keyword.WITH, Keyword.ORDINALITY);
            // Rust: `match self.maybe_parse_table_alias() { Ok(Some(a)) => Some(a), Ok(None) => None,
            // Err(e) => return Err(e) }` equivale a `?`.
            var alias = MaybeParseTableAlias();

            // Rust: `match self.expect_keywords(&[WITH, OFFSET]) { Ok(()) => true, Err(_) => false }`, sem
            // restaurar a posição (decisão 1 da B3). Equivalente exato sem exceção (caso 81): o
            // `expect_keywords` consome cada keyword que casa e para na primeira que não casa, sem voltar a
            // posição; o `&&` faz o mesmo (um `WITH` sem `OFFSET` fica consumido), e o erro descartado não
            // tem efeito colateral.
            var withOffset = ParseKeyword(Keyword.WITH) && ParseKeyword(Keyword.OFFSET);

            Ident? withOffsetAlias;
            if (withOffset)
            {
                // `keywords::RESERVED_FOR_COLUMN_ALIAS` (IReadOnlyList) passado ao `params Keyword[]`.
                withOffsetAlias = ParseOptionalAlias(Keywords.ReservedForColumnAlias.ToArray());
            }
            else
            {
                withOffsetAlias = null;
            }

            return new TableFactor.UNNEST(
                Alias: alias,
                ArrayExprs: arrayExprs,
                WithOffset: withOffset,
                WithOffsetAlias: withOffsetAlias,
                WithOrdinality: withOrdinality);
        }
        else if (_dialect.SupportsUnpivotExpr && PeekKeyword(Keyword.UNPIVOT))
        {
            return ParseUnpivotExprTableFactor();
        }
        else if (ParseKeywordWithTokens(Keyword.JSON_TABLE, new Token.LParen()))
        {
            var jsonExpr = ParseExpr();
            ExpectToken(new Token.Comma());
            var jsonPath = ParseValue();
            ExpectKeywordIs(Keyword.COLUMNS);
            ExpectToken(new Token.LParen());
            var columns = ParseCommaSeparated(p => p.ParseJsonTableColumnDef());
            ExpectToken(new Token.RParen());
            ExpectToken(new Token.RParen());
            var alias = MaybeParseTableAlias();
            return new TableFactor.JsonTable(
                JsonExpr: jsonExpr,
                JsonPath: jsonPath,
                Columns: columns,
                Alias: alias);
        }
        else if (ParseKeywordWithTokens(Keyword.OPENJSON, new Token.LParen()))
        {
            PrevToken();
            return ParseOpenJsonTableFactor();
        }
        else if (ParseKeywordWithTokens(Keyword.XMLTABLE, new Token.LParen()))
        {
            PrevToken();
            return ParseXmlTableFactor();
        }
        else if (_dialect.SupportsSemanticViewTableFactor
            && PeekKeywordWithTokens(Keyword.SEMANTIC_VIEW, new Token.LParen()))
        {
            return ParseSemanticViewTableFactor();
        }
        else if (_dialect.SupportsStages && PeekTokenRef().Token is Token.AtSign)
        {
            // Stage reference: @mystage or @namespace.stage (e.g. Snowflake)
            return ParseSnowflakeStageTableFactor();
        }
        else
        {
            var name = ParseObjectName(true);

            JsonPath? jsonPath;
            switch (PeekTokenRef().Token)
            {
                case Token.LBracket when _dialect.SupportsPartiql:
                    jsonPath = ParseJsonPath();
                    break;
                default:
                    jsonPath = null;
                    break;
            }

            Sequence<Ident> partitions;
            if (_dialect.SupportsTablePartitions
                && ParseKeyword(Keyword.PARTITION))
            {
                partitions = ParseParenthesizedIdentifiers();
            }
            else
            {
                partitions = Sequence<Ident>.Empty;
            }

            // Parse potential version qualifier
            var version = MaybeParseTableVersion();

            // Postgres, MSSQL, ClickHouse: table-valued functions:
            TableFunctionArgs? args;
            if (ConsumeToken(new Token.LParen()))
            {
                args = ParseTableFunctionArgs();
            }
            else
            {
                args = null;
            }

            var withOrdinality = ParseKeywords(Keyword.WITH, Keyword.ORDINALITY);

            TableSampleKind? sample = null;
            if (_dialect.SupportsTableSampleBeforeAlias)
            {
                if (MaybeParseTableSample() is { } parsedSample)
                {
                    sample = new TableSampleKind.BeforeTableAlias(parsedSample);
                }
            }

            var alias = MaybeParseTableAlias();

            // MYSQL-specific table hints:
            Sequence<TableIndexHints> indexHints;
            if (_dialect.SupportsTableHints)
            {
                indexHints = MaybeParseValue(p => p.ParseTableIndexHints())
                    ?? Sequence<TableIndexHints>.Empty;
            }
            else
            {
                indexHints = Sequence<TableIndexHints>.Empty;
            }

            // MSSQL-specific table hints:
            var withHints = Sequence<Expr>.Empty;
            if (ParseKeyword(Keyword.WITH))
            {
                if (ConsumeToken(new Token.LParen()))
                {
                    withHints = ParseCommaSeparated(p => p.ParseExpr());
                    ExpectToken(new Token.RParen());
                }
                else
                {
                    // rewind, as WITH may belong to the next statement's CTE
                    PrevToken();
                }
            }

            if (!_dialect.SupportsTableSampleBeforeAlias)
            {
                if (MaybeParseTableSample() is { } parsedSample)
                {
                    sample = new TableSampleKind.AfterTableAlias(parsedSample);
                }
            }

            TableFactor table = new TableFactor.Table(
                Name: name,
                Alias: alias,
                Args: args,
                WithHints: withHints,
                Version: version,
                Partitions: partitions,
                WithOrdinality: withOrdinality,
                JsonPath: jsonPath,
                Sample: sample,
                IndexHints: indexHints);

            while (ParseOneOfKeywords(Keyword.PIVOT, Keyword.UNPIVOT) is { } kw)
            {
                switch (kw)
                {
                    case Keyword.PIVOT:
                        table = ParsePivotTableFactor(table);
                        break;
                    case Keyword.UNPIVOT:
                        table = ParseUnpivotTableFactor(table);
                        break;
                    case var unexpectedKeyword:
                        throw new ParserException(new ParserError.ParserErrorValue(
                            $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in pivot/unpivot"));
                }
            }

            if (_dialect.SupportsMatchRecognize
                && ParseKeyword(Keyword.MATCH_RECOGNIZE))
            {
                table = ParseMatchRecognize(table);
            }

            return table;
        }
    }

    /// <summary>
    /// Parse a Snowflake stage reference as a table factor.
    /// Handles syntax like: <c>@mystage1 (file_format =&gt; 'myformat', pattern =&gt; '...')</c>
    /// </summary>
    /// <remarks>See: <see href="https://docs.snowflake.com/en/user-guide/querying-stage"/></remarks>
    internal TableFactor ParseSnowflakeStageTableFactor()
    {
        // Parse the stage name starting with @
        // Rust: `crate::dialect::parse_snowflake_stage_name(self)` (função livre de dialect/snowflake.rs,
        // reexportada por dialect/mod.rs; ainda não portada, ver B4.md).
        var name = SnowflakeDialect.ParseSnowflakeStageName(this);

        // Parse optional stage options like (file_format => 'myformat', pattern => '...')
        TableFunctionArgs? args;
        if (ConsumeToken(new Token.LParen()))
        {
            args = ParseTableFunctionArgs();
        }
        else
        {
            args = null;
        }

        var alias = MaybeParseTableAlias();

        return new TableFactor.Table(
            Name: name,
            Alias: alias,
            Args: args,
            WithHints: Sequence<Expr>.Empty,
            Version: null,
            Partitions: Sequence<Ident>.Empty,
            WithOrdinality: false,
            JsonPath: null,
            Sample: null,
            IndexHints: Sequence<TableIndexHints>.Empty);
    }

    internal TableSample? MaybeParseTableSample()
    {
        TableSampleModifier modifier;
        if (ParseKeyword(Keyword.TABLESAMPLE))
        {
            modifier = new TableSampleModifier.TableSample();
        }
        else if (ParseKeyword(Keyword.SAMPLE))
        {
            modifier = new TableSampleModifier.Sample();
        }
        else
        {
            return null;
        }
        return ParseTableSample(modifier);
    }

    internal TableSample ParseTableSample(TableSampleModifier modifier)
    {
        TableSampleMethod? name;
        switch (ParseOneOfKeywords(
            Keyword.BERNOULLI,
            Keyword.ROW,
            Keyword.SYSTEM,
            Keyword.BLOCK))
        {
            case Keyword.BERNOULLI:
                name = new TableSampleMethod.Bernoulli();
                break;
            case Keyword.ROW:
                name = new TableSampleMethod.Row();
                break;
            case Keyword.SYSTEM:
                name = new TableSampleMethod.System();
                break;
            case Keyword.BLOCK:
                name = new TableSampleMethod.Block();
                break;
            default:
                name = null;
                break;
        }

        var parenthesized = ConsumeToken(new Token.LParen());

        TableSampleQuantity? quantity;
        TableSampleBucket? bucket;
        if (parenthesized && ParseKeyword(Keyword.BUCKET))
        {
            var selectedBucket = ParseNumberValue();
            ExpectKeywords(Keyword.OUT, Keyword.OF);
            var total = ParseNumberValue();
            Expr? @on;
            if (ParseKeyword(Keyword.ON))
            {
                @on = ParseExpr();
            }
            else
            {
                @on = null;
            }
            (quantity, bucket) = (
                null,
                new TableSampleBucket(
                    Bucket: selectedBucket,
                    Total: total,
                    On: @on));
        }
        else
        {
            Expr value;
            switch (MaybeParse(p => p.ParseExpr()))
            {
                case { } num:
                    value = num;
                    break;
                case null:
                {
                    var nextToken = NextToken();
                    if (nextToken.Token is Token.Word { Value: var w })
                    {
                        value = new Expr.Value(new Value.Placeholder(w.Value).WithSpan(nextToken.Span));
                    }
                    else
                    {
                        throw ParserErr(
                            "Expecting number or byte length e.g. 100M",
                            PeekTokenRef().Span.Start);
                    }
                    break;
                }
            }
            TableSampleUnit? unit;
            if (ParseKeyword(Keyword.ROWS))
            {
                unit = new TableSampleUnit.Rows();
            }
            else if (ParseKeyword(Keyword.PERCENT))
            {
                unit = new TableSampleUnit.Percent();
            }
            else
            {
                unit = null;
            }
            (quantity, bucket) = (
                new TableSampleQuantity(
                    Parenthesized: parenthesized,
                    Value: value,
                    Unit: unit),
                null);
        }
        if (parenthesized)
        {
            ExpectToken(new Token.RParen());
        }

        TableSampleSeed? seed;
        if (ParseKeyword(Keyword.REPEATABLE))
        {
            seed = ParseTableSampleSeed(new TableSampleSeedModifier.Repeatable());
        }
        else if (ParseKeyword(Keyword.SEED))
        {
            seed = ParseTableSampleSeed(new TableSampleSeedModifier.Seed());
        }
        else
        {
            seed = null;
        }

        Expr? offset;
        if (ParseKeyword(Keyword.OFFSET))
        {
            offset = ParseExpr();
        }
        else
        {
            offset = null;
        }

        return new TableSample(
            Modifier: modifier,
            Name: name,
            Quantity: quantity,
            Seed: seed,
            Bucket: bucket,
            Offset: offset);
    }

    internal TableSampleSeed ParseTableSampleSeed(TableSampleSeedModifier modifier)
    {
        ExpectToken(new Token.LParen());
        var value = ParseNumberValue();
        ExpectToken(new Token.RParen());
        return new TableSampleSeed(Modifier: modifier, Value: value);
    }

    /// <summary>
    /// Parses <c>OPENJSON( jsonExpression [ , path ] )  [ &lt;with_clause&gt; ]</c> clause,
    /// assuming the <c>OPENJSON</c> keyword was already consumed.
    /// </summary>
    internal TableFactor ParseOpenJsonTableFactor()
    {
        ExpectToken(new Token.LParen());
        var jsonExpr = ParseExpr();
        ValueWithSpan? jsonPath;
        if (ConsumeToken(new Token.Comma()))
        {
            jsonPath = ParseValue();
        }
        else
        {
            jsonPath = null;
        }
        ExpectToken(new Token.RParen());
        Sequence<OpenJsonTableColumn> columns;
        if (ParseKeyword(Keyword.WITH))
        {
            ExpectToken(new Token.LParen());
            columns = ParseCommaSeparated(p => p.ParseOpenjsonTableColumnDef());
            ExpectToken(new Token.RParen());
        }
        else
        {
            columns = Sequence<OpenJsonTableColumn>.Empty;
        }
        var alias = MaybeParseTableAlias();
        return new TableFactor.OpenJsonTable(
            JsonExpr: jsonExpr,
            JsonPath: jsonPath,
            Columns: columns,
            Alias: alias);
    }

    internal TableFactor ParseXmlTableFactor()
    {
        ExpectToken(new Token.LParen());
        Sequence<XmlNamespaceDefinition> namespaces;
        if (ParseKeyword(Keyword.XMLNAMESPACES))
        {
            ExpectToken(new Token.LParen());
            namespaces = ParseCommaSeparated(p => p.ParseXmlNamespaceDefinition());
            ExpectToken(new Token.RParen());
            ExpectToken(new Token.Comma());
        }
        else
        {
            namespaces = Sequence<XmlNamespaceDefinition>.Empty;
        }
        var rowExpression = ParseExpr();
        var passing = ParseXmlPassingClause();
        ExpectKeywordIs(Keyword.COLUMNS);
        var columns = ParseCommaSeparated(p => p.ParseXmlTableColumn());
        ExpectToken(new Token.RParen());
        var alias = MaybeParseTableAlias();
        return new TableFactor.XmlTable(
            Namespaces: namespaces,
            RowExpression: rowExpression,
            Passing: passing,
            Columns: columns,
            Alias: alias);
    }

    internal XmlNamespaceDefinition ParseXmlNamespaceDefinition()
    {
        var uri = ParseExpr();
        ExpectKeywordIs(Keyword.AS);
        var name = ParseIdentifier();
        return new XmlNamespaceDefinition(Uri: uri, Name: name);
    }

    internal XmlTableColumn ParseXmlTableColumn()
    {
        var name = ParseIdentifier();

        XmlTableColumnOption option;
        if (ParseKeyword(Keyword.FOR))
        {
            ExpectKeyword(Keyword.ORDINALITY);
            option = new XmlTableColumnOption.ForOrdinality();
        }
        else
        {
            var type = ParseDataType();
            Expr? path = null;
            Expr? @default = null;

            if (ParseKeyword(Keyword.PATH))
            {
                path = ParseExpr();
            }

            if (ParseKeyword(Keyword.DEFAULT))
            {
                @default = ParseExpr();
            }

            var notNull = ParseKeywords(Keyword.NOT, Keyword.NULL);
            if (!notNull)
            {
                // NULL is the default but can be specified explicitly
                _ = ParseKeyword(Keyword.NULL);
            }

            option = new XmlTableColumnOption.NamedInfo(
                Type: type,
                Path: path,
                Default: @default,
                Nullable: !notNull);
        }
        return new XmlTableColumn(Name: name, Option: option);
    }

    internal XmlPassingClause ParseXmlPassingClause()
    {
        var arguments = new List<XmlPassingArgument>();
        if (ParseKeyword(Keyword.PASSING))
        {
            while (true)
            {
                // Rust: `self.parse_keyword(BY) && self.expect_keyword(VALUE).is_ok()` (decisão 1 da B3).
                // `expect_keyword(VALUE).is_ok()` é exatamente `parse_keyword(VALUE)`: só consome quando
                // casa, e o erro descartado não tem efeito colateral (caso 81).
                var byValue = ParseKeyword(Keyword.BY) && ParseKeyword(Keyword.VALUE);
                var expr = ParseExpr();
                Ident? alias;
                if (ParseKeyword(Keyword.AS))
                {
                    alias = ParseIdentifier();
                }
                else
                {
                    alias = null;
                }
                arguments.Add(new XmlPassingArgument(
                    Expr: expr,
                    Alias: alias,
                    ByValue: byValue));
                if (!ConsumeToken(new Token.Comma()))
                {
                    break;
                }
            }
        }
        return new XmlPassingClause(Arguments: arguments.ToSequence());
    }

    /// <summary>Parse a <see cref="TableFactor.SemanticView"/></summary>
    internal TableFactor ParseSemanticViewTableFactor()
    {
        ExpectKeyword(Keyword.SEMANTIC_VIEW);
        ExpectToken(new Token.LParen());

        var name = ParseObjectName(true);

        // Parse DIMENSIONS, METRICS, FACTS and WHERE clauses in flexible order
        var dimensions = Sequence<Expr>.Empty;
        var metrics = Sequence<Expr>.Empty;
        var facts = Sequence<Expr>.Empty;
        Expr? whereClause = null;

        while (PeekTokenRef().Token is not Token.RParen)
        {
            if (ParseKeyword(Keyword.DIMENSIONS))
            {
                if (!dimensions.IsEmpty)
                {
                    throw new ParserException(new ParserError.ParserErrorValue(
                        "DIMENSIONS clause can only be specified once"));
                }
                dimensions = ParseCommaSeparated(p => p.ParseWildcardExpr());
            }
            else if (ParseKeyword(Keyword.METRICS))
            {
                if (!metrics.IsEmpty)
                {
                    throw new ParserException(new ParserError.ParserErrorValue(
                        "METRICS clause can only be specified once"));
                }
                metrics = ParseCommaSeparated(p => p.ParseWildcardExpr());
            }
            else if (ParseKeyword(Keyword.FACTS))
            {
                if (!facts.IsEmpty)
                {
                    throw new ParserException(new ParserError.ParserErrorValue(
                        "FACTS clause can only be specified once"));
                }
                facts = ParseCommaSeparated(p => p.ParseWildcardExpr());
            }
            else if (ParseKeyword(Keyword.WHERE))
            {
                if (whereClause is not null)
                {
                    throw new ParserException(new ParserError.ParserErrorValue(
                        "WHERE clause can only be specified once"));
                }
                whereClause = ParseExpr();
            }
            else
            {
                var tok = PeekTokenRef();
                throw ParserErr(
                    $"Expected one of DIMENSIONS, METRICS, FACTS or WHERE, got {tok.Token}",
                    tok.Span.Start);
            }
        }

        ExpectToken(new Token.RParen());

        var alias = MaybeParseTableAlias();

        return new TableFactor.SemanticView(
            Name: name,
            Dimensions: dimensions,
            Metrics: metrics,
            Facts: facts,
            WhereClause: whereClause,
            Alias: alias);
    }

    internal TableFactor ParseMatchRecognize(TableFactor table)
    {
        ExpectToken(new Token.LParen());

        Sequence<Expr> partitionBy;
        if (ParseKeywords(Keyword.PARTITION, Keyword.BY))
        {
            partitionBy = ParseCommaSeparated(p => p.ParseExpr());
        }
        else
        {
            partitionBy = Sequence<Expr>.Empty;
        }

        Sequence<OrderByExpr> orderBy;
        if (ParseKeywords(Keyword.ORDER, Keyword.BY))
        {
            orderBy = ParseCommaSeparated(p => p.ParseOrderByExpr());
        }
        else
        {
            orderBy = Sequence<OrderByExpr>.Empty;
        }

        Sequence<Measure> measures;
        if (ParseKeyword(Keyword.MEASURES))
        {
            measures = ParseCommaSeparated(p =>
            {
                var expr = p.ParseExpr();
                _ = p.ParseKeyword(Keyword.AS);
                var alias = p.ParseIdentifier();
                return new Measure(Expr: expr, Alias: alias);
            });
        }
        else
        {
            measures = Sequence<Measure>.Empty;
        }

        RowsPerMatch? rowsPerMatch;
        if (ParseKeywords(Keyword.ONE, Keyword.ROW, Keyword.PER, Keyword.MATCH))
        {
            rowsPerMatch = new RowsPerMatch.OneRow();
        }
        else if (ParseKeywords(
            Keyword.ALL,
            Keyword.ROWS,
            Keyword.PER,
            Keyword.MATCH))
        {
            EmptyMatchesMode? emptyMatchesMode;
            if (ParseKeywords(Keyword.SHOW, Keyword.EMPTY, Keyword.MATCHES))
            {
                emptyMatchesMode = new EmptyMatchesMode.Show();
            }
            else if (ParseKeywords(
                Keyword.OMIT,
                Keyword.EMPTY,
                Keyword.MATCHES))
            {
                emptyMatchesMode = new EmptyMatchesMode.Omit();
            }
            else if (ParseKeywords(
                Keyword.WITH,
                Keyword.UNMATCHED,
                Keyword.ROWS))
            {
                emptyMatchesMode = new EmptyMatchesMode.WithUnmatched();
            }
            else
            {
                emptyMatchesMode = null;
            }
            rowsPerMatch = new RowsPerMatch.AllRows(emptyMatchesMode);
        }
        else
        {
            rowsPerMatch = null;
        }

        AfterMatchSkip? afterMatchSkip;
        if (ParseKeywords(Keyword.AFTER, Keyword.MATCH, Keyword.SKIP))
        {
            if (ParseKeywords(Keyword.PAST, Keyword.LAST, Keyword.ROW))
            {
                afterMatchSkip = new AfterMatchSkip.PastLastRow();
            }
            else if (ParseKeywords(Keyword.TO, Keyword.NEXT, Keyword.ROW))
            {
                afterMatchSkip = new AfterMatchSkip.ToNextRow();
            }
            else if (ParseKeywords(Keyword.TO, Keyword.FIRST))
            {
                afterMatchSkip = new AfterMatchSkip.ToFirst(ParseIdentifier());
            }
            else if (ParseKeywords(Keyword.TO, Keyword.LAST))
            {
                afterMatchSkip = new AfterMatchSkip.ToLast(ParseIdentifier());
            }
            else
            {
                var found = NextToken();
                return Expected<TableFactor>("after match skip option", found);
            }
        }
        else
        {
            afterMatchSkip = null;
        }

        ExpectKeywordIs(Keyword.PATTERN);
        var pattern = ParseParenthesized(p => p.ParsePattern());

        ExpectKeywordIs(Keyword.DEFINE);

        var symbols = ParseCommaSeparated(p =>
        {
            var symbol = p.ParseIdentifier();
            p.ExpectKeywordIs(Keyword.AS);
            var definition = p.ParseExpr();
            return new SymbolDefinition(Symbol: symbol, Definition: definition);
        });

        ExpectToken(new Token.RParen());

        var alias = MaybeParseTableAlias();

        return new TableFactor.MatchRecognize(
            Table: table,
            PartitionBy: partitionBy,
            OrderBy: orderBy,
            Measures: measures,
            RowsPerMatch: rowsPerMatch,
            AfterMatchSkip: afterMatchSkip,
            Pattern: pattern,
            Symbols: symbols,
            Alias: alias);
    }
}
