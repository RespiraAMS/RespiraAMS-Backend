#!/usr/bin/env python3
"""Structurally validate a generated RespiraAMS unit-test report.

    python .opencode/skills/unit-test-report/scripts/validate_report.py \
        docs/tests/UnitTestReport.xlsx \
        --trx-dir /tmp/opencode/trx \
        --detail-function AddDosageHandler.HandleAsync

Checks sheet order, table continuity, count arithmetic, subtotal/chart/formula
references, every detail sheet's layout and marker styles, and stale defined
names. Exits non-zero on the first failed check. Every sheet after the four
standard sheets is validated as a detail sheet.
"""

from __future__ import annotations

import argparse
import glob
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

import openpyxl
from openpyxl.utils import get_column_letter

EXPECTED_SHEETS = ["Guideline", "Cover", "Functions", "Statistics"]
DETAIL_LABELS = ("A2", "A3", "A4", "A5", "A6", "C6", "F6", "L6", "F2", "F3", "F4")
STALE_REFERENCE = "GetConfigurationsAsync"


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report")
    parser.add_argument("--trx-dir", default=None)
    parser.add_argument(
        "--detail-function",
        default=None,
        help="Expected function of the first detail sheet (optional).",
    )
    return parser.parse_args()


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    sys.exit(1)


def check(condition: bool, message: str) -> None:
    if not condition:
        fail(message)


def has_border(cell, side: str | None = None) -> bool:
    """True when the cell has a border on `side` (or on any side)."""
    border = cell.border
    sides = (side,) if side else ("left", "right", "top", "bottom")
    for name in sides:
        edge = getattr(border, name)
        if edge is not None and edge.style:
            return True
    return False


def trx_totals(trx_dir: str) -> tuple[int, Counter]:
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    seen: set[str] = set()
    outcomes: Counter = Counter()
    for path in sorted(glob.glob(str(Path(trx_dir) / "*.trx"))):
        root = ET.parse(path).getroot()
        for result in root.findall(".//t:UnitTestResult", ns):
            name = result.attrib.get("testName", "")
            if not name or name in seen:
                continue
            seen.add(name)
            outcomes[result.attrib.get("outcome", "")] += 1
    check(bool(seen), f"no UnitTestResult records in {trx_dir}")
    return len(seen), outcomes


