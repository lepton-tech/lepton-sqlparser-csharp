// Código próprio da camada pública (sem origem no sqlparser-rs nem no SqlParser-cs).
// Ver docs/adr/0009-api-publica-fase-2.md, decisão 6.

namespace SqlParser;

/// <summary>
/// Classificação de uma instrução pelo comando que a inicia (<see cref="SqlAnalysisExtensions.GetStatementKind"/>).
/// </summary>
/// <remarks>
/// <para>
/// É uma classificação sintática e grossa, para os casos comuns (consulta, DML, DDL, permissões, transações e
/// blocos <c>BEGIN ... END</c>).
/// Para distinguir qualquer variante, use o tipo da instrução (<c>statement is Statement.CreateTable</c>).
/// </para>
/// <para>
/// <see cref="Create"/>, <see cref="Alter"/> e <see cref="Drop"/> valem para toda variante de <c>Statement</c> cujo
/// nome começa com <c>Create</c>, <c>Alter</c> ou <c>Drop</c> (os nomes são os das variantes do Rust), inclusive as
/// que o upstream acrescentar. Os demais membros correspondem a variantes fixas, listadas em cada um.
/// </para>
/// </remarks>
public enum SqlStatementKind
{
    /// <summary>
    /// Qualquer instrução que não se encaixa nos demais membros: <c>SHOW</c>, <c>SET</c>, <c>EXPLAIN</c>,
    /// <c>USE</c>, <c>CALL</c>, <c>COPY</c>, <c>COMMENT ON</c>, <c>RENAME TABLE</c> etc.
    /// </summary>
    Other = 0,

    /// <summary>
    /// <c>Statement.Query</c>: <c>SELECT</c>, <c>VALUES</c>, <c>TABLE</c>, <c>WITH ... SELECT</c> e operações de
    /// conjunto (<c>UNION</c>...). Inclui <c>SELECT ... INTO</c>, que no Rust também é uma consulta.
    /// </summary>
    Query,

    /// <summary>
    /// <c>Statement.Insert</c> (<c>INSERT</c>; no MySQL, também <c>REPLACE</c>), inclusive na forma
    /// <c>WITH ... INSERT</c>.
    /// </summary>
    Insert,

    /// <summary><c>Statement.Update</c>, inclusive na forma <c>WITH ... UPDATE</c>.</summary>
    Update,

    /// <summary><c>Statement.Delete</c>, inclusive na forma <c>WITH ... DELETE</c>.</summary>
    Delete,

    /// <summary><c>Statement.Merge</c>, inclusive na forma <c>WITH ... MERGE</c>.</summary>
    Merge,

    /// <summary>Variantes <c>Statement.Create*</c>: <c>CREATE TABLE</c>, <c>CREATE VIEW</c>, <c>CREATE INDEX</c>, <c>CREATE USER</c> etc.</summary>
    Create,

    /// <summary>Variantes <c>Statement.Alter*</c>: <c>ALTER TABLE</c>, <c>ALTER VIEW</c>, <c>ALTER SESSION</c> etc.</summary>
    Alter,

    /// <summary>Variantes <c>Statement.Drop*</c>: <c>DROP TABLE</c>, <c>DROP FUNCTION</c>, <c>DROP TRIGGER</c> etc.</summary>
    Drop,

    /// <summary><c>Statement.Truncate</c>.</summary>
    Truncate,

    /// <summary><c>Statement.Grant</c>.</summary>
    Grant,

    /// <summary><c>Statement.Revoke</c>.</summary>
    Revoke,

    /// <summary><c>Statement.Deny</c> (SQL Server).</summary>
    Deny,

    /// <summary>
    /// Controle de transação: <c>Statement.StartTransaction</c> sem bloco (<c>START TRANSACTION</c>,
    /// <c>BEGIN</c>, <c>BEGIN TRANSACTION</c>), <c>Statement.Commit</c> (<c>COMMIT</c>, <c>END</c>),
    /// <c>Statement.Rollback</c>, <c>Statement.Savepoint</c> e <c>Statement.ReleaseSavepoint</c>.
    /// </summary>
    Transaction,

    /// <summary>
    /// Bloco de instruções <c>BEGIN ... END</c> (SQL Server, BigQuery, Snowflake), inclusive com
    /// <c>EXCEPTION WHEN ...</c>. O Rust guarda o bloco como <c>Statement.StartTransaction</c> com
    /// <c>HasEndKeyword</c> verdadeiro e as instruções em <c>Statements</c>, mas ele não abre transação.
    /// </summary>
    Block,
}
