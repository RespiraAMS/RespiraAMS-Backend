#!/usr/bin/env python3
"""Generate the RespiraAMS unit-test report from TRX results.

The generator keeps the template layout and styles, then replaces the function
inventory, the statistics table, and the first function's detail sheet with the
current test run.

Typical usage (from the repository root):

    python .opencode/skills/unit-test-report/scripts/build_report.py \
        --trx-dir /tmp/opencode/trx \
        --detail-spec .opencode/skills/unit-test-report/references/add-dosage-detail.json \
        --date 10/11/2026 \
        --creator DangLNH

Requires: openpyxl (3.1+).
"""

from __future__ import annotations

import argparse
import glob
import json
import xml.etree.ElementTree as ET
from collections import Counter, defaultdict
from copy import copy
from pathlib import Path

import openpyxl
from openpyxl.cell.cell import MergedCell
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

MODULES = [
    ("Respira.Analysis.Application.Test", "Analysis Application"),
    ("Respira.Analysis.Domain.Test", "Analysis Domain"),
    ("Respira.Analysis.Infrastructure.Test", "Analysis Infrastructure"),
    ("Respira.Authentication.Domain.Test", "Authentication Domain"),
    ("Respira.Authentication.Infrastructure.Test", "Authentication Infrastructure"),
    ("Respira.Clinical.Application.Test", "Clinical Application"),
    ("Respira.Clinical.Domain.Test", "Clinical Domain"),
    ("Respira.Clinical.Infrastructure.Test", "Clinical Infrastructure"),
    ("Respira.Doctor.Application.Test", "Doctor Application"),
    ("Respira.Doctor.Domain.Test", "Doctor Domain"),
    ("Respira.Doctor.Infrastructure.Test", "Doctor Infrastructure"),
    ("Respira.Patient.Application.Test", "Patient Application"),
    ("Respira.Patient.Domain.Test", "Patient Domain"),
    ("Respira.Saga.Application.Test", "Saga Application"),
    ("Respira.Saga.Domain.Test", "Saga Domain"),
    ("Respira.Saga.Infrastructure.Test", "Saga Infrastructure"),
    ("Respira.ServiceDefaults.Application.Test", "ServiceDefaults Application"),
]

# Classes whose names do not follow the Handler/Mapper/Validator/Middleware
# convention. Extend this table when new special test classes appear.
SPECIAL_FUNCTIONS = {
    "AntibioticTest": ("Antibiotic", "IsAntibioticDosageValid"),
    "ClinicalMetricTest": ("ClinicalMetric", "IsScoringMetric"),
    "CriterionTest": ("Criterion", "IsCriterionSatisfied"),
    "ExpressionTest": ("Expression", "Evaluate"),
    "FormulaTest": ("Formula", "ToExpression"),
    "RangeTest": ("Range", "IsInRange"),
    "DiagnoseServiceTest": ("DiagnoseService", "Diagnose"),
    "FormulaSerializerTest": ("FormulaSerializer", "Serialize"),
    "NameNormalizerTest": ("NameNormalizer", "Normalize"),
    "CreateSagaTest": ("CreateSaga", "StartAsync"),
    # The flow suites exercise the remaining saga steps; they share the entry
    # point so a saga keeps one Functions row (the records are merged by code).
    "CreateSagaFlowTest": ("CreateSaga", "StartAsync"),
    "DeleteSagaFlowTest": ("DeleteSaga", "HandleAsync"),
    "UpdateSagaFlowTest": ("UpdateSaga", "HandleAsync"),
    "ClinicalRollbackServiceTest": ("ClinicalRollbackService", "Rollback"),
    "ProfileCreationPolicyTest": ("ProfileCreationPolicy", "CanCreate"),
    # Analysis, Authentication, Doctor, Saga domain/infrastructure, and
    # ServiceDefaults suites whose class names do not follow the suffix rules.
    "AcademicTitleTest": ("AcademicTitle", "Parse"),
    "AccountEntityTest": ("Account", "Constructor"),
    "AnalysisDbContextFactoryTest": ("AnalysisDbContextFactory", "CreateDbContext"),
    "AnalysisDbContextTest": ("AnalysisDbContext", "SaveChangesAsync"),
    "AuditEventFactoryTest": ("AuditEventFactory", "Create"),
    "AuditLogEntryTest": ("AuditLogEntry", "Constructor"),
    "AuditLoggingRegistrationTest": ("AuditLoggingExtension", "AddAuditLogging"),
    "AuditOutboxForwarderTest": ("AuditOutboxForwarder", "ForwardAsync"),
    "AuditSnapshotTest": ("AuditSnapshot", "Apply"),
    "AuditableDbContextTest": ("AuditableDbContext", "SaveChangesAsync"),
    "AuthDbContextTest": ("AuthDbContext", "SaveChangesAsync"),
    "BlacklistTokenEntityTest": ("BlacklistToken", "Constructor"),
    "CacheServiceTest": ("CacheService", "SetAsync"),
    "ClinicalAnalysisJsonTest": ("ClinicalAnalysis", "ReportJson"),
    "ClinicalAnalysisTest": ("ClinicalAnalysis", "Constructor"),
    "ClosedXmlAnalysisReportExcelWriterTest": (
        "ClosedXmlAnalysisReportExcelWriter",
        "Write",
    ),
    "DegreeTypeTest": ("DegreeType", "Parse"),
    "DependencyInjectionTest": ("DependencyInjection", "AddSagaInfrastructure"),
    "DevelopmentAnalysisReportSeederTest": (
        "DevelopmentAnalysisReportSeeder",
        "SeedAnalysisReportsAsync",
    ),
    "DoctorDbContextTest": ("DoctorDbContext", "SaveChangesAsync"),
    "DoctorEntityTest": ("Doctor", "Constructor"),
    "DoctorProfileEnricherTest": ("DoctorProfileEnricher", "EnrichAsync"),
    "EmailServiceTest": ("EmailService", "SendAsync"),
    "GenderTypeTest": ("GenderType", "Parse"),
    "HashServiceTest": ("HashService", "HashToken"),
    "HeadDepartmentDoctorEntityTest": ("HeadDepartmentDoctor", "Constructor"),
    "HttpContextAuditContextAccessorTest": (
        "HttpContextAuditContextAccessor",
        "GetCurrent",
    ),
    "JwtServiceAccessTokenTest": ("JwtService", "ValidateAccessToken"),
    "JwtServiceRefreshTokenTest": ("JwtService", "ValidateRefreshToken"),
    "MediaClientTest": ("MediaClient", "UploadAvatarAsync"),
    "PaginationFactoryTest": ("PaginationFactory", "Create"),
    "PositionTypeTest": ("PositionType", "Parse"),
    "ProfileRoleTest": ("ProfileRole", "Parse"),
    "RoleTypeTest": ("RoleType", "Parse"),
    "SagaAuditDbContextTest": ("SagaAuditDbContext", "SaveChangesAsync"),
    "SagaStateReaderPostgresTest": ("SagaStateReader", "ReadAsync"),
    "SagaStateReaderUnitTest": ("SagaStateReader", "Constructor"),
    "SagaStepTest": ("SagaStep", "Parse"),
    "StatusTypeTest": ("StatusType", "Parse"),
    "TokenEntityTest": ("Token", "IsExpired"),
    "TokenTypeTest": ("TokenType", "Parse"),
}

