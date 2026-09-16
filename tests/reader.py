import urllib.request,urllib.error,http.cookiejar,json,os,time,subprocess
from pathlib import Path
base=os.getenv('LIBRARY_BASE_URL','http://localhost:5088');password=os.environ['LIBRARY_DEMO_PASSWORD'];checks=[];fixture=None;loan=None
assert base.startswith(('http://localhost:','http://127.0.0.1:'))
class Client:
 def __init__(self):self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()));self.token=''
 def get(self,path,method='GET',body=None,status=200):
  req=urllib.request.Request(base+path,data=json.dumps(body).encode() if body is not None else None,headers={'Content-Type':'application/json','X-CSRF-Token':self.token},method=method)
  try:r=self.opener.open(req,timeout=90)
  except urllib.error.HTTPError as e:r=e
  raw=r.read();assert r.code==status,(path,r.code,raw[:500]);assert b'PasswordHash' not in raw and b'SecurityStamp' not in raw
  if '/reader' in path or '/preview' in path: assert 'no-store' in r.headers.get('Cache-Control','') or r.code==401
  return json.loads(raw) if raw else None
 def login(self,email):
  self.token=self.get('/api/session')['csrfToken'];self.get('/api/session/login','POST',{'Email':email,'Password':password});self.token=self.get('/api/session')['csrfToken']
def ok(name,result=True):
 assert result,name;checks.append(name);print('PASS '+name,flush=True)
try:
 anon=Client();member=Client();staff=Client();outsider=Client();member.login('member01@library.local');staff.login('librarian01@library.local');outsider.login('member19@library.local')
 p=anon.get('/api/books/11/preview');ok('Public excerpt with demo label',p['isDemo'] and 'Chương 2' not in p['text'])
 info=anon.get('/api/books/11/information');ok('Author, work and publisher information',all(info[k] for k in ['authorBiography','workIntroduction','publisherInformation']))
 anon.get('/api/books/11/reader',status=401);ok('Anonymous cannot read full book')
 outsider.get('/api/books/11/reader',status=403);ok('Other member cannot use borrower access')
 d=member.get('/api/books/11/reader');ok('Active borrower reads first chapter',d['totalChapters']==3 and d['chapter']==1)
 d2=member.get('/api/books/11/reader?chapter=2');ok('Chapter navigation returns requested content',d2['chapter']==2 and d2['text']!=d['text'])
 member.get('/api/books/11/reader?chapter=0',status=400);member.get('/api/books/11/reader?chapter=99',status=400);ok('Invalid chapters rejected')
 member.get('/api/books/1/reader',status=403);ok('Overdue loan cannot read full book')
 member.get('/api/books/11/material',status=401);member.get('/api/books/11/material','PUT',{},401);ok('Member cannot access editor or publish')
 ok('Staff can inspect content without loan',staff.get('/api/books/11/reader')['staffAccess'])
 ok('Public endpoints never contain full text','FullText' not in json.dumps(anon.get('/api/books/11')) and 'FullText' not in json.dumps(info))
 category=anon.get('/api/categories')[0]['Id']
 fixture=staff.get('/api/books','POST',{'Title':'Reader verification fixture','Author':'Test Author','Publisher':'Test Publisher','ISBN':str(9780000000000+int(time.time())),'PublicationYear':2026,'Description':'Temporary reader verification fixture','CategoryId':category,'TotalCopies':1,'IsActive':True})['Id']
 mat={'AuthorBiography':'Author biography test','WorkIntroduction':'Work information test','PublisherInformation':'Publisher information test','PreviewText':'PUBLIC_SAMPLE','FullText':'Chapter One\n\nFULL_ONLY_SENTINEL\n---\nChapter Two\n\nSECOND_PRIVATE_SENTINEL','IsPublished':True,'IsDemo':False}
 staff.get(f'/api/books/{fixture}/material','PUT',mat);ok('Staff publishes content and separate excerpt')
 sample=anon.get(f'/api/books/{fixture}/preview');ok('Public preview has no private text',sample['text']=='PUBLIC_SAMPLE' and 'FULL_ONLY' not in json.dumps(sample))
 loan=outsider.get('/api/loans','POST',{'BookId':fixture})['Id'];outsider.get(f'/api/books/{fixture}/reader',status=403);ok('Reservation alone does not grant reading access')
 staff.get(f'/api/loans/{loan}/actions','POST',{'Action':'checkout'});ok('Checkout grants reading access','FULL_ONLY_SENTINEL' in outsider.get(f'/api/books/{fixture}/reader')['text'])
 mat['Version']=staff.get(f'/api/books/{fixture}/material')['Version'];mat['IsPublished']=False
 staff.get(f'/api/books/{fixture}/material','PUT',mat);outsider.get(f'/api/books/{fixture}/reader',status=404);anon.get(f'/api/books/{fixture}/preview',status=404);ok('Unpublishing revokes full content and preview')
 staff.get(f'/api/books/{fixture}/material','PUT',mat,409);ok('Stale editor update rejected')
 mat['Version']=staff.get(f'/api/books/{fixture}/material')['Version'];mat['IsPublished']=True;staff.get(f'/api/books/{fixture}/material','PUT',mat)
 staff.get(f'/api/loans/{loan}/actions','POST',{'Action':'return'});outsider.get(f'/api/books/{fixture}/reader',status=403);ok('Return revokes reading access immediately')
 ok('Returned member access flag is false',not outsider.get(f'/api/books/{fixture}/information')['canRead'])
 mat['FullText']='';mat['Version']=staff.get(f'/api/books/{fixture}/material')['Version'];staff.get(f'/api/books/{fixture}/material','PUT',mat,409);ok('Cannot publish empty content')
 mat['FullText']='x'*500001;staff.get(f'/api/books/{fixture}/material','PUT',mat,400);ok('Content length validation enforced')
finally:
 # Remove only this test's newly-created fixture, preserving all demo/user data.
 if fixture:
  query=f"SET XACT_ABORT ON; BEGIN TRAN; IF EXISTS(SELECT 1 FROM Books WHERE Id={fixture} AND Title=N'Reader verification fixture') BEGIN DELETE FROM Notifications WHERE EventKey IN ('reserved:{loan}','loan:{loan}:checkout','loan:{loan}:return'); DELETE FROM Loans WHERE Id={loan or -1} AND BookId={fixture}; DELETE FROM Books WHERE Id={fixture}; END; COMMIT;"
  subprocess.run(['sqlcmd','-S','.', '-E','-C','-d','LibraryOnlineDemo','-b','-Q',query],check=True,capture_output=True)
 out=Path('TestResults/reader.json');out.parent.mkdir(exist_ok=True);out.write_text(json.dumps({'passed':len(checks),'checks':checks},indent=2),encoding='utf-8')
print('All',len(checks),'reader checks passed.')
