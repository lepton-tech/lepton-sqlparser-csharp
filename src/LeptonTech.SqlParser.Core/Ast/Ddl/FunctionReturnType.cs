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

/// <summary>The return type of a <c>CREATE FUNCTION</c> statement.</summary>
public abstract partial record FunctionReturnType : IDisplay
{
    /// <summary><c>RETURNS &lt;type&gt;</c></summary>
    // Variante tupla cujo tipo tem o mesmo nome: tipo qualificado (convencoes.md §6 e §13, caso 38).
    public sealed record DataType(Ast.DataType Value) : FunctionReturnType;

    /// <summary>
    /// <c>RETURNS SETOF &lt;type&gt;</c>
    /// <para><see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see></para>
    /// </summary>
    // Dentro de FunctionReturnType, `DataType` é a variante: o tipo de topo é qualificado (caso 38).
    public sealed record SetOf(Ast.DataType Value) : FunctionReturnType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case DataType { Value: var dataType }:
                f.Write($"{dataType}");
                break;
            case SetOf { Value: var dataType }:
                f.Write($"SETOF {dataType}");
                break;
            default:
                throw new InvalidOperationException($"Variante de FunctionReturnType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
