// Código próprio do port (sem origem direta no sqlparser-rs): equivalente de
// `core::iter::Peekable<core::str::Chars<'_>>`, o iterador de caracteres com "olhar adiante" que o
// tokenizer usa e que `Dialect::peek_nested_delimited_identifier_quotes` recebe por valor.
// Ver docs/port/convencoes.md, seções 5, 10 e 13.

namespace LeptonTech.SqlParser.Core;

/// <summary>
/// Equivalente de <c>Peekable&lt;Chars&lt;'_&gt;&gt;</c> do Rust: um cursor sobre uma <c>string</c> com
/// <see cref="Peek"/> e <see cref="Next"/>.
/// </summary>
/// <remarks>
/// <para>
/// É um <c>struct</c> mutável de propósito: copiar o valor (atribuição ou passagem por parâmetro)
/// equivale ao <c>.clone()</c> do iterador no Rust, e avançar a cópia não afeta o original.
/// </para>
/// <para>
/// Percorre unidades UTF-16. Um <c>char</c> do Rust fora do BMP chega aqui como duas unidades (par
/// substituto); ver divergencias.md (DIV-003).
/// </para>
/// </remarks>
public struct PeekableChars
{
    private readonly string? _source;
    private int _position;

    /// <summary>Rust: <c>s.chars().peekable()</c>.</summary>
    public PeekableChars(string source)
    {
        _source = source;
        _position = 0;
    }

    /// <summary>Rust: <c>peek()</c>: o próximo caractere, sem avançar; <c>null</c> no fim.</summary>
    public readonly char? Peek()
    {
        if (_source is null || _position >= _source.Length)
        {
            return null;
        }
        return _source[_position];
    }

    /// <summary>Rust: <c>next()</c>: o próximo caractere, avançando; <c>null</c> no fim.</summary>
    public char? Next()
    {
        if (_source is null || _position >= _source.Length)
        {
            return null;
        }
        return _source[_position++];
    }

    /// <summary>
    /// Rust: <c>Iterator::nth(n)</c>: consome <c>n + 1</c> caracteres e devolve o último deles
    /// (<c>nth(0)</c> equivale a <see cref="Next"/>); <c>null</c> se o texto acabar antes.
    /// </summary>
    public char? Nth(int n)
    {
        for (var i = 0; i < n; i++)
        {
            if (Next() is null)
            {
                return null;
            }
        }
        return Next();
    }

    /// <summary>
    /// Rust: <c>chars.skip_while(predicate).peekable()</c>: uma cópia do cursor posicionada no primeiro
    /// caractere que não satisfaz <paramref name="predicate"/>. Este valor não é alterado (no Rust,
    /// o iterador original é consumido pelo <c>skip_while</c>).
    /// </summary>
    public readonly PeekableChars SkipWhile(Func<char, bool> predicate)
    {
        var rest = this;
        while (rest.Peek() is { } ch && predicate(ch))
        {
            rest.Next();
        }
        return rest;
    }
}
