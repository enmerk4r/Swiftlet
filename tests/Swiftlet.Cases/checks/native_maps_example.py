"""Build and verify native GH2 route/stop geometry against local fixtures only."""
import argparse,importlib.util,json,pathlib,sys,threading
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent))
from native_examples import ROOT,PRE,ROUNDTRIP,save_example
from cases import call,run_csharp
BUILD=r'''
Note("ROUTES + TRANSIT STOPS / GH2 / LOCAL FIXTURES\nRun python maps_fixture.py, then enable Route request and Stop request.\nExpected: both status 200, 5-vertex blue route, 3 orange stops.\nSynthetic coordinates; this is not navigation data.\nBoth requests are saved disabled. No API keys. See maps-gh2.md.",40,20);
dynamic Key(dynamic src,string field,string title,float x,float y){var k=Add("GetJsonObjectKeyComponent",title,x,y);Put(k,1,new[]{field});Wire(src,0,k,0);return k;}
dynamic Items(dynamic src,string title,float x,float y){var a=Add("ReadJsonArrayComponent",title,x,y);Wire(src,0,a,0);return a;}
dynamic Scalar(dynamic src,string title,float x,float y){var v=Add("ReadJsonValueComponent",title,x,y);Wire(src,0,v,0);return v;}
var request=Add("ParseJsonStringComponent","Route options",120,290);Put(request,0,new[]{"{\"origin\":{\"location\":{\"latLng\":{\"latitude\":40.75,\"longitude\":-73.99}}},\"destination\":{\"location\":{\"latLng\":{\"latitude\":40.758,\"longitude\":-73.98}}},\"travelMode\":\"DRIVE\",\"polylineEncoding\":\"GEO_JSON_LINESTRING\",\"computeAlternativeRoutes\":false}"});
var body=Add("CreateTextBodyComponent","Route JSON body",390,290);Wire(request,0,body,0);
var header=Add("CreateHttpHeaderComponent","Response field mask",680,100);Put(header,0,new[]{"X-Goog-FieldMask"});Put(header,1,new[]{"routes.duration,routes.distanceMeters,routes.polyline.geoJsonLinestring"});
var post=Add("PostRequestComponent","Route request",680,290);Put(post,0,new[]{"http://127.0.0.1:18746/directions/v2:computeRoutes"});Wire(body,0,post,1);Wire(header,0,post,3);Enable(post,false);Panel(post,0,"Route status",970,100);
var parsed=Add("ParseJsonStringComponent","Route response",970,290);Wire(post,1,parsed,0);
var routes=Items(Key(parsed,"routes","routes",1240,290),"Route items",1510,290);Wire(routes,0,Find("First route"),0);Put(Find("First route"),1,new[]{0});
var distance=Scalar(Key(Find("First route"),"distanceMeters","Distance",2050,100),"Metres",2320,100);Panel(distance,1,"Distance metres",2590,100);
var duration=Scalar(Key(Find("First route"),"duration","Duration",2050,290),"Duration text",2320,290);Panel(duration,0,"Duration seconds",2590,290);
var poly=Key(Find("First route"),"polyline","polyline",120,540);var geo=Key(poly,"geoJsonLinestring","GeoJSON LineString",390,540);var coords=Key(geo,"coordinates","Route coordinates",680,540);var points=Items(coords,"Coordinate pairs",970,540);var numbers=Items(points,"Route lon lat",970,780);
for(int k=0;k<2;k++){dynamic index=Find("Route coordinate "+k);Wire(numbers,0,index,0);Put(index,1,new[]{k});var val=Scalar(index,"Route value "+k,1510,730+170*k);Wire(val,1,Find("Route points"),k);}
Wire(Find("Route points"),0,Find("Flatten route"),0);Wire(Find("Flatten route"),0,Find("Route polyline"),0);Put(Find("Route polyline"),1,new[]{false});Wire(Find("Route polyline"),0,Find("Blue route"),0);
var get=Add("GetRequestComponent","Stop request",120,1340);Put(get,0,new[]{"http://127.0.0.1:18746/api/v2/rest/stops"});var query=Add("CreateQueryParamComponent","Stop search parameters",120,1130);Put(query,0,new[]{"lat","lon","radius"});Put(query,1,new[]{"40.754","-73.985","1000"});Wire(query,0,get,1);Enable(get,false);Panel(get,0,"Stop status",390,1150);
var stopJson=Add("ParseJsonStringComponent","Stop response",390,1340);Wire(get,1,stopJson,0);var stops=Items(Key(stopJson,"stops","stops",680,1340),"Stop items",970,1340);
var name=Scalar(Key(stops,"stop_name","Stop name",1240,1160),"Stop name text",1510,1160);Panel(name,0,"Stop names",1780,1160);
var geometry=Key(stops,"geometry","Stop geometry",1240,1340);var stopCoords=Key(geometry,"coordinates","Stop coordinates",1510,1340);var stopNumbers=Items(stopCoords,"Stop lon lat",1780,1340);
for(int k=0;k<2;k++){dynamic index=Find("Stop coordinate "+k);Wire(stopNumbers,0,index,0);Put(index,1,new[]{k});var val=Scalar(index,"Stop value "+k,2320,1340+170*k);Wire(val,1,Find("Stop points"),k);}
Wire(Find("Stop points"),0,Find("Orange stops"),0);
var ct=gh.GetType("Grasshopper2.Types.Colour.Colour");void Colour(dynamic c,double r,double g,double b){var v=ct.GetMethod("FromRgb").Invoke(null,new object[]{r,g,b,1f});var a=Array.CreateInstance(ct,1);a.SetValue(v,0);Put(c,1,a);}Colour(Find("Blue route"),.1,.4,.9);Colour(Find("Orange stops"),1,.5,.1);
Note("Coordinates: X = longitude, Y = latitude, Z = 0 (degrees).\nThis is a schematic, not a projected map; do not measure distances from the geometry.\nUse the returned distanceMeters field. Zoom to route/stop outputs to see the geometry.\nThe fixture always returns the same synthetic route and stops, regardless of options.\nLive Google Routes and Transitland requests are NOT VERIFIED.",40,1770);
doc.Solution.StartWait();
'''

