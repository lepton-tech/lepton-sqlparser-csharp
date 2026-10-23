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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

// Rust: `impl fmt::Display for Expr` (mod.rs, logo depois de `impl fmt::Display for CastFormat`).
// O `match` inteiro vira um único `switch`, na mesma ordem dos braços do Rust.
public abstract partial record Expr
{
    public void Fmt(Formatter f)
    {
        // Rust: #[cfg_attr(feature = "recursive-protection", recursive::recursive)] (convencoes.md §13, caso 123).
        if (StackGuard.IsStackLow())
        {
            StackGuard.Grow(this, f, static (expr, formatter) => expr.Fmt(formatter));
            return;
        }

        switch (this)
        {
            case Identifier s:
                f.Write($"{s.Value}");
                break;
            case Wildcard:
                f.WriteStr("*");
                break;
            case QualifiedWildcard qualifiedWildcard:
                f.Write($"{qualifiedWildcard.Prefix}.*");
                break;
            case CompoundIdentifier s:
                f.Write($"{DisplaySeparated(s.Value, ".")}");
                break;
            case CompoundFieldAccess compoundFieldAccess:
            {
                f.Write($"{compoundFieldAccess.Root}");
                var prevIsNumber = IsNumberExpr(compoundFieldAccess.Root);
                foreach (var field in compoundFieldAccess.AccessChain)
                {
                    switch (field)
                    {
                        case AccessExpr.Dot { Value: var expr } when prevIsNumber:
                            f.Write($" . {expr}");
                            break;
                        default:
                            f.Write($"{field}");
                            break;
                    }
                    prevIsNumber = field is AccessExpr.Dot { Value: var dotExpr } && IsNumberExpr(dotExpr);
                }
                break;
            }
            case IsTrue ast:
                f.Write($"{ast.Value} IS TRUE");
                break;
            case IsNotTrue ast:
                f.Write($"{ast.Value} IS NOT TRUE");
                break;
            case IsFalse ast:
                f.Write($"{ast.Value} IS FALSE");
                break;
            case IsNotFalse ast:
                f.Write($"{ast.Value} IS NOT FALSE");
                break;
            case IsNull ast:
                f.Write($"{ast.Value} IS NULL");
                break;
            case IsNotNull ast:
                f.Write($"{ast.Value} IS NOT NULL");
                break;
            case IsUnknown ast:
                f.Write($"{ast.Value} IS UNKNOWN");
                break;
            case IsNotUnknown ast:
                f.Write($"{ast.Value} IS NOT UNKNOWN");
                break;
            case IsJson isJson:
            {
                f.Write($"{isJson.Expr} IS ");
                if (isJson.Negated)
                {
                    f.Write($"NOT ");
                }
                f.Write($"JSON");
                if (isJson.Kind is { } kind)
                {
                    f.Write($" {kind}");
                }
                if (isJson.UniqueKeys is { } uniqueKeys)
                {
                    f.Write($" {uniqueKeys}");
                }
                break;
            }
            case In inExpr:
                f.Write($"{inExpr.Expr} {(inExpr.Global ? "GLOBAL " : "")}{(inExpr.Negated ? "NOT " : "")}IN {inExpr.Target}");
                break;
            case Between between:
                f.Write($"{between.Expr} {(between.Negated ? "NOT " : "")}BETWEEN {between.Low} AND {between.High}");
                break;
            case BinaryOp binaryOp:
                f.Write($"{binaryOp.Left} {binaryOp.Op} {binaryOp.Right}");
                break;
            case Like like:
            {
                switch (like.EscapeChar)
                {
                    case { } ch:
                        f.Write($"{like.Expr} {(like.Negated ? "NOT " : "")}LIKE {(like.Any ? "ANY " : "")}{like.Pattern} ESCAPE {ch}");
                        break;
                    default:
                        f.Write($"{like.Expr} {(like.Negated ? "NOT " : "")}LIKE {(like.Any ? "ANY " : "")}{like.Pattern}");
                        break;
                }
                break;
            }
            case ILike iLike:
            {
                switch (iLike.EscapeChar)
                {
                    case { } ch:
                        // Igual ao Rust: com ESCAPE, "ANY" sai sem o espaço final.
                        f.Write($"{iLike.Expr} {(iLike.Negated ? "NOT " : "")}ILIKE {(iLike.Any ? "ANY" : "")}{iLike.Pattern} ESCAPE {ch}");
                        break;
                    default:
                        f.Write($"{iLike.Expr} {(iLike.Negated ? "NOT " : "")}ILIKE {(iLike.Any ? "ANY " : "")}{iLike.Pattern}");
                        break;
                }
                break;
            }
            case RLike rLike:
                f.Write($"{rLike.Expr} {(rLike.Negated ? "NOT " : "")}{(rLike.Regexp ? "REGEXP" : "RLIKE")} {rLike.Pattern}");
                break;
            case IsNormalized isNormalized:
            {
                var not_ = isNormalized.Negated ? "NOT " : "";
                if (isNormalized.Form is { } form)
                {
                    f.Write($"{isNormalized.Expr} IS {not_}{form} NORMALIZED");
                }
                else
                {
                    f.Write($"{isNormalized.Expr} IS {not_}NORMALIZED");
                }
                break;
            }
            case SimilarTo similarTo:
            {
                switch (similarTo.EscapeChar)
                {
                    case { } ch:
                        f.Write($"{similarTo.Expr} {(similarTo.Negated ? "NOT " : "")}SIMILAR TO {similarTo.Pattern} ESCAPE {ch}");
                        break;
                    default:
                        f.Write($"{similarTo.Expr} {(similarTo.Negated ? "NOT " : "")}SIMILAR TO {similarTo.Pattern}");
                        break;
                }
                break;
            }
            case AnyOp anyOp:
            {
                var addParens = !(anyOp.Right is Subquery);
                f.Write($"{anyOp.Left} {anyOp.CompareOp} {(anyOp.IsSome ? "SOME" : "ANY")}{(addParens ? "(" : "")}{anyOp.Right}{(addParens ? ")" : "")}");
                break;
            }
            case AllOp allOp:
            {
                var addParens = !(allOp.Right is Subquery);
                f.Write($"{allOp.Left} {allOp.CompareOp} ALL{(addParens ? "(" : "")}{allOp.Right}{(addParens ? ")" : "")}");
                break;
            }
            case UnaryOp unaryOp:
            {
                var op = unaryOp.Op;
                var expr = unaryOp.Expr;
                switch (op)
                {
                    case UnaryOperator.PGPostfixFactorial:
                        if (expr is UnaryOp { Op: UnaryOperator.PGPostfixFactorial })
                        {
                            f.Write($"{expr} {op}");
                        }
                        else
                        {
                            f.Write($"{expr}{op}");
                        }
                        break;
                    case UnaryOperator.Not
                        or UnaryOperator.BitwiseNot
                        or UnaryOperator.Hash
                        or UnaryOperator.AtDashAt
                        or UnaryOperator.DoubleAt
                        or UnaryOperator.PGAbs
                        or UnaryOperator.QuestionDash
                        or UnaryOperator.QuestionPipe
                        or UnaryOperator.PGSquareRoot
                        or UnaryOperator.PGCubeRoot:
                        f.Write($"{op} {expr}");
                        break;
                    case UnaryOperator.Minus or UnaryOperator.BangNot:
                        // Written before the check, so an enclosing
                        // `starts_with_operator_char` stops at this operator.
                        if (!f.Write($"{op}"))
                        {
                            return;
                        }
                        if (StartsWithOperatorChar(expr))
                        {
                            f.WriteStr(" ");
                        }
                        f.Write($"{expr}");
                        break;
                    case UnaryOperator.Plus or UnaryOperator.PGPrefixFactorial:
                        f.Write($"{op}{expr}");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de UnaryOperator não tratada: {op.GetType().Name}");
                }
                break;
            }
            case Convert convert:
            {
                f.Write($"{(convert.IsTry ? "TRY_" : "")}CONVERT(");
                if (convert.DataType is { } dataType)
                {
                    if (convert.Charset is { } charset)
                    {
                        f.Write($"{convert.Expr}, {dataType} CHARACTER SET {charset}");
                    }
                    else if (convert.TargetBeforeValue)
                    {
                        f.Write($"{dataType}, {convert.Expr}");
                    }
                    else
                    {
                        f.Write($"{convert.Expr}, {dataType}");
                    }
                }
                else if (convert.Charset is { } charset)
                {
                    f.Write($"{convert.Expr} USING {charset}");
                }
                else
                {
                    f.Write($"{convert.Expr}"); // This should never happen
                }
                if (!convert.Styles.IsEmpty)
                {
                    f.Write($", {DisplayCommaSeparated(convert.Styles)}");
                }
                f.Write($")");
                break;
            }
            case Cast cast:
            {
                switch (cast.Kind)
                {
                    case CastKind.Cast:
                    {
                        if (cast.DataType is DataType.Unspecified)
                        {
                            f.Write($"CAST({cast.Expr} AS");
                        }
                        else
                        {
                            f.Write($"CAST({cast.Expr} AS {cast.DataType}");
                        }
                        if (cast.Format is { } format)
                        {
                            f.Write($" FORMAT {format}");
                        }
                        f.Write($")");
                        break;
                    }
                    case CastKind.TryCast:
                    {
                        if (cast.Format is { } format)
                        {
                            f.Write($"TRY_CAST({cast.Expr} AS {cast.DataType} FORMAT {format})");
                        }
                        else
                        {
                            f.Write($"TRY_CAST({cast.Expr} AS {cast.DataType})");
                        }
                        break;
                    }
                    case CastKind.SafeCast:
                    {
                        if (cast.Format is { } format)
                        {
                            f.Write($"SAFE_CAST({cast.Expr} AS {cast.DataType} FORMAT {format})");
                        }
                        else
                        {
                            f.Write($"SAFE_CAST({cast.Expr} AS {cast.DataType})");
                        }
                        break;
                    }
                    case CastKind.DoubleColon:
                    {
                        f.Write($"{cast.Expr}::{cast.DataType}");
                        break;
                    }
                    default:
                        throw new InvalidOperationException($"Valor de CastKind não tratado: {cast.Kind}");
                }
                break;
            }
            case Extract extract:
            {
                switch (extract.Syntax)
                {
                    case ExtractSyntax.From:
                        f.Write($"EXTRACT({extract.Field} FROM {extract.Expr})");
                        break;
                    case ExtractSyntax.Comma:
                        f.Write($"EXTRACT({extract.Field}, {extract.Expr})");
                        break;
                    default:
                        throw new InvalidOperationException($"Valor de ExtractSyntax não tratado: {extract.Syntax}");
                }
                break;
            }
            case Ceil ceil:
            {
                switch (ceil.Field)
                {
                    case CeilFloorKind.DateTimeField { Value: DateTimeField.NoDateTime }:
                        f.Write($"CEIL({ceil.Expr})");
                        break;
                    case CeilFloorKind.DateTimeField { Value: var dtField }:
                        f.Write($"CEIL({ceil.Expr} TO {dtField})");
                        break;
                    case CeilFloorKind.Scale { Value: var s }:
                        f.Write($"CEIL({ceil.Expr}, {s})");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de CeilFloorKind não tratada: {ceil.Field.GetType().Name}");
                }
                break;
            }
            case Floor floor:
            {
                switch (floor.Field)
                {
                    case CeilFloorKind.DateTimeField { Value: DateTimeField.NoDateTime }:
                        f.Write($"FLOOR({floor.Expr})");
                        break;
                    case CeilFloorKind.DateTimeField { Value: var dtField }:
                        f.Write($"FLOOR({floor.Expr} TO {dtField})");
                        break;
                    case CeilFloorKind.Scale { Value: var s }:
                        f.Write($"FLOOR({floor.Expr}, {s})");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de CeilFloorKind não tratada: {floor.Field.GetType().Name}");
                }
                break;
            }
            case Position position:
                f.Write($"POSITION({position.Expr} IN {position.In})");
                break;
            case Collate collate:
                f.Write($"{collate.Expr} COLLATE {collate.Collation}");
                break;
            case Nested ast:
                f.Write($"({ast.Value})");
                break;
            case Value v:
                f.Write($"{v.Inner}");
                break;
            case Prefixed prefixed:
                f.Write($"{prefixed.Prefix} {prefixed.Value}");
                break;
            case TypedString ts:
                ts.Value.Fmt(f);
                break;
            case Function fun:
                fun.Value.Fmt(f);
                break;
            case Case caseExpr:
            {
                f.WriteStr("CASE");
                if (caseExpr.Operand is { } operand)
                {
                    f.WriteStr(" ");
                    operand.Fmt(f);
                }
                foreach (var when in caseExpr.Conditions)
                {
                    SpaceOrNewline.Instance.Fmt(f);
                    new Indent<CaseWhen>(when).Fmt(f);
                }
                if (caseExpr.ElseResult is { } elseResult)
                {
                    SpaceOrNewline.Instance.Fmt(f);
                    new Indent<string>("ELSE").Fmt(f);
                    SpaceOrNewline.Instance.Fmt(f);
                    new Indent<Indent<Expr>>(new Indent<Expr>(elseResult)).Fmt(f);
                }
                SpaceOrNewline.Instance.Fmt(f);
                f.WriteStr("END");
                break;
            }
            case Exists exists:
                f.Write($"{(exists.Negated ? "NOT " : "")}EXISTS ({exists.Subquery})");
                break;
            case Subquery s:
                f.Write($"({s.Value})");
                break;
            case GroupingSets groupingSets:
            {
                f.Write($"GROUPING SETS (");
                var sep = "";
                foreach (var set in groupingSets.Value)
                {
                    f.Write($"{sep}");
                    sep = ", ";
                    f.Write($"({DisplayCommaSeparated(set)})");
                }
                f.Write($")");
                break;
            }
            case Cube cube:
            {
                f.Write($"CUBE (");
                var sep = "";
                foreach (var set in cube.Value)
                {
                    f.Write($"{sep}");
                    sep = ", ";
                    if (set.Count == 1)
                    {
                        f.Write($"{set[0]}");
                    }
                    else
                    {
                        f.Write($"({DisplayCommaSeparated(set)})");
                    }
                }
                f.Write($")");
                break;
            }
            case Rollup rollup:
            {
                f.Write($"ROLLUP (");
                var sep = "";
                foreach (var set in rollup.Value)
                {
                    f.Write($"{sep}");
                    sep = ", ";
                    if (set.Count == 1)
                    {
                        f.Write($"{set[0]}");
                    }
                    else
                    {
                        f.Write($"({DisplayCommaSeparated(set)})");
                    }
                }
                f.Write($")");
                break;
            }
            case Substring substring:
            {
                f.WriteStr("SUBSTR");
                if (!substring.Shorthand)
                {
                    f.WriteStr("ING");
                }
                f.Write($"({substring.Expr}");
                if (substring.SubstringFrom is { } fromPart)
                {
                    if (substring.Special)
                    {
                        f.Write($", {fromPart}");
                    }
                    else
                    {
                        f.Write($" FROM {fromPart}");
                    }
                }
                if (substring.SubstringFor is { } forPart)
                {
                    if (substring.Special)
                    {
                        f.Write($", {forPart}");
                    }
                    else
                    {
                        f.Write($" FOR {forPart}");
                    }
                }

                f.Write($")");
                break;
            }
            case Overlay overlay:
            {
                f.Write($"OVERLAY({overlay.Expr} PLACING {overlay.OverlayWhat} FROM {overlay.OverlayFrom}");
                if (overlay.OverlayFor is { } forPart)
                {
                    f.Write($" FOR {forPart}");
                }

                f.Write($")");
                break;
            }
            case IsDistinctFrom isDistinctFrom:
                f.Write($"{isDistinctFrom.Left} IS DISTINCT FROM {isDistinctFrom.Right}");
                break;
            case IsNotDistinctFrom isNotDistinctFrom:
                f.Write($"{isNotDistinctFrom.Left} IS NOT DISTINCT FROM {isNotDistinctFrom.Right}");
                break;
            case Trim trim:
            {
                f.Write($"TRIM(");
                if (trim.TrimWhere is { } ident)
                {
                    f.Write($"{ident} ");
                }
                if (trim.TrimWhat is { } trimChar)
                {
                    f.Write($"{trimChar} FROM {trim.Expr}");
                }
                else
                {
                    f.Write($"{trim.Expr}");
                }
                if (trim.TrimCharacters is { } characters)
                {
                    f.Write($", {DisplayCommaSeparated(characters)}");
                }

                f.Write($")");
                break;
            }
            case Tuple exprs:
                f.Write($"({DisplayCommaSeparated(exprs.Value)})");
                break;
            case Struct structExpr:
            {
                if (!structExpr.Fields.IsEmpty)
                {
                    f.Write($"STRUCT<{DisplayCommaSeparated(structExpr.Fields)}>({DisplayCommaSeparated(structExpr.Values)})");
                }
                else
                {
                    f.Write($"STRUCT({DisplayCommaSeparated(structExpr.Values)})");
                }
                break;
            }
            case Named named:
                f.Write($"{named.Expr} AS {named.Name}");
                break;
            case Dictionary fields:
                f.Write($"{{{DisplayCommaSeparated(fields.Value)}}}");
                break;
            case Map map:
                f.Write($"{map.Value}");
                break;
            case Array set:
                f.Write($"{set.Value}");
                break;
            case JsonAccess jsonAccess:
                f.Write($"{jsonAccess.Value}{jsonAccess.Path}");
                break;
            case AtTimeZone atTimeZone:
                f.Write($"{atTimeZone.Timestamp} AT TIME ZONE {atTimeZone.TimeZone}");
                break;
            case Interval interval:
                f.Write($"{interval.Value}");
                break;
            case MatchAgainst matchAgainst:
            {
                var matchExpr = matchAgainst.MatchValue;
                f.Write($"MATCH ({DisplayCommaSeparated(matchAgainst.Columns)}) AGAINST ");

                if (matchAgainst.OptSearchModifier is { } searchModifier)
                {
                    f.Write($"({matchExpr} {searchModifier})");
                }
                else
                {
                    f.Write($"({matchExpr})");
                }
                break;
            }
            case OuterJoin outerJoin:
                f.Write($"{outerJoin.Value} (+)");
                break;
            case Prior prior:
                f.Write($"PRIOR {prior.Value}");
                break;
            case Lambda lambda:
                f.Write($"{lambda.Value}");
                break;
            case MemberOf memberOf:
                f.Write($"{memberOf.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de Expr não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
