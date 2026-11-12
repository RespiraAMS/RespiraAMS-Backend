#!/usr/bin/env bash
# Prints the number of test cases per feature (namespace) for each test project.
cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

for proj in Respira.Doctor Respira.Analysis Respira.Saga Respira.ServiceDefaults; do
    exe="test/$proj.Test/Application.Test/bin/Debug/net10.0/$proj.Application.Test"
    echo "=== $proj.Test ==="
    if [ ! -f "$exe" ]; then
        echo "  (not built)"
        continue
    fi
    "$exe" -list methods > /tmp/opencode/list.txt 2>&1
    sed -e 's/\x1b\[[0-9;]*m//g' /tmp/opencode/list.txt \
        | grep '^Respira' \
        | sed 's/\.[A-Za-z0-9_]*$//' \
        | sed 's/\.[A-Za-z0-9_]*Test$//' \
        | sort \
        | uniq -c \
        | awk '{printf "  %4d  %s\n", $1, $2}'
done
