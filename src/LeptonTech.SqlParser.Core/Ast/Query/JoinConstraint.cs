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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Represents how two tables are constrained in a join: <c>ON</c>, <c>USING</c>, <c>NATURAL</c>, or none.</summary>
// Sem `Display` no Rust: o tipo não implementa `IDisplay`.
public abstract partial record JoinConstraint
{
    /// <summary><c>ON &lt;expr&gt;</c> join condition.</summary>
    public sealed record On(Expr Value) : JoinConstraint;

    /// <summary><c>USING(...)</c> list of column names.</summary>
    public sealed record Using(Sequence<ObjectName> Value) : JoinConstraint;

    /// <summary><c>NATURAL</c> join (columns matched automatically).</summary>
    public sealed record Natural : JoinConstraint;

    /// <summary>No constraint specified (e.g. <c>CROSS JOIN</c>).</summary>
    public sealed record None : JoinConstraint;
}
