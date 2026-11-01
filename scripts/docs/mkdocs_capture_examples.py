#!/usr/bin/env python3
"""Compare the hand-written response examples with what the routes answer.

Every route block in docs/_api_macroses/controllers/*.yml carries a `response`
block with a JSON example somebody wrote by hand. Nothing compares that example
to the DTO the route actually serialises, so an example keeps fields a route no
longer produces and misses the ones it gained (#3871: the rooms example claimed
`cells_count` and no `contained_pawns_ids`).

This script repeats each block's own `curl` call against a running game (a debug
slot answers on its own port) and compares the key structure of the answer with
the example. A key the example has and the answer does not is a field no route
produces; a key the answer has and the example does not is a field nobody
documented. Only `data` is compared by default, because `success`, `errors`,
`warnings` and `timestamp` frame every answer and are documented once in the
developer guide.

    python3 scripts/docs/mkdocs_capture_examples.py --base-url http://localhost:8772
    python3 scripts/docs/mkdocs_capture_examples.py --base-url http://localhost:8772 --write

Name every colony you have (a debug slot each); a route is compared against the
union of the answers, because one colony is a poor witness for a key it happens
to have none of: a caravan list is empty until somebody sends one, and a map
without gold ore says nothing about the ore DTO. A key no colony answers with is
the one to drop.

`--write` repairs the example's shape and keeps its values: a missing key is
added with the value the game answered, a key no colony answers with is dropped,
and every key both sides have keeps the hand-written value. The diff is then
exactly the drift. Values themselves are not checked, so an example can still
claim `"cells_count": 0` for a room full of cells; that stays a matter for a
human reading the example.

A route the game answers with an error, or whose example carries no call to
repeat, is skipped and named with the reason: a check that cannot run is not a
check that passed.
"""

import argparse
import json
import os
import re
import sys
import urllib.error
import urllib.request

import yaml

YAML_DIR = os.path.join("docs", "_api_macroses", "controllers")

# The fenced JSON example inside a block's `response` text.
FENCE_RE = re.compile(r"```json\s*\n(.*?)```", re.DOTALL)
# The URL of a curl example, quoted or bare, single or double.
URL_RE = re.compile(r"--url\s+['\"]?(https?://[^\s'\"]+)['\"]?")
ORIGIN_RE = re.compile(r"^https?://[^/]+")

# A list of more elements than this only contributes its first ones: a colony's
# item lists run to thousands and the keys repeat.
MAX_LIST_ELEMENTS = 200

# The key every route answer wraps its payload in.
PAYLOAD_KEY = "data"


class Skip(Exception):
    """A route this run cannot check, with the reason the reader needs."""


def load_route_blocks(path):
    """Yield (route, block) for every documented route in one YAML file."""
    with open(path, "r", encoding="utf-8") as f:
        try:
            data = yaml.safe_load(f)
        except yaml.YAMLError as e:
            raise Skip(f"YAML does not parse: {e}")
    if not isinstance(data, dict):
        return
    for route, block in data.items():
        if isinstance(route, str) and route.startswith("/") and isinstance(block, dict):
            yield route, block


def example_json(block):
    """The parsed JSON example of a block, or None when it has none."""
    text = block.get("response") or ""
    match = FENCE_RE.search(text)
    if not match:
        return None
    try:
        return json.loads(match.group(1))
    except json.JSONDecodeError as e:
        raise Skip(f"example is not valid JSON: {e}")


def example_url(block):
    """The URL to repeat: the curl example's, moved onto the base URL."""
    curl = block.get("curl") or ""
    match = URL_RE.search(curl)
    if not match:
        raise Skip("no curl example to repeat")
    return match.group(1)


def payload(value, whole):
    """The part of an answer the check reads: its `data`, or everything."""
    if whole:
        return value
    if isinstance(value, dict) and PAYLOAD_KEY in value:
        return value[PAYLOAD_KEY]
    return value


