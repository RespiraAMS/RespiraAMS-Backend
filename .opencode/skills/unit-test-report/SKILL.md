---
name: Unit Test Report
description: Create or regenerate the RespiraAMS Excel unit test report from docs/tests/UnitTestReportTemplate.xlsx and TRX test results. Use when asked to build, update, or regenerate the unit test report, Functions/Statistics sheets, test coverage workbook, or a UTCID detail sheet for a function.
---

# Unit Test Report

Build the Excel unit test report for this repository without changing source or
test code. The output follows `docs/tests/UnitTestReportTemplate.xlsx`:
`Guideline`, `Cover`, `Functions`, `Statistics`, and one detail (UTCID) sheet
per function that has a detail spec. The spec for the first function is forced
to the first row of `Functions`; the other specs follow in `Functions` order.

## Defaults

| Item | Value |
| --- | --- |
| Template | `docs/tests/UnitTestReportTemplate.xlsx` |
| Output | `docs/tests/UnitTestReport.xlsx` |
| Detail specs | Repeat `--detail-spec`; the default first function uses `references/add-dosage-detail.json` |
| Default first detail | `AddDosageHandler.HandleAsync` |
| Creator | `DangLNH` |
| Issue date | Current report date, formatted `dd/MM/yyyy` |
| Version | `1.0.0` |
| Normal cases/KLOC | `100` |

Never overwrite the template. `--detail-spec` is repeatable: each spec creates
one detail sheet. Without any spec, `--detail-function` names the single
function and its counts use the heuristic classification.

## Workflow

1. **Find the test inventory.**
   - Prefer fresh TRX files from a complete run. If results already exist,
     confirm they are current before reusing them.
   - Build first:

     ```bash
     dotnet build Respira.slnx
     ```

   - Run each test executable directly. This project uses xUnit v3 / Microsoft
     Testing Platform; `dotnet test --project ...` can return a handshake error
     or zero tests. The working pattern is:

     ```bash
     test/Respira.Clinical.Test/Application.Test/bin/Debug/net10.0/Respira.Clinical.Application.Test \
       -noLogo -reporter quiet -result-trx /tmp/opencode/trx/clinical.trx
     ```

     Repeat for every test executable under `test/**/bin/Debug/net10.0/`
     (Analysis, Authentication, Clinical, Doctor, Patient, Saga, and
     ServiceDefaults application/domain/infrastructure). A failed run is still
     useful: capture the TRX and report failures instead of hiding them.
   - The report covers all of those projects: 263 function rows and 3,517 tests
     in the current run. `MODULES` maps each test project to its Requirement
     Name; a new test project must be added there.

2. **Parse the TRX results.** The generator does this for you:
   - `testName` is `Namespace.TestClass.TestMethod(...)`; parameters are
     stripped with `split("(", 1)[0]`, then the class/method split with
     `rsplit(".", 1)`.
   - Results are deduplicated by full `testName`, so reruns do not double-count.
   - Counts come from outcomes: `Passed`, `Failed`, everything else = untested.

3. **Map test classes to production functions.** The generator applies:

   | Test class suffix | Production class | Function |
   | --- | --- | --- |
   | `*HandlerTest` | `*Handler` | `HandleAsync` |
   | `*MapperTest` | `*Mapper` | `Map` |
   | `*ValidatorTest` | `*Validator` | `Validate` |
   | `*MiddlewareTest` | `*Middleware` | `InvokeAsync` |

   Class names without those suffixes live in `SPECIAL_FUNCTIONS` inside
   `scripts/build_report.py`. The script fails loudly for an unknown class; add
   it there instead of guessing. Function code is `{Class}.{Function}`.

