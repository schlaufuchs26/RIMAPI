import os
import re
import yaml

# Configuration paths
CS_DIR = os.path.join("Source", "RIMAPI", "RimworldRestApi")
YAML_DIR = os.path.join("docs", "_api_macroses", "controllers")

# Regex to find C# route attributes
ROUTE_REGEX = re.compile(
    r'\[(Get|Post|Put|Delete|Patch)\(\s*"([^"]+)"\s*\)\]', re.IGNORECASE
)


# --- PyYAML Formatting Safety Net ---
# This ensures multi-line strings (like curl/response blocks) stay formatted with the '|' symbol
def str_presenter(dumper, data):
    if len(data.splitlines()) > 1 or "\n" in data:
        cleaned_data = "\n".join([line.rstrip() for line in data.splitlines()])
        return dumper.represent_scalar("tag:yaml.org,2002:str", cleaned_data, style="|")
    return dumper.represent_scalar("tag:yaml.org,2002:str", data)


yaml.add_representer(str, str_presenter)
yaml.representer.SafeRepresenter.add_representer(str, str_presenter)
# ------------------------------------


def get_csharp_routes():
    """Scans all C# files recursively and returns a dictionary of correct methods."""
    csharp_data = {}
    if not os.path.exists(CS_DIR):
        print(f"❌ Error: C# directory not found at {CS_DIR}")
        return csharp_data

    for root, dirs, files in os.walk(CS_DIR):
        for filename in files:
            if filename.endswith("Controller.cs"):
                controller_name = filename[:-3]  # Remove .cs
                csharp_data[controller_name] = {}

                filepath = os.path.join(root, filename)
                with open(filepath, "r", encoding="utf-8") as f:
                    content = f.read()

                matches = ROUTE_REGEX.findall(content)
                for method, path in matches:
                    # a path can answer several methods (GET to read, POST to create)
                    csharp_data[controller_name].setdefault(path, set()).add(
                        method.upper()
                    )

    return csharp_data


def fix_yaml_methods():
    print("🛠️ Scanning and fixing YAML methods based on C# source of truth...\n")

    cs_routes = get_csharp_routes()
    if not cs_routes:
        return

    files_updated = 0
    methods_fixed = 0

    # os.walk to recursively go through all language folders (en, ja, ru, etc)
    for root_dir, dirs, files in os.walk(YAML_DIR):
        for filename in files:
            if not filename.endswith(".yml") or filename.startswith("_meta"):
                continue

            # Handle translated files (e.g. BuilderController.ja.yml -> BuilderController)
            controller_name = filename.split(".")[0]

            # If we don't have C# data for this controller, skip it
            if controller_name not in cs_routes:
                continue

            filepath = os.path.join(root_dir, filename)

            # Read the YAML
            with open(filepath, "r", encoding="utf-8") as f:
                try:
                    data = yaml.safe_load(f)
                except Exception as e:
                    print(f"❌ Error reading {filepath}: {e}")
                    continue

            if not data:
                continue

            made_changes = False

            # Compare YAML endpoints to C# truth
            for path, endpoint_data in data.items():
                if not path.startswith("/"):
                    continue
                # Look up what the C# file says this path should be
                cs_methods = cs_routes[controller_name].get(path)
                if not cs_methods:
                    continue

                if isinstance(endpoint_data, list):
                    # One block per method: the script cannot tell which block
                    # is wrong, so it only says that the sets disagree.
                    yml_methods = {
                        (block.get("method") or "GET").upper()
                        for block in endpoint_data
                        if isinstance(block, dict)
                    }
                    if yml_methods != cs_methods:
                        print(
                            f"⚠️ Cannot fix {path} in {filepath}: C# answers "
                            f"{'/'.join(sorted(cs_methods))}, documented as "
                            f"{'/'.join(sorted(yml_methods)) or 'nothing'}. "
                            "Edit its method blocks by hand."
                        )
                    continue

                if len(cs_methods) > 1:
                    print(
                        f"⚠️ Cannot fix {path} in {filepath}: C# answers "
                        f"{'/'.join(sorted(cs_methods))}, but the YAML holds one "
                        "block. Give the path a list with one block per method."
                    )
                    continue

                cs_method = next(iter(cs_methods))
                yml_method = (endpoint_data.get("method") or "GET").upper()
                if yml_method != cs_method or "method" not in endpoint_data:
                    endpoint_data["method"] = cs_method
                    made_changes = True
                    methods_fixed += 1
                    print(f"✅ Fixed {path} in {filepath} -> {cs_method}")

            # If we updated anything, write the file back out
            if made_changes:
                with open(filepath, "w", encoding="utf-8") as f:
                    yaml.dump(
                        data,
                        f,
                        allow_unicode=True,
                        sort_keys=False,
                        default_flow_style=False,
                        width=float("inf"),
                    )
                files_updated += 1

    print("\n" + "=" * 40)
    print(f"🎉 Done! Fixed {methods_fixed} methods across {files_updated} files.")
    print("=" * 40)


if __name__ == "__main__":
    fix_yaml_methods()