def key_paths(value, prefix=(), out=None):
    """Every key path of a JSON value; a list contributes its elements' paths."""
    if out is None:
        out = set()
    if isinstance(value, dict):
        for key, item in value.items():
            path = prefix + (str(key),)
            out.add(path)
            key_paths(item, path, out)
    elif isinstance(value, list):
        for item in value[:MAX_LIST_ELEMENTS]:
            key_paths(item, prefix + ("[]",), out)
    return out


def kind(value):
    """The JSON kind of a value, so a dict in the example and a string in the
    answer count as drift even though the key exists on both sides.

    A whole number and a decimal are one kind: `0` against `0.0` is not a
    documentation error, and the serializer picks the one its value needs.
    """
    if isinstance(value, dict):
        return "object"
    if isinstance(value, list):
        return "array"
    if value is None:
        return "null"
    if isinstance(value, bool):
        return "boolean"
    if isinstance(value, (int, float)):
        return "number"
    return "string"


def kinds(value, prefix=(), out=None):
    """Key path -> JSON kind."""
    if out is None:
        out = {}
    if isinstance(value, dict):
        for key, item in value.items():
            path = prefix + (str(key),)
            out[path] = kind(item)
            kinds(item, path, out)
    elif isinstance(value, list):
        for item in value[:MAX_LIST_ELEMENTS]:
            kinds(item, prefix + ("[]",), out)
    return out


def render_path(path):
    return ".".join(path)


