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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/data_type.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>Timestamp and Time data types information about TimeZone formatting.</summary>
/// <remarks>
/// This is more related to a display information than real differences between each variant. To
/// guarantee compatibility with the input query we must maintain its exact information.
/// </remarks>
public abstract record TimezoneInfo : IDisplay
{
    /// <summary>No information about time zone, e.g. TIMESTAMP</summary>
    public sealed record None : TimezoneInfo;

    /// <summary>
    /// Temporal type 'WITH TIME ZONE', e.g. TIMESTAMP WITH TIME ZONE,
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#datetime-type">SQL Standard</see>,
    /// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/12.2/nlspg/datetime-data-types-and-time-zone-support.html#GUID-3F1C388E-C651-43D5-ADBC-1A49E5C2CA05">Oracle</see>
    /// </summary>
    public sealed record WithTimeZone : TimezoneInfo;

    /// <summary>
    /// Temporal type 'WITHOUT TIME ZONE', e.g. TIME WITHOUT TIME ZONE,
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#datetime-type">SQL Standard</see>,
    /// <see href="https://www.postgresql.org/docs/current/datatype-datetime.html">Postgresql</see>
    /// </summary>
    public sealed record WithoutTimeZone : TimezoneInfo;

    /// <summary>
    /// Postgresql specific <c>WITH TIME ZONE</c> formatting, for both TIME and TIMESTAMP, e.g. TIMETZ,
    /// <see href="https://www.postgresql.org/docs/current/datatype-datetime.html">Postgresql</see>
    /// </summary>
    public sealed record Tz : TimezoneInfo;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case None:
                f.Write($"");
                break;
            case WithTimeZone:
                f.Write($" WITH TIME ZONE");
                break;
            case WithoutTimeZone:
                f.Write($" WITHOUT TIME ZONE");
                break;
            case Tz:
                // TZ is the only one that is displayed BEFORE the precision, so the datatype display
                // must be aware of that. Check <https://www.postgresql.org/docs/14/datatype-datetime.html>
                // for more information
                f.Write($"TZ");
                break;
            default:
                throw new InvalidOperationException($"Variante de TimezoneInfo não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
