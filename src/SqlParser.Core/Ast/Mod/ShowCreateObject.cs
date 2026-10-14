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

/// <summary>Object kinds supported by <c>SHOW CREATE</c> statements.</summary>
public abstract record ShowCreateObject : IDisplay
{
    /// <summary>An event object for <c>SHOW CREATE EVENT</c>.</summary>
    public sealed record Event : ShowCreateObject;

    /// <summary>A function object for <c>SHOW CREATE FUNCTION</c>.</summary>
    public sealed record Function : ShowCreateObject;

    /// <summary>A procedure object for <c>SHOW CREATE PROCEDURE</c>.</summary>
    public sealed record Procedure : ShowCreateObject;

    /// <summary>A table object for <c>SHOW CREATE TABLE</c>.</summary>
    public sealed record Table : ShowCreateObject;

    /// <summary>A trigger object for <c>SHOW CREATE TRIGGER</c>.</summary>
    public sealed record Trigger : ShowCreateObject;

    /// <summary>A view object for <c>SHOW CREATE VIEW</c>.</summary>
    public sealed record View : ShowCreateObject;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Event:
                f.WriteStr("EVENT");
                break;
            case Function:
                f.WriteStr("FUNCTION");
                break;
            case Procedure:
                f.WriteStr("PROCEDURE");
                break;
            case Table:
                f.WriteStr("TABLE");
                break;
            case Trigger:
                f.WriteStr("TRIGGER");
                break;
            case View:
                f.WriteStr("VIEW");
                break;
            default:
                throw new InvalidOperationException($"Variante de ShowCreateObject não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
