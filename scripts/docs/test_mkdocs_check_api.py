#!/usr/bin/env python3
"""Tests for scripts/docs/mkdocs_check_api.py (ticket #4109 in fuchs).

Run with: python3 scripts/docs/test_mkdocs_check_api.py

The checker is the fork's docs gate, so it has to tell three states apart:
a route nobody documented (an error, the gate), a route the fork deliberately
leaves undocumented (allowed, but only while it is on the list), and a list
entry that has gone stale because the route is documented now (a warning: the
line is dead weight and would hide a later deletion of that documentation).
Every case runs against a throwaway tree, so no real controller or YAML file
is read. The upstream Russian mirror is only checked with --translations
(ticket #4319), so the last four cases pin that it stays quiet by default.
"""

import contextlib
import io
import os
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import mkdocs_check_api as checker  # noqa: E402

BASE_CS = "Source/RIMAPI/RimworldRestApi/BaseControllers"
CONTROLLERS_CS = "Source/RIMAPI/RimworldRestApi/Controllers"
YAML_DIR = "docs/_api_macroses/controllers"


def write(root, rel, content):
    path = Path(root) / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(textwrap.dedent(content).lstrip(), encoding="utf-8")
    return path


def controller(routes):
    """C# controller source with one attribute per (method, path) pair."""
    body = "\n".join(f'        [{m}("{p}")]' for m, p in routes)
    return f"public class Whatever {{\n{body}\n}}\n"


def yaml_doc(routes):
    """YAML doc with one GET block per path (the checker only reads keys and method)."""
    blocks = []
    for path, method in routes:
        blocks.append(
            f"{path}:\n  method: {method}\n  desc: x\n  curl: x\n  request: ''\n  response: ''\n"
        )
    return "".join(blocks)


