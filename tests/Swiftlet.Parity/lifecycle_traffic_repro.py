"""Repeat only ordinary concurrent bursts to diagnose cross-component recompute loss."""
import argparse,concurrent.futures,json
from pathlib import Path
from lifecycle_stress import Stress
p=argparse.ArgumentParser();p.add_argument('--port',type=int,required=True);p.add_argument('--pid',type=int,required=True);p.add_argument('--cycles',type=int,default=200);p.add_argument('--report',type=Path,required=True);a=p.parse_args();s=Stress(a)
try:
 s.setup()
 with concurrent.futures.ThreadPoolExecutor(max_workers=40) as pool:
  for cycle in range(a.cycles):
   s.finish(s.burst(pool,cycle,True));s.command('restart-oauth');s.records.append({'cycle':cycle,'snapshot':s.command('snapshot')});s.save();print('PASS traffic-only',cycle,flush=True)
 s.report['status']='passed'
except Exception as e:
 s.report.update(status='failed',error=str(e));s.report['failure_snapshot']=s.command('snapshot');raise
finally:
 s.command('close');s.report['cleanup']=s.resources(set(),0);s.save();print(s.report['status'],s.report.get('error',''))
