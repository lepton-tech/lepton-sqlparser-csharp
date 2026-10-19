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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Fields for <see href="https://www.postgresql.org/docs/17/datatype-datetime.html">Postgres</see> <c>INTERVAL</c> type.
/// </summary>
public abstract partial record IntervalFields : IDisplay
{
    /// <summary><c>YEAR</c> field</summary>
    public sealed record Year : IntervalFields;

    /// <summary><c>MONTH</c> field</summary>
    public sealed record Month : IntervalFields;

    /// <summary><c>DAY</c> field</summary>
    public sealed record Day : IntervalFields;

    /// <summary><c>HOUR</c> field</summary>
    public sealed record Hour : IntervalFields;

    /// <summary><c>MINUTE</c> field</summary>
    public sealed record Minute : IntervalFields;

    /// <summary><c>SECOND</c> field</summary>
    public sealed record Second : IntervalFields;

    /// <summary><c>YEAR TO MONTH</c> field</summary>
    public sealed record YearToMonth : IntervalFields;

    /// <summary><c>DAY TO HOUR</c> field</summary>
    public sealed record DayToHour : IntervalFields;

    /// <summary><c>DAY TO MINUTE</c> field</summary>
    public sealed record DayToMinute : IntervalFields;

    /// <summary><c>DAY TO SECOND</c> field</summary>
    public sealed record DayToSecond : IntervalFields;

    /// <summary><c>HOUR TO MINUTE</c> field</summary>
    public sealed record HourToMinute : IntervalFields;

    /// <summary><c>HOUR TO SECOND</c> field</summary>
    public sealed record HourToSecond : IntervalFields;

    /// <summary><c>MINUTE TO SECOND</c> field</summary>
    public sealed record MinuteToSecond : IntervalFields;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Year:
                f.Write($"YEAR");
                break;
            case Month:
                f.Write($"MONTH");
                break;
            case Day:
                f.Write($"DAY");
                break;
            case Hour:
                f.Write($"HOUR");
                break;
            case Minute:
                f.Write($"MINUTE");
                break;
            case Second:
                f.Write($"SECOND");
                break;
            case YearToMonth:
                f.Write($"YEAR TO MONTH");
                break;
            case DayToHour:
                f.Write($"DAY TO HOUR");
                break;
            case DayToMinute:
                f.Write($"DAY TO MINUTE");
                break;
            case DayToSecond:
                f.Write($"DAY TO SECOND");
                break;
            case HourToMinute:
                f.Write($"HOUR TO MINUTE");
                break;
            case HourToSecond:
                f.Write($"HOUR TO SECOND");
                break;
            case MinuteToSecond:
                f.Write($"MINUTE TO SECOND");
                break;
            default:
                throw new InvalidOperationException($"Variante de IntervalFields não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
