namespace LeptonTech.SqlParser.Core.Tests;

// Testes próprios do port (não existem no Rust): validam a geração de Keywords.g.cs.
public class KeywordsTests
{
    [Fact]
    public void Lookup_encontra_keyword_e_ignora_nao_keyword()
    {
        Assert.Equal(Keyword.SELECT, Keywords.Lookup("SELECT"));
        Assert.Equal(Keyword.END_EXEC, Keywords.Lookup("END-EXEC"));
        Assert.Equal(Keyword.NoKeyword, Keywords.Lookup("NAO_E_KEYWORD"));
    }

    [Fact]
    public void Listas_geradas_sao_consistentes()
    {
        Assert.Equal(Keywords.AllKeywords.Count, Keywords.AllKeywordsIndex.Count);
        for (var i = 1; i < Keywords.AllKeywords.Count; i++)
        {
            Assert.True(string.CompareOrdinal(Keywords.AllKeywords[i - 1], Keywords.AllKeywords[i]) < 0);
        }
        Assert.Equal("END-EXEC", Keyword.END_EXEC.ToSql());
        Assert.Equal("NoKeyword", Keyword.NoKeyword.ToSql());
        Assert.Contains(Keyword.FROM, Keywords.ReservedForTableAlias);
    }
}