ABNORMAL_KEYWORDS = (
    "fail",
    "invalid",
    "throw",
    "notfound",
    "not_found",
    "missing",
    "empty",
    "null",
    "error",
    "exception",
    "duplicate",
    "reject",
    "without",
    "unknown",
    "mismatch",
    "badrequest",
    "unauthorized",
    "forbidden",
    "conflict",
    "negative",
    "overflow",
    "underflow",
)
BOUNDARY_KEYWORDS = ("boundary", "touch", "edgecase", "edge_case")

DETAIL_TEMPLATE_SHEET = "GetConfigurationsAsync"
# Labels on the detail sheet that live on merged-range anchors and must be put
# back after the sheet is cleared.
DETAIL_LABELS = ("A2", "A3", "A4", "A5", "A6", "C6", "F6", "L6", "F2", "F3", "F4")

DETAIL_MARKER_COLUMNS = 7  # template provides E:K

# Template rows whose style each part of the detail table must copy. The table
# is rebuilt from these roles, so a function with more or fewer preconditions,
# inputs, or returns still gets the template's exact borders, fills, and labels
# without leaving blank bordered rows or spilling past the table.
DETAIL_ROLE_ROWS = {
    "utc_header": 9,
    "condition": 10,
    "precondition": 11,
    "input_header": 16,
    "input": 17,
    "confirm": 19,
    "return": 20,
    "exception_header": 23,
    "exception_text": 24,
    "result": 25,
    "passed": 26,
    "date": 27,
    "defect": 28,
}

DETAIL_ROLE_HEIGHTS = {
    "utc_header": 46.5,
    "condition": 13.5,
    "precondition": 31.5,
    "input_header": 13.5,
    "input": 36.0,
    "confirm": 13.5,
    "return": 31.5,
    "exception_header": 13.5,
    "exception_text": 31.5,
    "result": 21.75,
    "passed": 21.75,
    "date": 21.75,
    "defect": 21.75,
}

DETAIL_TABLE_COLUMNS = 11  # template table spans A:K


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--template", default="docs/tests/UnitTestReportTemplate.xlsx")
    parser.add_argument("--output", default="docs/tests/UnitTestReport.xlsx")
    parser.add_argument("--trx-dir", default="/tmp/opencode/trx")
    parser.add_argument(
        "--detail-function",
        default=None,
        help="Function code for the first detail sheet when no spec is given.",
    )
    parser.add_argument(
        "--detail-spec",
        action="append",
        default=None,
        help="Detail spec JSON; repeat to add more detail sheets. The first "
        "spec is forced to the top of the Functions list.",
    )
    parser.add_argument("--date", default="10/11/2026")
    parser.add_argument("--creator", default="DangLNH")
    parser.add_argument("--version", default="1.0.0")
    parser.add_argument("--normal-cases-per-kloc", type=float, default=100)
    return parser.parse_args()


