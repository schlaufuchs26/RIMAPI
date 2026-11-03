# :material-rocket-launch: Release Flow

This document outlines the standard procedure for publishing a new release of RIMAPI. By following these steps, we ensure that version numbers, documentation, and compiled binaries stay perfectly in sync.

!!! info "Prerequisites"
    Before starting a release, ensure that:
    
    * All new features and bug fixes have been tested locally in RimWorld.
    * The Bruno API collection runs successfully against the new build.
    * Documentation for any new endpoints has been added to the respective `_api_macroses/controllers/` YAML files.

---

## 1. Update the Changelog
Before bumping the version, finalize the `CHANGELOG.md`. 

1. Review the `## [Unreleased]` section.
2. Change the `[Unreleased]` header to the new version number and today's date (e.g., `## v1.8.3 - 2026-03-28`).
3. Ensure all significant changes (Features, Fixes, Breaking Changes) are accurately categorized.

## 2. Format and Validate API Documentation
Before finalizing the release, ensure all YAML documentation is properly formatted, HTTP methods are synced, and no endpoints are missing or undocumented. Run the following documentation scripts from the root directory:

```bash
echo "🧹 Formatting YAML files..."
python scripts/docs/mkdocs_format_yaml.py

echo "🔧 Auto-fixing HTTP methods..."
python scripts/docs/mkdocs_fix_yaml_methods.py

echo "🔍 Running final validation check..."
python scripts/docs/mkdocs_check_api.py
```

A route that is deliberately left undocumented goes into
`scripts/docs/undocumented_routes.txt`, one `<Controller>.cs <path>` line each.
The checker allows such a route and reports the line as stale as soon as the
route is documented or gone, so the list cannot hide a doc block that later
disappears.

The English YAML under `docs/_api_macroses/controllers/` is the documentation;
upstream's Russian mirror in `ru/` is not required. The checker therefore
reports a route with no `ru` entry only under `--translations`, which is how
that mirror is kept in sync on purpose rather than on every new endpoint.

`mkdocs_check_api.py` only proves that every route is documented, not that the
example under it is true. The examples are hand-written, so they keep fields a
route no longer sends. `mkdocs_capture_examples.py` repeats each block's own
`curl` call against a running game and compares the keys of the answer with the
keys of the example:

```bash
# a debug slot answers on its own port; name every colony you have, because one
# colony is a poor witness for a field it happens to have none of
python3 scripts/docs/mkdocs_capture_examples.py --base-url http://localhost:8766

# repair the shape of the examples that drifted, keeping their hand-written values
python3 scripts/docs/mkdocs_capture_examples.py --base-url http://localhost:8766 --write
```

It exits non-zero while any route still drifts. `stale` names a key the example
has and no colony answered with: the field is either gone or this colony has
none of it, so the report needs a human. `undocumented` names a key the game
sends and nobody wrote down, which `--write` adds. With `--prune` a route you
have read the report for drops its stale keys too:

```bash
python3 scripts/docs/mkdocs_capture_examples.py --base-url http://localhost:8766 \
  --write --prune --filter /api/v1/client/learning
```

Values are never compared, so an example can still claim `"cells_count": 0` for
a room full of cells. Fields added inside a list carry placeholder values (`0`,
`""`, `[]`), because the game's third building is not the example's third
building; fields added to a single object carry the value the game answered.

## 3. Run the Version Bump Script
RimAPI uses a Python script to enforce a "Single Source of Truth" for versioning. You do not need to manually edit XML or C# files.

From the root directory of the repository, run the bump script with your new version number:

```bash
python scripts/bump_version.py 1.8.3
```

What this script does automatically:

- Updates <modVersion> in About/About.xml
- Updates <version> in About/Manifest.xml
- Updates <Version> in Source/RIMAPI/RimApi.csproj
- Updates the tool version in .config/dotnet-tools.json
- Updates the documentation metadata in docs/_api_macroses/controllers/_meta.yml
- Stages all modified files in Git (git add -u)

## 4. Build the Mod

Build the 1.6 assembly (`Release-1.6`), which lands in `1.6/Assemblies/`. The 1.5
configuration was dropped (#3285): it stopped compiling once 1.6-only APIs came
in, and only the 1.6 assembly is shipped.

## 5. Commit and Tag

Once the script finishes and the files are staged, commit the release and create a Git tag. We follow Conventional Commits for our release history.

```bash

# Commit the version bump
git commit -m "Release v1.8.3"

# Create an annotated tag
git tag -a v1.8.3 -m "Release v1.8.3"

# Push the commit and the tag to GitHub
git push origin main
git push origin v1.8.3
```
