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

/// <summary>
/// Represents an INTERVAL expression, roughly in the following format:
/// <c>INTERVAL '&lt;value&gt;' [ &lt;leading_field&gt; [ (&lt;leading_precision&gt;) ] ]
/// [ TO &lt;last_field&gt; [ (&lt;fractional_seconds_precision&gt;) ] ]</c>,
/// e.g. <c>INTERVAL '123:45.67' MINUTE(3) TO SECOND(2)</c>.
/// </summary>
/// <remarks>
/// The parser does not validate the <c>&lt;value&gt;</c>, nor does it ensure
/// that the <c>&lt;leading_field&gt;</c> units &gt;= the units in <c>&lt;last_field&gt;</c>,
/// so the user will have to reject intervals like <c>HOUR TO YEAR</c>.
/// </remarks>
/// <param name="Value">The interval value expression (commonly a string literal).</param>
/// <param name="LeadingField">Optional leading time unit (e.g., <c>HOUR</c>, <c>MINUTE</c>).</param>
/// <param name="LeadingPrecision">Optional leading precision for the leading field.</param>
/// <param name="LastField">Optional trailing time unit for a range (e.g., <c>SECOND</c>).</param>
/// <param name="FractionalSecondsPrecision">
/// The fractional seconds precision, when specified.
/// <para>See SQL <c>SECOND(n)</c> or <c>SECOND(m, n)</c> forms.</para>
/// </param>
public sealed partial record Interval(
    Expr Value,
    DateTimeField? LeadingField,
    ulong? LeadingPrecision,
    DateTimeField? LastField,
    ulong? FractionalSecondsPrecision) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var value = Value;
        switch ((LeadingField, LeadingPrecision, FractionalSecondsPrecision))
        {
            case (DateTimeField.Second, { } leadingPrecision, { } fractionalSecondsPrecision):
            {
                // When the leading field is SECOND, the parser guarantees that
                // the last field is None.
                if (!(LastField is null))
                {
                    throw new InvalidOperationException("assertion failed: self.last_field.is_none()");
                }
                f.Write($"INTERVAL {value} SECOND ({leadingPrecision}, {fractionalSecondsPrecision})");
                break;
            }
            default:
            {
                f.Write($"INTERVAL {value}");
                if (LeadingField is { } leadingField)
                {
                    f.Write($" {leadingField}");
                }
                if (LeadingPrecision is { } leadingPrecision)
                {
                    f.Write($" ({leadingPrecision})");
                }
                if (LastField is { } lastField)
                {
                    f.Write($" TO {lastField}");
                }
                if (FractionalSecondsPrecision is { } fractionalSecondsPrecision)
                {
                    f.Write($" ({fractionalSecondsPrecision})");
                }
                break;
            }
        }
    }

    public override string ToString() => Formatter.Format(this);
}
