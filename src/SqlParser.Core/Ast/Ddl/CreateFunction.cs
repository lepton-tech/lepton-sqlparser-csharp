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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>CREATE FUNCTION statement</summary>
/// <param name="OrAlter">
/// True if this is a <c>CREATE OR ALTER FUNCTION</c> statement
/// <para><see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql?view=sql-server-ver16#or-alter">MsSql</see></para>
/// </param>
/// <param name="OrReplace">True if this is a <c>CREATE OR REPLACE FUNCTION</c> statement</param>
/// <param name="Temporary">True if this is a <c>CREATE TEMPORARY FUNCTION</c> statement</param>
/// <param name="IfNotExists">True if this is a <c>CREATE IF NOT EXISTS FUNCTION</c> statement</param>
/// <param name="Name">Name of the function to be created.</param>
/// <param name="Args">List of arguments for the function.</param>
/// <param name="ReturnType">The return type of the function.</param>
/// <param name="FunctionBody">
/// The expression that defines the function.
/// <para>Examples:</para>
/// <code>
/// AS ((SELECT 1))
/// AS "console.log();"
/// </code>
/// </param>
/// <param name="Behavior">
/// Behavior attribute for the function
/// <para>IMMUTABLE | STABLE | VOLATILE</para>
/// <para><see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see></para>
/// </param>
/// <param name="CalledOnNull">
/// CALLED ON NULL INPUT | RETURNS NULL ON NULL INPUT | STRICT
/// <para><see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see></para>
/// </param>
/// <param name="Parallel">
/// PARALLEL { UNSAFE | RESTRICTED | SAFE }
/// <para><see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see></para>
/// </param>
/// <param name="Security">
/// SECURITY { DEFINER | INVOKER }
/// <para><see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see></para>
/// </param>
/// <param name="SetParams">
/// SET configuration_parameter clauses
/// <para><see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see></para>
/// </param>
/// <param name="Using">USING ... (Hive only)</param>
/// <param name="Language">
/// Language used in a UDF definition.
/// <para>Example:</para>
/// <code>
/// CREATE FUNCTION foo() LANGUAGE js AS "console.log();"
/// </code>
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_a_javascript_udf">BigQuery</see>
/// </param>
/// <param name="DeterminismSpecifier">
/// Determinism keyword used for non-sql UDF definitions.
/// <para><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#syntax_11">BigQuery</see></para>
/// </param>
/// <param name="Options">
/// List of options for creating the function.
/// <para><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#syntax_11">BigQuery</see></para>
/// </param>
/// <param name="RemoteConnection">
/// Connection resource for a remote function.
/// <para>Example:</para>
/// <code>
/// CREATE FUNCTION foo()
/// RETURNS FLOAT64
/// REMOTE WITH CONNECTION us.myconnection
/// </code>
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_a_remote_function">BigQuery</see>
/// </param>
public sealed partial record CreateFunction(
    bool OrAlter,
    bool OrReplace,
    bool Temporary,
    bool IfNotExists,
    ObjectName Name,
    Sequence<OperateFunctionArg>? Args,
    FunctionReturnType? ReturnType,
    CreateFunctionBody? FunctionBody,
    FunctionBehavior? Behavior,
    FunctionCalledOnNull? CalledOnNull,
    FunctionParallel? Parallel,
    FunctionSecurity? Security,
    Sequence<FunctionDefinitionSetParam> SetParams,
    CreateFunctionUsing? Using,
    Ident? Language,
    FunctionDeterminismSpecifier? DeterminismSpecifier,
    Sequence<SqlOption>? Options,
    ObjectName? RemoteConnection) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var temp = Temporary ? "TEMPORARY " : "";
        var orAlter = OrAlter ? "OR ALTER " : "";
        var orReplace = OrReplace ? "OR REPLACE " : "";
        var ifNotExists = IfNotExists ? "IF NOT EXISTS " : "";
        f.Write($"CREATE {orAlter}{orReplace}{temp}FUNCTION {ifNotExists}{Name}");
        if (Args is { } args)
        {
            f.Write($"({DisplayCommaSeparated(args)})");
        }
        if (ReturnType is { } returnType)
        {
            f.Write($" RETURNS {returnType}");
        }
        if (DeterminismSpecifier is { } determinismSpecifier)
        {
            f.Write($" {determinismSpecifier}");
        }
        if (Language is { } language)
        {
            f.Write($" LANGUAGE {language}");
        }
        if (Behavior is { } behavior)
        {
            f.Write($" {behavior}");
        }
        if (CalledOnNull is { } calledOnNull)
        {
            f.Write($" {calledOnNull}");
        }
        if (Parallel is { } parallel)
        {
            f.Write($" {parallel}");
        }
        if (Security is { } security)
        {
            f.Write($" {security}");
        }
        foreach (var setParam in SetParams)
        {
            f.Write($" {setParam}");
        }
        if (RemoteConnection is { } remoteConnection)
        {
            f.Write($" REMOTE WITH CONNECTION {remoteConnection}");
        }
        if (FunctionBody is CreateFunctionBody.AsBeforeOptions { Body: var body, LinkSymbol: var linkSymbol })
        {
            f.Write($" AS {body}");
            if (linkSymbol is { } linkSymbolValue)
            {
                f.Write($", {linkSymbolValue}");
            }
        }
        if (FunctionBody is CreateFunctionBody.Return { Value: var returnBody })
        {
            f.Write($" RETURN {returnBody}");
        }
        if (FunctionBody is CreateFunctionBody.AsReturnExpr { Value: var asReturnExprBody })
        {
            f.Write($" AS RETURN {asReturnExprBody}");
        }
        if (FunctionBody is CreateFunctionBody.AsReturnSelect { Value: var asReturnSelectBody })
        {
            f.Write($" AS RETURN {asReturnSelectBody}");
        }
        if (Using is { } @using)
        {
            f.Write($" {@using}");
        }
        if (Options is { } options)
        {
            f.Write($" OPTIONS({DisplayCommaSeparated(options)})");
        }
        if (FunctionBody is CreateFunctionBody.AsAfterOptions { Value: var asAfterOptionsBody })
        {
            f.Write($" AS {asAfterOptionsBody}");
        }
        if (FunctionBody is CreateFunctionBody.AsBeginEnd { Value: var bes })
        {
            f.Write($" AS {bes}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
