# Workbook layout

Cell map for `docs/tests/UnitTestReportTemplate.xlsx`. The generator keeps this
layout; only the values and the row/column counts change.

## Sheet order

1. `Guideline` — template instructions, never edited.
2. `Cover` — project metadata and change log.
3. `Functions` — one row per production function under test.
4. `Statistics` — per-function counts, subtotal, coverage percentages, charts.
5. `GetConfigurationsAsync` — template detail sheet, renamed and cloned once per
   detail spec (`AddDosageHandler`, `CreateAntibiogramHandler`, …). Sheets follow
   `Functions` order.

## Cover

| Cell | Value |
| --- | --- |
| `B4` / `B5` | `RespiraAMS` |
| `F4` | Creator |
| `F5` | `=INDEX(A11:A1000,COUNTA(A11:A1000))` — latest effective date |
| `F6` | `=INDEX(B11:B1000,COUNTA(B11:B1000))` — latest version |
| `B6` | `=B5&"_"&"Report5_Unit_Test"&"_"&"v"&F6` (template, keep) |
| `A11:F11` | change-log row: date, version, `All`, `A`, `Create document`, `N/A` |

Only one change-log row is written. `F5` and `F6` must stay formulas; Excel
recalculates them (`fullCalcOnLoad` is enabled by the generator).

## Functions

| Cell | Value |
| --- | --- |
| `E4` | `=Cover!B4` |
| `E5` | `=Cover!B5` |
| `E6` | Normal number of test cases / KLOC (default `100`) |
| `E7` | Test environment description (multi-line text) |
| `A10:H10` | `No`, `Requirement Name`, `Class Name`, `Function Name`, `Function Code(Optional)`, `Sheet Name`, `Description`, `Pre-Condition` |

Data starts at row 11 and is contiguous. Columns:

- `A` — 1-based index
- `B` — module (`Clinical Application`, `Clinical Domain`, …)
- `C` — production class
- `D` — production function
- `E` — `{Class}.{Function}`
- `F` — detail-sheet name for functions that have a spec, blank otherwise
- `G` — `Tests for {Class}.{Function}`
- `H` — short precondition summary

The template holds 258 data rows (11-268). The generator clears them, writes the
current inventory, and deletes or inserts rows so the list ends at
`10 + function_count`.

## Statistics

| Cell | Value |
| --- | --- |
| `B4` | `=Cover!B4` |
| `B6` | `=Cover!B6` |
| `F4` | Creator |
| `F6` | `=Cover!F5` (issue date) |
| `B7` | Summary sentence with the total case count and run date |
| `A11:I11` | `No`, `Function code`, `Passed`, `Failed`, `Untested`, `N`, `A`, `B`, `Total Test Cases` |

Data starts at row 12, one row per function in the same order as `Functions`.
The subtotal row follows the last data row and contains
`=SUM(<col>12:<col>{last_data_row})` in `C:I`; `A:B` are merged with
`Sub total`.

Coverage block (fixed columns `K`-`N`, rows 18-22) references the subtotal row:

| Cell | Label | Formula (subtotal row = `S`) |
| --- | --- | --- |
| `M18` | Test coverage | `=(C{S}+D{S})*100/(I{S})` |
| `M19` | Test successful coverage | `=C{S}*100/(I{S})` |
| `M20` | Normal case | `=F{S}*100/I{S}` |
| `M21` | Abnormal case | `=G{S}*100/I{S}` |
| `M22` | Boundary case | `=H{S}*100/I{S}` |

Charts (two pie charts on the sheet):

- cases chart → `Statistics!$F${S}:$H${S}` (N/A/B), categories `$F$11:$H$11`
- results chart → `Statistics!$C${S}:$E${S}` (passed/failed/untested),
  categories `$C$11:$E$11`

`insert_rows()` / `delete_rows()` do **not** update chart references, defined
names, or the autofilter. Set them explicitly after resizing:

- autofilter `E11:E{last_data_row}`
- `_xlnm._FilterDatabase` follows the autofilter automatically
- merged `A{S}:B{S}`

The template needs at least 12 functions so the coverage block (rows 18-22)
does not collide with the data rows; the generator fails loudly below that.

## Detail sheet (`GetConfigurationsAsync` → `AddDosageHandler`, …)

Every detail sheet uses the same layout and is built from one spec. Sheet names
come from the spec's `sheet_name`; the spec's `function_code` ties it to its
`Functions`/`Statistics` row. The counts in `Statistics` for that row are
overridden with the spec's `N`/`A`/`B` classification.

