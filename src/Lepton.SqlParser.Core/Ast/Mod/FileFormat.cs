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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>External table's available file format</summary>
public abstract partial record FileFormat : IDisplay
{
    /// <summary>Text file format.</summary>
    public sealed record TEXTFILE : FileFormat;

    /// <summary>Sequence file format.</summary>
    public sealed record SEQUENCEFILE : FileFormat;

    /// <summary>ORC file format.</summary>
    public sealed record ORC : FileFormat;

    /// <summary>Parquet file format.</summary>
    public sealed record PARQUET : FileFormat;

    /// <summary>Avro file format.</summary>
    public sealed record AVRO : FileFormat;

    /// <summary>RCFile format.</summary>
    public sealed record RCFILE : FileFormat;

    /// <summary>JSON file format.</summary>
    public sealed record JSONFILE : FileFormat;

    public void Fmt(Formatter f)
    {
        f.WriteStr(this switch
        {
            TEXTFILE => "TEXTFILE",
            SEQUENCEFILE => "SEQUENCEFILE",
            ORC => "ORC",
            PARQUET => "PARQUET",
            AVRO => "AVRO",
            RCFILE => "RCFILE",
            JSONFILE => "JSONFILE",
            _ => throw new InvalidOperationException($"Variante de FileFormat não tratada: {GetType().Name}"),
        });
    }

    public sealed override string ToString() => Formatter.Format(this);
}
