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

/// <summary>Option for <c>ALTER OPERATOR SET</c> operation</summary>
public abstract partial record OperatorOption : IDisplay
{
    /// <summary><c>RESTRICT = { res_proc | NONE }</c></summary>
    public sealed record Restrict(ObjectName? Value) : OperatorOption;

    /// <summary><c>JOIN = { join_proc | NONE }</c></summary>
    public sealed record Join(ObjectName? Value) : OperatorOption;

    /// <summary><c>COMMUTATOR = com_op</c></summary>
    public sealed record Commutator(ObjectName Value) : OperatorOption;

    /// <summary><c>NEGATOR = neg_op</c></summary>
    public sealed record Negator(ObjectName Value) : OperatorOption;

    /// <summary><c>HASHES</c></summary>
    public sealed record Hashes : OperatorOption;

    /// <summary><c>MERGES</c></summary>
    public sealed record Merges : OperatorOption;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Restrict { Value: { } procName }:
                f.Write($"RESTRICT = {procName}");
                break;
            case Restrict { Value: null }:
                f.Write($"RESTRICT = NONE");
                break;
            case Join { Value: { } procName }:
                f.Write($"JOIN = {procName}");
                break;
            case Join { Value: null }:
                f.Write($"JOIN = NONE");
                break;
            case Commutator { Value: var opName }:
                f.Write($"COMMUTATOR = {opName}");
                break;
            case Negator { Value: var opName }:
                f.Write($"NEGATOR = {opName}");
                break;
            case Hashes:
                f.Write($"HASHES");
                break;
            case Merges:
                f.Write($"MERGES");
                break;
            default:
                throw new InvalidOperationException($"Variante de OperatorOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