4. **Choose and document the functions that get detail sheets.**
   - The detail sheet must belong to a row of `Functions`. The first spec is
     forced to row 1; every later spec keeps its natural `Functions` order.
   - Read the production file for `Lines of code` and the requirement summary.
   - Read the test class and build a detail spec, one UTCID per executed test
     case. Copy the schema from `references/add-dosage-detail.json`:

     ```json
     {
       "function_code": "AddDosageHandler.HandleAsync",
       "sheet_name": "AddDosageHandler",
       "lines_of_code": 65,
       "requirement": "One-paragraph requirement summary.",
       "preconditions": [{ "text": "...", "marks": [1, 7] }],
       "inputs": [{ "text": "...", "marks": [1] }],
       "returns": [{ "text": "...", "marks": [1, 2, 3] }],
       "exception_text": "No unhandled exception",
       "exception_marks": [4, 5],
       "cases": [{ "method": "AddDosage_StandardDoseToNewRoute_Success",
                   "type": "N", "result": "P" }]
     }
     ```

     `marks` are 1-based UTCID numbers and may repeat across rows. The `cases`
     list is ordered: case 1 is `UTCID01`. `method` must match a TRX method for
     the class; duplicate method names are fine for `[Theory]` cases. `type` is
     `N` (normal), `A` (abnormal/failure/exception), `B` (boundary), and
     `result` is `P` or `F`.
     - The number of cases must equal the class's TRX test count, and
       `N + A + B` must equal the counts written to `Statistics`.
     - The template supports at most 5 preconditions, 5 return rows, and at
       least one UTCID.
     - Classification for spec-less functions is heuristic (keywords); a spec
       is authoritative for its function.
   - `references/` documents 209 functions, one spec per function: the first
     ten `Functions` rows (`add-dosage-detail` plus the nine Analysis specs)
     and the previously completed batches (Antibiograms, antibiotics,
     clinical metrics/variables, criteria, diagnosis, pathogens, risk factors,
     formula/range, suspected causes, treatment protocols, patients/treatment
     plans, domain predicates, saga and domain utilities). The row numbers in
     those batches moved when the inventory grew to 263 rows; the `SPECS` list
     below is the authoritative set of file names and the spec's
     `function_code` selects its row.

