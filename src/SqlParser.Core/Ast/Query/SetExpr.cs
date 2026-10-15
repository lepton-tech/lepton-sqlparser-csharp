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

namespace SqlParser.Core.Ast;

/// <summary>
/// A node in a tree, representing a "query body" expression, roughly:
/// <c>SELECT ... [ {UNION|EXCEPT|INTERSECT} SELECT ...]</c>
/// </summary>
// As variantes `Select`, `Query`, `Values` e `Table` escondem os tipos de topo
// homônimos dentro deste record; por isso eles aparecem qualificados como `Ast.X`
// (convencoes.md §6 e §13, caso 38).
public abstract partial record SetExpr : IDisplay
{
    /// <summary>Restricted SELECT .. FROM .. HAVING (no ORDER BY or set operations)</summary>
    public sealed record Select(Ast.Select Value) : SetExpr;

    /// <summary>
    /// Parenthesized SELECT subquery, which may include more set operations
    /// in its body and an optional ORDER BY / LIMIT.
    /// </summary>
    public sealed record Query(Ast.Query Value) : SetExpr;

    /// <summary>
    /// UNION/EXCEPT/INTERSECT of two queries
    /// A set operation combining two query expressions.
    /// </summary>
    /// <param name="Left">Left operand of the set operation.</param>
    /// <param name="Op">The set operator used (e.g. <c>UNION</c>, <c>EXCEPT</c>).</param>
    /// <param name="SetQuantifier">Optional quantifier (<c>ALL</c>, <c>DISTINCT</c>, etc.).</param>
    /// <param name="Right">Right operand of the set operation.</param>
    public sealed record SetOperation(SetExpr Left, SetOperator Op, SetQuantifier SetQuantifier, SetExpr Right) : SetExpr;

    /// <summary><c>VALUES (...)</c></summary>
    public sealed record Values(Ast.Values Value) : SetExpr;

    /// <summary><c>INSERT</c> statement</summary>
    public sealed record Insert(Statement Value) : SetExpr;

    /// <summary><c>UPDATE</c> statement</summary>
    public sealed record Update(Statement Value) : SetExpr;

    /// <summary><c>DELETE</c> statement</summary>
    public sealed record Delete(Statement Value) : SetExpr;

    /// <summary><c>MERGE</c> statement</summary>
    public sealed record Merge(Statement Value) : SetExpr;

    /// <summary><c>TABLE</c> command</summary>
    public sealed record Table(Ast.Table Value) : SetExpr;

    /// <summary>If this <c>SetExpr</c> is a <c>SELECT</c>, returns the <c>Select</c>.</summary>
    public Ast.Select? AsSelect()
    {
        if (this is Select select)
        {
            return select.Value;
        }
        else
        {
            return null;
        }
    }

    public void Fmt(Formatter f)
    {
        // PORT-DEVIATION DIV-005: o Rust não marca este método com #[recursive::recursive], mas aqui cada
        // operação de uma cadeia `SELECT 1 UNION ALL SELECT 2 ...` (não limitada pelo contador de recursão)
        // gasta KBs de pilha; recebe a mesma proteção do Display de Expr (convencoes.md §13, caso 123).
        if (StackGuard.IsStackLow())
        {
            StackGuard.Grow(this, f, static (setExpr, formatter) => setExpr.Fmt(formatter));
            return;
        }

        switch (this)
        {
            case Select s:
                s.Value.Fmt(f);
                break;
            case Query q:
                f.WriteStr("(");
                q.Value.Fmt(f);
                f.WriteStr(")");
                break;
            case Values v:
                v.Value.Fmt(f);
                break;
            case Insert v:
                v.Value.Fmt(f);
                break;
            case Update v:
                v.Value.Fmt(f);
                break;
            case Delete v:
                v.Value.Fmt(f);
                break;
            case Merge v:
                v.Value.Fmt(f);
                break;
            case Table t:
                t.Value.Fmt(f);
                break;
            case SetOperation setOperation:
            {
                setOperation.Left.Fmt(f);
                SpaceOrNewline.Instance.Fmt(f);
                setOperation.Op.Fmt(f);
                switch (setOperation.SetQuantifier)
                {
                    case SetQuantifier.All
                        or SetQuantifier.Distinct
                        or SetQuantifier.ByName
                        or SetQuantifier.AllByName
                        or SetQuantifier.DistinctByName:
                        f.WriteStr(" ");
                        setOperation.SetQuantifier.Fmt(f);
                        break;
                    case SetQuantifier.None:
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de SetQuantifier não tratada: {setOperation.SetQuantifier.GetType().Name}");
                }
                SpaceOrNewline.Instance.Fmt(f);
                setOperation.Right.Fmt(f);
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de SetExpr não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
