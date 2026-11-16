"""Compare archived GH1 MCP response adapter against its obsolete native counterpart, including exact sent results."""
import argparse,json,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'Swiftlet.Cases'))
from cases import run_csharp

p=argparse.ArgumentParser();p.add_argument('--port',type=int,required=True);p.add_argument('--report',type=Path,required=True);a=p.parse_args()
root=Path(__file__).resolve().parents[2]
cases=[]
for value in [{'unicode':'\u03bb','array':[1,True,None]},[1,'x'], 'boom', 17, True,None]:
    for error in [False,True]:
        cases.append({'name':f'{value!r} error={error}','gh1':'McpToolResponseComponent_ARCHIVED','inputs':[{'mcpcall':{}},{'json':json.dumps(value,ensure_ascii=False)},{'bool':error}], 'expectedValue':value,'isError':error})
source=(root/'tests/Swiftlet.Cases/runner.cs').read_text()
source=source.replace('var results = new List<object>();','''var results = new List<object>();
var captured = new List<object>();
Action<object> capture = value => captured.Add(((dynamic)value).ToJson().ToJsonString());''')
source=source.replace('System.Linq.Expressions.Expression.Constant(true),parameter).Compile();','''System.Linq.Expressions.Expression.Block(
                System.Linq.Expressions.Expression.Invoke(System.Linq.Expressions.Expression.Constant(capture),System.Linq.Expressions.Expression.Convert(parameter,typeof(object))),
                System.Linq.Expressions.Expression.Constant(true)),parameter).Compile();''')
source=source.replace('try { r[host] = run(c); }','try { captured.Clear(); var data = run(c); data["sentResults"]=captured.ToArray(); r[host] = data; }')
source=source.replace('__CASES_JSON__',json.dumps(cases,ensure_ascii=True).replace('"','""'))
stdout=run_csharp(a.port,source)
line=next(x for x in stdout.splitlines() if x.startswith('SWIFTLET_CASES_JSON:'))
results=json.loads(line.partition(':')[2]);problems=[]
for case,result in zip(cases,results):
    one,two=result['gh1'],result['gh2']
    if one.get('exception') or two.get('exception'):problems.append([case['name'],one,two]);continue
    if one['outputs']!=two['outputs'] or one['sentResults']!=two['sentResults']:problems.append([case['name'],'host mismatch',one,two]);continue
    if len(one['sentResults'])!=1:problems.append([case['name'],'no sent response',one]);continue
    sent=json.loads(one['sentResults'][0]);text=sent['content'][0]['text'];value=case['expectedValue']
    if sent.get('isError',False)!=case['isError']:problems.append([case['name'],'error flag',sent])
    if case['isError'] and isinstance(value,str):assert text==value,(case,sent)
    elif case['isError'] and value is None:assert text=='',(case,sent)
    else:assert json.loads(text)==('' if value is None else value),(case,sent)
report={'passed':len(results)-len(problems),'total':len(results),'problems':problems,'results':results}
a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(report,indent=2),encoding='utf-8')
print(f"{report['passed']}/{report['total']} exact archived response cases passed")
if problems:print(json.dumps(problems,indent=2));sys.exit(1)
