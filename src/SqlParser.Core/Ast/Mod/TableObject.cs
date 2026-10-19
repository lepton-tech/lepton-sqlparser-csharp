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

/// <summary>Represents the referenced table in an <c>INSERT INTO</c> statement</summary>
public abstract partial record TableObject : IDisplay
{
    /// <summary>
    /// Table specified by name.
    /// Example:
    /// <code>
    /// INSERT INTO my_table
    /// </code>
    /// </summary>
    public sealed record TableName([property: VisitWith("visit_relation")] ObjectName Value) : TableObject;

    /// <summary>
    /// Table specified as a function.
    /// Example:
    /// <code>
    /// INSERT INTO TABLE FUNCTION remote('localhost', default.simple_table)
    /// </code>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/table-functions">Clickhouse</see>
    /// </summary>
    public sealed record TableFunction(Function Value) : TableObject;

    /// <summary>
    /// Table specified through a sub-query
    /// Example:
    /// <code>
    /// INSERT INTO
    /// (SELECT employee_id, last_name, email, hire_date, job_id,  salary, commission_pct FROM employees)
    /// VALUES (207, 'Gregory', 'pgregory@example.com', sysdate, 'PU_CLERK', 1.2E3, NULL);
    /// </code>
    /// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/INSERT.html#GUID-903F8043-0254-4EE9-ACC1-CB8AC0AF3423__I2126242">Oracle</see>
    /// </summary>
    public sealed record TableQuery(Query Value) : TableObject;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case TableName tableName:
                f.Write($"{tableName.Value}");
                break;
            case TableFunction func:
                f.Write($"FUNCTION {func.Value}");
                break;
            case TableQuery tableQuery:
                f.Write($"({tableQuery.Value})");
                break;
            default:
                throw new InvalidOperationException($"Variante de TableObject não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
