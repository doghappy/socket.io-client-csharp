#!/bin/bash

if [ $# -lt 1 ]; then
  echo "Usage: $0 <project-path> [extra dotnet test args]"
  exit 1
fi

project="$1"
shift

count=1

while true
do
  echo "=============================="
  echo "Run #$count"
  echo "Project: $project"
  echo "Time: $(date)"
  echo "=============================="

  dotnet test "$project" "$@"
  result=$?

  if [ $result -ne 0 ]; then
    echo "❌ Test failed on run #$count"
    exit $result
  fi

  count=$((count+1))
done
