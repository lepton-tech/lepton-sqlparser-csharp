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

/// <summary>A MSSQL <c>THROW</c> statement.</summary>
/// <remarks>
/// <code>
/// THROW [ error_number, message, state ]
/// </code>
/// <para><see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/throw-transact-sql">MSSQL</see></para>
/// </remarks>
/// <param name="ErrorNumber">Error number expression.</param>
/// <param name="Message">Error message expression.</param>
/// <param name="State">State expression.</param>
public sealed record ThrowStatement(Expr? ErrorNumber, Expr? Message, Expr? State) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // let ThrowStatement { error_number, message, state } = self;
        var errorNumber = ErrorNumber;
        var message = Message;
        var state = State;

        f.Write($"THROW");
        if (errorNumber is { } errorNumberValue && message is { } messageValue && state is { } stateValue)
        {
            f.Write($" {errorNumberValue}, {messageValue}, {stateValue}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