5. **Generate the workbook.**

   Repeat `--detail-spec` once per detail sheet. The order of the flags does
   not matter: the first spec is placed at `Functions` row 1 and the rest follow
   `Functions` order.

   ```bash
   SKILL=.opencode/skills/unit-test-report/references
   SPECS=(
     add-dosage-detail
     audit-logging-add create-analysis-report-handler
     delete-analysis-report-handler analysis-report-excel-writer
     export-analysis-report-handler get-analysis-report-handler
     get-paged-analysis-report-handler clinical-analysis-constructor
     clinical-analysis-report-json
     create-antibiogram-handler create-antibiogram-mapper create-antibiogram-validator
     delete-antibiogram-handler delete-antibiogram-validator
     get-paged-antibiogram-handler get-paged-antibiogram-validator
     update-antibiogram-handler update-antibiogram-mapper update-antibiogram-validator
     create-antibiotic-group-handler create-antibiotic-group-mapper create-antibiotic-group-validator
     delete-antibiotic-group-handler delete-antibiotic-group-validator
     get-antibiotic-groups-handler get-paged-antibiotic-group-handler
     search-antibiotic-group-handler search-antibiotic-group-validator
     update-antibiotic-group-handler update-antibiotic-group-mapper update-antibiotic-group-validator
     add-dosage-mapper add-dosage-validator
     create-antibiotic-handler create-antibiotic-mapper create-antibiotic-validator
     delete-antibiotic-handler delete-antibiotic-validator
     delete-dosage-handler delete-dosage-validator
     get-antibiotic-by-id-handler get-antibiotic-by-id-validator
     get-antibiotics-handler get-paged-antibiotic-handler get-paged-antibiotic-validator
     search-antibiotic-handler search-antibiotic-validator
     update-antibiotic-handler
     update-antibiotic-mapper update-antibiotic-validator
     update-dosage-handler update-dosage-mapper update-dosage-validator
     add-metric-rule-handler add-metric-rule-mapper add-metric-rule-validator
     create-clinical-metric-handler create-clinical-metric-mapper
     create-clinical-metric-validator
     delete-clinical-metric-handler delete-clinical-metric-validator
     delete-metric-rule-handler delete-metric-rule-validator
     get-clinical-metric-by-id-handler get-clinical-metric-by-id-validator
     get-metric-rule-by-id-handler get-metric-rule-by-id-validator
     get-paged-clinical-metric-handler
     get-paged-clinical-metric-validator
     search-clinical-metric-handler search-clinical-metric-validator
     update-clinical-metric-handler update-clinical-metric-mapper
     update-clinical-metric-validator
     update-metric-rule-handler update-metric-rule-mapper
     update-metric-rule-validator
     create-clinical-variable-handler create-clinical-variable-mapper
     create-clinical-variable-validator
     delete-clinical-variable-handler delete-clinical-variable-validator
     get-clinical-variable-by-id-handler get-clinical-variable-by-id-validator
     get-clinical-variables-handler get-paged-clinical-variable-handler
     get-paged-clinical-variable-validator search-clinical-variable-handler
     search-clinical-variable-validator update-clinical-variable-handler
     update-clinical-variable-mapper update-clinical-variable-validator
     create-criterion-handler create-criterion-mapper create-criterion-validator
     delete-criterion-handler delete-criterion-validator get-criteria-handler
     get-criterion-by-id-handler get-criterion-by-id-validator
     get-paged-criterion-handler get-paged-criterion-validator
     get-referenced-entities-handler get-referenced-entities-validator
     update-criterion-handler update-criterion-mapper update-criterion-validator
     confirm-treatment-handler confirm-treatment-validator
     diagnose-handler diagnose-validator get-diagnose-form-handler
     warn-antibiotic-combination-handler warn-antibiotic-combination-validator
     create-pathogen-handler create-pathogen-mapper create-pathogen-validator
     delete-pathogen-handler delete-pathogen-validator
     get-paged-pathogens-handler get-paged-pathogens-validator
     get-pathogens-handler search-pathogen-handler search-pathogen-validator
     update-pathogen-handler update-pathogen-mapper update-pathogen-validator
     create-risk-factor-handler create-risk-factor-mapper
     create-risk-factor-validator delete-risk-factor-handler
     delete-risk-factor-validator get-paged-risk-factor-handler
     get-paged-risk-factor-validator get-risk-factor-by-id-handler
     get-risk-factor-by-id-validator update-risk-factor-handler
     update-risk-factor-mapper update-risk-factor-validator
     formula-dto-mapper formula-model-mapper formula-validator range-validator
     create-suspected-cause-handler create-suspected-cause-mapper
     create-suspected-cause-validator delete-suspected-cause-handler
     delete-suspected-cause-validator get-paged-suspected-cause-handler
     update-suspected-cause-handler update-suspected-cause-mapper
     update-suspected-cause-validator create-treatment-protocol-handler
     create-treatment-protocol-mapper create-treatment-protocol-validator
     delete-treatment-protocol-handler delete-treatment-protocol-validator
     get-paged-treatment-protocol-handler get-paged-treatment-protocol-validator
     update-treatment-protocol-handler update-treatment-protocol-mapper
     update-treatment-protocol-validator antibiotic-is-antibiotic-dosage-valid
     clinical-metric-is-scoring-metric criterion-is-criterion-satisfied
     expression-evaluate formula-to-expression range-is-in-range
     diagnose-service-diagnose formula-serializer-serialize
     add-next-of-kin-handler add-next-of-kin-mapper add-next-of-kin-validator
     create-patient-handler create-patient-mapper create-patient-validator
     delete-next-of-kin-handler delete-next-of-kin-validator
     delete-patient-handler delete-patient-validator
     discharge-patient-handler discharge-patient-validator
     get-paged-patient-handler get-paged-patient-validator
     get-patient-by-id-handler get-patient-by-id-validator
     search-patient-handler search-patient-validator
     update-next-of-kin-handler update-next-of-kin-mapper
     update-next-of-kin-validator update-patient-handler
     update-patient-validator close-treatment-plan-handler
     close-treatment-plan-validator get-treatment-plan-by-id-handler
     review-treatment-plan-handler review-treatment-plan-validator
     upsert-treatment-plan-handler upsert-treatment-plan-middleware
     upsert-treatment-plan-validator health-insurance-card-validator
     name-normalizer phone-number-validator
     record-saga-audit-event-handler search-saga-audit-validator
     create-saga-start profile-creation-policy
   )
   args=()
   for spec in "${SPECS[@]}"; do args+=(--detail-spec "$SKILL/$spec.json"); done

   /tmp/opencode/venv/bin/python .opencode/skills/unit-test-report/scripts/build_report.py \
     --trx-dir /tmp/opencode/trx "${args[@]}" \
     --date 10/11/2026 --creator DangLNH
   ```

   That list produces 209 detail sheets: rows 1-10 of `Functions` plus the
   completed batches. Drop entries to build a smaller workbook.
   `Functions` and `Statistics` list one row per function code; the current
   inventory has 263 rows and 3,517 tests. The extra rows come from the
   Analysis/Authentication/Doctor/ServiceDefaults suites and from the saga flow
   suites, which add Delete/Update saga and audit-query functions while the
   create-saga flow suite merges into `CreateSaga.StartAsync` (31 tests on one
   row and one detail sheet).
   If `openpyxl` is missing:

   ```bash
   python3 -m venv /tmp/opencode/venv
   /tmp/opencode/venv/bin/pip install openpyxl
   ```

   Use `--output /tmp/opencode/preview.xlsx` while experimenting so the
   reviewed report is only replaced when the result is approved.

