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

namespace SqlParser.Core.Ast;

/// <summary>Operator used to separate function arguments</summary>
public abstract partial record FunctionArgOperator : IDisplay
{
    /// <summary>function(arg1 = value1)</summary>
    /// <remarks>
    /// Rust: variante <c>Equals</c>. Renomeada com o sufixo <c>Variant</c>: um tipo aninhado <c>Equals</c>
    /// colidiria com o método <c>Equals</c> sintetizado pelo record (CS0102/CS0542; nomes.md).
    /// </remarks>
    public sealed record EqualsVariant : FunctionArgOperator;

    /// <summary>function(arg1 =&gt; value1)</summary>
    public sealed record RightArrow : FunctionArgOperator;

    /// <summary>function(arg1 := value1)</summary>
    public sealed record Assignment : FunctionArgOperator;

    /// <summary>function(arg1 : value1)</summary>
    public sealed record Colon : FunctionArgOperator;

    /// <summary>function(arg1 VALUE value1)</summary>
    public sealed record Value : FunctionArgOperator;

    /// <summary>
    /// function(arg1 value1), with no operator between the name and the value,
    /// as in PostgreSQL <c>XMLPARSE(DOCUMENT value)</c>
    /// <para><see href="https://www.postgresql.org/docs/current/datatype-xml.html#DATATYPE-XML-CREATING">PostgreSQL</see></para>
    /// </summary>
    public sealed record Space : FunctionArgOperator;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case EqualsVariant:
                f.WriteStr("=");
                break;
            case RightArrow:
                f.WriteStr("=>");
                break;
            case Assignment:
                f.WriteStr(":=");
                break;
            case Colon:
                f.WriteStr(":");
                break;
            case Value:
                f.WriteStr("VALUE");
                break;
            case Space:
                break;
            default:
                throw new InvalidOperationException($"Variante de FunctionArgOperator não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