| Cell | Value |
| --- | --- |
| `A2` / `C2` | `Function Code` / `{Class}.{Function}` |
| `F2` / `L2` | `Function Name` / `{Class}.{Function}` |
| `A3` / `C3` | `Created By` / creator |
| `F3` / `L3` | `Executed By` / creator |
| `A4` / `C4` | `Lines of code` / production LOC |
| `F4` / `L4` | `Lack of test cases` / `=IF(Functions!E6<>"N/A",SUM(C4*Functions!E6/1000,-O7),"N/A")` |
| `A5` / `C5` | `Test requirement` / requirement summary (merged `C5:T5`) |
| `A6` / `C6` / `F6` / `L6` | `Passed` / `Failed` / `Untested` / `N/A/B` |
| `A7` / `C7` / `F7` | passed / failed / untested counts |
| `L7` / `M7` / `N7` / `O7` | N / A / B / total UTCIDs |
| `E9:...` | `UTCID01`, `UTCID02`, … one per case |
| `A10` / `B10` | `Condition` / `Precondition` |
| `11` … `10+P`, col `D` | precondition text; `O` markers in the UTCID columns |
| `11+P`, col `B` | `Input` |
| `12+P` … `11+P+I`, col `D` | input text; `O` markers in the UTCID columns |
| `12+P+I` | `Confirm` / `Return` |
| `13+P+I` … `12+P+I+R` | return rows (max 5) |
| `13+P+I+R` | `Exception` / `Exception`; text row below it |
| `15+P+I+R` | `Result` / `Type(N : Normal, A : Abnormal, B : Boundary)` |
| +1 | `Passed/Failed` (`P` / `F`) |
| +2 | `Executed Date` |
| +3 | `Defect ID` |

Layout arithmetic (`P` = precondition count, `I` = input count, `R` = return
count, `T` = number of UTCIDs). The table is laid out compactly and each row
copies the style of a template role row (`DETAIL_ROLE_ROWS`):

| Row | Role (template row) | Height |
| --- | --- | --- |
| 9 | `utc_header` (9) | 46.5 |
| 10 | `condition` (10) | 13.5 |
| 11 … `10+P` | `precondition` (11) — one per precondition | 31.5 |
| `11+P` | `input_header` (16) | 13.5 |
| `12+P` … `11+P+I` | `input` (17) — one per input | 36 |
| `12+P+I` | `confirm` (19) | 13.5 |
| `13+P+I` … `12+P+I+R` | `return` (20) — one per return | 31.5 |
| `13+P+I+R` | `exception_header` (23) | 13.5 |
| `14+P+I+R` | `exception_text` (24) — always present | 31.5 |
| `15+P+I+R` | `result` (25) | 21.75 |
| +1 | `passed` (26) | 21.75 |
| +2 | `date` (27) | 21.75 |
| +3 | `defect` (28) — last row, keeps the bottom border | 21.75 |

The table spans `A` to the last UTCID column (`D + T`). Columns past `K` copy
column `K`; columns right of the last UTCID are cleared so no empty header cell
or stray border remains. The exception text is written even when no case
reaches it, so the table never shows a blank bordered line. Everything from row
9 down is cleared and re-styled on every build, which also removes the
template's leftover bottom row when the new table ends earlier.

Clear only cells whose labels you rewrite, or snapshot the template labels
(`A2:A5`, `A6`, `C6`, `F2`, `F3`, `F4`, `F6`, `L6`) first and restore them,
because `A2`, `A6` and `F2` are merged-range anchors and are wiped by a naive
`clear_values()`.

### Building several detail sheets

`copy_worksheet()` copies values, styles, merges, row heights, and column widths
but not sheet-local defined names (nor charts). Build in this order:

1. rename the pristine template sheet for the first spec and update its
   sheet-local `Z_*.wvu.Cols` defined names;
2. clone the still-pristine source once per remaining spec and rename the copy;
3. fill every sheet, including the first, with `build_detail_sheet()`.

The first spec is forced to `Functions` row 1; the remaining detail sheets
follow the natural `Functions` order. Each spec must define a unique
`sheet_name` and a `function_code` present in the TRX.

### Marker style

Every `O` in a UTCID column is **Tahoma 11, bold, horizontally and vertically
centered, thin border, no fill**. Every other table cell stays Tahoma 8: the
role prototypes carry the template's marker emphasis on some cells, so the
generator resets non-marker cells after writing them.
