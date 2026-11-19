"""Stack groups into aligned columns and give every group a text title.

    python build/examples/gh2_columns.py DUMP.json SPEC.json OPS.json

SPEC: {"top": 300, "gap": 56, "columns": [{"left": 160, "heading": "TEXTID or null", "groups": [["GROUPID", "Title"], ...]}]}
The dump must have been taken with the groups' built-in names already cleared.
"""
import json, sys
d = json.load(open(sys.argv[1], encoding='utf-8'))
spec = json.load(open(sys.argv[2], encoding='utf-8'))
objs = d['objects']
by = {o['id'][:8]: o for o in objs}


def rect(o):
    x, y, w, h = o['bounds']
    return x, y, x + w, y + h


ops = []
for col in spec['columns']:
    y = spec['top']
    for gid, title in col['groups']:
        g = by[gid]
        gx, gy, gw, gh = g['bounds']
        r = rect(g)
        ids = [i[:8] for i in g['content'] if i[:8] in by]
        # anything lying inside the outline belongs to the group, whether or not it was ever added to it
        strays = [o['id'][:8] for o in objs if o['kind'] != 'group' and o['id'][:8] not in ids
                  and r[0] <= rect(o)[0] and r[1] <= rect(o)[1] and r[2] >= rect(o)[2] and r[3] >= rect(o)[3]]
        for stray in strays:
            ops.append({"op": "group", "group": gid, "id": stray})
        ids += strays
        dx, dy = col['left'] - gx, y - gy
        if abs(dx) > 0.01 or abs(dy) > 0.01:
            ops.append({"op": "moveby", "ids": ids, "dx": dx, "dy": dy})
        key = "t_" + gid
        ops.append({"op": "add", "role": "heading", "text": title, "left": col['left'], "top": y - 30, "key": key})
        ops.append({"op": "title_over", "id": "@" + key, "of": gid})
        y += gh + spec['gap']
    if col.get('heading'):
        ops.append({"op": "heading_over", "id": col['heading'], "of": col['groups'][0][0]})
json.dump(ops, open(sys.argv[3], 'w', encoding='utf-8'), indent=1)
print(len(ops), 'ops')
