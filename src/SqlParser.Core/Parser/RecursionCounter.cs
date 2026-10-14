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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (mod recursion).
// Modificado: tradução de Rust para C#.

using System.Runtime.CompilerServices;

namespace SqlParser.Core;

/// <summary>
/// Tracks remaining recursion depth. This value is decremented on
/// each call to <see cref="TryDecrease"/>, when it reaches 0 an error will
/// be returned.
/// </summary>
/// <remarks>
/// O <c>Rc&lt;Cell&lt;usize&gt;&gt;</c> do Rust vira um campo mutável numa classe (convencoes.md §5).
/// <para>
/// A feature <c>recursive-protection</c> do Rust (crate <c>recursive</c>) faz a pilha crescer sob demanda.
/// Em .NET não é possível crescer a pilha, e um <see cref="StackOverflowException"/> derruba o processo.
/// Por isso <see cref="TryDecrease"/> também chama <see cref="RuntimeHelpers.EnsureSufficientExecutionStack"/>
/// e converte a falta de pilha em <see cref="ParserError.RecursionLimitExceeded"/> (DIV-005).
/// </para>
/// </remarks>
internal sealed class RecursionCounter
{
    private int _remainingDepth;

    /// <summary>Creates a <see cref="RecursionCounter"/> with the specified maximum depth</summary>
    public RecursionCounter(int remainingDepth) => _remainingDepth = remainingDepth;

    /// <summary>
    /// Decreases the remaining depth by 1.
    /// Returns <c>Err</c> if the remaining depth falls to 0.
    /// Returns a <see cref="DepthGuard"/> which will adds 1 to the remaining depth upon drop;
    /// </summary>
    /// <remarks>Uso: <c>using var _guard = _recursionCounter.TryDecrease();</c> (equivale ao <c>Drop</c> do Rust).</remarks>
    public DepthGuard TryDecrease()
    {
        var oldValue = _remainingDepth;
        // ran out of space
        if (oldValue == 0)
        {
            throw new ParserException(new ParserError.RecursionLimitExceeded());
        }

        // PORT-DEVIATION DIV-005: proteção de pilha do .NET no lugar do crate `recursive`.
        try
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
        }
        catch (InsufficientExecutionStackException)
        {
            throw new ParserException(new ParserError.RecursionLimitExceeded());
        }

        _remainingDepth = oldValue - 1;
        return new DepthGuard(this);
    }

    /// <summary>Guard that increases the remaining depth by 1 on drop</summary>
    public readonly struct DepthGuard : IDisposable
    {
        private readonly RecursionCounter? _counter;

        internal DepthGuard(RecursionCounter counter) => _counter = counter;

        /// <summary>Rust: <c>impl Drop for DepthGuard</c>.</summary>
        public void Dispose()
        {
            if (_counter is not null)
            {
                // saturating_add(1)
                _counter._remainingDepth = _counter._remainingDepth == int.MaxValue
                    ? int.MaxValue
                    : _counter._remainingDepth + 1;
            }
        }
    }
}
