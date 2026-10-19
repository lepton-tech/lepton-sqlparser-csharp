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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/value.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Represents the date/time fields used by functions like <c>EXTRACT</c>.</summary>
/// <remarks>
/// Each variant corresponds to a supported date/time part (for example
/// <c>YEAR</c>, <c>MONTH</c>, <c>DAY</c>, etc.). The <c>Custom</c> variant allows arbitrary
/// identifiers (e.g. dialect-specific abbreviations).
/// </remarks>
public abstract partial record DateTimeField : IDisplay
{
    /// <summary><c>YEAR</c></summary>
    public sealed record Year : DateTimeField;

    /// <summary><c>YEARS</c> (plural form)</summary>
    public sealed record Years : DateTimeField;

    /// <summary><c>MONTH</c></summary>
    public sealed record Month : DateTimeField;

    /// <summary><c>MONTHS</c> (plural form)</summary>
    public sealed record Months : DateTimeField;

    /// <summary>
    /// <c>WEEK</c>, optionally followed by a weekday, e.g. <c>WEEK(MONDAY)</c>.
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/date_functions#extract">BigQuery</see>
    /// </summary>
    public sealed record Week(Ident? Value) : DateTimeField;

    /// <summary><c>WEEKS</c> (plural form)</summary>
    public sealed record Weeks : DateTimeField;

    /// <summary><c>DAY</c></summary>
    public sealed record Day : DateTimeField;

    /// <summary><c>DAYOFWEEK</c></summary>
    public sealed record DayOfWeek : DateTimeField;

    /// <summary><c>DAYOFYEAR</c></summary>
    public sealed record DayOfYear : DateTimeField;

    /// <summary><c>DAYS</c> (plural form)</summary>
    public sealed record Days : DateTimeField;

    /// <summary><c>DATE</c></summary>
    public sealed record Date : DateTimeField;

    /// <summary><c>DATETIME</c></summary>
    public sealed record Datetime : DateTimeField;

    /// <summary><c>HOUR</c></summary>
    public sealed record Hour : DateTimeField;

    /// <summary><c>HOURS</c> (plural form)</summary>
    public sealed record Hours : DateTimeField;

    /// <summary><c>MINUTE</c></summary>
    public sealed record Minute : DateTimeField;

    /// <summary><c>MINUTES</c> (plural form)</summary>
    public sealed record Minutes : DateTimeField;

    /// <summary><c>SECOND</c></summary>
    public sealed record Second : DateTimeField;

    /// <summary><c>SECONDS</c> (plural form)</summary>
    public sealed record Seconds : DateTimeField;

    /// <summary><c>CENTURY</c></summary>
    public sealed record Century : DateTimeField;

    /// <summary><c>DECADE</c></summary>
    public sealed record Decade : DateTimeField;

    /// <summary><c>DOW</c> (day of week short form)</summary>
    public sealed record Dow : DateTimeField;

    /// <summary><c>DOY</c> (day of year short form)</summary>
    public sealed record Doy : DateTimeField;

    /// <summary><c>EPOCH</c></summary>
    public sealed record Epoch : DateTimeField;

    /// <summary><c>ISODOW</c></summary>
    public sealed record Isodow : DateTimeField;

    /// <summary><c>ISOYEAR</c></summary>
    public sealed record Isoyear : DateTimeField;

    /// <summary><c>ISOWEEK</c></summary>
    public sealed record IsoWeek : DateTimeField;

    /// <summary><c>JULIAN</c></summary>
    public sealed record Julian : DateTimeField;

    /// <summary><c>MICROSECOND</c></summary>
    public sealed record Microsecond : DateTimeField;

    /// <summary><c>MICROSECONDS</c> (plural form)</summary>
    public sealed record Microseconds : DateTimeField;

    /// <summary><c>MILLENIUM</c> (alternate spelling)</summary>
    public sealed record Millenium : DateTimeField;

    /// <summary><c>MILLENNIUM</c> (alternate spelling)</summary>
    public sealed record Millennium : DateTimeField;

    /// <summary><c>MILLISECOND</c></summary>
    public sealed record Millisecond : DateTimeField;

    /// <summary><c>MILLISECONDS</c> (plural form)</summary>
    public sealed record Milliseconds : DateTimeField;

    /// <summary><c>NANOSECOND</c></summary>
    public sealed record Nanosecond : DateTimeField;

    /// <summary><c>NANOSECONDS</c> (plural form)</summary>
    public sealed record Nanoseconds : DateTimeField;