def main() -> None:
    args = parse_args()
    wb = openpyxl.load_workbook(args.report, data_only=False)

    # Sheet order: four standard sheets followed by one detail sheet per function.
    check(
        wb.sheetnames[:4] == EXPECTED_SHEETS,
        f"unexpected leading sheets: {wb.sheetnames}",
    )
    detail_sheets = wb.sheetnames[4:]
    check(bool(detail_sheets), "workbook has no detail sheets")

    functions = wb["Functions"]
    first_code = functions["E11"].value
    if args.detail_function:
        check(
            first_code == args.detail_function,
            f"first function is {first_code!r}, expected {args.detail_function!r}",
        )
    check(
        functions["F11"].value == detail_sheets[0],
        f"detail sheet {detail_sheets[0]!r} is not the first function's Sheet Name",
    )

    # Functions continuity.
    codes: list[str] = []
    row = 11
    while functions.cell(row=row, column=5).value not in (None, ""):
        codes.append(functions.cell(row=row, column=5).value)
        row += 1
    function_count = len(codes)
    check(function_count > 0, "Functions sheet is empty")
    check(len(set(codes)) == function_count, "duplicate function codes in Functions")
    trailing = [
        r
        for r in range(row, 400)
        if functions.cell(row=r, column=5).value not in (None, "")
    ]
    check(not trailing, f"leftover Functions rows: {trailing[:5]}")
    for offset, code in enumerate(codes):
        check(
            functions.cell(row=11 + offset, column=1).value == offset + 1,
            f"Functions row {11 + offset} has an unexpected index",
        )

    # Statistics table.
    stats = wb["Statistics"]
    subtotal = None
    for r in range(11, 2000):
        value = stats.cell(row=r, column=1).value
        if isinstance(value, str) and value.strip().lower() == "sub total":
            subtotal = r
            break
    check(subtotal is not None, "Statistics has no subtotal row")
    last_data = subtotal - 1
    check(
        last_data - 12 + 1 == function_count,
        f"Statistics has {last_data - 12 + 1} data rows for {function_count} functions",
    )

    totals = Counter()
    for r in range(12, subtotal):
        passed, failed, untested, n, a, b, total = (
            stats.cell(row=r, column=c).value for c in range(3, 10)
        )
        values = (passed, failed, untested, n, a, b, total)
        check(
            all(
                isinstance(v, (int, float)) and float(v).is_integer()
                for v in values
            ),
            f"Statistics row {r} has non-integer counts: {values}",
        )
        check(
            n + a + b == total,
            f"Statistics row {r}: N+A+B={n + a + b} != total {total}",
        )
        check(
            passed + failed + untested == total,
            f"Statistics row {r}: results do not add up to total {total}",
        )
        check(
            stats.cell(row=r, column=2).value == codes[r - 12],
            f"Statistics row {r} does not match Functions order",
        )
        totals["total"] += total
        totals["passed"] += passed
        totals["failed"] += failed
        totals["untested"] += untested

    # Subtotal row + references.
    for col in range(3, 10):
        letter = get_column_letter(col)
        expected = f"=SUM({letter}12:{letter}{last_data})"
        check(
            stats.cell(row=subtotal, column=col).value == expected,
            f"Statistics!{letter}{subtotal} is not {expected}",
        )
    check(
        f"A{subtotal}:B{subtotal}" in {str(m) for m in stats.merged_cells.ranges},
        "subtotal A:B merge is missing",
    )
    expected_fill = f"=F{subtotal}*100/I{subtotal}"
    check(
        stats["M20"].value == expected_fill,
        f"Statistics!M20 should be {expected_fill}, found {stats['M20'].value}",
    )
    check(
        (stats.auto_filter.ref or "").replace("$", "") == f"E11:E{last_data}",
        f"autofilter is {stats.auto_filter.ref}, expected E11:E{last_data}",
    )

    charts = stats._charts
    check(len(charts) == 2, f"expected 2 Statistics charts, found {len(charts)}")
    refs = {
        (
            charts[0].ser[0].val.numRef.f,
            charts[0].ser[0].cat.strRef.f,
            charts[1].ser[0].val.numRef.f,
            charts[1].ser[0].cat.strRef.f,
        )
    }
    expected_refs = {
        (
            f"Statistics!$F${subtotal}:$H${subtotal}",
            "Statistics!$F$11:$H$11",
            f"Statistics!$C${subtotal}:$E${subtotal}",
            "Statistics!$C$11:$E$11",
        )
    }
    check(refs == expected_refs, f"chart references are stale: {refs.pop()}")

    # Cover.
    cover = wb["Cover"]
    check(
        cover["F5"].value in (
            "=INDEX(A11:A1000,COUNTA(A11:A1000))",
        )
        or getattr(cover["F5"].value, "text", None) == "=INDEX(A11:A1000,COUNTA(A11:A1000))",
        f"Cover!F5 is not the index formula: {cover['F5'].value!r}",
    )
    check(
        getattr(cover["F6"].value, "text", cover["F6"].value)
        == "=INDEX(B11:B1000,COUNTA(B11:B1000))",
        f"Cover!F6 is not the index formula: {cover['F6'].value!r}",
    )

    # Detail sheets: one per Functions row that names a sheet.
    named_sheets = {
        functions.cell(row=11 + index, column=6).value: code
        for index, code in enumerate(codes)
        if functions.cell(row=11 + index, column=6).value
    }
    check(
        list(named_sheets) == detail_sheets,
        f"Functions Sheet Name column {list(named_sheets)} does not match "
        f"detail sheets {detail_sheets}",
    )

    marker_total = 0
    summary: list[str] = []
    for sheet_name in detail_sheets:
        detail = wb[sheet_name]
        for coord in DETAIL_LABELS:
            check(
                detail[coord].value not in (None, ""),
                f"{sheet_name}: detail label {coord} is empty",
            )

        code = detail["C2"].value
        check(
            code in codes,
            f"{sheet_name}: detail C2 {code!r} is not a Functions code",
        )
        check(
            detail["C3"].value == wb.properties.creator,
            f"{sheet_name}: C3 {detail['C3'].value!r} is not the workbook creator",
        )
        check(
            isinstance(detail["C4"].value, (int, float)),
            f"{sheet_name}: C4 lines of code is not a number",
        )
        check(
            str(detail["L4"].value).startswith("=IF("),
            f"{sheet_name}: L4 lost the lack-of-test-cases formula",
        )

        function_index = codes.index(code)
        stats_row = 12 + function_index
        check(
            detail["O7"].value == stats.cell(row=stats_row, column=9).value,
            f"{sheet_name}: UTCID total does not match the Statistics total",
        )
        check(
            (detail["A7"].value, detail["C7"].value, detail["F7"].value)
            == tuple(stats.cell(row=stats_row, column=c).value for c in (3, 4, 5)),
            f"{sheet_name}: passed/failed/untested do not match Statistics",
        )
        check(
            (detail["L7"].value, detail["M7"].value, detail["N7"].value)
            == tuple(stats.cell(row=stats_row, column=c).value for c in (6, 7, 8)),
            f"{sheet_name}: N/A/B do not match Statistics",
        )

        utcids: list[str] = []
        col = 5
        while True:
            value = detail.cell(row=9, column=col).value
            if value in (None, ""):
                break
            utcids.append(value)
            col += 1
        check(
            len(utcids) == detail["O7"].value,
            f"{sheet_name}: has {len(utcids)} UTCIDs but O7={detail['O7'].value}",
        )
        check(
            utcids == [f"UTCID{i:02d}" for i in range(1, len(utcids) + 1)],
            f"{sheet_name}: UTCID headers are out of order: {utcids}",
        )

        markers = 0
        for r in range(1, detail.max_row + 1):
            for c in range(5, 5 + len(utcids)):
                cell = detail.cell(row=r, column=c)
                if cell.value != "O":
                    continue
                check(
                    cell.font.name == "Tahoma"
                    and cell.font.sz == 11
                    and cell.font.b
                    and cell.alignment.horizontal == "center"
                    and cell.alignment.vertical == "center",
                    f"{sheet_name}: marker {cell.coordinate} is not Tahoma 11 bold "
                    f"centered ({cell.font.name} {cell.font.sz} bold={cell.font.b} "
                    f"{cell.alignment.horizontal}/{cell.alignment.vertical})",
                )
                markers += 1

        check(markers > 0, f"{sheet_name}: detail sheet has no O markers")
        marker_total += markers

        # Table geometry: the detail table must be one contiguous block that
        # starts at the UTCID header, ends on the Defect ID row, keeps its right
        # and bottom borders, and leaves no styled row behind it.
        table_columns = 4 + len(utcids)

        def is_used(cell) -> bool:
            if cell.value not in (None, ""):
                return True
            return has_border(cell)

        table_rows = [
            r
            for r in range(9, detail.max_row + 1)
            if any(
                is_used(detail.cell(row=r, column=c))
                for c in range(1, table_columns + 1)
            )
        ]
        check(table_rows, f"{sheet_name}: the detail table has no rows")
        check(table_rows[0] == 9, f"{sheet_name}: the table starts at row {table_rows[0]}")
        last_row = table_rows[-1]
        gaps = [r for r in range(9, last_row + 1) if r not in table_rows]
        check(not gaps, f"{sheet_name}: blank rows inside the table: {gaps}")
        check(
            detail.cell(row=last_row, column=2).value == "Defect ID",
            f"{sheet_name}: the table ends on row {last_row} "
            f"({detail.cell(row=last_row, column=2).value!r}), expected Defect ID",
        )
        check(
            all(
                has_border(detail.cell(row=last_row, column=c), "bottom")
                for c in range(5, table_columns + 1)
            ),
            f"{sheet_name}: the table's bottom border is missing",
        )
        check(
            all(
                has_border(detail.cell(row=r, column=table_columns), "right")
                for r in range(10, last_row + 1)
            ),
            f"{sheet_name}: the table's right border is missing",
        )
        stray = [
            r
            for r in range(last_row + 1, detail.max_row + 1)
            if any(is_used(detail.cell(row=r, column=c)) for c in range(1, 20))
        ]
        check(not stray, f"{sheet_name}: cells remain below the table: {stray[:5]}")

        summary.append(f"{sheet_name} ({len(utcids)} UTCIDs, {markers} markers)")

    # Defined names and stale references.
    for name, defined in wb.defined_names.items():
        check(
            STALE_REFERENCE not in (defined.attr_text or ""),
            f"defined name {name} still references {STALE_REFERENCE}",
        )
    for sheet in wb.worksheets:
        for name, defined in sheet.defined_names.items():
            check(
                STALE_REFERENCE not in (defined.attr_text or ""),
                f"{sheet.title} defined name {name} still references "
                f"{STALE_REFERENCE}",
            )
        check(
            STALE_REFERENCE not in sheet.title,
            f"stale sheet name {sheet.title}",
        )

    # TRX cross-check.
    if args.trx_dir:
        expected_total, outcomes = trx_totals(args.trx_dir)
        check(
            totals["total"] == expected_total,
            f"report has {totals['total']} cases, TRX has {expected_total}",
        )
        check(
            totals["passed"] == outcomes.get("Passed", 0),
            f"report passed={totals['passed']} but TRX has "
            f"{outcomes.get('Passed', 0)}",
        )
        check(
            totals["failed"] == outcomes.get("Failed", 0),
            f"report failed={totals['failed']} but TRX has "
            f"{outcomes.get('Failed', 0)}",
        )
        print(
            f"TRX: {expected_total} cases "
            f"({outcomes.get('Passed', 0)} passed, "
            f"{outcomes.get('Failed', 0)} failed)"
        )

    print(
        f"OK: {function_count} functions, {totals['total']} cases, "
        f"subtotal row {subtotal}, {len(detail_sheets)} detail sheets, "
        f"{marker_total} markers"
    )
    for line in summary:
        print(f"  {line}")


if __name__ == "__main__":
    main()
