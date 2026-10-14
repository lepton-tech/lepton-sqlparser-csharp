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

/// <summary>[MySQL] <c>CREATE VIEW</c> additional parameters</summary>
/// <remarks><see href="https://dev.mysql.com/doc/refman/9.1/en/create-view.html">MySQL</see></remarks>
/// <param name="Algorithm">Optional view algorithm (e.g., MERGE, TEMPTABLE).</param>
/// <param name="Definer">Optional definer (the security principal that will own the view).</param>
/// <param name="Security">Optional SQL SECURITY setting for the view.</param>
public sealed partial record CreateViewParams(CreateViewAlgorithm? Algorithm, GranteeName? Definer, CreateViewSecurity? Security) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (Algorithm is { } algorithm)
        {
            f.Write($"ALGORITHM = {algorithm} ");
        }
        if (Definer is { } definers)
        {
            f.Write($"DEFINER = {definers} ");
        }
        if (Security is { } security)
        {
            f.Write($"SQL SECURITY {security} ");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
