// Testes próprios do port (não existem no Rust): no Rust, `&[T]` devolvido por um dialeto é imutável. No C#,
// as listas devolvidas como IReadOnlyList<T> não podem ser arrays, senão um cast para T[] alteraria o estado
// global de todas as instâncias (revisão R3, achado 1; docs/port/convencoes.md §13, caso 10).

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

public class DialectImmutabilityTests
{
    [Fact]
    public void Listas_dos_dialetos_nao_podem_ser_alteradas_por_cast()
    {
        var grantees = new MsSqlDialect().GetReservedGranteesTypes;
        Assert.False(grantees is GranteesType[]);
        Assert.Throws<NotSupportedException>(() => ((IList<GranteesType>)grantees)[0] = GranteesType.User);
        Assert.Equal(new[] { GranteesType.Public }, new MsSqlDialect().GetReservedGranteesTypes);

        foreach (var dialect in new Dialect[] { new OracleDialect(), new SnowflakeDialect() })
        {
            var keywords = dialect.GetReservedKeywordsForSelectItemOperator;
            Assert.False(keywords is Keyword[]);
            Assert.Throws<NotSupportedException>(() => ((IList<Keyword>)keywords)[0] = Keyword.SELECT);
            Assert.Equal(new[] { Keyword.CONNECT_BY_ROOT }, dialect.GetReservedKeywordsForSelectItemOperator);
        }
    }

    [Fact]
    public void Listas_publicas_de_keywords_nao_podem_ser_alteradas_por_cast()
    {
        Assert.False(Keywords.AllKeywords is string[]);
        Assert.False(Keywords.AllKeywordsIndex is Keyword[]);
        Assert.False(Keywords.ReservedForTableAlias is Keyword[]);
        Assert.False(Keywords.ReservedForColumnAlias is Keyword[]);
        Assert.False(Keywords.ReservedForTableFactor is Keyword[]);
        Assert.False(Keywords.ReservedForIdentifier is Keyword[]);

        var first = Keywords.ReservedForTableAlias[0];
        Assert.Throws<NotSupportedException>(() => ((IList<Keyword>)Keywords.ReservedForTableAlias)[0] = Keyword.NoKeyword);
        Assert.Equal(first, Keywords.ReservedForTableAlias[0]);
    }
}
