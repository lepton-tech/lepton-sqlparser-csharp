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

/// <summary>Hive-specific <c>SET LOCATION</c> helper used in some <c>LOAD DATA</c> statements.</summary>
/// <param name="HasSet">Whether the <c>SET</c> keyword was present.</param>
/// <param name="Location">The location identifier.</param>
public sealed record HiveSetLocation(bool HasSet, Ident Location) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (HasSet)
        {
            f.Write($"SET ");
        }
        f.Write($"LOCATION {Location}");
    }

    public override string ToString() => Formatter.Format(this);
}
