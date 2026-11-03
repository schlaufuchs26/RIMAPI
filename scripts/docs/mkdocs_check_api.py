#!/usr/bin/env python3
"""Cross-check the C# route attributes against the YAML documentation.

Three states matter (ticket #4109 in fuchs):
- a route in C# that no YAML file documents is an error, the gate;
- a route the fork deliberately leaves undocumented is allowed, but only while
  its line stands in scripts/docs/undocumented_routes.txt;
- a line in that list whose route is documented now, or gone from C#, is stale
  and counts as a warning: it would hide a later deletion of that documentation.

The upstream Russian mirror under docs/_api_macroses/controllers/ru/ is checked
only with --translations (ticket #4319): the fork documents in English, so a
route that upstream's translator has not reached yet must not fail the gate.
"""

import argparse
import json
import os
import re
import sys
from dataclasses import dataclass

import yaml

# Configuration paths, relative to the repository root
CS_DIRS = [
    os.path.join("Source", "RIMAPI", "RimworldRestApi", "BaseControllers"),
    os.path.join("Source", "RIMAPI", "RimworldRestApi", "Controllers"),
]
YAML_DIR = os.path.join("docs", "_api_macroses", "controllers")
ALLOWLIST = os.path.join("scripts", "docs", "undocumented_routes.txt")

# Regex to find C# route attributes like [Post("/api/v1/builder/copy")]
# Group 1 = HTTP Method (Get, Post, etc), Group 2 = Route Path
ROUTE_REGEX = re.compile(
    r'\[(Get|Post|Put|Delete|Patch)\(\s*"([^"]+)"\s*\)\]', re.IGNORECASE
)


@dataclass
class Result:
    """What one validation run found."""

    errors: int = 0
    warnings: int = 0
    allowed: int = 0


def get_csharp_routes(root="."):
    """Scans all C# files recursively and returns a dict of {controller: {path: methods}}."""
    csharp_data = {}
    for cs_dir in CS_DIRS:
        directory = os.path.join(root, cs_dir)
        if not os.path.exists(directory):
            print(f"❌ Error: C# directory not found at {cs_dir}")
            continue

        for dirpath, _, filenames in os.walk(directory):
            for filename in filenames:
                if not filename.endswith(".cs"):
                    continue
                controller_name = filename[:-3]  # Remove .cs
                filepath = os.path.join(dirpath, filename)
                with open(filepath, "r", encoding="utf-8") as f:
                    content = f.read()

                matches = ROUTE_REGEX.findall(content)
                if matches:
                    routes = csharp_data.setdefault(controller_name, {})
                    for method, path in matches:
                        # a path can answer several methods (GET to read, POST to create)
                        routes.setdefault(path, set()).add(method.upper())

    return csharp_data


def load_undocumented(filepath):
    """Reads the allowlist of deliberately undocumented routes.

    One route per line, "<Controller>.cs <path>"; '#' starts a comment. Returns
    ({(controller, path)}, [(line_number, text)] for lines that do not parse).
    A missing file is an empty list, so the checker works without one.
    """
    allowed = set()
    malformed = []
    if not os.path.exists(filepath):
        return allowed, malformed

    with open(filepath, "r", encoding="utf-8") as f:
        for lineno, raw in enumerate(f, 1):
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            parts = line.split()
            if len(parts) != 2 or not parts[1].startswith("/"):
                malformed.append((lineno, line))
                continue
            controller = parts[0]
            if controller.endswith(".cs"):
                controller = controller[:-3]
            allowed.add((controller, parts[1]))

    return allowed, malformed


def _load_yaml_file(filepath, yaml_dir=YAML_DIR):
    """Returns parsed YAML data or None on parse failure; prints the error."""
    with open(filepath, "r", encoding="utf-8") as f:
        try:
            return yaml.safe_load(f)
        except Exception as e:
            rel = os.path.relpath(filepath, yaml_dir)
            print(f"❌ YAML ERROR in {rel}: {e}")
            return None


def _route_blocks(value):
    """The method blocks of one route key: a list for a path that answers
    several methods, the single block itself otherwise."""
    if isinstance(value, list):
        return [block for block in value if isinstance(block, dict)]
    if isinstance(value, dict):
        return [value]
    return []


