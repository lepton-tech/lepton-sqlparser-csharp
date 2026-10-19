// Código próprio do port: delegate para closures Rust que recebem `&mut T`.
// Ver convencoes.md §13 (caso 126).

namespace Lepton.SqlParser.Core;

/// <summary>
/// Rust: <c>FnMut(&amp;mut T) -&gt; R</c>. Como <see cref="Func{T, TResult}"/>, mas o argumento é passado por
/// <c>ref</c>: a função pode trocar o valor no lugar (<c>*x = ...</c> no Rust).
/// </summary>
/// <example><c>RefFunc&lt;ObjectName, ControlFlow&lt;ValueTuple&gt;&gt; f = (ref ObjectName table) =&gt; { table = ...; return ControlFlow&lt;ValueTuple&gt;.Continue; };</c></example>
public delegate TResult RefFunc<T, out TResult>(ref T arg);
