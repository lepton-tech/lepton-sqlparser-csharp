// Teste do pacote LeptonTech.SqlParser como um consumidor externo o usaria (tools/pack/Test-Package.ps1).
// Compilado em net48 e net8.0, em C# 7.3 de propósito: o pacote precisa servir a projetos antigos do .NET Framework.
// Sai com código 0 se tudo conferir e 1 se algo divergir.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using LeptonTech.SqlParser;
using LeptonTech.SqlParser.Core;
using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace PackageSmoke
{
    internal static class Program
    {
        private static int failures;

        private static int Main()
        {
            Console.WriteLine("Runtime: " + RuntimeInformation.FrameworkDescription);
            PrintAssembly(typeof(SqlQueryParser).Assembly);   // LeptonTech.SqlParser.dll (API pública)
            PrintAssembly(typeof(GenericDialect).Assembly);   // LeptonTech.SqlParser.Core.dll (port fiel)

            var parser = new SqlQueryParser();

            // Três dialetos: por instância e por nome.
            Check("PostgreSQL", parser.Parse(
                    "SELECT a::INT, b FROM public.t1 JOIN t2 ON t1.id = t2.id WHERE b ILIKE '%x%'",
                    new PostgreSqlDialect()),
                "SELECT a::INT, b FROM public.t1 JOIN t2 ON t1.id = t2.id WHERE b ILIKE '%x%'",
                "public.t1", "t2");
            Check("MsSql", parser.Parse(
                    "SELECT TOP 5 [nome] FROM [dbo].[clientes] WHERE id IN (SELECT cliente_id FROM pedidos)",
                    "mssql"),
                "SELECT TOP 5 [nome] FROM [dbo].[clientes] WHERE id IN (SELECT cliente_id FROM pedidos)",
                "[dbo].[clientes]", "pedidos");
            Check("MySQL", parser.Parse(
                    "INSERT INTO `log` (id, msg) SELECT id, msg FROM `staging` WHERE id > 10",
                    new MySqlDialect()),
                "INSERT INTO `log` (id, msg) SELECT id, msg FROM `staging` WHERE id > 10",
                "`log`", "`staging`");

            // Erro com linha e coluna, sem exceção.
            Sequence<Statement> ignored;
            SqlParseException error;
            if (parser.TryParse("SELECT *\nFROM t WHERE x = = 1", out ignored, out error))
            {
                Fail("TryParse aceitou SQL inválido");
            }
            else
            {
                Console.WriteLine("Erro esperado: linha " + error.Line + ", coluna " + error.Column + ": " + error.Message);
                Expect("posição do erro", "2:18", error.Line + ":" + error.Column);
            }

            // Reescrita (visitor gerado do Core, atravessando as duas DLLs).
            var rewritten = parser.Parse("SELECT * FROM clientes WHERE id = 1")
                .RewriteRelations(name => name.ToSql() == "clientes"
                    ? ObjectName.From(Sequence.Of(new Ident("crm"), new Ident("clientes")))
                    : name);
            Expect("reescrita", "SELECT * FROM crm.clientes WHERE id = 1", rewritten.ToSql());

            Console.WriteLine();
            Console.WriteLine(failures == 0 ? "OK: todas as verificações passaram." : "FALHA: " + failures + " verificação(ões).");
            return failures == 0 ? 0 : 1;
        }

        private static void Check(string dialect, Sequence<Statement> statements, string expectedSql, params string[] expectedTables)
        {
            Console.WriteLine();
            Console.WriteLine("== " + dialect);
            Console.WriteLine(statements.ToSql(pretty: true));
            var tables = statements.GetReferencedTables().Select(t => t.ToSql()).ToList();
            Console.WriteLine("Tabelas: " + string.Join(", ", tables));
            Expect(dialect + ": ToSql()", expectedSql, statements.ToSql());
            Expect(dialect + ": tabelas", string.Join(", ", expectedTables), string.Join(", ", tables));
        }

        private static void Expect(string what, string expected, string actual)
        {
            if (expected != actual)
            {
                Fail(what + ": esperado <" + expected + ">, obtido <" + actual + ">");
            }
        }

        private static void Fail(string message)
        {
            failures++;
            Console.WriteLine("FALHA: " + message);
        }

        private static void PrintAssembly(Assembly assembly)
        {
            var info = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .Cast<AssemblyInformationalVersionAttribute>()
                .Select(a => a.InformationalVersion)
                .FirstOrDefault();
            Console.WriteLine(assembly.GetName().Name + " " + info + " (" + assembly.Location + ")");
        }
    }
}
