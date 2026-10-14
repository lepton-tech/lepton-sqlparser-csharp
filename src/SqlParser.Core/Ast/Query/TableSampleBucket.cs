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

/// <summary>Bucket-based sampling clause: <c>BUCKET &lt;bucket&gt; OUT OF &lt;total&gt; [ON &lt;expr&gt;]</c>.</summary>
/// <param name="Bucket">The bucket index expression.</param>
/// <param name="Total">The total number of buckets expression.</param>
/// <param name="On">Optional <c>ON &lt;expr&gt;</c> specification.</param>
public sealed record TableSampleBucket(ValueWithSpan Bucket, ValueWithSpan Total, Expr? On) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"BUCKET {Bucket} OUT OF {Total}");
        if (On is { } onExpr)
        {
            f.Write($" ON {onExpr}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
