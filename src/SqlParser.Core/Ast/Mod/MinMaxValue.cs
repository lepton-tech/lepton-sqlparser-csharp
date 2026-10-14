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

/// <summary>
/// Can use to describe options in  create sequence or table column type identity
/// [ MINVALUE minvalue | NO MINVALUE ] [ MAXVALUE maxvalue | NO MAXVALUE ]
/// </summary>
public abstract partial record MinMaxValue
{
    /// <summary>Clause is not specified.</summary>
    public sealed record Empty : MinMaxValue;

    /// <summary>NO MINVALUE / NO MAXVALUE.</summary>
    public sealed record None : MinMaxValue;

    /// <summary><c>MINVALUE &lt;expr&gt;</c> / <c>MAXVALUE &lt;expr&gt;</c>.</summary>
    public sealed record Some(Expr Value) : MinMaxValue;
}
