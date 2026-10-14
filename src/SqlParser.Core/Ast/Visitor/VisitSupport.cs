// Código próprio do port: apoio ao código gerado do visitor (Ast/Visitor/Generated), sem equivalente no Rust.
// O Rust altera os nós no lugar (`&mut`); aqui a AST é imutável e a visita mutável reconstrói só os pais
// do que mudou. Estes métodos dizem se um valor "mudou" (outra instância/outra memória) e despacham a
// visita de valores de tipo genérico. Ver docs/adr/0007-visitor.md.

namespace SqlParser.Core.Ast;

/// <summary>Apoio ao visitor gerado.</summary>
internal static class VisitSupport
{
    /// <summary>A sequência é a mesma (mesma memória): a visita mutável não trocou nenhum elemento.</summary>
    internal static bool IsSame<T>(Sequence<T> a, Sequence<T> b) => a.IsSameStorage(b);

    /// <summary>Como <see cref="IsSame{T}(Sequence{T}, Sequence{T})"/>, para <c>Option&lt;Vec&lt;T&gt;&gt;</c>.</summary>
    internal static bool IsSame<T>(Sequence<T>? a, Sequence<T>? b) =>
        a.HasValue ? b.HasValue && a.GetValueOrDefault().IsSameStorage(b.GetValueOrDefault()) : !b.HasValue;

    /// <summary>
    /// Mesma instância (tipos-referência) ou mesma memória (<see cref="Sequence{T}"/> em contexto genérico).
    /// A igualdade estrutural dos records não serve: <c>Ident</c> ignora o <c>Span</c>, por exemplo.
    /// </summary>
    internal static bool IsSame<T>(T a, T b)
    {
        if (typeof(T).IsValueType)
        {
            if (a is ISequenceStorage sa && b is ISequenceStorage sb)
            {
                return ReferenceEquals(sa.Storage, sb.Storage);
            }
            return EqualityComparer<T>.Default.Equals(a, b);
        }
        return ReferenceEquals(a, b);
    }

    /// <summary>
    /// Visita mutável de um valor de tipo genérico (<c>Parens&lt;T&gt;</c>, <c>OneOrManyWithParens&lt;T&gt;</c>,
    /// <c>WrappedCollection&lt;T&gt;</c>, <c>Sequence&lt;T&gt;</c>): equivale ao <c>T: VisitMut</c> do Rust,
    /// decidido em tempo de execução. Valores que não implementam <see cref="IVisitMut{TSelf}"/> são folhas.
    /// </summary>
    internal static ControlFlow<TBreak> VisitMutGeneric<T, TBreak>(ref T value, VisitorMut<TBreak> visitor)
    {
        if (value is IVisitMut<T> visitable)
        {
            var flow = visitable.Visit(visitor, out var result);
            value = result;
            return flow;
        }
        return ControlFlow<TBreak>.Continue;
    }

    // #[cfg_attr(feature = "recursive-protection", recursive::recursive)] no `fn visit` gerado pelo derive:
    // o código gerado testa StackIsLow() na entrada e, se faltar pilha, continua a visita numa pilha nova
    // (StackGuard.Grow, outra thread). A closure fica nestes métodos, e não no código gerado, para não ser
    // alocada a cada nó.

    /// <summary>Sobra pouca pilha para continuar a visita (<see cref="StackGuard.IsStackLow"/>).</summary>
    internal static bool StackIsLow() => StackGuard.IsStackLow();

    /// <summary>Continua a visita de <paramref name="node"/> numa pilha nova.</summary>
    internal static ControlFlow<TBreak> VisitOnNewStack<TBreak>(IVisit node, Visitor<TBreak> visitor) =>
        StackGuard.Grow(() => node.Visit(visitor));

    /// <summary>Continua a visita mutável de <paramref name="node"/> numa pilha nova.</summary>
    internal static ControlFlow<TBreak> VisitMutOnNewStack<T, TBreak>(ref T node, VisitorMut<TBreak> visitor)
        where T : IVisitMut<T>
    {
        var copy = node;
        var flow = StackGuard.Grow(() => copy.Visit(visitor, out copy));
        node = copy;
        return flow;
    }
}
