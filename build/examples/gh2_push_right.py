"""Make room after value boxes that were widened: shift what is to the right of them, within their group.

    python build/examples/gh2_push_right.py DUMP.json OPS.json [CLEARANCE]
"""
import json, sys
d = json.load(open(sys.argv[1], encoding='utf-8'))
clear = float(sys.argv[3]) if len(sys.argv) > 3 else 24
objs = d['objects']
by = {o['id']: o for o in objs}
ops = []
for g in [o for o in objs if o['kind'] == 'group']:
    gx, gy, gw, gh = g['bounds']
    members = [o for o in objs if o['kind'] != 'group' and (o['id'] in g['content'] or (
        gx <= o['bounds'][0] and gy <= o['bounds'][1] and gx + gw >= o['bounds'][0] + o['bounds'][2] and gy + gh >= o['bounds'][1] + o['bounds'][3]))]
    ids = {m['id'] for m in members}
    need = 0
    edge = None
    for target in members:
        for w in target['wires']:
            src = by.get(w['source'])
            if not src or src['id'] not in ids or src['type'] != 'ValueObject':
                continue
            right = src['bounds'][0] + src['bounds'][2]
            short = right + clear - target['bounds'][0]
            if short > need:
                need = short
            if short > 0:
                edge = target['bounds'][0] if edge is None else min(edge, target['bounds'][0])
    if need > 0:
        moved = [m['id'][:8] for m in members if m['bounds'][0] >= edge - 0.5]
        ops.append({"op": "moveby", "ids": moved, "dx": round(need), "dy": 0})
        print(g['id'][:8], 'shift', len(moved), 'objects right by', round(need))
json.dump(ops, open(sys.argv[2], 'w', encoding='utf-8'), indent=1)