6. **Validate before returning.**

   ```bash
   /tmp/opencode/venv/bin/python .opencode/skills/unit-test-report/scripts/validate_report.py \
     /tmp/opencode/preview.xlsx \
     --trx-dir /tmp/opencode/trx \
     --detail-function AddDosageHandler.HandleAsync
   ```

   `--detail-function` is optional; the validator treats every sheet after
   `Statistics` as a detail sheet and cross-checks it against its `Functions`
   row. Then execute the validation checklist below.

## Formatting rules

- Preserve the template's fonts, fills, borders, merges, row heights, and
  column widths unless a change is required to fit more UTCIDs.
- Body/table text is **Tahoma 8**; header cells keep the template dark-blue
  fill with bold white text.
- **Every `O` marker in a UTCID column must be Tahoma 11 bold, horizontally and
  vertically centered, with thin borders and no fill.** Table cells copy the
  style of a template row role, which may carry another cell's marker emphasis,
  so the generator writes markers through `write_marker()` and resets every
  non-marker table cell to Tahoma 8; never rely on the copied style.
- `clear_values()` wipes merged-range anchors such as `A2`, `A5`, `A6`, `F2`,
  `F4`, `L6`. Snapshot and restore the detail labels (`DETAIL_LABELS` in the
  generator), otherwise the header block loses `Function Code`,
  `Function Name`, `Created By`, `Lines of code`, `Test requirement`,
  `Passed`, `Failed`, `Untested`, and `N/A/B`.
- Keep `Cover!F5` and `Cover!F6` as formulas:
  - `=INDEX(A11:A1000,COUNTA(A11:A1000))`
  - `=INDEX(B11:B1000,COUNTA(B11:B1000))`
- Do not add decorative formatting that is not in the template.

## Validation checklist

- [ ] Workbook opens without repair and `unzip -t` reports no errors.
- [ ] Sheet order is `Guideline`, `Cover`, `Functions`, `Statistics`, then one
      detail sheet per function with a spec.
- [ ] `Functions` has one contiguous row per test class, no leftovers, and the
      first row is the first detail function with its `Sheet Name` set.
- [ ] Every `Functions` row with a `Sheet Name` has a matching detail sheet,
      and every detail sheet maps back to its `Functions` row.
- [ ] Every `Statistics` row satisfies `N + A + B = Total Test Cases` and
      `Passed + Failed + Untested = Total Test Cases`.
