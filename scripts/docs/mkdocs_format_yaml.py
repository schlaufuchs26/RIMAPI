#!/usr/bin/env python3
"""Reformat the hand-written controller YAML of the API documentation.

Run it from the repository root; it walks docs/_api_macroses/controllers/*.yml,
round-trips each file through ruamel.yaml and writes it back only when the
result differs from what is on disk. A file that is already formatted keeps its
content and its modification time, so a plain run leaves no dirty checkout
behind.

    python3 scripts/docs/mkdocs_format_yaml.py
    python3 scripts/docs/mkdocs_format_yaml.py --check

`--check` writes nothing and exits 1 when a file would change, which is how a
run tells "the tree is formatted" from "I just reformatted it".
"""

import argparse
import io
import os
import sys

from ruamel.yaml import YAML

YAML_DIR = os.path.join("docs", "_api_macroses", "controllers")


def make_yaml():
    y = YAML()
    y.preserve_quotes = True
    # Keep block style for multi-line strings instead of collapsing to flow style
    y.default_flow_style = False
    y.width = float("inf")  # Prevents random line wrapping
    # YAML keys must be unique; when a file has duplicate route keys (e.g. GET
    # and POST on the same path), don't crash; keep the last occurrence, which
    # matches pyyaml's silent behaviour and what the checker already relies on.
    y.allow_duplicate_keys = True
    return y


def format_file(y, filepath, write):
    """Reformat one file. Returns (state, message); state is changed,
    unchanged, skipped or failed."""
    name = os.path.basename(filepath)
    with open(filepath, "r", encoding="utf-8") as f:
        original = f.read()

    try:
        data = y.load(original)
    except Exception as exc:
        return "failed", f"Failed to read {name}: {exc}"

    if data is None:
        return "skipped", None

    out = io.StringIO()
    y.dump(data, out)
    formatted = out.getvalue()

    if formatted == original:
        return "unchanged", None

    if write:
        with open(filepath, "w", encoding="utf-8", newline="\n") as f:
            f.write(formatted)
        return "changed", f"Cleaned up {name}"
    return "changed", f"Would reformat {name}"


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument(
        "--check",
        action="store_true",
        help="write nothing and exit 1 when a file's formatting differs",
    )
    parser.add_argument(
        "--dir",
        default=YAML_DIR,
        help=f"directory of controller YAML files (default: {YAML_DIR})",
    )
    args = parser.parse_args()

    if not os.path.isdir(args.dir):
        print(f"Could not find directory: {args.dir}; run this from the repository root")
        return 2

    y = make_yaml()
    counts = {"changed": 0, "unchanged": 0, "skipped": 0, "failed": 0}

    for filename in sorted(os.listdir(args.dir)):
        if not filename.endswith(".yml"):
            continue
        filepath = os.path.join(args.dir, filename)
        if not os.path.isfile(filepath):
            continue
        state, message = format_file(y, filepath, write=not args.check)
        counts[state] += 1
        if message:
            print(message)

    changed = counts["changed"]
    verb = "would reformat" if args.check else "reformatted"
    print(
        f"{verb} {changed} files ({counts['unchanged']} already formatted, "
        f"{counts['skipped']} empty, {counts['failed']} unreadable)"
    )

    if counts["failed"] or (args.check and changed):
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
