"""Derive editable stock Kawaii capsule geometry from local FModel JSON exports.

Export the certified UI animation class packages with FModel export-raw and
pass the directory containing Client/Content. Raw game packages and exports
are local inputs. Only the normalized capsule catalog is used by WW Poser.
"""

import argparse
from collections import Counter
from hashlib import sha256
import json
from pathlib import Path


ARRAYS = ("CapsuleLimits", "CapsuleLimitsData")
NODE_PREFIX = "AnimGraphNode_KawaiiPhysicsGroup"


def read(path):
    return json.loads(path.read_bytes())


def digest(path):
    with path.open("rb") as stream:
        result = sha256()
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            result.update(chunk)
    return result.hexdigest()


def game_path(asset):
    package, _, class_name = asset.removeprefix("/Game/").partition(".")
    return Path("Client/Content", package + ".json"), class_name


def name_key(curve, roots):
    return (curve or "None").casefold(), tuple(sorted({root.casefold() for root in roots}))


def bounded(value, low, high):
    if not isinstance(value, (int, float)) or not low <= value <= high:
        raise ValueError("capsule field outside editor range")
    return value


def vector(value, axes, maximum):
    return [bounded(value[axis], -maximum, maximum) for axis in axes]


def capsules(asset, node, properties, known_bones):
    if properties.get("LimitsDataAsset") is not None:
        raise ValueError("external collision asset")
    output = []
    for array, field in enumerate(ARRAYS):
        for index, row in enumerate(properties[field]):
            bone = row["DrivingBone"]["BoneName"]
            if bone in ("None", ""):
                bone = None
            elif bone.casefold() not in known_bones:
                raise ValueError("capsule driver bone missing from character skeleton")
            source_id = (array << 16) + index + 1
            output.append({
                "id": sha256(f"{asset}\n{node}\n{source_id}".encode()).hexdigest()[:32],
                "source_id": source_id,
                "bone": bone,
                "array": array,
                "radius": bounded(row["Radius"], 0, 100),
                "length": bounded(row["Length"], 0, 300),
                "offset": vector(row["OffsetLocation"], ("X", "Y", "Z"), 300),
                "rotation": vector(row["OffsetRotation"], ("Pitch", "Yaw", "Roll"), 3600),
                "enabled": True,
            })
    return output


def layout(root, raw, profile, asset):
    relative, class_name = game_path(asset)
    export = raw / relative
    descriptor = read(root / "data/native_graphs/runtime" / (profile + ".json"))
    candidates = [row for row in descriptor["candidate_sources"]
                  if row.get("ui_class_asset", "").casefold() == asset.casefold()
                  and row.get("evidence_kind") == "actual_nodes"]
    if len(candidates) != 1:
        raise ValueError("certified UI class is ambiguous or missing")
    source = candidates[0]
    exported = read(export)
    defaults = [row["Properties"] for row in exported if row.get("Name") == f"Default__{class_name}"]
    if len(defaults) != 1:
        raise ValueError("exported animation class defaults are ambiguous or missing")
    nodes = {key: node for key, node in defaults[0].items() if key.startswith(NODE_PREFIX)}
    signatures = source["group_signatures"]
    if not signatures or set(nodes) != {row["node_property"] for row in signatures}:
        raise ValueError("Kawaii node set differs from the certified UI class")
    character = read(root / "data/characters/profiles" / (profile + ".json"))
    bones = {bone.casefold() for bone in character["bone_names"]}
    identities = Counter(name_key(row["alpha_curve"], row["root_bones"]) for row in signatures)
    groups = []
    issues = []
    for expected in signatures:
        node = expected["node_property"]
        properties = nodes[node]
        curve = expected["alpha_curve"] or "None"
        roots = expected["root_bones"]
        try:
            actual_roots = [bone["BoneName"] for bone in properties["RootBones"]]
            actual_curve = properties.get("AlphaCurveName") or "None"
            actual_alpha = properties["AlphaInputType"].split("::")[-1]
            key = name_key(curve, roots)
            if key != name_key(actual_curve, actual_roots) or actual_alpha != expected["alpha_input_type"]:
                raise ValueError("curve, roots or alpha type differ from the certified node")
            if identities[key] != 1:
                raise ValueError("curve and root signature are shared by multiple nodes")
            groups.append({
                "node": node, "curve_name": curve, "root_bones": roots,
                "shapes": capsules(asset, node, properties, bones),
            })
        except (KeyError, TypeError, ValueError) as error:
            issues.append(f"{node}: {error}")
    return {"variant": source["variant"], "export_sha256": digest(export), "groups": groups}, issues


def format_catalog(catalog):
    # Keep one entry per physical line in the generated shape tables.
    lines = ["{", f'  "schema": {catalog["schema"]},',
             f'  "source_build_sha256": "{catalog["source_build_sha256"]}",', '  "characters": {']
    characters = [f"    {json.dumps(key)}: {json.dumps(value)}" for key, value in catalog["characters"].items()]
    lines.extend([",\n".join(characters), "  },", '  "layouts": {'])
    layouts = []
    for key, item in catalog["layouts"].items():
        entries = []
        for group in item["groups"]:
            shapes = ",\n".join("            " + json.dumps(shape) for shape in group["shapes"])
            signature = json.dumps({key: value for key, value in group.items() if key != "shapes"})[:-1]
            shape_rows = f"[\n{shapes}\n          ]" if shapes else "[]"
            entries.append(f'        {signature}, "shapes": {shape_rows}}}')
        groups = ",\n".join(entries)
        fields = ",\n".join(f"      {json.dumps(k)}: {json.dumps(v)}" for k, v in item.items() if k != "groups")
        layouts.append(f"    {json.dumps(key)}: {{\n{fields},\n      \"groups\": [\n{groups}\n      ]\n    }}")
    lines.extend([",\n".join(layouts), "  }", "}"])
    return "\n".join(lines) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("raw", type=Path, help="FModel export root containing Client/Content")
    parser.add_argument("game", type=Path, help="Client-Win64-Shipping.exe used to verify the supported build")
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.repo
    compat = read(root / "data/game_compat/ww_runtime.json")
    expected_build = compat["shared"]["SourceBuildSha256"]["value"]
    if digest(args.game) != expected_build:
        parser.error("installed executable differs from the repository compatibility profile")
    cert = read(root / "data/native_graphs/owned_ui_source_certificate.json")
    forms = read(root / "data/characters/character_forms.json")["forms"]
    selections = [(r["character_id"], r["profile_id"], r["ui_class_asset"]) for r in cert["bindings"]]
    selections += [(r["character_id"], r["profile_id"], r["load"]["anim_class_path"]) for r in forms]
    catalog = {"schema": 1, "source_build_sha256": expected_build, "characters": {}, "layouts": {}}
    for character, profile, asset in sorted(selections):
        key = f"{profile}:{asset}"
        if key not in catalog["layouts"]:
            try:
                result, issues = layout(root, args.raw, profile, asset)
            except (OSError, ValueError) as error:
                print(f"{character}: {asset}: {error}")
                continue
            for issue in issues:
                print(f"{character}: {issue}")
            if not result["groups"]:
                continue
            catalog["layouts"][key] = result
        catalog["characters"][character] = {"profile_id": profile, "layout": key}
    if not catalog["characters"]:
        parser.error("no certified capsule layouts could be derived")
    destination = root / "data/characters/capsule_stock.json"
    destination.write_text(format_catalog(catalog), encoding="utf-8", newline="\n")
    print(f"{len(catalog['characters'])}/{len(selections)} character selections have verified offline groups")


if __name__ == "__main__":
    main()