- [ ] Statistics totals equal the deduplicated TRX outcome counts.
- [ ] Subtotal row formulas, `M18:M22`, and both pie chart ranges point at the
      actual subtotal row; autofilter ends at the last data row.
- [ ] Every detail sheet has its header labels, counts and `Lines of code`
      match its spec, and `L4` keeps the `Lack of test cases` formula.
- [ ] Every detail sheet has one UTCID column per test case; columns past `K`
      are styled like column `K`.
- [ ] Every detail table is one contiguous block: no blank bordered rows, the
      right and bottom borders close on the last UTCID column and the
      `Defect ID` row, and no styled cells remain below the table.
- [ ] All `O` markers on every detail sheet are Tahoma 11 bold and centered.
- [ ] `Cover` has one change-log row and the issue-date/version formulas
      resolve as intended.
- [ ] No `GetConfigurationsAsync` references remain in sheets or defined names.
- [ ] Template file is unchanged; output is a separate workbook.

## Pitfalls

- `insert_rows()` / `delete_rows()` do not update chart references, defined
  names, autofilter, or merged ranges. Rebuild them explicitly; a merged
  `A{B}:B{B}` subtotal label disappears otherwise.
- `insert_rows()` does not copy cell styles. Copy the style of a data row into
  inserted rows before writing values.
- The detail table is rebuilt from the template's row roles (`DETAIL_ROLE_ROWS`
  in the generator), not by inserting rows. Each part of the table copies the
  style of its role row, so there are no blank bordered precondition/return
  rows and the Result block never falls below the table's bottom border: the
  last row is always the bordered `Defect ID` row.
- A detail sheet can be much taller than the template: input rows grow by one
  per UTCID (`DiagnoseService` has 194 cases). The validator scans to
  `detail.max_row`, not a fixed 120 rows.
- One UTCID column per case. Columns past `K` copy column `K`; with fewer than
  seven cases the table ends at the last real UTCID, so the header band and the
  right border close on that column.
- Excel caps sheet names at 31 characters. When the class name is too long the
  spec uses a shorter `sheet_name` (`ClinicalVariableByIdValidator`,
  `PagedClinicalVariableValidator`, `WarnAntibioticComboHandler`,
  `WarnAntibioticComboValidator`, `CreateTreatmentProtoValidator`,
  `DeleteTreatmentProtoValidator`, `GetPagedTreatmentProtoHandler`,
  `GetPagedTreatmentProtoValidator`, `UpdateTreatmentProtoValidator`); the
  generator warns but does not enforce the limit, so check new spec names.
- `clear_values()` must skip `MergedCell` objects; assigning to one raises.
- The template's row/column dimensions extend to row 1000. Validate against the
  actual data range, not `max_row`.
- Google Sheets artifacts include hidden sheet-local defined names containing
  the old detail sheet name; rename them when renaming the sheet.
- `copy_worksheet()` clones styles, merges, row heights, and column widths but
  not sheet-local defined names or charts. Rename the template sheet for the
  first spec, clone it while it is still pristine for the remaining specs, then
  fill each sheet; clone only as many copies as there are extra specs.
- Cloning an already-rendered page to add a new sheet copies the wrong role
  rows: template rows 9-28 then hold that page's inputs instead of the role
  prototypes. `build_detail_sheet()` reads those rows for its styles, so the
  result/executed-date/defect block loses the rotated date cells, the bold
  labels, and the bottom borders. Seed rows 9-28 from
  `UnitTestReportTemplate.xlsx` (`GetConfigurationsAsync`) before filling.
- Each spec is applied to its own record in `Functions`/`Statistics`, so the
  `N/A/B` counts written there come from the spec, not the keyword heuristic.
  All `N + A + B = Total` checks must still hold.
- The generator rejects a spec whose case count differs from its class's
  current TRX count. If a completed page's test class grew (for example a
  handler suite that gained idempotency or column-limit cases), the page has to
  be refreshed as well; updating only `Functions`/`Statistics` leaves that
  sheet's UTCID count stale.