# ---------------------------------------------------------------------------
# TRX inventory
# ---------------------------------------------------------------------------


def parse_trx(trx_dir: str) -> dict[str, list[dict]]:
    """Return test-class -> test records, deduplicated by full test name.

    Each record contains the base method name, the raw test name, and the TRX
    outcome. Rerunning a project must not inflate the inventory, so a test name
    seen in an earlier file is ignored.
    """
    paths = sorted(glob.glob(str(Path(trx_dir) / "*.trx")))
    if not paths:
        raise SystemExit(f"No .trx files found in {trx_dir}")

    by_class: dict[str, list[dict]] = defaultdict(list)
    seen: set[str] = set()

    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    for path in paths:
        root = ET.parse(path).getroot()
        for result in root.findall(".//t:UnitTestResult", ns):
            raw_name = result.attrib.get("testName", "")
            outcome = result.attrib.get("outcome", "")
            if not raw_name or raw_name in seen:
                continue
            seen.add(raw_name)

            base = raw_name.split("(", 1)[0]
            try:
                test_class, method = base.rsplit(".", 1)
            except ValueError as exc:
                raise SystemExit(f"Cannot parse test name: {raw_name!r}") from exc

            by_class[test_class].append(
                {"method": method, "raw": raw_name, "outcome": outcome}
            )

    if not by_class:
        raise SystemExit(f"No UnitTestResult records found in {trx_dir}")

    return dict(by_class)


def module_and_feature(fqn: str) -> tuple[str, str]:
    for prefix, module in MODULES:
        if fqn.startswith(prefix + "."):
            rest = fqn[len(prefix) + 1 :]
            if rest.startswith("Features."):
                rest = rest[len("Features.") :]
            rest = rest.rsplit(".", 1)[0]  # drop the test class
            return module, rest.replace(".", " / ")
    # Fallback keeps unknown namespaces visible instead of dropping the class.
    return "Other", fqn.rsplit(".", 1)[0].replace(".", " / ")


def describe_class(fqn: str) -> tuple[str, str]:
    simple = fqn.rsplit(".", 1)[-1]
    if simple in SPECIAL_FUNCTIONS:
        return SPECIAL_FUNCTIONS[simple]
    if simple.endswith("HandlerTest"):
        return simple[: -len("Test")], "HandleAsync"
    if simple.endswith("MapperTest"):
        return simple[: -len("Test")], "Map"
    if simple.endswith("ValidatorTest"):
        return simple[: -len("Test")], "Validate"
    if simple.endswith("MiddlewareTest"):
        return simple[: -len("Test")], "InvokeAsync"
    raise SystemExit(
        f"Unmapped test class {simple!r}. Add it to SPECIAL_FUNCTIONS in "
        "scripts/build_report.py."
    )


def classify(method: str) -> str:
    """Heuristically classify a test method as N, A, or B.

    Only used for functions without a hand-written detail spec. A detail spec is
    authoritative for the function that gets a UTCID sheet.
    """
    text = method.lower()
    if any(keyword in text for keyword in BOUNDARY_KEYWORDS):
        return "B"
    if any(keyword in text for keyword in ABNORMAL_KEYWORDS):
        return "A"
    return "N"


def build_inventory(by_class: dict[str, list[dict]]) -> list[dict]:
    functions: list[dict] = []

    for fqn, records in by_class.items():
        module, feature = module_and_feature(fqn)
        cls, func = describe_class(fqn)
        outcomes = Counter(record["outcome"] for record in records)
        passed = outcomes.get("Passed", 0)
        failed = outcomes.get("Failed", 0)
        total = len(records)
        untested = total - passed - failed

        counts = Counter(classify(record["method"]) for record in records)
        functions.append(
            {
                "fqn": fqn,
                "module": module,
                "feature": feature,
                "class": cls,
                "function": func,
                "code": f"{cls}.{func}",
                "total": total,
                "passed": passed,
                "failed": failed,
                "untested": untested,
                "n": counts.get("N", 0),
                "a": counts.get("A", 0),
                "b": counts.get("B", 0),
                "records": records,
            }
        )

    # Two test classes can cover the same production function (a start suite
    # plus a flow suite). Merge them so Functions and Statistics keep one row
    # per function code; the detail spec sees the combined record set.
    merged: dict[str, dict] = {}
    result: list[dict] = []
    for function in functions:
        existing = merged.get(function["code"])
        if existing is None:
            merged[function["code"]] = function
            result.append(function)
            continue
        existing["records"] = existing["records"] + function["records"]
        for key in ("total", "passed", "failed", "untested", "n", "a", "b"):
            existing[key] += function[key]

    return result


