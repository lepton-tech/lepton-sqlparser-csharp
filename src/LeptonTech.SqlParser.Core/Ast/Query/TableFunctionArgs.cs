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

/// <summary>Arguments to a table-valued function</summary>
/// <param name="Args">The list of arguments passed to the table-valued function.</param>
/// <param name="Settings">
/// ClickHouse-specific <c>SETTINGS</c> clause.
/// For example,
/// <c>SELECT * FROM executable('generate_random.py', TabSeparated, 'id UInt32, random String', SETTINGS send_chunk_header = false, pool_size = 16)</c>
/// <see href="https://clickhouse.com/docs/en/engines/table-functions/executable"><c>executable</c> table function</see>
/// </param>
// Sem `Display` no Rust: o tipo não implementa `IDisplay`.
public sealed partial record TableFunctionArgs(Sequence<FunctionArg> Args, Sequence<Setting>? Settings);
