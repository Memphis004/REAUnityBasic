"""Caller/callee report for the UISpineActor system from members_subset.json."""
import os
from collections import defaultdict

import orjson

d = orjson.loads(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "members_subset.json"), "rb").read())
TARGET_TYPES = {"ActionSystem.SpineActor", "ActionSystem.UISpineActor", "UIModelShow", "ActionSystem.FollowSpineActor"}
tm = {m["token"]: m for m in d["methods"]}


def short(name):
    return name.rsplit(".", 1)[-1]


incoming = defaultdict(set)
outgoing = defaultdict(set)
for e in d["call_edges"]:
    caller = e.get("caller") or e["caller_token"]
    tgt = e.get("target_declaring_type"), e.get("target_name")
    if e["target_token"] in tm:
        incoming[e["target_token"]].add(caller)
    if e["caller_token"] in tm and tgt[0]:
        outgoing[e["caller_token"]].add(f"{short(tgt[0])}.{tgt[1]}")

print("== external callers into the system ==")
for tok, callers in incoming.items():
    ext = sorted(c for c in callers if c.rsplit(".", 1)[0] not in TARGET_TYPES)
    if ext:
        m = tm[tok]
        print(f"{short(m['declaring_type'])}.{m['name']} <- " + ", ".join(ext))

print()
print("== outgoing calls that leave the system ==")
for tok, callees in outgoing.items():
    ext = sorted(c for c in callees if c.split(".")[0] not in {"SpineActor", "UISpineActor", "UIModelShow", "FollowSpineActor"})
    if ext:
        m = tm[tok]
        print(f"{short(m['declaring_type'])}.{m['name']} -> " + ", ".join(ext))
