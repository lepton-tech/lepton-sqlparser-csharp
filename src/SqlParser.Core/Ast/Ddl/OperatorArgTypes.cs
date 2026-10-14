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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>Operator argument types for CREATE OPERATOR CLASS</summary>
/// <param name="Left">Left-hand operand data type for the operator.</param>
/// <param name="Right">Right-hand operand data type for the operator.</param>
public sealed partial record OperatorArgTypes(DataType Left, DataType Right) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Left}, {Right}");
    }

    public override string ToString() => Formatter.Format(this);
}
