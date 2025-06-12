# SqlParser

A small SQL parser in C# that turns SQL text into an AST (lexer → tokens → parser → tree).

## What it does

- **SELECT:** `SELECT` column list, `FROM` one table, multiple **JOIN**s with **ON** conditions, optional **WHERE**, **GROUP BY**, **HAVING**, **ORDER BY**, **LIMIT**/OFFSET.
- **DELETE:** `DELETE FROM` table, optional **WHERE** (same condition style as SELECT).
- **UPDATE:** `UPDATE` table `SET` col = value [, col = value ...], optional **WHERE** (value: literal, column ref, or parenthesized expression).
- **Columns:** `*`, `col`, `table.col`, `table.*`, and `AS alias`.
- **Tables:** Table name, optional `schema.table`, optional table alias.
- **Joins:** INNER, LEFT, RIGHT, FULL (with optional OUTER), CROSS. ON conditions (except CROSS) support column refs, literals, `=`, `<>`, `AND`, `OR`, and parentheses; AND has higher precedence than OR.
- **WHERE:** Same expression style as ON; AND/OR precedence and parentheses.
- **GROUP BY:** Comma-separated column list (same column style as SELECT).
- **HAVING:** Same expression style as WHERE; AND/OR precedence and parentheses.
- **ORDER BY:** Comma-separated column list, optional ASC/DESC per column (default ASC).
- **LIMIT / OFFSET:** `LIMIT n` and optional `OFFSET m`.
- **UNION / UNION ALL:** `SELECT ... UNION SELECT ...` and `UNION ALL`; chained unions build a left-nested tree.

## What it doesn’t do (planned)

- **SELECT clauses:** (all main clauses parsed).
- **Other statements:** INSERT.
- **Advanced:** subqueries, function calls (e.g. `COUNT(*)`), DISTINCT, TOP, WITH.

See `docs/work_plan.md` for the full design spec and implementation order.
