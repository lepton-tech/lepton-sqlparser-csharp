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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Objects on which privileges are granted in a GRANT statement.</summary>
public abstract partial record GrantObjects : IDisplay
{
    /// <summary>Grant privileges on <c>ALL SEQUENCES IN SCHEMA &lt;schema_name&gt; [, ...]</c></summary>
    /// <param name="Schemas">The target schema names.</param>
    public sealed record AllSequencesInSchema(Sequence<ObjectName> Schemas) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Schemas` (CS8866).
        /// <summary>The target schema names.</summary>
        public new Sequence<ObjectName> Schemas { get; init; } = Schemas;
    }

    /// <summary>Grant privileges on <c>ALL TABLES IN SCHEMA &lt;schema_name&gt; [, ...]</c></summary>
    /// <param name="Schemas">The target schema names.</param>
    public sealed record AllTablesInSchema(Sequence<ObjectName> Schemas) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Schemas` (CS8866).
        /// <summary>The target schema names.</summary>
        public new Sequence<ObjectName> Schemas { get; init; } = Schemas;
    }

    /// <summary>Grant privileges on <c>ALL VIEWS IN SCHEMA &lt;schema_name&gt; [, ...]</c></summary>
    /// <param name="Schemas">The target schema names.</param>
    public sealed record AllViewsInSchema(Sequence<ObjectName> Schemas) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Schemas` (CS8866).
        /// <summary>The target schema names.</summary>
        public new Sequence<ObjectName> Schemas { get; init; } = Schemas;
    }

    /// <summary>Grant privileges on <c>ALL MATERIALIZED VIEWS IN SCHEMA &lt;schema_name&gt; [, ...]</c></summary>
    /// <param name="Schemas">The target schema names.</param>
    public sealed record AllMaterializedViewsInSchema(Sequence<ObjectName> Schemas) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Schemas` (CS8866).
        /// <summary>The target schema names.</summary>
        public new Sequence<ObjectName> Schemas { get; init; } = Schemas;
    }

    /// <summary>Grant privileges on <c>ALL EXTERNAL TABLES IN SCHEMA &lt;schema_name&gt; [, ...]</c></summary>
    /// <param name="Schemas">The target schema names.</param>
    public sealed record AllExternalTablesInSchema(Sequence<ObjectName> Schemas) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Schemas` (CS8866).
        /// <summary>The target schema names.</summary>
        public new Sequence<ObjectName> Schemas { get; init; } = Schemas;
    }

    /// <summary>Grant privileges on <c>ALL FUNCTIONS IN SCHEMA &lt;schema_name&gt; [, ...]</c></summary>
    /// <param name="Schemas">The target schema names.</param>
    public sealed record AllFunctionsInSchema(Sequence<ObjectName> Schemas) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Schemas` (CS8866).
        /// <summary>The target schema names.</summary>
        public new Sequence<ObjectName> Schemas { get; init; } = Schemas;
    }

    /// <summary>Grant privileges on <c>FUTURE SCHEMAS IN DATABASE &lt;database_name&gt; [, ...]</c></summary>
    /// <param name="Databases">The target database names.</param>
    public sealed record FutureSchemasInDatabase(Sequence<ObjectName> Databases) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Databases` (CS8866).
        /// <summary>The target database names.</summary>
        public new Sequence<ObjectName> Databases { get; init; } = Databases;
    }

    /// <summary>Grant privileges on <c>FUTURE TABLES IN SCHEMA &lt;schema_name&gt; [, ...]</c></summary>
    /// <param name="Schemas">The target schema names.</param>
    public sealed record FutureTablesInSchema(Sequence<ObjectName> Schemas) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Schemas` (CS8866).
        /// <summary>The target schema names.</summary>
        public new Sequence<ObjectName> Schemas { get; init; } = Schemas;
    }

    /// <summary>Grant privileges on <c>FUTURE VIEWS IN SCHEMA &lt;schema_name&gt; [, ...]</c></summary>
    /// <param name="Schemas">The target schema names.</param>
    public sealed record FutureViewsInSchema(Sequence<ObjectName> Schemas) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Schemas` (CS8866).
        /// <summary>The target schema names.</summary>
        public new Sequence<ObjectName> Schemas { get; init; } = Schemas;
    }

    /// <summary>Grant privileges on <c>FUTURE EXTERNAL TABLES IN SCHEMA &lt;schema_name&gt; [, ...]</c></summary>
    /// <param name="Schemas">The target schema names.</param>
    public sealed record FutureExternalTablesInSchema(Sequence<ObjectName> Schemas) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Schemas` (CS8866).
        /// <summary>The target schema names.</summary>
        public new Sequence<ObjectName> Schemas { get; init; } = Schemas;
    }

    /// <summary>Grant privileges on <c>FUTURE MATERIALIZED VIEWS IN SCHEMA &lt;schema_name&gt; [, ...]</c></summary>
    /// <param name="Schemas">The target schema names.</param>
    public sealed record FutureMaterializedViewsInSchema(Sequence<ObjectName> Schemas) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Schemas` (CS8866).
        /// <summary>The target schema names.</summary>
        public new Sequence<ObjectName> Schemas { get; init; } = Schemas;
    }

    /// <summary>Grant privileges on <c>FUTURE SEQUENCES IN SCHEMA &lt;schema_name&gt; [, ...]</c></summary>
    /// <param name="Schemas">The target schema names.</param>
    public sealed record FutureSequencesInSchema(Sequence<ObjectName> Schemas) : GrantObjects
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `GrantObjects.Schemas` (CS8866).
        /// <summary>The target schema names.</summary>
        public new Sequence<ObjectName> Schemas { get; init; } = Schemas;
    }

    /// <summary>Grant privileges on specific databases</summary>
    public sealed record Databases(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on specific schemas</summary>
    public sealed record Schemas(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on specific sequences</summary>
    public sealed record Sequences(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on specific tables</summary>
    public sealed record Tables(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on specific views</summary>
    public sealed record Views(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on specific warehouses</summary>
    public sealed record Warehouses(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on specific integrations</summary>
    public sealed record Integrations(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on resource monitors</summary>
    public sealed record ResourceMonitors(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on users</summary>
    public sealed record Users(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on compute pools</summary>
    public sealed record ComputePools(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on connections</summary>
    public sealed record Connections(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on failover groups</summary>
    public sealed record FailoverGroup(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on replication group</summary>
    public sealed record ReplicationGroup(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>Grant privileges on external volumes</summary>
    public sealed record ExternalVolumes(Sequence<ObjectName> Value) : GrantObjects;

    /// <summary>
    /// Grant privileges on a procedure. In dialects that
    /// support overloading, the argument types must be specified.
    /// <para>For example: <c>GRANT USAGE ON PROCEDURE foo(varchar) TO ROLE role1</c></para>
    /// </summary>
    /// <param name="Name">The procedure name.</param>
    /// <param name="ArgTypes">Optional argument types for overloaded procedures.</param>
    public sealed record Procedure(ObjectName Name, Sequence<DataType> ArgTypes) : GrantObjects;

    /// <summary>
    /// Grant privileges on a function. In dialects that
    /// support overloading, the argument types must be specified.
    /// <para>For example: <c>GRANT USAGE ON FUNCTION foo(varchar) TO ROLE role1</c></para>
    /// </summary>
    /// <param name="Name">The function name.</param>
    /// <param name="ArgTypes">Optional argument types for overloaded functions.</param>
    public sealed record Function(ObjectName Name, Sequence<DataType> ArgTypes) : GrantObjects;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Sequences sequences:
                f.Write($"SEQUENCE {DisplayCommaSeparated(sequences.Value)}");
                break;
            case Databases databases:
                f.Write($"DATABASE {DisplayCommaSeparated(databases.Value)}");
                break;
            case Schemas schemas:
                f.Write($"SCHEMA {DisplayCommaSeparated(schemas.Value)}");
                break;
            case Tables tables:
                f.Write($"{DisplayCommaSeparated(tables.Value)}");
                break;
            case Views views:
                f.Write($"VIEW {DisplayCommaSeparated(views.Value)}");
                break;
            case Warehouses warehouses:
                f.Write($"WAREHOUSE {DisplayCommaSeparated(warehouses.Value)}");
                break;
            case Integrations integrations:
                f.Write($"INTEGRATION {DisplayCommaSeparated(integrations.Value)}");
                break;
            case AllSequencesInSchema allSequencesInSchema:
                f.Write($"ALL SEQUENCES IN SCHEMA {DisplayCommaSeparated(allSequencesInSchema.Schemas)}");
                break;
            case AllTablesInSchema allTablesInSchema:
                f.Write($"ALL TABLES IN SCHEMA {DisplayCommaSeparated(allTablesInSchema.Schemas)}");
                break;
            case AllExternalTablesInSchema allExternalTablesInSchema:
                f.Write($"ALL EXTERNAL TABLES IN SCHEMA {DisplayCommaSeparated(allExternalTablesInSchema.Schemas)}");
                break;
            case AllViewsInSchema allViewsInSchema:
                f.Write($"ALL VIEWS IN SCHEMA {DisplayCommaSeparated(allViewsInSchema.Schemas)}");
                break;
            case AllMaterializedViewsInSchema allMaterializedViewsInSchema:
                f.Write($"ALL MATERIALIZED VIEWS IN SCHEMA {DisplayCommaSeparated(allMaterializedViewsInSchema.Schemas)}");
                break;
            case AllFunctionsInSchema allFunctionsInSchema:
                f.Write($"ALL FUNCTIONS IN SCHEMA {DisplayCommaSeparated(allFunctionsInSchema.Schemas)}");
                break;
            case FutureSchemasInDatabase futureSchemasInDatabase:
                f.Write($"FUTURE SCHEMAS IN DATABASE {DisplayCommaSeparated(futureSchemasInDatabase.Databases)}");
                break;
            case FutureTablesInSchema futureTablesInSchema:
                f.Write($"FUTURE TABLES IN SCHEMA {DisplayCommaSeparated(futureTablesInSchema.Schemas)}");
                break;
            case FutureExternalTablesInSchema futureExternalTablesInSchema:
                f.Write($"FUTURE EXTERNAL TABLES IN SCHEMA {DisplayCommaSeparated(futureExternalTablesInSchema.Schemas)}");
                break;
            case FutureViewsInSchema futureViewsInSchema:
                f.Write($"FUTURE VIEWS IN SCHEMA {DisplayCommaSeparated(futureViewsInSchema.Schemas)}");
                break;
            case FutureMaterializedViewsInSchema futureMaterializedViewsInSchema:
                f.Write($"FUTURE MATERIALIZED VIEWS IN SCHEMA {DisplayCommaSeparated(futureMaterializedViewsInSchema.Schemas)}");
                break;
            case FutureSequencesInSchema futureSequencesInSchema:
                f.Write($"FUTURE SEQUENCES IN SCHEMA {DisplayCommaSeparated(futureSequencesInSchema.Schemas)}");
                break;
            case ResourceMonitors resourceMonitors:
                f.Write($"RESOURCE MONITOR {DisplayCommaSeparated(resourceMonitors.Value)}");
                break;
            case Users users:
                f.Write($"USER {DisplayCommaSeparated(users.Value)}");
                break;
            case ComputePools computePools:
                f.Write($"COMPUTE POOL {DisplayCommaSeparated(computePools.Value)}");
                break;
            case Connections connections:
                f.Write($"CONNECTION {DisplayCommaSeparated(connections.Value)}");
                break;
            case FailoverGroup failoverGroup:
                f.Write($"FAILOVER GROUP {DisplayCommaSeparated(failoverGroup.Value)}");
                break;
            case ReplicationGroup replicationGroup:
                f.Write($"REPLICATION GROUP {DisplayCommaSeparated(replicationGroup.Value)}");
                break;
            case ExternalVolumes externalVolumes:
                f.Write($"EXTERNAL VOLUME {DisplayCommaSeparated(externalVolumes.Value)}");
                break;
            case Procedure procedure:
                f.Write($"PROCEDURE {procedure.Name}");
                if (!procedure.ArgTypes.IsEmpty)
                {
                    f.Write($"({DisplayCommaSeparated(procedure.ArgTypes)})");
                }
                break;
            case Function function:
                f.Write($"FUNCTION {function.Name}");
                if (!function.ArgTypes.IsEmpty)
                {
                    f.Write($"({DisplayCommaSeparated(function.ArgTypes)})");
                }
                break;
            default:
                throw new InvalidOperationException($"Variante de GrantObjects não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