def fetch(url, timeout):
    request = urllib.request.Request(url, headers={"Accept": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            body = response.read()
    except urllib.error.HTTPError as e:
        body = e.read()
        detail = ""
        try:
            answer = json.loads(body)
            detail = answer.get("error") or answer.get("message") or ""
        except (json.JSONDecodeError, AttributeError):
            detail = body[:120].decode("utf-8", "replace")
        raise Skip(f"HTTP {e.code} {detail}".strip())
    except urllib.error.URLError as e:
        raise Skip(f"unreachable: {e.reason}")
    except TimeoutError:
        raise Skip(f"no answer within {timeout}s")
    try:
        return json.loads(body)
    except json.JSONDecodeError as e:
        raise Skip(f"answer is not JSON: {e}")


def fetch_payloads(base_urls, block, timeout, whole):
    """The payload of this route's answer from every colony named.

    One colony is a poor witness: a list it has no element of says nothing about
    the DTO. So the route is asked everywhere, and a colony that answers with an
    error only contributes its reason.

    Returns the payloads, the reasons of the colonies that failed, and whether
    the answers carried the `data` wrapper at all: a route that answers with a
    bare object has no payload to point at, and an example without `data` is
    then not a missing wrapper.
    """
    payloads = []
    reasons = []
    wrapped = True
    url_path = example_url(block)
    for base_url in base_urls:
        url = ORIGIN_RE.sub(base_url.rstrip("/"), url_path)
        try:
            answer = fetch(url, timeout)
        except Skip as e:
            reasons.append(f"{base_url}: {e}")
            continue
        if not isinstance(answer, dict):
            reasons.append(f"{base_url}: answer is not a JSON object")
            continue
        if not whole and PAYLOAD_KEY not in answer:
            wrapped = False
        payloads.append(payload(answer, whole))
    if not payloads:
        raise Skip("; ".join(reasons))
    return payloads, reasons, wrapped


def union_shape(values):
    """One shape from several values: every key any of them has.

    List elements are unioned too, because the report reads a list's elements
    as one shape: a field only the fifth building carries is still a field the
    route produces. The values themselves never reach an example (a new field
    inside a list gets a placeholder), so a representative standing in for the
    rest costs nothing; the most informative value wins, so a `null` in one
    element does not hide the object another element has.
    """
    dicts = [value for value in values if isinstance(value, dict)]
    if dicts:
        merged = {}
        for one in dicts:
            for key, value in one.items():
                if key in merged:
                    merged[key] = union_shape([merged[key], value])
                else:
                    # Recurse even for the first sighting: a list keeps the
                    # union of its own elements.
                    merged[key] = union_shape([value])
        return merged
    lists = [value for value in values if isinstance(value, list)]
    if lists:
        items = [item for one in lists for item in one[:MAX_LIST_ELEMENTS]]
        return [union_shape(items)] if items else []
    return max(values, key=informativeness)


def informativeness(value):
    """How much a value says about the shape of its field."""
    if isinstance(value, dict):
        return 3
    if isinstance(value, list):
        return 2
    if value is None:
        return -1
    if isinstance(value, str) and not value:
        return 0
    return 1


def placeholder(value):
    """A value of the same shape that carries no data.

    Used where an example has no counterpart to copy from: the reader learns
    which fields exist and what they are, without another object's values
    pretending to belong here. A container contributes its fields, not its
    elements: a list of 4,000 cells documents nothing a reader can use.
    """
    if isinstance(value, dict):
        return {key: placeholder(item) for key, item in value.items()}
    if isinstance(value, list):
        # An item's fields are the shape a reader wants; its numbers are not.
        if value and isinstance(value[0], dict):
            return [placeholder(value[0])]
        return []
    if isinstance(value, bool):
        return False
    if isinstance(value, float):
        return 0.0
    if isinstance(value, int):
        return 0
    if isinstance(value, str):
        return ""
    return None


def added_value(value):
    """What a new key of a single object carries: the game's value when it is
    small enough to read as an illustration, its shape otherwise."""
    if isinstance(value, dict):
        return placeholder(value)
    if isinstance(value, list):
        if not value:
            return []
        if len(value) <= 4 and all(
            not isinstance(item, (dict, list)) for item in value
        ):
            return value
        # One element shows which fields an item has; its values stay out.
        return [placeholder(value[0])]
    if isinstance(value, str) and len(value) > 80:
        # A base64 image or a long tooltip: the example shows the kind of
        # value, not a page of it.
        return value[:40] + "..."
    return value


def compare(example, answer):
    """Return (stale, undocumented, retyped) for one route's payload."""
    example_paths = key_paths(example)
    answer_paths = key_paths(answer)
    stale = sorted(example_paths - answer_paths)
    undocumented = sorted(answer_paths - example_paths)

    example_kinds = kinds(example)
    answer_kinds = kinds(answer)
    retyped = []
    for path in sorted(example_paths & answer_paths):
        before, after = example_kinds.get(path), answer_kinds.get(path)
        if before is None or after is None:
            continue
        # null and an empty container say nothing about the shape.
        if "null" in (before, after):
            continue
        if before != after:
            retyped.append((path, before, after))
    return stale, undocumented, retyped


def merge_shape(example, answer, prune=False, in_list=False):
    """Repair the example's shape from the answer, keeping the example's values.

    A key only the answer has is added. Where the example's object is the
    answer's object (a single response object), it carries the value the game
    answered: the game is the only source for a field nobody wrote down. Inside
    a list that does not hold: the answer's second item is not the example's
    second item, so an item's new fields get a placeholder value instead of
    another item's numbers.

    A key both sides have keeps its hand-written value, so the example stays an
    illustration instead of a snapshot of one colony.

    A key only the example has is kept by default. Absence from the answers is
    weak evidence: a caravan list is empty until somebody sends one, and a map
    without gold ore answers without an ore entry, so dropping every key this
    colony happens not to produce would delete true documentation. `prune` drops
    them anyway, for the caller who has read the report and knows the field is
    gone.
    """
    if isinstance(example, dict) and isinstance(answer, dict):
        merged = {}
        for key, value in example.items():
            if key in answer:
                merged[key] = merge_shape(value, answer[key], prune, in_list)
            elif not prune:
                merged[key] = value
        for key, value in answer.items():
            if key not in example:
                merged[key] = placeholder(value) if in_list else added_value(value)
        return merged
    if isinstance(example, list) and isinstance(answer, list):
        if not example:
            return [placeholder(answer[0])] if answer else []
        if not answer:
            return example
        merged = []
        for index, item in enumerate(example):
            if index < len(answer):
                merged.append(merge_shape(item, answer[index], prune, True))
            else:
                # No counterpart left: the item keeps its own values and gains
                # the fields the answer's first item shows, as placeholders.
                merged.append(merge_shape(item, placeholder(answer[0]), prune, True))
        return merged
    if isinstance(example, list) or isinstance(answer, list):
        # The two sides disagree on the kind of value; document the kind the
        # game sends, with placeholders rather than one colony's data.
        return placeholder(answer)
    if isinstance(example, dict) or isinstance(answer, dict):
        return placeholder(answer)
    return example


INDENT = "    "
# A value that renders shorter than this stays on one line, the way the
# examples are written by hand: a position is `{ "x": 121, "y": 0, "z": 119 }`,
# not five lines.
MAX_INLINE = 100


def compact(value):
    """One-line JSON for a value, or None when it nests too deeply to inline."""
    if isinstance(value, dict):
        parts = []
        for key, item in value.items():
            one = compact(item)
            if one is None:
                return None
            parts.append(f"{json.dumps(key, ensure_ascii=False)}: {one}")
        return "{" + ", ".join(parts) + "}"
    if isinstance(value, list):
        parts = []
        for item in value:
            one = compact(item)
            if one is None:
                return None
            parts.append(one)
        return "[" + ", ".join(parts) + "]"
    return json.dumps(value, ensure_ascii=False)


def render(value, level=0):
    """JSON text with the examples' layout: short values inline, the rest nested."""
    pad = INDENT * level
    one = compact(value)
    if one is not None and len(one) + len(pad) <= MAX_INLINE:
        return one
    if isinstance(value, dict):
        if not value:
            return "{}"
        lines = [
            INDENT * (level + 1)
            + json.dumps(key, ensure_ascii=False)
            + ": "
            + render(item, level + 1)
            for key, item in value.items()
        ]
        return "{\n" + ",\n".join(lines) + "\n" + pad + "}"
    if isinstance(value, list):
        if not value:
            return "[]"
        lines = [INDENT * (level + 1) + render(item, level + 1) for item in value]
        return "[\n" + ",\n".join(lines) + "\n" + pad + "]"
    return json.dumps(value, ensure_ascii=False)


def rewrite_example(path, route, merged):
    """Replace one route's JSON example in the file text, keeping the layout.

    The YAML is edited as text rather than re-dumped: a dump would reflow every
    block in the file and bury the drift in formatting noise.
    """
    with open(path, "r", encoding="utf-8") as f:
        lines = f.read().split("\n")

    route_line = f"{route}:"
    start = None
    for i, line in enumerate(lines):
        if line.rstrip() == route_line:
            start = i
            break
    if start is None:
        raise Skip(f"route {route} not found as a top-level key")

    # Only the `response` block: a route's block also carries a `request`
    # example, and that one is a request body, not an answer.
    response_at = None
    for i in range(start + 1, len(lines)):
        line = lines[i]
        if line and not line.startswith(" "):
            break  # the next route begins
        if line.strip().startswith("response:"):
            response_at = i
            break
    if response_at is None:
        raise Skip("no response block")

    fence_start = fence_end = None
    for i in range(response_at + 1, len(lines)):
        line = lines[i]
        if line and not line.startswith(" "):
            break  # the next route begins
        if fence_start is None and line.strip() == "```json":
            fence_start = i
        elif fence_start is not None and line.strip() == "```":
            fence_end = i
            break
    if fence_start is None or fence_end is None:
        raise Skip("no ```json fence in the response block")

    indent = " " * (len(lines[fence_start]) - len(lines[fence_start].lstrip()))
    body = render(merged)
    lines[fence_start + 1 : fence_end] = [
        indent + line if line else line for line in body.split("\n")
    ]
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument(
        "--base-url",
        action="append",
        default=None,
        help="origin of a game to ask; repeat it for a second colony"
        " (default: http://localhost:8765)",
    )
    parser.add_argument(
        "--write",
        action="store_true",
        help="repair the shape of a drifted example, keeping its values",
    )
    parser.add_argument(
        "--prune",
        action="store_true",
        help="with --write, also drop example keys no colony answered with",
    )
    parser.add_argument(
        "--all",
        action="store_true",
        help="compare the whole answer, frame keys included",
    )
    parser.add_argument(
        "--filter",
        default="",
        help="only routes whose path contains this text",
    )
    parser.add_argument("--timeout", type=float, default=20.0, help="seconds per request")
    parser.add_argument(
        "--verbose", action="store_true", help="also name the routes that agree"
    )
    args = parser.parse_args()
    base_urls = args.base_url or ["http://localhost:8765"]

    if not os.path.isdir(YAML_DIR):
        print(f"run this from the repository root; {YAML_DIR} is not here")
        return 2

    ok = drifted = skipped = 0
    written = 0
    partial = 0
    skip_reasons = {}

    for dirpath, _, filenames in os.walk(YAML_DIR):
        for filename in sorted(filenames):
            if not filename.endswith(".yml") or filename.startswith("_"):
                continue
            if filename.count(".") != 1:
                continue  # a translation file, not the base example
            path = os.path.join(dirpath, filename)
            try:
                blocks = list(load_route_blocks(path))
            except Skip as e:
                print(f"{filename:32} skip: {e}")
                skipped += 1
                continue

            for route, block in blocks:
                if (block.get("method") or "GET").upper() != "GET":
                    continue
                if args.filter and args.filter not in route:
                    continue
                try:
                    example = example_json(block)
                    if example is None:
                        raise Skip("no response example")
                    payloads, reasons, wrapped = fetch_payloads(
                        base_urls, block, args.timeout, args.all
                    )
                except Skip as e:
                    reason = str(e)
                    skip_reasons[reason] = skip_reasons.get(reason, 0) + 1
                    print(f"{route:60} skip: {reason}")
                    skipped += 1
                    continue
                if reasons:
                    partial += 1
                    print(f"{route:60} note: {len(reasons)} colony/colonies failed")
                    for reason in reasons:
                        print(f"{'':60}   {reason}")

                answer = union_shape(payloads)
                example_body = payload(example, args.all)
                # An example that documents only the frame says nothing about
                # the route's own fields; read it as an empty payload so the
                # report names the wrapper instead of every frame key.
                frame_only = (
                    not args.all
                    and wrapped
                    and isinstance(example, dict)
                    and PAYLOAD_KEY not in example
                )
                if frame_only:
                    example_body = {}
                stale, undocumented, retyped = compare(example_body, answer)
                if not (stale or undocumented or retyped or frame_only):
                    ok += 1
                    if args.verbose:
                        print(f"{route:60} ok")
                    continue

                drifted += 1
                parts = []
                if frame_only:
                    parts.append("the example carries no data payload")
                if stale:
                    parts.append("stale: " + ", ".join(render_path(p) for p in stale))
                if undocumented:
                    parts.append(
                        "undocumented: "
                        + ", ".join(render_path(p) for p in undocumented)
                    )
                if retyped:
                    parts.append(
                        "retyped: "
                        + ", ".join(f"{render_path(p)} {b}->{a}" for p, b, a in retyped)
                    )
                print(f"{route:60} drift: " + "; ".join(parts))

                if args.write:
                    repaired = merge_shape(example_body, answer, args.prune)
                    if args.all:
                        target = repaired
                    else:
                        target = dict(example)
                        target[PAYLOAD_KEY] = repaired
                    try:
                        rewrite_example(path, route, target)
                        written += 1
                    except Skip as e:
                        print(f"{route:60} not written: {e}")

    print()
    print(
        f"{ok} routes agree, {drifted} drifted, {skipped} skipped"
        f" ({written} examples repaired, {partial} compared against a partial answer set)"
    )
    for reason, count in sorted(skip_reasons.items(), key=lambda kv: -kv[1]):
        print(f"  skip x{count}: {reason}")
    if drifted and not args.write:
        print("rerun with --write to repair the drifted examples")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
