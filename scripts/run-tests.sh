#!/usr/bin/env bash
# Runs every test project of the solution and prints a summary line per project.
#
# `dotnet test` cannot be used here: the Microsoft.Testing.Platform handshake fails
# in this environment, so the test executables are invoked directly instead.
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

TOTAL=0
FAILED=0

run_project() {
    local dir="$1"
    local exe="$2"
    local path="$ROOT/$dir/bin/Debug/net10.0/$exe"

    if [ ! -x "$path" ]; then
        printf '%-46s SKIPPED (not built)\n' "$dir"
        return
    fi

    local output
    output="$("$path" 2>&1)"
    local summary
    summary="$(printf '%s' "$output" | sed -e 's/\x1b\[[0-9;]*m//g' | grep -oE 'Total: [0-9]+, Errors: [0-9]+, Failed: [0-9]+, Skipped: [0-9]+' | head -1)"

    if [ -z "$summary" ]; then
        printf '%-46s NO SUMMARY\n' "$dir"
        printf '%s\n' "$output" | tail -20
        FAILED=1
        return
    fi

    local count errors failures
    count="$(printf '%s' "$summary" | grep -oE 'Total: [0-9]+' | grep -oE '[0-9]+')"
    errors="$(printf '%s' "$summary" | grep -oE 'Errors: [0-9]+' | grep -oE '[0-9]+')"
    failures="$(printf '%s' "$summary" | grep -oE 'Failed: [0-9]+' | grep -oE '[0-9]+')"

    TOTAL=$((TOTAL + count))
    if [ "$errors" != "0" ] || [ "$failures" != "0" ]; then
        FAILED=1
        printf '%-46s %s  <-- FAILURES\n' "$dir" "$summary"
        printf '%s\n' "$output" | sed -e 's/\x1b\[[0-9;]*m//g' | grep -E '\[FAIL\]' | head -20
    else
        printf '%-46s %s\n' "$dir" "$summary"
    fi
}

run_project "test/Respira.Authentication.Test/Domain.Test" "Respira.Authentication.Domain.Test"
run_project "test/Respira.Authentication.Test/Infrastructure.Test" "Respira.Authentication.Infrastructure.Test"
run_project "test/Respira.Clinical.Test/Application.Test" "Respira.Clinical.Application.Test"
run_project "test/Respira.Clinical.Test/Domain.Test" "Respira.Clinical.Domain.Test"
run_project "test/Respira.Clinical.Test/Infrastructure.Test" "Respira.Clinical.Infrastructure.Test"
run_project "test/Respira.Patient.Test/Application.Test" "Respira.Patient.Application.Test"
run_project "test/Respira.Patient.Test/Domain.Test" "Respira.Patient.Domain.Test"
run_project "test/Respira.Doctor.Test/Application.Test" "Respira.Doctor.Application.Test"
run_project "test/Respira.Doctor.Test/Domain.Test" "Respira.Doctor.Domain.Test"
run_project "test/Respira.Doctor.Test/Infrastructure.Test" "Respira.Doctor.Infrastructure.Test"
run_project "test/Respira.Analysis.Test/Application.Test" "Respira.Analysis.Application.Test"
run_project "test/Respira.Analysis.Test/Domain.Test" "Respira.Analysis.Domain.Test"
run_project "test/Respira.Analysis.Test/Infrastructure.Test" "Respira.Analysis.Infrastructure.Test"
run_project "test/Respira.Saga.Test/Application.Test" "Respira.Saga.Application.Test"
run_project "test/Respira.Saga.Test/Domain.Test" "Respira.Saga.Domain.Test"
run_project "test/Respira.Saga.Test/Infrastructure.Test" "Respira.Saga.Infrastructure.Test"
run_project "test/Respira.ServiceDefaults.Test/Application.Test" "Respira.ServiceDefaults.Application.Test"

echo "-----------------------------------------------------------------"
echo "TOTAL TESTS: $TOTAL"
if [ "$FAILED" != "0" ]; then
    echo "RESULT: FAILURES PRESENT"
    exit 1
fi
echo "RESULT: ALL GREEN"
