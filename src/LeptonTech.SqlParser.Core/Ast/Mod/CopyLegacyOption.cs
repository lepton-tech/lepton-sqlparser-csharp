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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>An option in <c>COPY</c> statement before PostgreSQL version 9.0.</summary>
/// <remarks>
/// <see href="https://www.postgresql.org/docs/8.4/sql-copy.html">PostgreSQL</see>
/// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_COPY-alphabetical-parm-list.html">Redshift</see>
/// </remarks>
public abstract partial record CopyLegacyOption : IDisplay
{
    /// <summary>ACCEPTANYDATE</summary>
    public sealed record AcceptAnyDate : CopyLegacyOption;

    /// <summary>ACCEPTINVCHARS</summary>
    public sealed record AcceptInvChars(string? Value) : CopyLegacyOption;

    /// <summary>ADDQUOTES</summary>
    public sealed record AddQuotes : CopyLegacyOption;

    /// <summary>ALLOWOVERWRITE</summary>
    public sealed record AllowOverwrite : CopyLegacyOption;

    /// <summary>BINARY</summary>
    public sealed record Binary : CopyLegacyOption;

    /// <summary>BLANKSASNULL</summary>
    public sealed record BlankAsNull : CopyLegacyOption;

    /// <summary>BZIP2</summary>
    public sealed record Bzip2 : CopyLegacyOption;

    /// <summary>CLEANPATH</summary>
    public sealed record CleanPath : CopyLegacyOption;

    /// <summary>COMPUPDATE [ PRESET | { ON | TRUE } | { OFF | FALSE } ]</summary>
    /// <param name="Preset">Whether the COMPUPDATE PRESET option was used.</param>
    /// <param name="Enabled">Optional enabled flag for COMPUPDATE.</param>
    public sealed record CompUpdate(bool Preset, bool? Enabled) : CopyLegacyOption;

    /// <summary>CSV ...</summary>
    public sealed record Csv(Sequence<CopyLegacyCsvOption> Value) : CopyLegacyOption;

    /// <summary>DATEFORMAT [ AS ] {'dateformat_string' | 'auto' }</summary>
    public sealed record DateFormat(string? Value) : CopyLegacyOption;

    /// <summary>DELIMITER [ AS ] 'delimiter_character'</summary>
    public sealed record Delimiter(char Value) : CopyLegacyOption;

    /// <summary>EMPTYASNULL</summary>
    public sealed record EmptyAsNull : CopyLegacyOption;

    /// <summary><c>ENCRYPTED [ AUTO ]</c></summary>
    /// <param name="Auto">Whether <c>AUTO</c> was specified for encryption.</param>
    public sealed record Encrypted(bool Auto) : CopyLegacyOption;

    /// <summary>ESCAPE</summary>
    public sealed record Escape : CopyLegacyOption;

    /// <summary>EXTENSION 'extension-name'</summary>
    public sealed record Extension(string Value) : CopyLegacyOption;

    /// <summary>FIXEDWIDTH [ AS ] 'fixedwidth-spec'</summary>
    public sealed record FixedWidth(string Value) : CopyLegacyOption;

    /// <summary>GZIP</summary>
    public sealed record Gzip : CopyLegacyOption;

    /// <summary>HEADER</summary>
    public sealed record Header : CopyLegacyOption;

    /// <summary>IAM_ROLE { DEFAULT | 'arn:aws:iam::123456789:role/role1' }</summary>
    public sealed record IamRole(IamRoleKind Value) : CopyLegacyOption;

    /// <summary>IGNOREHEADER [ AS ] number_rows</summary>
    public sealed record IgnoreHeader(ulong Value) : CopyLegacyOption;

    /// <summary>JSON [ AS ] 'json_option'</summary>
    public sealed record Json(string? Value) : CopyLegacyOption;

    /// <summary>MANIFEST [ VERBOSE ]</summary>
    /// <param name="Verbose">Whether the MANIFEST is verbose.</param>
    public sealed record Manifest(bool Verbose) : CopyLegacyOption;

    /// <summary>MAXFILESIZE [ AS ] max-size [ MB | GB ]</summary>
    public sealed record MaxFileSize(FileSize Value) : CopyLegacyOption;

    /// <summary><c>NULL [ AS ] 'null_string'</c></summary>
    public sealed record Null(string Value) : CopyLegacyOption;

    /// <summary><c>PARALLEL [ { ON | TRUE } | { OFF | FALSE } ]</c></summary>
    public sealed record Parallel(bool? Value) : CopyLegacyOption;

    /// <summary>PARQUET</summary>
    public sealed record Parquet : CopyLegacyOption;

    /// <summary>PARTITION BY ( column_name [, ... ] ) [ INCLUDE ]</summary>
    public sealed record PartitionBy(UnloadPartitionBy Value) : CopyLegacyOption;

    /// <summary>REGION [ AS ] 'aws-region' }</summary>
    public sealed record Region(string Value) : CopyLegacyOption;

    /// <summary>REMOVEQUOTES</summary>
    public sealed record RemoveQuotes : CopyLegacyOption;

    /// <summary>ROWGROUPSIZE [ AS ] size [ MB | GB ]</summary>
    public sealed record RowGroupSize(FileSize Value) : CopyLegacyOption;

    /// <summary>STATUPDATE [ { ON | TRUE } | { OFF | FALSE } ]</summary>
    public sealed record StatUpdate(bool? Value) : CopyLegacyOption;

    /// <summary>TIMEFORMAT [ AS ] {'timeformat_string' | 'auto' | 'epochsecs' | 'epochmillisecs' }</summary>
    public sealed record TimeFormat(string? Value) : CopyLegacyOption;

