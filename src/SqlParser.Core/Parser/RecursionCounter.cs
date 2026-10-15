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

namespace SqlParser.Core;

/// <summary>
/// Tracks remaining recursion depth. This value is decremented on
/// each call to <see cref="TryDecrease"/>, when it reaches 0 an error will
/// be returned.
/// </summary>
/// <remarks>
/// <para>
/// Note: Uses an <c>alloc::rc::Rc</c> and <c>core::cell::Cell</c> in order to satisfy the Rust
/// borrow checker so the automatic <see cref="DepthGuard"/> decrement a
/// reference to the counter.
/// </para>
/// <para>
/// Note: when "recursive-protection" feature is enabled, this crate uses additional stack overflow protection
/// for some of its recursive methods. See <c>recursive::recursive</c> for more information.
/// </para>
/// </remarks>
// O `Rc<Cell<usize>>` do Rust vira um campo mutável numa classe (convencoes.md §5).
// Como no Rust, o erro ParserError.RecursionLimitExceeded só ocorre quando o contador chega a 0.
// A proteção de pilha fica nas funções que o Rust marca com `#[recursive::recursive]`, via
// StackGuard (DIV-005).
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
    // Uso: `using var _guard = _recursionCounter.TryDecrease();` (equivale ao `Drop` do Rust).
    public DepthGuard TryDecrease()
    {
        var oldValue = _remainingDepth;
        // ran out of space
        if (oldValue == 0)
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

        // Rust: `impl Drop for DepthGuard`.
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
