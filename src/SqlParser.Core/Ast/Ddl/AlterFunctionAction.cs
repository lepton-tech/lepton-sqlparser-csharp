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

namespace SqlParser.Core.Ast;

/// <summary>Function action in <c>ALTER FUNCTION ... action [ ... ] [ RESTRICT ]</c>.</summary>
public abstract partial record AlterFunctionAction : IDisplay
{
    /// <summary><c>CALLED ON NULL INPUT</c> / <c>RETURNS NULL ON NULL INPUT</c> / <c>STRICT</c></summary>
    public sealed record CalledOnNull(FunctionCalledOnNull Value) : AlterFunctionAction;

    /// <summary><c>IMMUTABLE</c> / <c>STABLE</c> / <c>VOLATILE</c></summary>
    public sealed record Behavior(FunctionBehavior Value) : AlterFunctionAction;

    /// <summary><c>[ NOT ] LEAKPROOF</c></summary>
    public sealed record Leakproof(bool Value) : AlterFunctionAction;

    /// <summary><c>[ EXTERNAL ] SECURITY { DEFINER | INVOKER }</c></summary>
    /// <param name="External">Whether the optional <c>EXTERNAL</c> keyword was present.</param>
    /// <param name="SecurityValue">
    /// Security mode.
    /// <para>Rust: campo <c>security</c>. Sufixo <c>Value</c> por CS0542 (convencoes.md §13, caso 41).</para>
    /// </param>
    public sealed record Security(bool External, FunctionSecurity SecurityValue) : AlterFunctionAction;

    /// <summary><c>PARALLEL { UNSAFE | RESTRICTED | SAFE }</c></summary>
    public sealed record Parallel(FunctionParallel Value) : AlterFunctionAction;

    /// <summary><c>COST execution_cost</c></summary>
    public sealed record Cost(Expr Value) : AlterFunctionAction;

    /// <summary><c>ROWS result_rows</c></summary>
    public sealed record Rows(Expr Value) : AlterFunctionAction;

    /// <summary><c>SUPPORT support_function</c></summary>
    public sealed record Support(ObjectName Value) : AlterFunctionAction;

    /// <summary>
    /// <c>SET configuration_parameter { TO | = } { value | DEFAULT }</c>
    /// or <c>SET configuration_parameter FROM CURRENT</c>
    /// </summary>
    public sealed record Set(FunctionDefinitionSetParam Value) : AlterFunctionAction;

    /// <summary><c>RESET configuration_parameter</c> or <c>RESET ALL</c></summary>
    public sealed record Reset(ResetConfig Value) : AlterFunctionAction;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case CalledOnNull { Value: var calledOnNull }:
                f.Write($"{calledOnNull}");
                break;
            case Behavior { Value: var behavior }:
                f.Write($"{behavior}");
                break;
            case Leakproof { Value: var leakproof }:
                if (leakproof)
                {
                    f.Write($"LEAKPROOF");
                }
                else
                {
                    f.Write($"NOT LEAKPROOF");
                }
                break;
            case Security { External: var external, SecurityValue: var security }:
                if (external)
                {
                    f.Write($"EXTERNAL ");
                }
                f.Write($"{security}");
                break;
            case Parallel { Value: var parallel }:
                f.Write($"{parallel}");
                break;
            case Cost { Value: var executionCost }:
                f.Write($"COST {executionCost}");
                break;
            case Rows { Value: var resultRows }:
                f.Write($"ROWS {resultRows}");
                break;
            case Support { Value: var supportFunction }:
                f.Write($"SUPPORT {supportFunction}");
                break;
            case Set { Value: var setParam }:
                f.Write($"{setParam}");
                break;
            case Reset { Value: var resetConfig }:
            {
                switch (resetConfig)
                {
                    case ResetConfig.ALL:
                        f.Write($"RESET ALL");
                        break;
                    case ResetConfig.ConfigName { Value: var name }:
                        f.Write($"RESET {name}");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de ResetConfig não tratada: {resetConfig.GetType().Name}");
                }
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de AlterFunctionAction não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
