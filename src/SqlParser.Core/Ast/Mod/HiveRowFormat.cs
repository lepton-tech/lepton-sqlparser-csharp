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

/// <summary>Row format specification for Hive tables (SERDE or DELIMITED).</summary>
public abstract record HiveRowFormat
{
    /// <summary>SerDe class specification with the implementing class name.</summary>
    /// <param name="Class">The SerDe implementation class name.</param>
    public sealed record SERDE(string Class) : HiveRowFormat;

    /// <summary>Delimited row format with one or more delimiter specifications.</summary>
    /// <param name="Delimiters">The list of delimiters used for delimiting fields/lines.</param>
    public sealed record DELIMITED(Sequence<HiveRowDelimiter> Delimiters) : HiveRowFormat;
}
