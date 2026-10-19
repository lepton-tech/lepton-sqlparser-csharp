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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// CREATE OPERATOR statement
/// See <see href="https://www.postgresql.org/docs/current/sql-createoperator.html"/>
/// </summary>
/// <param name="Name">Operator name (can be schema-qualified)</param>
/// <param name="Function">FUNCTION or PROCEDURE parameter (function name)</param>
/// <param name="IsProcedure">Whether PROCEDURE keyword was used (vs FUNCTION)</param>
/// <param name="LeftArg">LEFTARG parameter (left operand type)</param>
/// <param name="RightArg">RIGHTARG parameter (right operand type)</param>
/// <param name="Options">Operator options (COMMUTATOR, NEGATOR, RESTRICT, JOIN, HASHES, MERGES)</param>
public sealed partial record CreateOperator(
    ObjectName Name,
    ObjectName Function,
    bool IsProcedure,
    DataType? LeftArg,
    DataType? RightArg,
    Sequence<OperatorOption> Options) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"CREATE OPERATOR {Name} (");

        var functionKeyword = IsProcedure ? "PROCEDURE" : "FUNCTION";
        // Rust: `params` é um Vec<String> montado com `format!`/`to_string()` e escrito com
        // `params.join(", ")`. Escrita direta na mesma ordem: o primeiro item sempre existe, então cada
        // item seguinte leva o separador ", " na frente (docs/port/onda-a/A2.md, decisão 3; caso 30).
        f.Write($"{functionKeyword} = {Function}");

        if (LeftArg is { } leftArg)
        {
            f.Write($", LEFTARG = {leftArg}");
        }
        if (RightArg is { } rightArg)
        {
            f.Write($", RIGHTARG = {rightArg}");
        }

        foreach (var option in Options)
        {
            f.Write($", {option}");
        }

        f.Write($")");
    }

    public override string ToString() => Formatter.Format(this);
}
