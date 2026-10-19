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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>A single <c>JOIN</c> clause including relation and join operator/options.</summary>
/// <param name="Relation">The joined table factor (table reference or derived table).</param>
/// <param name="Global">
/// ClickHouse supports the optional <c>GLOBAL</c> keyword before the join operator.
/// See <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/join">ClickHouse</see>
/// </param>
/// <param name="JoinOperator">The join operator and its constraint (INNER/LEFT/RIGHT/CROSS/ASOF/etc.).</param>
public sealed partial record Join(TableFactor Relation, bool Global, JoinOperator JoinOperator) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // Rust: `fn prefix` e `fn suffix` aninhadas em `fmt` → funções locais.
        static string Prefix(JoinConstraint constraint)
        {
            return constraint switch
            {
                JoinConstraint.Natural => "NATURAL ",
                _ => "",
            };
        }

        static IDisplay Suffix(JoinConstraint constraint)
        {
            return new Suffix(constraint);
        }

        if (Global)
        {
            f.Write($"GLOBAL ");
        }

        // f.write_fmt(format_args!(...)) equivale a write!(f, ...) → f.Write($"...") (caso 30).
        switch (JoinOperator)
        {
            case JoinOperator.Join { Value: var constraint }:
                f.Write($"{Prefix(constraint)}JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.Inner { Value: var constraint }:
                f.Write($"{Prefix(constraint)}INNER JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.Left { Value: var constraint }:
                f.Write($"{Prefix(constraint)}LEFT JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.LeftOuter { Value: var constraint }:
                f.Write($"{Prefix(constraint)}LEFT OUTER JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.Right { Value: var constraint }:
                f.Write($"{Prefix(constraint)}RIGHT JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.RightOuter { Value: var constraint }:
                f.Write($"{Prefix(constraint)}RIGHT OUTER JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.FullOuter { Value: var constraint }:
                f.Write($"{Prefix(constraint)}FULL JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.CrossJoin { Value: var constraint }:
                f.Write($"CROSS JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.Semi { Value: var constraint }:
                f.Write($"{Prefix(constraint)}SEMI JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.LeftSemi { Value: var constraint }:
                f.Write($"{Prefix(constraint)}LEFT SEMI JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.RightSemi { Value: var constraint }:
                f.Write($"{Prefix(constraint)}RIGHT SEMI JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.Anti { Value: var constraint }:
                f.Write($"{Prefix(constraint)}ANTI JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.LeftAnti { Value: var constraint }:
                f.Write($"{Prefix(constraint)}LEFT ANTI JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.RightAnti { Value: var constraint }:
                f.Write($"{Prefix(constraint)}RIGHT ANTI JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.CrossApply:
                f.Write($"CROSS APPLY {Relation}");
                break;
            case JoinOperator.OuterApply:
                f.Write($"OUTER APPLY {Relation}");
                break;
            case JoinOperator.AsOf { MatchCondition: var matchCondition, Constraint: var constraint }:
                f.Write($"ASOF JOIN {Relation} MATCH_CONDITION ({matchCondition}){Suffix(constraint)}");
                break;
            case JoinOperator.StraightJoin { Value: var constraint }:
                f.Write($"STRAIGHT_JOIN {Relation}{Suffix(constraint)}");
                break;
            case JoinOperator.ArrayJoin:
                f.Write($"ARRAY JOIN {Relation}");
                break;
            case JoinOperator.LeftArrayJoin:
                f.Write($"LEFT ARRAY JOIN {Relation}");
                break;
            case JoinOperator.InnerArrayJoin:
                f.Write($"INNER ARRAY JOIN {Relation}");
                break;
            default:
                throw new InvalidOperationException($"Variante de JoinOperator não tratada: {JoinOperator.GetType().Name}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // Rust: `struct Suffix<'a>(&'a JoinConstraint)`, declarado dentro de `fn suffix` no
    // `impl Display for Join`. C# não declara tipos dentro de métodos: vira classe privada aninhada.
    private sealed class Suffix : IDisplay
    {
        public Suffix(JoinConstraint value) => Value = value;

        // Rust: campo `.0`.
        public JoinConstraint Value { get; }

        public void Fmt(Formatter f)
        {
            switch (Value)
            {
                case JoinConstraint.On { Value: var expr }:
                    f.Write($" ON {expr}");
                    break;
                case JoinConstraint.Using { Value: var attrs }:
                    f.Write($" USING({DisplayCommaSeparated(attrs)})");
                    break;
                default:
                    break;
            }
        }

        public override string ToString() => Formatter.Format(this);
    }
}
