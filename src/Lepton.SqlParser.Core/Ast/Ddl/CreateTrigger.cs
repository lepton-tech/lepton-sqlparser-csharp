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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// CREATE TRIGGER
/// <para>Examples:</para>
/// <code>
/// CREATE TRIGGER trigger_name
/// BEFORE INSERT ON table_name
/// FOR EACH ROW
/// EXECUTE FUNCTION trigger_function();
/// </code>
/// <para>Postgres: <see href="https://www.postgresql.org/docs/current/sql-createtrigger.html"/></para>
/// <para>SQL Server: <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-trigger-transact-sql"/></para>
/// </summary>
/// <param name="OrAlter">
/// True if this is a <c>CREATE OR ALTER TRIGGER</c> statement
/// <para><see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-trigger-transact-sql?view=sql-server-ver16#arguments">MsSql</see></para>
/// </param>
/// <param name="Temporary">
/// True if this is a temporary trigger.
/// <para>Examples:</para>
/// <code>
/// CREATE TEMP TRIGGER trigger_name
/// </code>
/// <para>or</para>
/// <code>
/// CREATE TEMPORARY TRIGGER trigger_name;
/// CREATE TEMP TRIGGER trigger_name;
/// </code>
/// <see href="https://sqlite.org/lang_createtrigger.html#temp_triggers_on_non_temp_tables">SQLite</see>
/// </param>
/// <param name="OrReplace">
/// The <c>OR REPLACE</c> clause is used to re-create the trigger if it already exists.
/// <para>Example:</para>
/// <code>
/// CREATE OR REPLACE TRIGGER trigger_name
/// AFTER INSERT ON table_name
/// FOR EACH ROW
/// EXECUTE FUNCTION trigger_function();
/// </code>
/// </param>
/// <param name="IsConstraint">The <c>CONSTRAINT</c> keyword is used to create a trigger as a constraint.</param>
/// <param name="Name">The name of the trigger to be created.</param>
/// <param name="Period">
/// Determines whether the function is called before, after, or instead of the event.
/// <para>Example of BEFORE:</para>
/// <code>
/// CREATE TRIGGER trigger_name
/// BEFORE INSERT ON table_name
/// FOR EACH ROW
/// EXECUTE FUNCTION trigger_function();
/// </code>
/// <para>Example of AFTER:</para>
/// <code>
/// CREATE TRIGGER trigger_name
/// AFTER INSERT ON table_name
/// FOR EACH ROW
/// EXECUTE FUNCTION trigger_function();
/// </code>
/// <para>Example of INSTEAD OF:</para>
/// <code>
/// CREATE TRIGGER trigger_name
/// INSTEAD OF INSERT ON table_name
/// FOR EACH ROW
/// EXECUTE FUNCTION trigger_function();
/// </code>
/// </param>
/// <param name="PeriodBeforeTable">
/// Whether the trigger period was specified before the target table name.
/// This does not refer to whether the period is BEFORE, AFTER, or INSTEAD OF,
/// but rather the position of the period clause in relation to the table name.
/// <code>
/// -- period_before_table == true: Postgres, MySQL, and standard SQL
/// CREATE TRIGGER t BEFORE INSERT ON table_name ...;
/// -- period_before_table == false: MSSQL
/// CREATE TRIGGER t ON table_name BEFORE INSERT ...;
/// </code>
/// </param>
/// <param name="Events">Multiple events can be specified using OR, such as <c>INSERT</c>, <c>UPDATE</c>, <c>DELETE</c>, or <c>TRUNCATE</c>.</param>
/// <param name="TableName">The table on which the trigger is to be created.</param>
/// <param name="ReferencedTableName">
/// The optional referenced table name that can be referenced via
/// the <c>FROM</c> keyword.
/// </param>
/// <param name="Referencing">This keyword immediately precedes the declaration of one or two relation names that provide access to the transition relations of the triggering statement.</param>
/// <param name="TriggerObject">
/// This specifies whether the trigger function should be fired once for
/// every row affected by the trigger event, or just once per SQL statement.
/// This is optional in some SQL dialects, such as SQLite, and if not specified, in
/// those cases, the implied default is <c>FOR EACH ROW</c>.
/// </param>
/// <param name="Condition">Triggering conditions</param>
/// <param name="ExecBody">Execute logic block</param>
/// <param name="StatementsAs">For MSSQL and dialects where statements are preceded by <c>AS</c></param>
/// <param name="Statements">For SQL dialects with statement(s) for a body</param>
/// <param name="Characteristics">The characteristic of the trigger, which include whether the trigger is <c>DEFERRABLE</c>, <c>INITIALLY DEFERRED</c>, or <c>INITIALLY IMMEDIATE</c>,</param>
public sealed partial record CreateTrigger(
    bool OrAlter,
    bool Temporary,
    bool OrReplace,
    bool IsConstraint,
    ObjectName Name,
    TriggerPeriod? Period,
    bool PeriodBeforeTable,
    Sequence<TriggerEvent> Events,
    ObjectName TableName,
    ObjectName? ReferencedTableName,
    Sequence<TriggerReferencing> Referencing,
    TriggerObjectKind? TriggerObject,
    Expr? Condition,
    TriggerExecBody? ExecBody,
    bool StatementsAs,
    ConditionalStatements? Statements,
    ConstraintCharacteristics? Characteristics) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var temporary = Temporary ? "TEMPORARY " : "";
        var orAlter = OrAlter ? "OR ALTER " : "";
        var orReplace = OrReplace ? "OR REPLACE " : "";
        var isConstraint = IsConstraint ? "CONSTRAINT " : "";
        f.Write($"CREATE {temporary}{orAlter}{orReplace}{isConstraint}TRIGGER {Name} ");

        if (PeriodBeforeTable)
        {
            if (Period is { } p)
            {
                f.Write($"{p} ");
            }
            if (!Events.IsEmpty)
            {
                f.Write($"{DisplaySeparated(Events, " OR ")} ");
            }
            f.Write($"ON {TableName}");
        }
        else
        {
            f.Write($"ON {TableName} ");
            if (Period is { } p)
            {
                f.Write($"{p}");
            }
            if (!Events.IsEmpty)
            {
                f.Write($" {DisplaySeparated(Events, ", ")}");
            }
        }

        if (ReferencedTableName is { } referencedTableName)
        {
            f.Write($" FROM {referencedTableName}");
        }

        if (Characteristics is { } characteristics)
        {
            f.Write($" {characteristics}");
        }

        if (!Referencing.IsEmpty)
        {
            f.Write($" REFERENCING {DisplaySeparated(Referencing, " ")}");
        }

        if (TriggerObject is { } triggerObject)
        {
            f.Write($" {triggerObject}");
        }
        if (Condition is { } condition)
        {
            f.Write($" WHEN {condition}");
        }
        if (ExecBody is { } execBody)
        {
            f.Write($" EXECUTE {execBody}");
        }
        if (Statements is { } statements)
        {
            if (StatementsAs)
            {
                f.Write($" AS");
            }
            f.Write($" {statements}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