    /// <summary><c>QUARTER</c></summary>
    public sealed record Quarter : DateTimeField;

    /// <summary><c>TIME</c></summary>
    public sealed record Time : DateTimeField;

    /// <summary><c>TIMEZONE</c></summary>
    public sealed record Timezone : DateTimeField;

    /// <summary><c>TIMEZONE_ABBR</c></summary>
    public sealed record TimezoneAbbr : DateTimeField;

    /// <summary><c>TIMEZONE_HOUR</c></summary>
    public sealed record TimezoneHour : DateTimeField;

    /// <summary><c>TIMEZONE_MINUTE</c></summary>
    public sealed record TimezoneMinute : DateTimeField;

    /// <summary><c>TIMEZONE_REGION</c></summary>
    public sealed record TimezoneRegion : DateTimeField;

    /// <summary><c>NODATETIME</c> indicates no date/time part</summary>
    public sealed record NoDateTime : DateTimeField;

    /// <summary>Arbitrary abbreviation or custom date-time part.</summary>
    /// <remarks>
    /// <code>
    /// EXTRACT(q FROM CURRENT_TIMESTAMP)
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/functions-date-time#supported-date-and-time-parts">Snowflake</see>
    /// </remarks>
    public sealed record Custom(Ident Value) : DateTimeField;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Year:
                f.Write($"YEAR");
                break;
            case Years:
                f.Write($"YEARS");
                break;
            case Month:
                f.Write($"MONTH");
                break;
            case Months:
                f.Write($"MONTHS");
                break;
            case Week week:
                f.Write($"WEEK");
                if (week.Value is { } weekDay)
                {
                    f.Write($"({weekDay})");
                }
                break;
            case Weeks:
                f.Write($"WEEKS");
                break;
            case Day:
                f.Write($"DAY");
                break;
            case DayOfWeek:
                f.Write($"DAYOFWEEK");
                break;
            case DayOfYear:
                f.Write($"DAYOFYEAR");
                break;
            case Days:
                f.Write($"DAYS");
                break;
            case Date:
                f.Write($"DATE");
                break;
            case Datetime:
                f.Write($"DATETIME");
                break;
            case Hour:
                f.Write($"HOUR");
                break;
            case Hours:
                f.Write($"HOURS");
                break;
            case Minute:
                f.Write($"MINUTE");
                break;
            case Minutes:
                f.Write($"MINUTES");
                break;
            case Second:
                f.Write($"SECOND");
                break;
            case Seconds:
                f.Write($"SECONDS");
                break;
            case Century:
                f.Write($"CENTURY");
                break;
            case Decade:
                f.Write($"DECADE");
                break;
            case Dow:
                f.Write($"DOW");
                break;
            case Doy:
                f.Write($"DOY");
                break;
            case Epoch:
                f.Write($"EPOCH");
                break;
            case Isodow:
                f.Write($"ISODOW");
                break;
            case Isoyear:
                f.Write($"ISOYEAR");
                break;
            case IsoWeek:
                f.Write($"ISOWEEK");
                break;
            case Julian:
                f.Write($"JULIAN");
                break;
            case Microsecond:
                f.Write($"MICROSECOND");
                break;
            case Microseconds:
                f.Write($"MICROSECONDS");
                break;
            case Millenium:
                f.Write($"MILLENIUM");
                break;
            case Millennium:
                f.Write($"MILLENNIUM");
                break;
            case Millisecond:
                f.Write($"MILLISECOND");
                break;
            case Milliseconds:
                f.Write($"MILLISECONDS");
                break;
            case Nanosecond:
                f.Write($"NANOSECOND");
                break;
            case Nanoseconds:
                f.Write($"NANOSECONDS");
                break;
            case Quarter:
                f.Write($"QUARTER");
                break;
            case Time:
                f.Write($"TIME");
                break;
            case Timezone:
                f.Write($"TIMEZONE");
                break;
            case TimezoneAbbr:
                f.Write($"TIMEZONE_ABBR");
                break;
            case TimezoneHour:
                f.Write($"TIMEZONE_HOUR");
                break;
            case TimezoneMinute:
                f.Write($"TIMEZONE_MINUTE");
                break;
            case TimezoneRegion:
                f.Write($"TIMEZONE_REGION");
                break;
            case NoDateTime:
                f.Write($"NODATETIME");
                break;
            case Custom custom:
                f.Write($"{custom.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de DateTimeField não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
