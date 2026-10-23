"""Filter the REA inspect-managed-members JSON dump down to the UISpineActor system.

Input : %TEMP%/csharp-members.json  (rea inspect-managed-members --format json --full-output)
Output: notes/uispineactor/members_subset.json + per-class CSVs

The dump is ~208 MB, so this loads it once with orjson and writes only the rows that
belong to the selected declaring types plus every call edge that touches them.
"""
import json
import os
import sys

import orjson

SRC = os.path.join(os.environ["TEMP"], "csharp-members.json")
OUT = os.path.dirname(os.path.abspath(__file__))

TARGETS = [
    "ActionSystem.UISpineActor",
    "ActionSystem.SpineActor",
    "UIModelShow",
]
# nested / helper types that define behaviour used by the three targets
TARGETS_EXTRA = [
    "ActionSystem.UISpineActor/IndexTransform",
    "UIModelShow/FollowPair",
    "ActionSystem.FollowSpineActor",
    "ActionSystem.ReplaceAttachmentReq",
]


def main() -> int:
    print(f"loading {SRC} ...", flush=True)
    with open(SRC, "rb") as fh:
        doc = orjson.loads(fh.read())
    nr = doc["data"]["normalized_result"]

    wanted = set(TARGETS) | set(TARGETS_EXTRA)
    types = [t for t in nr["types"] if t["full_name"] in wanted]
    found = {t["full_name"] for t in types}
    missing = sorted(wanted - found)

    fields = [f for f in nr["fields"] if f.get("declaring_type") in wanted]
    methods = [m for m in nr["methods"] if m.get("declaring_type") in wanted]
    method_tokens = {m["token"] for m in methods}

    # every edge that either leaves the selected types or enters them
    edges = [
        e
        for e in nr["call_edges"]
        if e["caller_token"] in method_tokens
        or e.get("target_declaring_type") in wanted
    ]
    accesses = [a for a in nr["field_accesses"] if a["method_token"] in method_tokens]

    subset = {
        "artifact": nr["artifact"],
        "module": nr["module"],
        "identity_scope": nr["identity_scope"],
        "missing_types": missing,
        "types": types,
        "fields": fields,
        "methods": methods,
        "call_edges": edges,
        "field_accesses": accesses,
    }
    with open(os.path.join(OUT, "members_subset.json"), "wb") as fh:
        fh.write(orjson.dumps(subset, option=orjson.OPT_INDENT_2))

    # method table: token, declaring type, name, signature, IL size + hash, calls out
    by_caller = {}
    for e in edges:
        by_caller.setdefault(e["caller_token"], []).append(
            f'{e.get("target_declaring_type") or "?"}::{e.get("target_name") or "?"}'
        )

    sig_of = {m["token"]: m for m in methods}
    for cls in TARGETS:
        rows = [m for m in methods if m.get("declaring_type") == cls]
        rows.sort(key=lambda m: m["token"])
        csv = os.path.join(OUT, cls.replace(".", "_") + "_methods.csv")
        with open(csv, "w", encoding="utf-8", newline="") as fh:
            fh.write("token,name,params,return,il_size,il_sha256,calls\n")
            for m in rows:
                sig = m["signature"]
                calls = ";".join(sorted(set(by_caller.get(m["token"], []))))
                params = "|".join(sig.get("parameter_types") or [])
                fh.write(
                    f'{m["token"]},{m["name"]},{params},{sig.get("return_type") or ""},'
                    f'{m["body"]["il_size"]},{m["body"].get("il_sha256") or ""},"{calls}"\n'
                )
        print(f"{cls}: {len(rows)} methods -> {csv}")

    print(f"edges kept: {len(edges)} / {len(nr['call_edges'])}")
    print(f"missing types: {missing}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
