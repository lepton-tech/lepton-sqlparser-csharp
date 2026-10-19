// Código próprio do port (sem origem direta no sqlparser-rs): equivalente da feature padrão
// `recursive-protection` do sqlparser-rs, isto é, do atributo `#[recursive::recursive]` do crate
// `recursive` 0.1.1, que envolve o corpo da função em `stacker::maybe_grow(..)`.
// Ver docs/port/convencoes.md §13 (caso 123), docs/port/divergencias.md (DIV-005) e docs/port/onda-d/D1.md.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Lepton.SqlParser.Core;

/// <summary>
/// Crescimento de pilha sob demanda, como o <c>#[recursive::recursive]</c> do Rust: quando resta pouca
/// pilha, o resto da chamada roda numa pilha nova (uma <see cref="Thread"/> com pilha de
/// <see cref="StackAllocationSize"/> bytes), e a thread atual espera o resultado.
/// </summary>
/// <remarks>
/// <para>
/// No Rust, <c>#[recursive::recursive]</c> transforma o corpo em
/// <c>stacker::maybe_grow(get_minimum_stack_size(), get_stack_allocation_size(), || corpo)</c>: se a pilha
/// restante for menor que a zona vermelha (128 KiB por padrão), o corpo roda num segmento de pilha novo
/// (2 MiB por padrão). Em C#, a forma equivalente, no início da função marcada no Rust, é:
/// </para>
/// <code>
/// // Rust: #[cfg_attr(feature = "recursive-protection", recursive::recursive)]
/// if (StackGuard.IsStackLow())
/// {
///     return StackGuard.Grow(this, precedence, static (parser, p) => parser.ParseSubexpr(p));
/// }
/// </code>
/// <para>
/// A chamada repetida roda na pilha nova, passa pela verificação (agora com folga) e executa o corpo
/// normalmente; o resultado, ou a exceção, volta para a thread original. O lambda é <c>static</c> e recebe o
/// estado por parâmetro, para não alocar nada no caminho comum (pilha com folga).
/// </para>
/// <para>
/// Diferenças em relação ao <c>stacker</c>: a pilha nova é de outra thread. Estado ligado à thread
/// (<c>[ThreadStatic]</c>, <c>ThreadLocal</c>) não é visto pela continuação; a cultura atual
/// (<see cref="CultureInfo.CurrentCulture"/> e <see cref="CultureInfo.CurrentUICulture"/>) é copiada, e o
/// <c>ExecutionContext</c> (inclusive <c>AsyncLocal</c>) flui pelo <see cref="Thread.Start()"/>. Numa thread STA
/// do .NET Framework, o <see cref="Thread.Join()"/> bombeia mensagens COM enquanto espera.
/// </para>
/// </remarks>
internal static class StackGuard
{
    /// <summary>
    /// Tamanho, em bytes, de cada pilha nova. Rust: <c>recursive::get_stack_allocation_size()</c>, cujo
    /// padrão é 2 MiB.
    /// </summary>
    internal const int StackAllocationSize = 2 * 1024 * 1024;

    /// <summary>
    /// Indica se a pilha restante está abaixo da zona vermelha. Rust: o teste
    /// <c>stacker::remaining_stack() &lt; recursive::get_minimum_stack_size()</c> feito por <c>maybe_grow</c>.
    /// </summary>
    /// <remarks>
    /// O <c>netstandard2.0</c> não expõe a pilha restante. A zona vermelha é a de
    /// <see cref="RuntimeHelpers.EnsureSufficientExecutionStack"/>: no .NET Core/.NET 5+, ela lança quando restam
    /// menos de 128 KiB em 64 bits (64 KiB em 32 bits), o mesmo padrão do crate <c>recursive</c>
    /// (<c>get_minimum_stack_size()</c>, 128 KiB); no .NET Framework, quando metade da pilha da thread já foi
    /// usada (mais conservador). Sem folga, a chamada é interna e barata; a exceção só ocorre quando a pilha
    /// está baixa, isto é, uma vez por troca de pilha.
    /// </remarks>
    internal static bool IsStackLow()
    {
        try
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            return false;
        }
        catch (InsufficientExecutionStackException)
        {
            return true;
        }
    }

    /// <summary>
    /// Rust: <c>stacker::grow(get_stack_allocation_size(), callback)</c>. Roda <paramref name="callback"/> numa
    /// pilha nova e devolve o resultado. Uma exceção de <paramref name="callback"/> é relançada na thread
    /// atual, com o rastro de pilha original preservado.
    /// </summary>
    internal static TResult Grow<TResult>(Func<TResult> callback)
    {
        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        TResult result = default!;
        ExceptionDispatchInfo? error = null;
        var thread = new Thread(
            () =>
            {
                var current = Thread.CurrentThread;
                current.CurrentCulture = culture;
                current.CurrentUICulture = uiCulture;
                try
                {
                    result = callback();
                }
                catch (Exception e)
                {
                    error = ExceptionDispatchInfo.Capture(e);
                }
            },
            StackAllocationSize);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();
        error?.Throw();
        return result;
    }

    /// <summary><see cref="Grow{TResult}(Func{TResult})"/> para um método sem argumentos.</summary>
    internal static TResult Grow<TState, TResult>(TState state, Func<TState, TResult> callback) =>
        Grow(() => callback(state));

    /// <summary><see cref="Grow{TResult}(Func{TResult})"/> para um método com um argumento.</summary>
    internal static TResult Grow<TState, TArg, TResult>(TState state, TArg arg, Func<TState, TArg, TResult> callback) =>
        Grow(() => callback(state, arg));

    /// <summary><see cref="Grow{TResult}(Func{TResult})"/> para um método <c>void</c> com um argumento.</summary>
    internal static void Grow<TState, TArg>(TState state, TArg arg, Action<TState, TArg> callback) =>
        Grow(() =>
        {
            callback(state, arg);
            return true;
        });

    /// <summary>
    /// Relança <paramref name="error"/> preservando o rastro de pilha original, como <c>throw;</c>, mas
    /// <b>fora</b> do bloco <c>catch</c>. Uso: <c>throw StackGuard.Rethrow(error);</c> (o retorno nunca é usado;
    /// existe só para o compilador saber que o fluxo termina ali).
    /// </summary>
    /// <remarks>
    /// No .NET, o corpo de um <c>catch</c> roda em cima dos quadros do despachante de exceções, abaixo do ponto
    /// do <c>throw</c> original. Um <c>throw;</c> dentro do <c>catch</c> inicia um novo despacho sobre essa pilha,
    /// e uma cadeia de <c>catch { ...; throw; }</c> acumula cerca de 8–10 KB de pilha por nível até alguém
    /// tratar a exceção. Com o erro propagado por dezenas de níveis de recursão do parser, isso estoura a pilha
    /// (onda-d/D1.md). Relançar depois que o <c>catch</c> termina libera essa pilha antes do novo despacho,
    /// como a propagação de <c>Err</c> pelo <c>?</c> do Rust, que não consome pilha extra.
    /// </remarks>
    internal static Exception Rethrow(Exception error)
    {
        ExceptionDispatchInfo.Capture(error).Throw();
        return error;
    }
}
