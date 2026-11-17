"""List the members of one group (or every object) from a gh2_layout dump, row by row.

    python build/examples/gh2_members.py DUMP.json [GROUPID]
"""
import json, sys
d = json.load(open(sys.argv[1], encoding='utf-8'))
by = {o['id']: o for o in d['objects']}
if len(sys.argv) > 2:
    g = next(o for o in d['objects'] if o['id'].startswith(sys.argv[2]))
    mem = [by[i] for i in g['content'] if i in by]
    print('group', g['id'][:8], g['bounds'], repr(g['name']))
else:
    mem = [o for o in d['objects'] if o['kind'] != 'group']
for o in sorted(mem, key=lambda o: (round(o['bounds'][1] / 40), o['bounds'][0])):
    x, y, w, h = o['bounds']
    src = ','.join(sorted({w_['source'][:4] for w_ in o['wires']}))
    print(f"{o['id'][:8]} {o['kind'][:1]} x={x:7.0f} y={y:7.0f} w={w:5.0f} h={h:5.0f} {o['type'][:24]:24} {(o.get('text') or o['name'])[:30]!r:32} <- {src}")
print(len(mem), 'objects')
