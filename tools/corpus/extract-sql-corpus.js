// Extract SQL string literals passed to sqlparser-rs test helpers.
// Usage: node extract_sql_corpus.js <repo_root> [out.jsonl]
const fs = require('fs'), path = require('path');
const root = process.argv[2];
const outFile = process.argv[3];
const testsDir = path.join(root, 'tests');
const files = fs.readdirSync(testsDir).filter(f => f.endsWith('.rs')).map(f => path.join(testsDir, f));

const HELPERS = ['verified_stmt', 'verified_query', 'verified_expr', 'verified_only_select',
  'verified_query_with_canonical', 'verified_only_select_with_canonical', 'one_statement_parses_to',
  'statements_parse_to', 'expr_parses_to', 'parse_sql_statements', 'run_parser_method'];
const TWO_ARG = new Set(['verified_query_with_canonical', 'verified_only_select_with_canonical',
  'one_statement_parses_to', 'statements_parse_to', 'expr_parses_to']);

// ---- Rust lexing helpers -------------------------------------------------
function unescapeRust(s) {
  let out = '';
  for (let i = 0; i < s.length; i++) {
    const c = s[i];
    if (c !== '\\') { out += c; continue; }
    const n = s[++i];
    if (n === 'n') out += '\n'; else if (n === 't') out += '\t'; else if (n === 'r') out += '\r';
    else if (n === '0') out += '\0'; else if (n === '\\') out += '\\'; else if (n === '"') out += '"';
    else if (n === "'") out += "'";
    else if (n === 'x') { out += String.fromCharCode(parseInt(s.substr(i + 1, 2), 16)); i += 2; }
    else if (n === 'u') { const e = s.indexOf('}', i); out += String.fromCodePoint(parseInt(s.slice(i + 2, e), 16)); i = e; }
    else if (n === '\n' || n === '\r') { // line continuation: skip newline + leading whitespace
      while (i + 1 < s.length && /\s/.test(s[i + 1])) i++;
    } else out += '\\' + n;
  }
  return out;
}

