# SqlParser

A small SQL parser in C# that turns SQL text into an AST (lexer → tokens → parser → tree).

## What it does

- **SELECT only:** `SELECT` column list, `FROM` one table, multiple **JOIN**s with **ON** conditions.
- **Columns:** `*`, `col`, `table.col`, `table.*`, and `AS alias`.
- **Tables:** Table name, optional `schema.table`, optional table alias.
- **Joins:** INNER, LEFT, RIGHT, FULL (with optional OUTER). ON conditions support column refs, literals, `=`, `<>`, `AND`, `OR`, and parentheses; AND has higher precedence than OR.

## What it doesn’t do (planned)

- **SELECT clauses:** WHERE, GROUP BY, HAVING, ORDER BY, LIMIT/OFFSET (AST nodes exist, not parsed yet).
- **Other statements:** INSERT, UPDATE, DELETE.
- **Advanced:** UNION/UNION ALL, subqueries, function calls (e.g. `COUNT(*)`), CROSS JOIN, DISTINCT, TOP, WITH.

See `docs/work_plan.md` for the full design spec and implementation order.