# ---------------------------------------------------------------------------
# Workbook helpers
# ---------------------------------------------------------------------------


def copy_style(src, dst) -> None:
    dst._style = copy(src._style)


def clear_values(ws, min_row: int, max_row: int, max_col: int) -> None:
    for row in ws.iter_rows(min_row=min_row, max_row=max_row, max_col=max_col):
        for cell in row:
            if isinstance(cell, MergedCell):
                continue
            cell.value = None
            cell.hyperlink = None


def write_body_text(cell, value) -> None:
    cell.value = value
    cell.alignment = Alignment(horizontal="right", vertical="center", wrap_text=True)
    cell.font = Font(name="Tahoma", size=8)


def write_marker(ws, row: int, col: int) -> None:
    """Write the `O` marker with the template's intended emphasis style.

    Inserted rows copy their style from a body row (Tahoma 8, not centered), so
    every marker is styled explicitly: Tahoma 11 bold and centered.
    """
    cell = ws.cell(row=row, column=col, value="O")
    cell.font = Font(name="Tahoma", size=11, bold=True)
    cell.alignment = Alignment(horizontal="center", vertical="center")
    cell.border = Border(
        left=Side(style="thin"),
        right=Side(style="thin"),
        top=Side(style="thin"),
        bottom=Side(style="thin"),
    )
    cell.fill = PatternFill(fill_type=None)
    cell.number_format = "General"


def load_detail_spec(path: str | None, detail_function: str) -> dict:
    if not path:
        return {
            "function_code": detail_function,
            "sheet_name": detail_function.split(".", 1)[0],
            "lines_of_code": 0,
            "requirement": f"Unit tests for {detail_function}.",
            "preconditions": [
                {
                    "text": "The test fixture and dependencies are initialized",
                    "marks": [],
                }
            ],
            "inputs": [],
            "returns": [],
            "exception_text": "No unhandled exception",
            "exception_marks": [],
            "cases": [],
        }

    with open(path, encoding="utf-8") as fh:
        spec = json.load(fh)
    spec.setdefault("function_code", detail_function)
    spec.setdefault("sheet_name", spec["function_code"].split(".", 1)[0])
    spec.setdefault("lines_of_code", 0)
    spec.setdefault("requirement", f"Unit tests for {spec['function_code']}.")
    spec.setdefault("preconditions", [])
    spec.setdefault("inputs", [])
    spec.setdefault("returns", [])
    spec.setdefault("exception_text", "No unhandled exception")
    spec.setdefault("exception_marks", [])
    spec.setdefault("cases", [])
    return spec


def normalise_detail_cases(spec: dict, detail_record: dict) -> list[dict]:
    """Validate the detail spec and return one case per UTCID."""
    records = detail_record["records"]
    total = len(records)

    if spec.get("cases"):
        cases = spec["cases"]
    elif spec.get("types"):
        cases = [{"type": kind} for kind in spec["types"]]
    else:
        cases = [
            {
                "method": record["method"],
                "type": classify(record["method"]),
                "result": "P" if record["outcome"] == "Passed" else "F",
            }
            for record in records
        ]

    if len(cases) != total:
        raise SystemExit(
            f"Detail spec has {len(cases)} cases but TRX has {total} for "
            f"{detail_record['code']}."
        )

    outcome_by_method = {record["method"]: record["outcome"] for record in records}
    methods = set(outcome_by_method)
    for case in cases:
        kind = case.get("type")
        if kind not in {"N", "A", "B"}:
            raise SystemExit(f"Invalid detail case type {kind!r}; use N, A, or B.")
        method = case.get("method")
        if method and method not in methods:
            raise SystemExit(
                f"Detail case method {method!r} is not present in the TRX for "
                f"{detail_record['code']}."
            )
        if case.get("result") is None:
            if method:
                case["result"] = "P" if outcome_by_method[method] == "Passed" else "F"
            else:
                case["result"] = "P" if detail_record["failed"] == 0 else "F"
    return cases


def assert_mark_range(marks: list[int], total: int, label: str) -> None:
    for mark in marks:
        if not 1 <= mark <= total:
            raise SystemExit(f"{label} marker {mark} is outside 1..{total}")


def marks_of(item: dict) -> list[int]:
    if "marks" in item:
        return list(item["marks"])
    if "mark" in item:
        return [item["mark"]]
    return []


# ---------------------------------------------------------------------------
# Cover
# ---------------------------------------------------------------------------