def _extract_routes(data):
    """Given a parsed YAML dict, return {path: {methods}} for all route keys."""
    if not data:
        return {}
    routes = {}
    for key, value in data.items():
        if not (isinstance(key, str) and key.startswith("/")):
            continue
        methods = routes.setdefault(key, set())
        for block in _route_blocks(value):
            methods.add((block.get("method") or "GET").upper())
    return routes


def get_yaml_routes(root="."):
    """
    Walks the entire YAML_DIR tree.
    Returns:
        base_routes:        {controller: {path: method}}   (from *.yml base files)
        translation_routes: {lang: {controller: {path: method}}}  (from *.lang.yml files)
        parse_errors:       count of files that failed to parse
    """
    base_routes = {}
    translation_routes = {}
    parse_errors = 0

    yaml_root = os.path.join(root, YAML_DIR)
    if not os.path.exists(yaml_root):
        print(f"❌ Error: YAML directory not found at {YAML_DIR}")
        return base_routes, translation_routes, parse_errors

    for dirpath, _, filenames in os.walk(yaml_root):
        for filename in filenames:
            if not filename.endswith(".yml"):
                continue
            if filename.startswith("_"):
                continue  # skip _meta.yml etc.

            filepath = os.path.join(dirpath, filename)
            parts = filename.split(".")  # e.g. ["GameController", "ru", "yml"]

            if len(parts) == 2:
                # Base English file: GameController.yml
                controller = parts[0]
                data = _load_yaml_file(filepath, yaml_root)
                if data is None:
                    parse_errors += 1
                    continue
                base_routes[controller] = _extract_routes(data)

            elif len(parts) == 3:
                # Translation file: GameController.ru.yml
                controller, lang = parts[0], parts[1]
                data = _load_yaml_file(filepath, yaml_root)
                if data is None:
                    parse_errors += 1
                    continue
                translation_routes.setdefault(lang, {})[controller] = _extract_routes(data)

    return base_routes, translation_routes, parse_errors


def check_examples(root="."):
    """Every documented response example must at least be valid JSON.

    Whether the example is *true* is a different question, answered against a
    running game by mkdocs_capture_examples.py; a hand-written example that no
    parser accepts is wrong no matter what the game answers.
    """
    yaml_root = os.path.join(root, YAML_DIR)
    errors = 0
    for dirpath, _, filenames in os.walk(yaml_root):
        for filename in filenames:
            if not filename.endswith(".yml") or filename.startswith("_"):
                continue
            filepath = os.path.join(dirpath, filename)
            data = _load_yaml_file(filepath, yaml_root)
            if not isinstance(data, dict):
                continue
            rel = os.path.relpath(filepath, yaml_root)
            for route, value in data.items():
                if not (isinstance(route, str) and route.startswith("/")):
                    continue
                for block in _route_blocks(value):
                    match = re.search(
                        r"```json\s*\n(.*?)```", block.get("response") or "", re.DOTALL
                    )
                    if not match:
                        continue
                    try:
                        json.loads(match.group(1))
                    except json.JSONDecodeError as e:
                        print(f"❌ INVALID EXAMPLE: '{route}' in {rel}: {e}")
                        errors += 1
    return errors


