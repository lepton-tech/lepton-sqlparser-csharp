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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/tokenizer.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core;

/// <summary>An error reported by the tokenizer, with a human-readable <c>message</c> and a <c>location</c>.</summary>
/// <param name="Message">A descriptive error message.</param>
/// <param name="Location">The <c>Location</c> where the error was detected.</param>
public sealed record TokenizerError(string Message, Location Location) : IDisplay
{
    /// <inheritdoc/>
    public void Fmt(Formatter f)
    {
        // write!(f, "{}{}", self.message, self.location,)
        f.WriteStr(Message);
        f.Write(Location); // "{}" de Location: string vazia se line == 0
    }

    // Rust: `impl fmt::Display for TokenizerError`.
    public override string ToString() => Formatter.Format(this);
}