def main():
 ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--port',required=True,type=int);a=ap.parse_args()
 spec=importlib.util.spec_from_file_location('maps_fixture',ROOT/'Examples/swiftlet-google-maps-api/maps_fixture.py');fixture=importlib.util.module_from_spec(spec);spec.loader.exec_module(fixture)
 server=fixture.ThreadingHTTPServer(('127.0.0.1',18746),fixture.Handler);worker=threading.Thread(target=server.serve_forever,daemon=True);worker.start()
 def cs(code):return run_csharp(a.port,PRE+code,timeout=60)
 natives=[('Item From Index','First route',1780,290),('Point XYZ','Route points',1780,800),('Flatten Tree','Flatten route',2050,800),('Polyline From Points','Route polyline',2320,800),('00000000-c6c7-4dc9-b89a-579fdfe6bcd5','Blue route',2590,800),('Point XYZ','Stop points',2590,1425),('00000000-c6c7-4dc9-b89a-579fdfe6bcd5','Orange stops',2860,1425)]
 for k in range(2):natives += [('Item From Index','Route coordinate '+str(k),1240,730+170*k),('Item From Index','Stop coordinate '+str(k),2050,1340+170*k)]
 try:
  call(a.port,'g2_start',{});cs('foreach(dynamic o in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray())doc.Objects.Remove(o);')
  for selector,key,x,y in natives:
   result,text=call(a.port,'g2_place_component',{'selector':selector,'x':x,'y':y});assert not result.get('isError'),text
   ident=json.loads(text)['id'];cs('foreach(dynamic o in doc.Objects.Forwards)if(o.InstanceId.ToString()=="'+ident+'")o.UserName='+json.dumps(key)+';')
  cs(BUILD)
  report={'roundtrip':save_example(a.port,cs,ROOT/'Examples/swiftlet-google-maps-api/swiftlet-google-maps-api.ghz','maps')}
  cs('foreach(var name in new[]{"Route request","Stop request"}){Check(Find(name).Activity.ToString()=="Disabled","saved disabled");Enable(Find(name),true);}doc.Solution.StartWait();Healthy();Check(Text(Find("Route request"),0)=="200"&&Text(Find("Stop request"),0)=="200","HTTP status");')
  for key,expected in [('Route points',fixture.ROUTE),('Orange stops',fixture.STOPS)]:
   actual=json.loads(cs('Console.WriteLine(JsonSerializer.Serialize(Out(Find("'+key+'"),0).Select(x=>{dynamic p=x;return new[]{(double)p.X,(double)p.Y,(double)p.Z};})));'))
   assert actual==[p+[0] for p in expected],(key,actual)
  line=json.loads(cs('dynamic line=Out(Find("Blue route"),0).Single();var pts=new List<double[]>();for(int k=0;k<(int)line.Count;k++)pts.Add(new[]{(double)line[k].X,(double)line[k].Y});Console.WriteLine(JsonSerializer.Serialize(pts));'))
  assert line==fixture.ROUTE
  names=json.loads(cs('Console.WriteLine(JsonSerializer.Serialize(Out(Find("Stop name text"),0)));'));assert names==['Fixture stop 1','Fixture stop 2','Fixture stop 3']
  cs('Check(Text(Find("Metres"),1)=="1200"&&Text(Find("Duration text"),0)=="240s","route metadata");Render(@"'+str(ROOT/'artifacts/native-examples/maps.png')+'");')
  assert sorted(r['method'] for r in fixture.Handler.requests)==['GET','POST']
  report.update(route_vertices=5,stop_points=3,coordinates_exact=True,stop_names=names,distance_m=1200,duration='240s',requests=2,live_services_verified=False)
  (ROOT/'artifacts/native-examples/maps-report.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
 finally:
  try:cs('doc.Solution.Stop();foreach(dynamic o in ((IEnumerable)doc.Objects.Forwards).Cast<object>().ToArray())doc.Objects.Remove(o);')
  finally:server.shutdown();server.server_close();worker.join()
if __name__=='__main__':main()