def run_validation(root=".", allowlist=None, translations=False):
    """Runs every check and returns the counts; prints one line per finding.

    translations=False skips the mirror checks (section 3), so a route without
    a <Controller>.<lang>.yml entry is not reported: the fork keeps English docs
    and upstream's Russian files lag behind its new routes (ticket #4319).
    """
    print("🔍 Scanning C# Controllers and YAML Documentation...\n")

    allowlist_path = allowlist or os.path.join(root, ALLOWLIST)
    allowlist_rel = os.path.relpath(allowlist_path, root)

    cs_routes = get_csharp_routes(root)
    base_routes, translation_routes, parse_errors = get_yaml_routes(root)
    allowed_keys, malformed_lines = load_undocumented(allowlist_path)

    result = Result(errors=parse_errors)

    for lineno, text in malformed_lines:
        print(
            f"❌ ALLOWLIST: line {lineno} of {allowlist_rel} is '{text}', "
            f"expected '<Controller>.cs <path>'."
        )
        result.errors += 1

    # ── 1. C# vs base YAML ────────────────────────────────────────────────────

    documented = set()  # (controller, path) pairs a YAML file covers

    # Missing docs and method mismatches
    for controller, routes in cs_routes.items():
        if not routes:
            continue

        yml_controller = base_routes.get(controller, {})
        for path, methods in routes.items():
            if path not in yml_controller:
                if (controller, path) in allowed_keys:
                    result.allowed += 1
                    continue
                print(f"❌ MISSING DOCS: '{path}' in {controller}.cs is not documented in YAML.")
                result.errors += 1
            else:
                documented.add((controller, path))
                yml_methods = yml_controller[path]
                if methods != yml_methods:
                    print(
                        f"⚠️ METHOD MISMATCH: '{path}' in {controller} is "
                        f"{'/'.join(sorted(methods))} in C# but "
                        f"{'/'.join(sorted(yml_methods))} in YAML."
                    )
                    result.warnings += 1

    # Ghost routes (documented but deleted from C#)
    for controller, routes in base_routes.items():
        cs_controller = cs_routes.get(controller, {})
        for path in routes:
            if path not in cs_controller:
                print(
                    f"👻 GHOST ROUTE: '{path}' is documented in {controller}.yml but does not exist in {controller}.cs."
                )
                result.warnings += 1

    # ── 2. The allowlist itself ───────────────────────────────────────────────

    for controller, path in sorted(allowed_keys):
        if (controller, path) in documented:
            print(
                f"⚠️ STALE ALLOWLIST: '{path}' in {controller}.cs is documented now; "
                f"remove its line from {allowlist_rel}."
            )
            result.warnings += 1
        elif path not in cs_routes.get(controller, {}):
            print(
                f"⚠️ STALE ALLOWLIST: '{path}' no longer exists in {controller}.cs; "
                f"remove its line from {allowlist_rel}."
            )
            result.warnings += 1

    # ── 3. Translation checks (opt-in, --translations) ────────────────────────

    for lang, lang_controllers in (
        translation_routes.items() if translations else ()
    ):
        for controller, trans_routes in lang_controllers.items():

            # Orphaned translation: no base file exists
            if controller not in base_routes:
                print(
                    f"👻 ORPHANED TRANSLATION: {controller}.{lang}.yml has no base {controller}.yml."
                )
                result.warnings += 1
                continue

            base = base_routes[controller]

            # Routes in base missing from translation
            for path in base:
                if path not in trans_routes:
                    print(
                        f"🌐 MISSING TRANSLATION: '{path}' from {controller}.yml is not in {controller}.{lang}.yml."
                    )
                    result.warnings += 1

            # Routes in translation that no longer exist in base
            for path in trans_routes:
                if path not in base:
                    print(
                        f"👻 GHOST IN TRANSLATION: '{path}' in {controller}.{lang}.yml has no matching route in {controller}.yml."
                    )
                    result.warnings += 1

    # ── 4. Response examples ──────────────────────────────────────────────────

    result.errors += check_examples(root)

    # ── Summary ───────────────────────────────────────────────────────────────
    print("\n" + "=" * 40)
    if result.allowed:
        print(
            f"ℹ️ {result.allowed} route(s) are deliberately undocumented, "
            f"listed in {allowlist_rel}."
        )
    if result.errors == 0 and result.warnings == 0:
        print("✅ SUCCESS: Documentation matches the C# codebase.")
    else:
        print(f"⚠️ SUMMARY: Found {result.errors} errors and {result.warnings} warnings.")
        print("=" * 40)

    return result


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument(
        "--root", default=".", help="repository root to scan (default: the current directory)"
    )
    parser.add_argument(
        "--allowlist",
        default=None,
        help=f"routes deliberately left undocumented (default: <root>/{ALLOWLIST})",
    )
    parser.add_argument(
        "--translations",
        action="store_true",
        help="also require a <Controller>.<lang>.yml entry for every documented "
        "route (off by default: the fork documents in English, ticket #4319)",
    )
    args = parser.parse_args(argv)

    result = run_validation(
        root=args.root, allowlist=args.allowlist, translations=args.translations
    )
    return 0 if result.errors == 0 and result.warnings == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
