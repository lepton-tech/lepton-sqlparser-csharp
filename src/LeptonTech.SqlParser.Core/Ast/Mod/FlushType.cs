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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Types of flush operations supported by <c>FLUSH</c>.</summary>
public abstract partial record FlushType : IDisplay
{
    /// <summary>Flush binary logs.</summary>
    public sealed record BinaryLogs : FlushType;

    /// <summary>Flush engine logs.</summary>
    public sealed record EngineLogs : FlushType;

    /// <summary>Flush error logs.</summary>
    public sealed record ErrorLogs : FlushType;

    /// <summary>Flush general logs.</summary>
    public sealed record GeneralLogs : FlushType;

    /// <summary>Flush hosts information.</summary>
    public sealed record Hosts : FlushType;

    /// <summary>Flush logs.</summary>
    public sealed record Logs : FlushType;

    /// <summary>Flush privileges.</summary>
    public sealed record Privileges : FlushType;

    /// <summary>Flush optimizer costs.</summary>
    public sealed record OptimizerCosts : FlushType;

    /// <summary>Flush relay logs.</summary>
    public sealed record RelayLogs : FlushType;

    /// <summary>Flush slow logs.</summary>
    public sealed record SlowLogs : FlushType;

    /// <summary>Flush status.</summary>
    public sealed record Status : FlushType;

    /// <summary>Flush user resources.</summary>
    public sealed record UserResources : FlushType;

    /// <summary>Flush table data.</summary>
    public sealed record Tables : FlushType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case BinaryLogs:
                f.WriteStr("BINARY LOGS");
                break;
            case EngineLogs:
                f.WriteStr("ENGINE LOGS");
                break;
            case ErrorLogs:
                f.WriteStr("ERROR LOGS");
                break;
            case GeneralLogs:
                f.WriteStr("GENERAL LOGS");
                break;
            case Hosts:
                f.WriteStr("HOSTS");
                break;
            case Logs:
                f.WriteStr("LOGS");
                break;
            case Privileges:
                f.WriteStr("PRIVILEGES");
                break;
            case OptimizerCosts:
                f.WriteStr("OPTIMIZER_COSTS");
                break;
            case RelayLogs:
                f.WriteStr("RELAY LOGS");
                break;
            case SlowLogs:
                f.WriteStr("SLOW LOGS");
                break;
            case Status:
                f.WriteStr("STATUS");
                break;
            case UserResources:
                f.WriteStr("USER_RESOURCES");
                break;
            case Tables:
                f.WriteStr("TABLES");
                break;
            default:
                throw new InvalidOperationException($"Variante de FlushType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