- Do not hardcode an inventory dump; derive everything from the TRX so counts
  stay current.
- Several test classes can cover the same production function. `build_inventory`
  merges records that resolve to the same function code, so one Functions and
  Statistics row (and one detail spec) carries the combined test set. The saga
  flow suites are mapped that way in `SPECIAL_FUNCTIONS`; a flow suite that
  lacks a mapping stops the generator instead of being dropped. Domain and
  infrastructure suites whose names do not follow the Handler/Mapper/Validator/
  Middleware convention (entities, enums, DbContexts, services, DI helpers) also
  need an explicit `SPECIAL_FUNCTIONS` entry.
- The `Statistics` template has 258 data rows and a subtotal at row 270. The
  generator inserts/deletes rows to fit and keeps the coverage block at rows
  18-22, so an inventory below 12 functions is rejected.

## Files

- `scripts/build_report.py` — generator for `Cover`, `Functions`, `Statistics`,
  and one detail sheet per `--detail-spec`.
- `scripts/validate_report.py` — structural validation of a generated report,
  including every detail sheet.
- `references/add-dosage-detail.json` — detail spec for the default first
  function.
- `references/audit-logging-add.json`,
  `references/create-analysis-report-handler.json`,
  `references/delete-analysis-report-handler.json`,
  `references/analysis-report-excel-writer.json`,
  `references/export-analysis-report-handler.json`,
  `references/get-analysis-report-handler.json`,
  `references/get-paged-analysis-report-handler.json`,
  `references/clinical-analysis-constructor.json`,
  `references/clinical-analysis-report-json.json` — detail specs for the
  Analysis functions in `Functions` rows 2-10.
- `references/create-antibiogram-*.json`, `delete-antibiogram-*.json`,
  `get-paged-antibiogram-*.json`, `update-antibiogram-*.json` — detail specs for
  the ten Antibiogram functions in `Functions` rows 2-11.
- `references/*antibiotic-group*.json` — detail specs for the ten
  AntibioticGroup functions in `Functions` rows 12-20.
- `references/update-antibiotic-group-*.json`, `references/add-dosage-mapper.json`,
  `references/add-dosage-validator.json`, `references/create-antibiotic-*.json`,
  `references/delete-antibiotic-*.json` — detail specs for the ten functions in
  `Functions` rows 21-30.
- `references/delete-dosage-*.json`, `references/get-antibiotic-by-id-*.json`,
  `references/get-antibiotics-handler.json`,
  `references/get-paged-antibiotic-*.json`,
  `references/search-antibiotic-*.json`, `references/update-antibiotic-handler.json`
  — detail specs for the ten functions in `Functions` rows 31-40.
- `references/update-antibiotic-mapper.json`,
  `references/update-antibiotic-validator.json`,
  `references/update-dosage-*.json`, `references/add-metric-rule-*.json`,
  `references/create-clinical-metric-*.json` — detail specs for the ten
  functions in `Functions` rows 41-50.
- `references/delete-clinical-metric-*.json`,
  `references/delete-metric-rule-*.json`,
  `references/get-clinical-metric-by-id-*.json`,
  `references/get-metric-rule-by-id-*.json`,
  `references/get-paged-clinical-metric-handler.json` — detail specs for the ten
  functions in `Functions` rows 51-60.
- `references/get-paged-clinical-metric-validator.json`,
  `references/search-clinical-metric-*.json`,
  `references/update-clinical-metric-*.json`,
  `references/update-metric-rule-*.json`,
  `references/create-clinical-variable-handler.json` — detail specs for the ten
  functions in `Functions` rows 61-70.
- `references/create-clinical-variable-*.json`,
  `references/delete-clinical-variable-*.json`,
  `references/get-clinical-variable-by-id-*.json`,
  `references/get-clinical-variables-handler.json`,
  `references/get-paged-clinical-variable-*.json`,
  `references/search-clinical-variable-handler.json` — detail specs for the ten
  functions in `Functions` rows 71-80.