class CheckerCase(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.root = self._tmp.name
        self.allowlist = Path(self.root) / "scripts" / "docs" / "undocumented_routes.txt"

    def tearDown(self):
        self._tmp.cleanup()

    def run_checker(self, allowlist_text=None, translations=False):
        if allowlist_text is not None:
            write(self.root, "scripts/docs/undocumented_routes.txt", allowlist_text)
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            result = checker.run_validation(
                root=self.root,
                allowlist=str(self.allowlist),
                translations=translations,
            )
        return result, out.getvalue()

    def test_undocumented_route_is_an_error(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        result, out = self.run_checker()
        self.assertEqual(result.errors, 1)
        self.assertIn("MISSING DOCS", out)

    def test_allowlisted_route_is_not_an_error(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        result, out = self.run_checker("XController.cs /api/v1/x\n")
        self.assertEqual(result.errors, 0)
        self.assertEqual(result.allowed, 1)
        self.assertNotIn("MISSING DOCS", out)

    def test_stale_allowlist_entry_is_a_warning(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        write(self.root, f"{YAML_DIR}/XController.yml", yaml_doc([("/api/v1/x", "GET")]))
        result, out = self.run_checker("XController.cs /api/v1/x\n")
        self.assertEqual(result.errors, 0)
        self.assertEqual(result.warnings, 1)
        self.assertIn("STALE ALLOWLIST", out)
        self.assertIn("undocumented_routes.txt", out)

    def test_allowlist_entry_for_a_deleted_route_is_stale(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/other")]))
        write(self.root, f"{YAML_DIR}/XController.yml", yaml_doc([("/api/v1/other", "GET")]))
        result, out = self.run_checker("XController.cs /api/v1/gone\n")
        self.assertEqual(result.warnings, 1)
        self.assertIn("STALE ALLOWLIST", out)

    def test_documented_route_passes(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        write(self.root, f"{YAML_DIR}/XController.yml", yaml_doc([("/api/v1/x", "GET")]))
        result, _ = self.run_checker()
        self.assertEqual((result.errors, result.warnings, result.allowed), (0, 0, 0))

    def test_method_mismatch_is_a_warning(self):
        write(
            self.root,
            f"{CONTROLLERS_CS}/XController.cs",
            controller([("Get", "/api/v1/x"), ("Post", "/api/v1/x")]),
        )
        write(self.root, f"{YAML_DIR}/XController.yml", yaml_doc([("/api/v1/x", "GET")]))
        result, out = self.run_checker()
        self.assertEqual(result.warnings, 1)
        self.assertIn("METHOD MISMATCH", out)

    def test_ghost_route_is_a_warning(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        write(
            self.root,
            f"{YAML_DIR}/XController.yml",
            yaml_doc([("/api/v1/x", "GET"), ("/api/v1/gone", "GET")]),
        )
        result, out = self.run_checker()
        self.assertEqual(result.warnings, 1)
        self.assertIn("GHOST ROUTE", out)

    def test_malformed_allowlist_line_is_an_error(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        write(self.root, f"{YAML_DIR}/XController.yml", yaml_doc([("/api/v1/x", "GET")]))
        result, out = self.run_checker("/api/v1/x\n")
        self.assertEqual(result.errors, 1)
        self.assertIn("ALLOWLIST", out)

    def test_comments_and_blank_lines_are_ignored(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        result, _ = self.run_checker("# a comment\n\nXController.cs /api/v1/x\n")
        self.assertEqual((result.errors, result.warnings, result.allowed), (0, 0, 1))

    def test_missing_allowlist_file_is_no_allowlist(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        result, _ = self.run_checker()
        self.assertEqual(result.errors, 1)

    def test_exit_code_is_one_while_an_error_stands(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        with contextlib.redirect_stdout(io.StringIO()):
            code = checker.main(["--root", self.root, "--allowlist", str(self.allowlist)])
        self.assertEqual(code, 1)

    # ── the upstream Russian mirror is opt-in (ticket #4319) ──────────────────

    def test_a_route_without_a_translation_is_not_a_warning_by_default(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        write(self.root, f"{YAML_DIR}/XController.yml", yaml_doc([("/api/v1/x", "GET")]))
        write(self.root, f"{YAML_DIR}/ru/XController.ru.yml", "{}\n")
        result, out = self.run_checker()
        self.assertEqual(result.warnings, 0)
        self.assertNotIn("MISSING TRANSLATION", out)

    def test_missing_translation_is_a_warning_when_asked(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        write(self.root, f"{YAML_DIR}/XController.yml", yaml_doc([("/api/v1/x", "GET")]))
        write(self.root, f"{YAML_DIR}/ru/XController.ru.yml", "{}\n")
        result, out = self.run_checker(translations=True)
        self.assertEqual(result.warnings, 1)
        self.assertIn("MISSING TRANSLATION", out)

    def test_orphaned_translation_needs_the_flag(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        write(self.root, f"{YAML_DIR}/XController.yml", yaml_doc([("/api/v1/x", "GET")]))
        write(self.root, f"{YAML_DIR}/ru/Gone.ru.yml", yaml_doc([("/api/v1/gone", "GET")]))
        _, quiet = self.run_checker()
        self.assertNotIn("ORPHANED TRANSLATION", quiet)
        result, loud = self.run_checker(translations=True)
        self.assertEqual(result.warnings, 1)
        self.assertIn("ORPHANED TRANSLATION", loud)

    def test_the_flag_reaches_main(self):
        write(self.root, f"{CONTROLLERS_CS}/XController.cs", controller([("Get", "/api/v1/x")]))
        write(self.root, f"{YAML_DIR}/XController.yml", yaml_doc([("/api/v1/x", "GET")]))
        write(self.root, f"{YAML_DIR}/ru/XController.ru.yml", "{}\n")
        argv = ["--root", self.root, "--allowlist", str(self.allowlist)]
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(checker.main(argv), 0)
            self.assertEqual(checker.main(argv + ["--translations"]), 1)


if __name__ == "__main__":
    unittest.main()
