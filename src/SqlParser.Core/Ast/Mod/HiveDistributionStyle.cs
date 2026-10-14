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

/// <summary>Distribution style options for Hive tables.</summary>
public abstract record HiveDistributionStyle
{
    /// <summary>Partitioned distribution with the given columns.</summary>
    /// <param name="Columns">Columns used for partitioning.</param>
    public sealed record PARTITIONED(Sequence<ColumnDef> Columns) : HiveDistributionStyle;

    /// <summary>Skewed distribution definition.</summary>
    /// <param name="Columns">Columns participating in the skew definition.</param>
    /// <param name="On">Columns listed in the <c>ON</c> clause for skewing.</param>
    /// <param name="StoredAsDirectories">Whether skewed data is stored as directories.</param>
    public sealed record SKEWED(Sequence<ColumnDef> Columns, Sequence<ColumnDef> On, bool StoredAsDirectories) : HiveDistributionStyle;

    /// <summary>No distribution style specified.</summary>
    public sealed record NONE : HiveDistributionStyle;
}