def update_cover(wb, args: argparse.Namespace) -> None:
    cover = wb["Cover"]
    cover["B4"] = "RespiraAMS"
    cover["B5"] = "RespiraAMS"
    cover["F4"] = args.creator
    cover["F5"] = "=INDEX(A11:A1000,COUNTA(A11:A1000))"
    cover["F6"] = "=INDEX(B11:B1000,COUNTA(B11:B1000))"
    cover["A11"] = args.date
    cover["B11"] = args.version
    cover["C11"] = "All"
    cover["D11"] = "A"
    cover["E11"] = "Create document"
    cover["F11"] = "N/A"
    for row in range(12, 15):
        for col in range(1, 7):
            cover.cell(row=row, column=col).value = None


# ---------------------------------------------------------------------------
# Functions
# ---------------------------------------------------------------------------


def update_functions(wb, functions: list[dict], args: argparse.Namespace) -> None:
    ws = wb["Functions"]
    ws["E4"] = "=Cover!B4"
    ws["E5"] = "=Cover!B5"
    ws["E6"] = args.normal_cases_per_kloc
    ws["E7"] = (
        ".NET 10 SDK (net10.0)\n"
        "xUnit v3 with Microsoft Testing Platform\n"
        "Testcontainers PostgreSQL for integration tests\n"
        "Moq mocking framework"
    )

    template_first = 11
    template_rows = 258  # rows 11-268
    needed = len(functions)

    if needed > template_rows:
        extra = needed - template_rows
        ws.insert_rows(template_first + template_rows, extra)
        for row in range(template_first + template_rows, template_first + template_rows + extra):
            for col in range(1, 9):
                copy_style(
                    ws.cell(row=template_first + template_rows - 1, column=col),
                    ws.cell(row=row, column=col),
                )

    clear_values(ws, template_first, template_first + max(template_rows, needed), 8)
    for row in range(template_first, template_first + needed):
        for col in range(1, 9):
            copy_style(ws.cell(row=template_first, column=col), ws.cell(row=row, column=col))

    for index, function in enumerate(functions, start=1):
        row = 10 + index
        ws.cell(row=row, column=1, value=index)
        ws.cell(row=row, column=2, value=function["module"])
        ws.cell(row=row, column=3, value=function["class"])
        ws.cell(row=row, column=4, value=function["function"])
        ws.cell(row=row, column=5, value=function["code"])
        ws.cell(row=row, column=6, value=function.get("sheet", ""))
        ws.cell(row=row, column=7, value=f"Tests for {function['code']}")
        ws.cell(
            row=row,
            column=8,
            value=(
                "Various validation, state, and exception scenarios for "
                f"{function['function']}"
            ),
        )

    last_row = 10 + needed
    template_last = template_first + template_rows - 1
    if last_row < template_last:
        ws.delete_rows(last_row + 1, template_last - last_row)


# ---------------------------------------------------------------------------
# Statistics
# ---------------------------------------------------------------------------


def find_subtotal_row(ws) -> int:
    for row in range(11, min(ws.max_row, 2000) + 1):
        value = ws.cell(row=row, column=1).value
        if isinstance(value, str) and value.strip().lower() == "sub total":
            return row
    raise SystemExit("Statistics sheet has no 'Sub total' row.")