- `references/search-clinical-variable-validator.json`,
  `references/update-clinical-variable-*.json`,
  `references/create-criterion-*.json`,
  `references/delete-criterion-*.json`, `references/get-criteria-handler.json`
  — detail specs for the ten functions in `Functions` rows 81-90.
- `references/get-criterion-by-id-*.json`,
  `references/get-paged-criterion-*.json`,
  `references/get-referenced-entities-*.json`,
  `references/update-criterion-*.json`,
  `references/confirm-treatment-handler.json` — detail specs for the ten
  functions in `Functions` rows 91-100.
- `references/confirm-treatment-validator.json`, `references/diagnose-*.json`,
  `references/get-diagnose-form-handler.json`,
  `references/warn-antibiotic-combination-*.json`,
  `references/create-pathogen-*.json`, `references/delete-pathogen-handler.json`
  — detail specs for the ten functions in `Functions` rows 101-110.
- `references/delete-pathogen-validator.json`,
  `references/get-paged-pathogens-*.json`, `references/get-pathogens-handler.json`,
  `references/search-pathogen-*.json`, `references/update-pathogen-*.json`,
  `references/create-risk-factor-handler.json` — detail specs for the ten
  functions in `Functions` rows 111-120.
- `references/create-risk-factor-*.json`, `references/delete-risk-factor-*.json`,
  `references/get-paged-risk-factor-*.json`,
  `references/get-risk-factor-by-id-*.json`,
  `references/update-risk-factor-*.json` — detail specs for the ten functions in
  `Functions` rows 121-130.
- `references/update-risk-factor-validator.json`,
  `references/formula-dto-mapper.json`, `references/formula-model-mapper.json`,
  `references/formula-validator.json`, `references/range-validator.json`,
  `references/create-suspected-cause-*.json`,
  `references/delete-suspected-cause-*.json` — detail specs for the ten
  functions in `Functions` rows 131-140.
- `references/get-paged-suspected-cause-handler.json`,
  `references/update-suspected-cause-*.json`,
  `references/create-treatment-protocol-*.json`,
  `references/delete-treatment-protocol-*.json`,
  `references/get-paged-treatment-protocol-handler.json` — detail specs for the
  ten functions in `Functions` rows 141-150.
- `references/get-paged-treatment-protocol-validator.json`,
  `references/update-treatment-protocol-*.json`,
  `references/antibiotic-is-antibiotic-dosage-valid.json`,
  `references/clinical-metric-is-scoring-metric.json`,
  `references/criterion-is-criterion-satisfied.json`,
  `references/expression-evaluate.json`,
  `references/formula-to-expression.json`, `references/range-is-in-range.json`
  — detail specs for the ten functions in `Functions` rows 151-160.
- `references/diagnose-service-diagnose.json`,
  `references/formula-serializer-serialize.json`,
  `references/add-next-of-kin-*.json`,
  `references/create-patient-*.json`,
  `references/delete-next-of-kin-*.json` — detail specs for the ten functions in
  `Functions` rows 161-170.
- `references/delete-patient-*.json`,
  `references/discharge-patient-*.json`,
  `references/get-paged-patient-*.json`,
  `references/get-patient-by-id-*.json`,
  `references/search-patient-*.json` — detail specs for the ten functions in
  `Functions` rows 171-180.
- `references/update-next-of-kin-*.json`,
  `references/update-patient-*.json`,
  `references/close-treatment-plan-*.json`,
  `references/get-treatment-plan-by-id-handler.json`,
  `references/review-treatment-plan-*.json` — detail specs for the ten functions
  in `Functions` rows 181-190.
- `references/upsert-treatment-plan-*.json`,
  `references/health-insurance-card-validator.json`,
  `references/name-normalizer.json`, `references/phone-number-validator.json`,
  `references/record-saga-audit-event-handler.json`,
  `references/search-saga-audit-validator.json`,
  `references/create-saga-start.json`, `references/profile-creation-policy.json`
  — detail specs for the ten functions in `Functions` rows 191-200.
- `references/workbook-layout.md` — cell map, formulas, and layout arithmetic.
