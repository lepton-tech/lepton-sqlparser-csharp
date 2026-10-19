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
/// <code>
/// CREATE CONNECTOR [IF NOT EXISTS] connector_name
/// [TYPE datasource_type]
/// [URL datasource_url]
/// [COMMENT connector_comment]
/// [WITH DCPROPERTIES(property_name=property_value, ...)]
/// </code>
/// <see href="https://cwiki.apache.org/confluence/pages/viewpage.action?pageId=27362034#LanguageManualDDL-CreateDataConnectorCreateConnector">Hive</see>
/// </summary>
/// <param name="Name">The name of the connector to be created.</param>
/// <param name="IfNotExists">Whether <c>IF NOT EXISTS</c> was specified.</param>
/// <param name="ConnectorType">The type of the connector.</param>
/// <param name="Url">The URL of the connector.</param>
/// <param name="Comment">The comment for the connector.</param>
/// <param name="WithDcproperties">The DC properties for the connector.</param>
public sealed partial record CreateConnector(
    Ident Name,
    bool IfNotExists,
    string? ConnectorType,
    string? Url,
    CommentDef? Comment,
    Sequence<SqlOption>? WithDcproperties) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var ifNotExists = IfNotExists ? "IF NOT EXISTS " : "";
        f.Write($"CREATE CONNECTOR {ifNotExists}{Name}");

        if (ConnectorType is { } connectorType)
        {
            f.Write($" TYPE '{connectorType}'");
        }

        if (Url is { } url)
        {
            f.Write($" URL '{url}'");
        }

        if (Comment is { } comment)
        {
            f.Write($" COMMENT = '{comment}'");
        }

        if (WithDcproperties is { } withDcproperties)
        {
            f.Write($" WITH DCPROPERTIES({DisplayCommaSeparated(withDcproperties)})");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