def update_statistics(
    wb, functions: list[dict], args: argparse.Namespace, total_cases: int
) -> int:
    ws = wb["Statistics"]
    ws["F4"] = args.creator

    failed_total = sum(function["failed"] for function in functions)
    untested_total = sum(function["untested"] for function in functions)
    passed_total = total_cases - failed_total - untested_total
    if failed_total == 0 and untested_total == 0:
        summary = (
            f"All {total_cases:,} automated unit tests were executed and passed "
            f"on {args.date}."
        )
    else:
        summary = (
            f"All {total_cases:,} automated unit tests were executed on "
            f"{args.date}: {passed_total:,} passed, {failed_total:,} failed, "
            f"{untested_total:,} untested."
        )
    ws["B7"] = summary

    old_subtotal = find_subtotal_row(ws)
    template_data_rows = old_subtotal - 12
    if len(functions) < 12:
        raise SystemExit(
            "The Statistics template keeps its coverage block at rows 18-22; "
            "at least 12 functions are required so the subtotal follows it."
        )

    # openpyxl does not move merged ranges on insert/delete; rebuild this merge.
    for merged_range in list(ws.merged_cells.ranges):
        if merged_range.min_row == old_subtotal:
            ws.unmerge_cells(str(merged_range))

    subtotal = 12 + len(functions)
    if len(functions) > template_data_rows:
        extra = len(functions) - template_data_rows
        ws.insert_rows(old_subtotal, extra)

    clear_values(ws, 12, max(template_data_rows, len(functions)) + 11, 9)
    for row in range(12, subtotal):
        for col in range(1, 10):
            copy_style(ws.cell(row=12, column=col), ws.cell(row=row, column=col))

    for index, function in enumerate(functions, start=1):
        row = 11 + index
        ws.cell(row=row, column=1, value=index)
        ws.cell(row=row, column=2, value=function["code"])
        ws.cell(row=row, column=3, value=function["passed"])
        ws.cell(row=row, column=4, value=function["failed"])
        ws.cell(row=row, column=5, value=function["untested"])
        ws.cell(row=row, column=6, value=function["n"])
        ws.cell(row=row, column=7, value=function["a"])
        ws.cell(row=row, column=8, value=function["b"])
        ws.cell(row=row, column=9, value=function["total"])

    for col in range(1, 10):
        copy_style(ws.cell(row=12, column=col), ws.cell(row=subtotal, column=col))
    ws.cell(row=subtotal, column=1, value="Sub total")
    ws.cell(row=subtotal, column=2, value=None)
    for col in range(3, 10):
        letter = get_column_letter(col)
        ws.cell(
            row=subtotal,
            column=col,
            value=f"=SUM({letter}12:{letter}{subtotal - 1})",
        )
    ws.merge_cells(start_row=subtotal, start_column=1, end_row=subtotal, end_column=2)

    # Drop the leftover template rows below the new subtotal.
    if subtotal < old_subtotal:
        ws.delete_rows(subtotal + 1, old_subtotal - subtotal)

    if subtotal < 23:
        raise SystemExit(
            "The Statistics template keeps the coverage block at rows 18-22; "
            "with fewer than 12 functions the subtotal lands inside it."
        )

    ws["M18"] = f"=(C{subtotal}+D{subtotal})*100/(I{subtotal})"
    ws["M19"] = f"=C{subtotal}*100/(I{subtotal})"
    ws["M20"] = f"=F{subtotal}*100/I{subtotal}"
    ws["M21"] = f"=G{subtotal}*100/I{subtotal}"
    ws["M22"] = f"=H{subtotal}*100/I{subtotal}"

    ws.auto_filter.ref = f"E11:E{subtotal - 1}"

    charts = ws._charts
    if len(charts) != 2:
        raise SystemExit(f"Expected 2 Statistics charts, found {len(charts)}")
    charts[0].ser[0].val.numRef.f = f"Statistics!$F${subtotal}:$H${subtotal}"
    charts[0].ser[0].cat.strRef.f = "Statistics!$F$11:$H$11"
    charts[1].ser[0].val.numRef.f = f"Statistics!$C${subtotal}:$E${subtotal}"
    charts[1].ser[0].cat.strRef.f = "Statistics!$C$11:$E$11"

    return subtotal


# ---------------------------------------------------------------------------
# Detail sheet
# ---------------------------------------------------------------------------


def rename_detail_sheet(wb, sheet_name: str) -> None:
    if DETAIL_TEMPLATE_SHEET not in wb.sheetnames:
        raise SystemExit(
            f"Template detail sheet {DETAIL_TEMPLATE_SHEET!r} not found; the "
            "template must still contain that sheet."
        )
    ws = wb[DETAIL_TEMPLATE_SHEET]
    ws.title = sheet_name
    for defined_name in ws.defined_names.values():
        if defined_name.attr_text and DETAIL_TEMPLATE_SHEET in defined_name.attr_text:
            defined_name.attr_text = defined_name.attr_text.replace(
                DETAIL_TEMPLATE_SHEET, sheet_name
            )


