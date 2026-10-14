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
/// Specifies the data processed by a window function, e.g.
/// <c>RANGE UNBOUNDED PRECEDING</c> or <c>ROWS BETWEEN 5 PRECEDING AND CURRENT ROW</c>.
/// </summary>
/// <remarks>
/// Note: The parser does not validate the specified bounds; the caller should
/// reject invalid bounds like <c>ROWS UNBOUNDED FOLLOWING</c> before execution.
/// </remarks>
/// <param name="Units">Units for the frame (e.g. <c>ROWS</c>, <c>RANGE</c>, <c>GROUPS</c>).</param>
/// <param name="StartBound">The start bound of the window frame.</param>
/// <param name="EndBound">
/// The right bound of the <c>BETWEEN .. AND</c> clause. The end bound of <c>None</c>
/// indicates the shorthand form (e.g. <c>ROWS 1 PRECEDING</c>), which must
/// behave the same as <c>end_bound = WindowFrameBound::CurrentRow</c>.
/// </param>
public sealed record WindowFrame(WindowFrameUnits Units, WindowFrameBound StartBound, WindowFrameBound? EndBound)
{
    // TBD: EXCLUDE

    /// <summary>
    /// Returns default value for window frame
    /// <para>See <see href="https://www.sqlite.org/windowfunctions.html#frame_specifications">this page</see> for more details.</para>
    /// </summary>
    /// <remarks>Rust: <c>impl Default for WindowFrame</c> (convencoes.md §13, caso 42).</remarks>
    public static WindowFrame Default() =>
        new WindowFrame(
            Units: new WindowFrameUnits.Range(),
            StartBound: new WindowFrameBound.Preceding(null),
            EndBound: null);
}
