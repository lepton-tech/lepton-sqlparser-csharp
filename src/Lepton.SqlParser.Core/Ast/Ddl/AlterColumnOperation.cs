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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>An <c>ALTER COLUMN</c> (<c>Statement::AlterTable</c>) operation</summary>
public abstract partial record AlterColumnOperation : IDisplay
{
    /// <summary><c>SET NOT NULL</c></summary>
    public sealed record SetNotNull : AlterColumnOperation;

    /// <summary><c>DROP NOT NULL</c></summary>
    public sealed record DropNotNull : AlterColumnOperation;

    /// <summary>
    /// <c>SET DEFAULT &lt;expr&gt;</c>
    /// Set the column default value.
    /// </summary>
    /// <param name="Value">Expression representing the new default value.</param>
    public sealed record SetDefault(Expr Value) : AlterColumnOperation;

    /// <summary><c>DROP DEFAULT</c></summary>
    public sealed record DropDefault : AlterColumnOperation;

    /// <summary><c>[SET DATA] TYPE &lt;data_type&gt; [USING &lt;expr&gt;]</c></summary>
    /// <param name="DataType">Target data type for the column.</param>
    /// <param name="Using">PostgreSQL-specific <c>USING &lt;expr&gt;</c> expression for conversion.</param>
    /// <param name="HadSet">Set to true if the statement includes the <c>SET DATA TYPE</c> keywords.</param>
    public sealed record SetDataType(DataType DataType, Expr? Using, bool HadSet) : AlterColumnOperation;

    /// <summary>
    /// <c>ADD GENERATED { ALWAYS | BY DEFAULT } AS IDENTITY [ ( sequence_options ) ]</c>
    /// <para>Note: this is a PostgreSQL-specific operation.</para>
    /// </summary>
    /// <param name="GeneratedAs">Optional <c>GENERATED AS</c> specifier (e.g. <c>ALWAYS</c> or <c>BY DEFAULT</c>).</param>
    /// <param name="SequenceOptions">Optional sequence options for identity generation.</param>
    public sealed record AddGenerated(GeneratedAs? GeneratedAs, Sequence<SequenceOptions>? SequenceOptions) : AlterColumnOperation;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case SetNotNull:
                f.Write($"SET NOT NULL");
                break;
            case DropNotNull:
                f.Write($"DROP NOT NULL");
                break;
            case SetDefault setDefault:
                f.Write($"SET DEFAULT {setDefault.Value}");
                break;
            case DropDefault:
                f.Write($"DROP DEFAULT");
                break;
            case SetDataType setDataType:
            {
                if (setDataType.HadSet)
                {
                    f.Write($"SET DATA ");
                }
                f.Write($"TYPE {setDataType.DataType}");
                if (setDataType.Using is { } expr)
                {
                    f.Write($" USING {expr}");
                }
                break;
            }
            case AddGenerated addGenerated:
            {
                var generatedAs = addGenerated.GeneratedAs switch
                {
                    GeneratedAs.Always => " ALWAYS",
                    GeneratedAs.ByDefault => " BY DEFAULT",
                    _ => "",
                };

                f.Write($"ADD GENERATED{generatedAs} AS IDENTITY");
                if (addGenerated.SequenceOptions is { } options)
                {
                    f.Write($" (");

                    foreach (var sequenceOption in options)
                    {
                        f.Write($"{sequenceOption}");
                    }

                    f.Write($" )");
                }
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de AlterColumnOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
