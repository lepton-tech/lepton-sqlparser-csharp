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
/// <code>
/// ALTER USER &lt;role_specification&gt; [ WITH ] PASSWORD { 'password' | NULL }``
/// </code>
/// </summary>
/// <param name="Encrypted">Whether the password is encrypted.</param>
/// <param name="Password">The password string, or <c>None</c> for <c>NULL</c>.</param>
public sealed partial record AlterUserPassword(bool Encrypted, string? Password) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (Encrypted)
        {
            f.Write($"ENCRYPTED ");
        }
        f.Write($"PASSWORD");
        switch (Password)
        {
            case null:
                f.Write($" NULL");
                break;
            case { } password:
                f.Write($" '{Ast.Value.EscapeSingleQuoteString(password)}'");
                break;
        }
    }

    public override string ToString() => Formatter.Format(this);
}