    /// <summary>TRUNCATECOLUMNS</summary>
    public sealed record TruncateColumns : CopyLegacyOption;

    /// <summary>ZSTD</summary>
    public sealed record Zstd : CopyLegacyOption;

    /// <summary>
    /// Redshift <c>CREDENTIALS 'auth-args'</c>
    /// <see href="https://docs.aws.amazon.com/redshift/latest/dg/copy-parameters-authorization.html"/>
    /// </summary>
    public sealed record Credentials(string Value) : CopyLegacyOption;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case AcceptAnyDate:
                f.Write($"ACCEPTANYDATE");
                break;
            case AcceptInvChars acceptInvChars:
            {
                f.Write($"ACCEPTINVCHARS");
                if (acceptInvChars.Value is { } ch)
                {
                    f.Write($" '{Ast.Value.EscapeSingleQuoteString(ch)}'");
                }
                break;
            }
            case AddQuotes:
                f.Write($"ADDQUOTES");
                break;
            case AllowOverwrite:
                f.Write($"ALLOWOVERWRITE");
                break;
            case Binary:
                f.Write($"BINARY");
                break;
            case BlankAsNull:
                f.Write($"BLANKSASNULL");
                break;
            case Bzip2:
                f.Write($"BZIP2");
                break;
            case CleanPath:
                f.Write($"CLEANPATH");
                break;
            case CompUpdate compUpdate:
            {
                f.Write($"COMPUPDATE");
                if (compUpdate.Preset)
                {
                    f.Write($" PRESET");
                }
                else if (compUpdate.Enabled is { } enabled)
                {
                    var text = enabled switch
                    {
                        true => " TRUE",
                        false => " FALSE",
                    };
                    f.Write($"{text}");
                }
                break;
            }
            case Csv opts:
            {
                f.Write($"CSV");
                if (!opts.Value.IsEmpty)
                {
                    f.Write($" {DisplaySeparated(opts.Value, " ")}");
                }
                break;
            }
            case DateFormat dateFormat:
            {
                f.Write($"DATEFORMAT");
                if (dateFormat.Value is { } fmt)
                {
                    f.Write($" '{Ast.Value.EscapeSingleQuoteString(fmt)}'");
                }
                break;
            }
            case Delimiter delimiter:
                f.Write($"DELIMITER '{delimiter.Value}'");
                break;
            case EmptyAsNull:
                f.Write($"EMPTYASNULL");
                break;
            case Encrypted encrypted:
                f.Write($"ENCRYPTED{(encrypted.Auto ? " AUTO" : "")}");
                break;
            case Escape:
                f.Write($"ESCAPE");
                break;
            case Extension ext:
                f.Write($"EXTENSION '{Ast.Value.EscapeSingleQuoteString(ext.Value)}'");
                break;
            case FixedWidth spec:
                f.Write($"FIXEDWIDTH '{Ast.Value.EscapeSingleQuoteString(spec.Value)}'");
                break;
            case Gzip:
                f.Write($"GZIP");
                break;
            case Header:
                f.Write($"HEADER");
                break;
            case IamRole role:
                f.Write($"IAM_ROLE {role.Value}");
                break;
            case IgnoreHeader numRows:
                f.Write($"IGNOREHEADER {numRows.Value}");
                break;
            case Json json:
            {
                f.Write($"JSON");
                if (json.Value is { } opt)
                {
                    f.Write($" AS '{Ast.Value.EscapeSingleQuoteString(opt)}'");
                }
                break;
            }
            case Manifest manifest:
                f.Write($"MANIFEST{(manifest.Verbose ? " VERBOSE" : "")}");
                break;
            case MaxFileSize fileSize:
                f.Write($"MAXFILESIZE {fileSize.Value}");
                break;
            case Null nullString:
                f.Write($"NULL '{Ast.Value.EscapeSingleQuoteString(nullString.Value)}'");
                break;
            case Parallel parallel:
            {
                var text = parallel.Value switch
                {
                    true => " TRUE",
                    false => " FALSE",
                    _ => "",
                };
                f.Write($"PARALLEL{text}");
                break;
            }
            case Parquet:
                f.Write($"PARQUET");
                break;
            case PartitionBy p:
                f.Write($"{p.Value}");
                break;
            case Region region:
                f.Write($"REGION '{Ast.Value.EscapeSingleQuoteString(region.Value)}'");
                break;
            case RemoveQuotes:
                f.Write($"REMOVEQUOTES");
                break;
            case RowGroupSize fileSize:
                f.Write($"ROWGROUPSIZE {fileSize.Value}");
                break;
            case StatUpdate statUpdate:
            {
                var text = statUpdate.Value switch
                {
                    true => " TRUE",
                    false => " FALSE",
                    _ => "",
                };
                f.Write($"STATUPDATE{text}");
                break;
            }
            case TimeFormat timeFormat:
            {
                f.Write($"TIMEFORMAT");
                if (timeFormat.Value is { } fmt)
                {
                    f.Write($" '{Ast.Value.EscapeSingleQuoteString(fmt)}'");
                }
                break;
            }
            case TruncateColumns:
                f.Write($"TRUNCATECOLUMNS");
                break;
            case Zstd:
                f.Write($"ZSTD");
                break;
            case Credentials s:
                f.Write($"CREDENTIALS '{Ast.Value.EscapeSingleQuoteString(s.Value)}'");
                break;
            default:
                throw new InvalidOperationException($"Variante de CopyLegacyOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