def build_detail_sheet(
    ws,
    detail_record: dict,
    spec: dict,
    cases: list[dict],
    args: argparse.Namespace,
) -> None:
    total = len(cases)
    if total < 1:
        raise SystemExit("A detail sheet needs at least one UTCID.")

    preconditions = spec["preconditions"]
    inputs = spec["inputs"] or [
        {"text": "", "marks": [index]} for index in range(1, total + 1)
    ]
    returns = spec["returns"]
    exception_marks = spec.get("exception_marks", [])

    if len(preconditions) > 5:
        raise SystemExit("The template supports at most 5 precondition rows.")
    if len(returns) > 5:
        raise SystemExit("The template supports at most 5 return rows.")
    for item in inputs:
        assert_mark_range(marks_of(item), total, "Input")
    for item in preconditions:
        assert_mark_range(marks_of(item), total, "Precondition")
    for item in returns:
        assert_mark_range(marks_of(item), total, "Return")
    assert_mark_range(exception_marks, total, "Exception")

    # Compact layout: no blank rows between the table's parts.
    row_utc = 9
    row_condition = row_utc + 1
    first_precondition = row_condition + 1
    row_input_header = first_precondition + len(preconditions)
    first_input = row_input_header + 1
    row_confirm = first_input + len(inputs)
    first_return = row_confirm + 1
    row_exception_header = first_return + len(returns)
    row_exception_text = row_exception_header + 1
    row_result = row_exception_text + 1
    row_defect = row_result + 3

    # Snapshot the template's own cell styles and the header labels before the
    # table is cleared; the styles are re-applied role by role below.
    labels = {coord: ws[coord].value for coord in DETAIL_LABELS}
    role_styles = {
        role: [copy(ws.cell(row=row, column=col)._style) for col in range(1, DETAIL_TABLE_COLUMNS + 1)]
        for role, row in DETAIL_ROLE_ROWS.items()
    }

    table_columns = 4 + total
    strip_columns = max(DETAIL_TABLE_COLUMNS, 4 + total)
    old_table_end = max(DETAIL_ROLE_ROWS["defect"], row_defect)

    clear_values(ws, 2, old_table_end, 41)
    for coord, value in labels.items():
        ws[coord] = value

    # Wipe the whole old table area so shrunk tables leave no stray borders or
    # row heights behind.
    for row in range(row_utc, old_table_end + 1):
        for col in range(1, strip_columns + 1):
            cell = ws.cell(row=row, column=col)
            cell.value = None
            cell.font = Font(name="Tahoma", size=8)
            cell.border = Border()
            cell.fill = PatternFill(fill_type=None)
            cell.alignment = Alignment()
            cell.number_format = "General"

    def apply_role(row: int, role: str) -> None:
        for col in range(1, table_columns + 1):
            source_col = col if col <= DETAIL_TABLE_COLUMNS else DETAIL_TABLE_COLUMNS
            ws.cell(row=row, column=col)._style = copy(role_styles[role][source_col - 1])
        ws.row_dimensions[row].height = DETAIL_ROLE_HEIGHTS[role]

    # Header block.
    ws["C2"] = spec["function_code"]
    ws["L2"] = spec["function_code"]
    ws["C3"] = args.creator
    ws["L3"] = args.creator
    ws["C4"] = spec["lines_of_code"]
    ws["L4"] = '=IF(Functions!E6<>"N/A",SUM(C4*Functions!E6/1000,-O7),"N/A")'
    ws["C5"] = spec["requirement"]

    passed = detail_record["passed"]
    failed = detail_record["failed"]
    untested = detail_record["untested"]
    counts = Counter(case["type"] for case in cases)
    normal, abnormal, boundary = counts.get("N", 0), counts.get("A", 0), counts.get("B", 0)

    ws["A7"] = passed
    ws["C7"] = failed
    ws["F7"] = untested
    ws["L7"] = normal
    ws["M7"] = abnormal
    ws["N7"] = boundary
    ws["O7"] = total

    # One UTCID column per case.
    apply_role(row_utc, "utc_header")
    for index in range(total):
        ws.cell(row=row_utc, column=5 + index, value=f"UTCID{index + 1:02d}")

    apply_role(row_condition, "condition")
    ws.cell(row=row_condition, column=1, value="Condition")
    ws.cell(row=row_condition, column=2, value="Precondition")

    for offset, precondition in enumerate(preconditions):
        row = first_precondition + offset
        apply_role(row, "precondition")
        write_body_text(ws.cell(row=row, column=4), precondition["text"])
        for mark in marks_of(precondition):
            write_marker(ws, row, 4 + mark)

    apply_role(row_input_header, "input_header")
    ws.cell(row=row_input_header, column=2, value="Input")

    for offset, item in enumerate(inputs):
        row = first_input + offset
        apply_role(row, "input")
        write_body_text(ws.cell(row=row, column=4), item["text"])
        for mark in marks_of(item):
            write_marker(ws, row, 4 + mark)

    apply_role(row_confirm, "confirm")
    ws.cell(row=row_confirm, column=1, value="Confirm")
    ws.cell(row=row_confirm, column=2, value="Return")

    for offset, item in enumerate(returns):
        row = first_return + offset
        apply_role(row, "return")
        write_body_text(ws.cell(row=row, column=4), item["text"])
        for mark in marks_of(item):
            write_marker(ws, row, 4 + mark)

    apply_role(row_exception_header, "exception_header")
    ws.cell(row=row_exception_header, column=1, value="Exception")
    ws.cell(row=row_exception_header, column=2, value="Exception")

    # The exception text row always exists so the table never shows a blank
    # bordered line, even when no case reaches the exception path.
    apply_role(row_exception_text, "exception_text")
    write_body_text(ws.cell(row=row_exception_text, column=4), spec["exception_text"])
    for mark in exception_marks:
        write_marker(ws, row_exception_text, 4 + mark)

    apply_role(row_result, "result")
    ws.cell(row=row_result, column=1, value="Result")
    ws.cell(
        row=row_result,
        column=2,
        value="Type(N : Normal, A : Abnormal, B : Boundary)",
    )
    apply_role(row_result + 1, "passed")
    ws.cell(row=row_result + 1, column=2, value="Passed/Failed")
    apply_role(row_result + 2, "date")
    ws.cell(row=row_result + 2, column=2, value="Executed Date")
    apply_role(row_defect, "defect")
    ws.cell(row=row_defect, column=2, value="Defect ID")

    for index, case in enumerate(cases):
        ws.cell(row=row_result, column=5 + index, value=case["type"])
        ws.cell(row=row_result + 1, column=5 + index, value=case.get("result", "P"))
        ws.cell(row=row_result + 2, column=5 + index, value=args.date)
        ws.cell(row=row_defect, column=5 + index, value=None)

    # The role prototypes carry the template's marker emphasis on a few cells;
    # only real markers keep it, every other table cell stays Tahoma 8.
    for row in range(row_condition, row_defect + 1):
        for col in range(5, table_columns + 1):
            cell = ws.cell(row=row, column=col)
            if cell.value == "O":
                continue
            cell.font = Font(name="Tahoma", size=8)

    ws.column_dimensions["D"].width = 23.0
    for col in range(5, table_columns + 1):
        ws.column_dimensions[get_column_letter(col)].width = 6.63

    ws.freeze_panes = "A10"


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def main() -> None:
    args = parse_args()

    template_path = Path(args.template)
    output_path = Path(args.output)
    if not template_path.exists():
        raise SystemExit(f"Template not found: {template_path}")
    if template_path.resolve() == output_path.resolve():
        raise SystemExit("Refusing to overwrite the template. Choose another --output.")

    by_class = parse_trx(args.trx_dir)
    functions = build_inventory(by_class)
    total_cases = sum(function["total"] for function in functions)
    by_code = {function["code"]: function for function in functions}

    spec_paths = args.detail_spec or []
    if not spec_paths and not args.detail_function:
        args.detail_function = "AddDosageHandler.HandleAsync"
    if not spec_paths and not args.detail_function:
        raise SystemExit("Provide --detail-spec or --detail-function.")

    specs = [load_detail_spec(path, args.detail_function or "") for path in spec_paths]
    if not specs:
        specs = [load_detail_spec(None, args.detail_function)]

    applied: list[dict] = []
    used_sheets: set[str] = set()
    for spec in specs:
        code = spec.get("function_code")
        if not code:
            raise SystemExit("Each detail spec must define function_code.")
        record = by_code.get(code)
        if record is None:
            available = ", ".join(sorted(by_code)[:12])
            raise SystemExit(
                f"Detail function {code!r} not found in the TRX. "
                f"Examples: {available}"
            )
        cases = normalise_detail_cases(spec, record)
        record["n"] = sum(1 for case in cases if case["type"] == "N")
        record["a"] = sum(1 for case in cases if case["type"] == "A")
        record["b"] = sum(1 for case in cases if case["type"] == "B")
        record["sheet"] = spec["sheet_name"]
        if spec["sheet_name"] in used_sheets:
            raise SystemExit(f"Duplicate detail sheet name {spec['sheet_name']!r}.")
        used_sheets.add(spec["sheet_name"])
        applied.append({"record": record, "spec": spec, "cases": cases})

    for function in functions:
        if function["n"] + function["a"] + function["b"] != function["total"]:
            raise SystemExit(f"Classification mismatch for {function['code']}")

    primary_code = applied[0]["record"]["code"]
    # The first row of Functions is the function with the first detail sheet.
    functions.sort(
        key=lambda item: (item["module"], item["feature"], item["class"], item["function"])
    )
    functions.sort(key=lambda item: 0 if item["code"] == primary_code else 1)

    order = {function["code"]: index for index, function in enumerate(functions)}
    applied.sort(key=lambda item: order[item["record"]["code"]])

    wb = openpyxl.load_workbook(template_path)
    wb.properties.creator = args.creator
    wb.properties.lastModifiedBy = args.creator
    wb.properties.title = "RespiraAMS Unit Test Report"
    wb.properties.subject = "Unit test report for the RespiraAMS backend"
    wb.calculation.fullCalcOnLoad = True
    wb.calculation.calcMode = "auto"

    update_cover(wb, args)
    update_functions(wb, functions, args)
    subtotal = update_statistics(wb, functions, args, total_cases)

    # Rename the template sheet for the first detail, then clone it for the
    # remaining ones while it is still pristine. Copies share the same styles,
    # merges, row heights and column widths; sheet-local Google names stay with
    # the first sheet.
    rename_detail_sheet(wb, applied[0]["spec"]["sheet_name"])
    source_ws = wb[applied[0]["spec"]["sheet_name"]]
    for item in applied[1:]:
        copy_ws = wb.copy_worksheet(source_ws)
        copy_ws.title = item["spec"]["sheet_name"]

    for item in applied:
        build_detail_sheet(
            wb[item["spec"]["sheet_name"]],
            item["record"],
            item["spec"],
            item["cases"],
            args,
        )

    output_path.parent.mkdir(parents=True, exist_ok=True)
    wb.save(output_path)

    print(f"Wrote {output_path}")
    print(f"Functions: {len(functions)}; test cases: {total_cases}")
    print(f"Statistics subtotal row: {subtotal}")
    print(f"First function: {functions[0]['code']} -> {functions[0]['sheet']}")
    print(f"Detail sheets: {len(applied)}")
    for item in applied:
        record = item["record"]
        print(
            f"  {item['spec']['sheet_name']}: N={record['n']} A={record['a']} "
            f"B={record['b']} total={record['total']}"
        )


if __name__ == "__main__":
    main()