// Try to read a string literal (normal or raw) at position p (after skipping whitespace).
function readStringLit(src, p) {
  while (/\s/.test(src[p])) p++;
  if (src[p] === '&') { p++; while (/\s/.test(src[p])) p++; }
  let m;
  if (src[p] === 'r' && (m = /^r(#*)"/.exec(src.slice(p, p + 20)))) {
    const hashes = m[1]; const start = p + m[0].length; const end = src.indexOf('"' + hashes, start);
    return { value: src.slice(start, end), end: end + 1 + hashes.length };
  }
  if (src[p] === '"') {
    let i = p + 1; let raw = '';
    while (src[i] !== '"') { if (src[i] === '\\') { raw += src[i] + src[i + 1]; i += 2; } else raw += src[i++]; }
    return { value: unescapeRust(raw), end: i + 1 };
  }
  if (src.startsWith('concat!(', p)) {
    let q = p + 8; let acc = '';
    for (;;) {
      const s = readStringLit(src, q); if (!s) return null; acc += s.value; q = s.end;
      while (/\s/.test(src[q])) q++;
      if (src[q] === ',') { q++; while (/\s/.test(src[q])) q++; if (src[q] === ')') return { value: acc, end: q + 1 }; continue; }
      if (src[q] === ')') return { value: acc, end: q + 1 };
      return null;
    }
  }
  return null;
}

// Mask comments and string contents so paren matching is not confused.
function maskSource(src) {
  const a = src.split('');
  let i = 0;
  while (i < a.length) {
    if (src.startsWith('//', i)) { while (i < a.length && src[i] !== '\n') a[i++] = ' '; continue; }
    if (src.startsWith('/*', i)) { const e = src.indexOf('*/', i + 2); for (let k = i; k < e + 2; k++) if (a[k] !== '\n') a[k] = ' '; i = e + 2; continue; }
    let m;
    if (src[i] === 'r' && !/[A-Za-z0-9_]/.test(src[i - 1] || '') && (m = /^r(#*)"/.exec(src.slice(i, i + 20)))) {
      const end = src.indexOf('"' + m[1], i + m[0].length);
      for (let k = i + m[0].length; k < end; k++) if (a[k] !== '\n') a[k] = 'x';
      i = end + 1 + m[1].length; continue;
    }
    if (src[i] === '"') { let k = i + 1; while (src[k] !== '"') { if (src[k] === '\\') { a[k] = 'x'; a[k + 1] = 'x'; k += 2; } else { if (a[k] !== '\n') a[k] = 'x'; k++; } } i = k + 1; continue; }
    if (src[i] === "'" ) { // char literal vs lifetime
      const m2 = /^'(\\.|[^\\'])'/.exec(src.slice(i, i + 12)) || /^'\\u\{[0-9a-fA-F]+\}'/.exec(src.slice(i, i + 14));
      if (m2) { for (let k = i + 1; k < i + m2[0].length - 1; k++) a[k] = 'x'; i += m2[0].length; continue; }
    }
    i++;
  }
  return a.join('');
}

function matchClose(masked, openIdx) { // openIdx at '('
  let depth = 0;
  for (let i = openIdx; i < masked.length; i++) {
    const c = masked[i];
    if (c === '(' || c === '[' || c === '{') depth++;
    else if (c === ')' || c === ']' || c === '}') { depth--; if (depth === 0) return i; }
  }
  return -1;
}
function matchOpenBack(masked, closeIdx) {
  let depth = 0;
  for (let i = closeIdx; i >= 0; i--) {
    const c = masked[i];
    if (c === ')' || c === ']' || c === '}') depth++;
    else if (c === '(' || c === '[' || c === '{') { depth--; if (depth === 0) return i; }
  }
  return -1;
}
// Walk back over a receiver expression ending right before '.' at dotIdx.
function receiverStart(masked, dotIdx) {
  let i = dotIdx - 1;
  for (;;) {
    while (i >= 0 && /\s/.test(masked[i])) i--;
    if (masked[i] === ')' || masked[i] === ']') { i = matchOpenBack(masked, i) - 1; }
    while (i >= 0 && /[A-Za-z0-9_!]/.test(masked[i])) i--;
    if (masked[i] === ':' && masked[i - 1] === ':') { i -= 2; continue; }
    // allow chained method receivers: foo().bar
    let j = i; while (j >= 0 && /\s/.test(masked[j])) j--;
    if (masked[j] === '.' ) { i = j - 1; continue; }
    return i + 1;
  }
}

// ---- dialect selector resolution -----------------------------------------
const DIALECTS = ['Generic', 'PostgreSql', 'MsSql', 'Ansi', 'Snowflake', 'Hive', 'RedshiftSql', 'MySql', 'BigQuery', 'SQLite', 'DuckDb', 'Databricks', 'ClickHouse', 'Oracle', 'Teradata', 'SparkSql'];
function dialectsIn(text) { return DIALECTS.filter(d => new RegExp('\\b' + d + 'Dialect\\b').test(text)); }

const results = [];
const stats = { calls: 0, extracted: 0, unresolvedArg: 0, byHelper: {}, bySelectorKind: {}, negative: 0, astBound: 0, discarded: 0 };
const fileFnDialects = {};

for (const f of files) {
  const src = fs.readFileSync(f, 'utf8');
  const masked = maskSource(src);
  const base = path.basename(f);
  // per-file helper fns returning TestedDialects
  const fnMap = {};
  const fnRe = /fn\s+([a-z_0-9]+)\s*\(\s*\)\s*->\s*TestedDialects\s*\{/g; let fm;
  while ((fm = fnRe.exec(masked))) {
    const open = masked.indexOf('{', fm.index + fm[0].length - 1); const close = matchClose(masked, open);
    const body = src.slice(open, close);
    fnMap[fm[1]] = /all_dialects/.test(body) ? 'all_dialects*' : dialectsIn(body).join('+') || '?';
  }
  fileFnDialects[base] = fnMap;
  // locate enclosing test fn start for each call, for `let` resolution
  const fnStarts = []; const fre = /\bfn\s+([a-z_0-9]+)/g; let fs2;
  while ((fs2 = fre.exec(masked))) fnStarts.push([fs2.index, fs2[1]]);
  const callRe = new RegExp('(\\.\\s*)?(?<![A-Za-z0-9_])(' + HELPERS.join('|') + ')\\s*\\(', 'g');
  // free-function wrappers like  fn verified_stmt(q) -> Statement { all_dialects().verified_stmt(q) }
  const wrappers = {}; const wre = /fn\s+([a-z_]+)\s*\([^)]*\)\s*->[^{]+\{\s*([a-z_]+\(\))\s*\.\s*([a-z_]+)\s*\(/g; let wm;
  while ((wm = wre.exec(masked))) if (HELPERS.includes(wm[1])) wrappers[wm[1]] = wm[2];
  let cm;
  while ((cm = callRe.exec(masked))) {
    const helper = cm[2];
    const isFree = !cm[1];
    if (isFree) { const pre = masked.slice(Math.max(0, cm.index - 6), cm.index); if (/fn\s*$/.test(pre) || !wrappers[helper]) continue; }
    const dotIdx = cm.index; const openIdx = masked.indexOf('(', dotIdx + (cm[1]||"").length + helper.length);
    const closeIdx = matchClose(masked, openIdx);
    if (closeIdx < 0) continue;
    // skip definitions in tests/test_utils
    stats.calls++;
    stats.byHelper[helper] = stats.byHelper[helper] || { calls: 0, literal: 0 };
    stats.byHelper[helper].calls++;
    const rs = isFree ? dotIdx : receiverStart(masked, dotIdx);
    const receiver = isFree ? wrappers[helper] : src.slice(rs, dotIdx).replace(/\s+/g, ' ').trim();
    const encl = fnStarts.filter(x => x[0] < dotIdx).pop() || [0, '?'];
    // classify selector
    let kind, dialects;
    const rname = receiver.replace(/\(\)$/, '');
    if (/^all_dialects\(\)$/.test(receiver)) { kind = 'all_dialects()'; dialects = 'ALL'; }
    else if (/^all_dialects_where/.test(receiver)) { kind = 'all_dialects_where(..)'; dialects = 'WHERE:' + (receiver.match(/supports_[a-z_]+|is::<\w+>|[A-Z]\w+Dialect/g) || []).join('|'); }
    else if (/^all_dialects_except/.test(receiver)) { kind = 'all_dialects_except(..)'; dialects = 'EXCEPT:' + (receiver.match(/supports_[a-z_]+|is::<\w+>|[A-Z]\w+Dialect/g) || []).join('|'); }
    else if (/^all_dialects_with_options/.test(receiver)) { kind = 'all_dialects_with_options(..)'; dialects = 'ALL+options'; }
    else if (/TestedDialects::new/.test(receiver)) { kind = 'TestedDialects::new(..)'; dialects = dialectsIn(receiver).join('+'); }
    else if (/\(\)$/.test(receiver) && fnMap[rname]) { kind = 'per-file fn ' + rname + '()'; dialects = fnMap[rname]; }
    else if (/^[a-z_0-9]+$/.test(receiver)) {
      // local variable: resolve `let receiver = ...;` within enclosing fn
      const scope = src.slice(encl[0], dotIdx);
      const lm = [...scope.matchAll(new RegExp('let\\s+(?:mut\\s+)?' + receiver + '\\s*(?::[^=]+)?=\\s*([\\s\\S]*?);', 'g'))].pop();
      if (lm) {
        const e = lm[1];
        if (/all_dialects_where|all_dialects_except/.test(e)) { kind = 'var->all_dialects_where/except'; dialects = 'WHERE/EXCEPT:' + (e.match(/supports_[a-z_]+|is::<\w+>|[A-Z]\w+Dialect/g) || []).join('|'); }
        else if (/all_dialects/.test(e)) { kind = 'var->all_dialects'; dialects = 'ALL'; }
        else if (/TestedDialects/.test(e)) { kind = 'var->TestedDialects::new'; dialects = dialectsIn(e).join('+'); }
        else { const fn = (e.match(/^([a-z_0-9]+)\(\)/) || [])[1]; kind = 'var->other'; dialects = fn && fnMap[fn] ? fnMap[fn] : '?'; }
      } else { kind = 'var(unresolved: ' + receiver + ')'; dialects = '?'; }
    } else { kind = 'other'; dialects = dialectsIn(receiver).join('+') || '?'; }
    stats.bySelectorKind[kind.startsWith('per-file fn') ? 'per-file dialect fn (e.g. pg(), snowflake())' : kind.replace(/\(unresolved: .*\)/, '(unresolved)')] =
      (stats.bySelectorKind[kind.startsWith('per-file fn') ? 'per-file dialect fn (e.g. pg(), snowflake())' : kind.replace(/\(unresolved: .*\)/, '(unresolved)')] || 0) + 1;
    // arguments
    let lit = readStringLit(src, openIdx + 1);
    let sql = null, canonical = null, argSource = 'literal';
    if (lit) sql = lit.value;
    else {
      const argText = src.slice(openIdx + 1, closeIdx).trim();
      const vm = /^&?\s*([a-z_0-9]+)\s*(,|$)/.exec(argText);
      if (vm) {
        const scope = src.slice(encl[0], dotIdx);
        const lm = [...scope.matchAll(new RegExp('let\\s+(?:mut\\s+)?' + vm[1] + '\\s*(?::[^=]+)?=\\s*', 'g'))].pop();
        if (lm) { const l2 = readStringLit(src, encl[0] + lm.index + lm[0].length); if (l2) { sql = l2.value; argSource = 'let-bound literal'; } }
        if (sql === null) argSource = 'variable (loop/format/unresolved)';
      } else argSource = /format!/.test(argText) ? 'format!' : 'other expr';
    }
    if (TWO_ARG.has(helper) && sql !== null) {
      // second argument
      const afterFirst = lit ? lit.end : null;
      if (afterFirst) { let q = afterFirst; while (/\s/.test(src[q])) q++; if (src[q] === ',') { const l2 = readStringLit(src, q + 1); if (l2) canonical = l2.value; } }
    }
    // negative / AST-bound classification
    const tail = masked.slice(closeIdx + 1, closeIdx + 40);
    const negative = helper === 'parse_sql_statements' && /^\s*\.\s*(is_err|unwrap_err|expect_err)/.test(tail);
    let ctxBefore = masked.slice(Math.max(0, rs - 60), rs);
    const bound = /(let\s+[^;=]*=\s*$|match\s*$|=\s*$|\(\s*$|,\s*$|return\s*$|assert[a-z_]*!\s*\(\s*$)/.test(ctxBefore) || /^\s*\./.test(tail) && !negative;
    if (negative) stats.negative++;
    if (helper !== 'parse_sql_statements') { if (bound) stats.astBound++; else stats.discarded++; }
    if (sql === null) { stats.unresolvedArg++; continue; }
    stats.extracted++; stats.byHelper[helper].literal++;
    results.push({ file: base, fn: encl[1], helper, selector: kind, dialects, sql, canonical, negative, astChecked: helper !== 'parse_sql_statements' ? bound : undefined, argSource });
  }
}

const distinct = new Set(results.map(r => r.sql));
const distinctRT = new Set(results.filter(r => r.helper !== 'parse_sql_statements').map(r => r.sql));
const distinctNeg = new Set(results.filter(r => r.negative).map(r => r.sql));
const distinctPairs = new Set(results.filter(r => r.canonical !== null && r.canonical !== undefined).map(r => r.sql + '\u0000' + r.canonical));
console.log(JSON.stringify({ ...stats, distinctSql: distinct.size, distinctRoundTripSql: distinctRT.size, distinctNegativeSql: distinctNeg.size, distinctCanonicalPairs: distinctPairs.size }, null, 1));
// per-file counts
const perFile = {};
for (const r of results) { perFile[r.file] = perFile[r.file] || { total: 0, distinct: new Set() }; perFile[r.file].total++; perFile[r.file].distinct.add(r.sql); }
console.log('per file (extracted calls / distinct sql):');
for (const [k, v] of Object.entries(perFile).sort((a, b) => b[1].total - a[1].total)) console.log('  ', k, v.total, v.distinct.size);
// dialect selector top values
const sel = {}; for (const r of results) sel[r.dialects] = (sel[r.dialects] || 0) + 1;
console.log('top dialect selectors:', JSON.stringify(Object.entries(sel).sort((a, b) => b[1] - a[1]).slice(0, 30)));
console.log('per-file dialect fns:', JSON.stringify(fileFnDialects));
if (outFile) fs.writeFileSync(outFile, results.map(r => JSON.stringify(r)).join('\n'));
