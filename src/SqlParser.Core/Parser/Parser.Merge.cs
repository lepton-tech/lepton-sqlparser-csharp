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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/merge.rs.
// Modificado: tradução de Rust para C#.
//
// Rust: `//! SQL Parser for a `MERGE` statement`. O submódulo tem um bloco `impl Parser<'_>`; em C#,
// um `partial` do Parser (convencoes.md §2). `pub(super)` → `internal` (caso 52).

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>Parse a <c>MERGE</c> statement, returning a <c>Box</c>ed SetExpr</summary>
    /// <remarks>This is used to reduce the size of the stack frames in debug builds</remarks>
    internal SetExpr ParseMergeSetexprBoxed(TokenWithSpan mergeToken) =>
        new SetExpr.Merge(Statement.From(ParseMerge(mergeToken)));

    /// <summary>Parse a <c>MERGE</c> statement</summary>
    public Merge ParseMerge(TokenWithSpan mergeToken)
    {
        var optimizerHints = MaybeParseOptimizerHints();
        var @into = ParseKeyword(Keyword.INTO);

        var table = ParseTableFactor();

        ExpectKeywordIs(Keyword.USING);
        var source = ParseTableFactor();
        ExpectKeywordIs(Keyword.ON);
        var @on = ParseExpr();
        var clauses = ParseMergeClauses();
        OutputClause? output;
        switch (ParseOneOfKeywords(Keyword.OUTPUT, Keyword.RETURNING))
        {
            case { } keyword:
                output = ParseOutput(keyword, GetCurrentToken());
                break;
            case null:
                output = null;
                break;
        }

        return new Merge(
            MergeToken: AttachedToken.From(mergeToken),
            OptimizerHints: optimizerHints,
            Into: @into,
            Table: table,
            Source: source,
            On: @on,
            Clauses: clauses,
            Output: output);
    }

    internal Sequence<MergeClause> ParseMergeClauses()
    {
        var clauses = new List<MergeClause>();
        while (true)
        {
            if (!ParseKeyword(Keyword.WHEN))
            {
                break;
            }
            var whenToken = GetCurrentToken();

            MergeClauseKind clauseKind = new MergeClauseKind.Matched();
            if (ParseKeyword(Keyword.NOT))
            {
                clauseKind = new MergeClauseKind.NotMatched();
            }
            ExpectKeywordIs(Keyword.MATCHED);

            if (clauseKind is MergeClauseKind.NotMatched
                && ParseKeywords(Keyword.BY, Keyword.SOURCE))
            {
                clauseKind = new MergeClauseKind.NotMatchedBySource();
            }
            else if (clauseKind is MergeClauseKind.NotMatched
                && ParseKeywords(Keyword.BY, Keyword.TARGET))
            {
                clauseKind = new MergeClauseKind.NotMatchedByTarget();
            }

            Expr? predicate;
            if (ParseKeyword(Keyword.AND))
            {
                predicate = ParseExpr();
            }
            else
            {
                predicate = null;
            }

            ExpectKeywordIs(Keyword.THEN);

            MergeAction mergeClause;
            switch (ParseOneOfKeywords(
                Keyword.UPDATE,
                Keyword.INSERT,
                Keyword.DELETE,
                Keyword.DO))
            {
                case Keyword.DO:
                {
                    var doToken = GetCurrentToken();
                    var nothingToken = ExpectKeyword(Keyword.NOTHING);
                    mergeClause = new MergeAction.DoNothing(
                        DoToken: AttachedToken.From(doToken),
                        NothingToken: AttachedToken.From(nothingToken));
                    break;
                }
                case Keyword.UPDATE:
                {
                    if (clauseKind is MergeClauseKind.NotMatched or MergeClauseKind.NotMatchedByTarget)
                    {
                        throw ParserErr(
                            $"UPDATE is not allowed in a {clauseKind} merge clause",
                            GetCurrentToken().Span.Start);
                    }

                    var updateToken = GetCurrentToken();
                    ExpectKeywordIs(Keyword.SET);
                    MergeUpdateKind kind;
                    if (ConsumeToken(new Token.Mul()))
                    {
                        kind = new MergeUpdateKind.Wildcard();
                    }
                    else
                    {
                        kind = new MergeUpdateKind.Set(ParseCommaSeparated(p => p.ParseAssignment()));
                    }
                    Expr? updatePredicate;
                    if (ParseKeyword(Keyword.WHERE))
                    {
                        updatePredicate = ParseExpr();
                    }
                    else
                    {
                        updatePredicate = null;
                    }
                    Expr? deletePredicate;
                    if (ParseKeyword(Keyword.DELETE))
                    {
                        _ = ExpectKeyword(Keyword.WHERE);
                        deletePredicate = ParseExpr();
                    }
                    else
                    {
                        deletePredicate = null;
                    }
                    mergeClause = new MergeAction.Update(new MergeUpdateExpr(
                        UpdateToken: AttachedToken.From(updateToken),
                        Kind: kind,
                        UpdatePredicate: updatePredicate,
                        DeletePredicate: deletePredicate));
                    break;
                }
                case Keyword.DELETE:
                {
                    if (clauseKind is MergeClauseKind.NotMatched or MergeClauseKind.NotMatchedByTarget)
                    {
                        throw ParserErr(
                            $"DELETE is not allowed in a {clauseKind} merge clause",
                            GetCurrentToken().Span.Start);
                    }

                    var deleteToken = GetCurrentToken();
                    mergeClause = new MergeAction.Delete(
                        DeleteToken: AttachedToken.From(deleteToken));
                    break;
                }
                case Keyword.INSERT:
                {
                    if (clauseKind is not (MergeClauseKind.NotMatched or MergeClauseKind.NotMatchedByTarget))
                    {
                        throw ParserErr(
                            $"INSERT is not allowed in a {clauseKind} merge clause",
                            GetCurrentToken().Span.Start);
                    }

                    var insertToken = GetCurrentToken();

                    if (ConsumeToken(new Token.Mul()))
                    {
                        var starToken = GetCurrentToken();
                        mergeClause = new MergeAction.Insert(new MergeInsertExpr(
                            InsertToken: AttachedToken.From(insertToken),
                            Columns: Sequence<ObjectName>.Empty,
                            KindToken: AttachedToken.From(starToken),
                            Kind: new MergeInsertKind.Wildcard(),
                            InsertPredicate: null));
                    }
                    else
                    {
                        var isMysql = _dialect is MySqlDialect;
                        var columns = ParseMergeClauseInsertColumns(isMysql);
                        // Rust: `let (kind, kind_token) = if .. { (..) } else { .. };` (decisão 7 da B2).
                        MergeInsertKind kind;
                        TokenWithSpan kindToken;
                        if ((_dialect is BigQueryDialect or GenericDialect)
                            && ParseKeyword(Keyword.ROW))
                        {
                            (kind, kindToken) = (new MergeInsertKind.Row(), GetCurrentToken());
                        }
                        else
                        {
                            ExpectKeywordIs(Keyword.VALUES);
                            var valuesToken = GetCurrentToken();
                            var values = ParseValues(isMysql, false);
                            (kind, kindToken) = (new MergeInsertKind.Values(values), valuesToken);
                        }
                        Expr? insertPredicate;
                        if (ParseKeyword(Keyword.WHERE))
                        {
                            insertPredicate = ParseExpr();
                        }
                        else
                        {
                            insertPredicate = null;
                        }

                        mergeClause = new MergeAction.Insert(new MergeInsertExpr(
                            InsertToken: AttachedToken.From(insertToken),
                            Columns: columns,
                            KindToken: AttachedToken.From(kindToken),
                            Kind: kind,
                            InsertPredicate: insertPredicate));
                    }
                    break;
                }
                default:
                {
                    throw ParserErr(
                        "expected UPDATE, DELETE, INSERT or DO NOTHING in merge clause",
                        PeekTokenRef().Span.Start);
                }
            }
            clauses.Add(new MergeClause(
                WhenToken: AttachedToken.From(whenToken),
                ClauseKind: clauseKind,
                Predicate: predicate,
                Action: mergeClause));
        }
        return clauses.ToSequence();
    }

    internal Sequence<ObjectName> ParseMergeClauseInsertColumns(bool allowEmpty) =>
        ParseParenthesizedQualifiedColumnList(IsOptional.Optional, allowEmpty);

    /// <summary>Parses an <c>OUTPUT</c> clause if present (MSSQL).</summary>
    internal OutputClause? MaybeParseOutputClause()
    {
        if (ParseKeyword(Keyword.OUTPUT))
        {
            return ParseOutput(
                Keyword.OUTPUT,
                GetCurrentToken());
        }
        else
        {
            return null;
        }
    }

    internal OutputClause ParseOutput(Keyword startKeyword, TokenWithSpan startToken)
    {
        var selectItems = ParseProjection();
        SelectInto? intoTable;
        if (startKeyword == Keyword.OUTPUT && PeekKeyword(Keyword.INTO))
        {
            ExpectKeywordIs(Keyword.INTO);
            intoTable = ParseSelectInto();
        }
        else
        {
            intoTable = null;
        }

        // Rust: `Ok(if .. { OutputClause::Output { .. } } else { OutputClause::Returning { .. } })`.
        if (startKeyword == Keyword.OUTPUT)
        {
            return new OutputClause.Output(
                OutputToken: AttachedToken.From(startToken),
                SelectItems: selectItems,
                IntoTable: intoTable);
        }
        else
        {
            return new OutputClause.Returning(
                ReturningToken: AttachedToken.From(startToken),
                SelectItems: selectItems);
        }
    }
}
