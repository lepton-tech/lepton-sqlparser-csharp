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

namespace SqlParser.Core.Ast;

/// <summary>A left table followed by zero or more joins.</summary>
/// <param name="Relation">The starting table factor (left side) of the join chain.</param>
/// <param name="Joins">The sequence of joins applied to the relation.</param>
public sealed partial record TableWithJoins(TableFactor Relation, Sequence<Join> Joins) : IDisplay
{
    public void Fmt(Formatter f)
    {
        Relation.Fmt(f);
        foreach (var join in Joins)
        {
            SpaceOrNewline.Instance.Fmt(f);
            join.Fmt(f);
        }
    }

    public override string ToString() => Formatter.Format(this);
}
