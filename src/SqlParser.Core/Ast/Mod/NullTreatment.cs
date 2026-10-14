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
/// Specifies Ignore / Respect NULL within window functions.
/// For example
/// <c>FIRST_VALUE(column2) IGNORE NULLS OVER (PARTITION BY column1)</c>
/// </summary>
/// <remarks>How NULL values are treated in certain window functions.</remarks>
public abstract partial record NullTreatment : IDisplay
{
    /// <summary>Ignore NULL values (e.g. <c>IGNORE NULLS</c>).</summary>
    public sealed record IgnoreNulls : NullTreatment;

    /// <summary>Respect NULL values (e.g. <c>RESPECT NULLS</c>).</summary>
    public sealed record RespectNulls : NullTreatment;

    public void Fmt(Formatter f)
    {
        f.WriteStr(this switch
        {
            IgnoreNulls => "IGNORE NULLS",
            RespectNulls => "RESPECT NULLS",
            _ => throw new InvalidOperationException($"Variante de NullTreatment não tratada: {GetType().Name}"),
        });
    }

    public sealed override string ToString() => Formatter.Format(this);
}
