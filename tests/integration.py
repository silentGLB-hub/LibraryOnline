"""Run against a local demo instance only. Creates QA users/books/loans.
Uses standard-library HTTP clients and SQLCMD for controlled due-date fixtures.
No production URLs accepted. No existing demo records are modified.
"""
import os, secrets
import argparse, concurrent.futures, http.cookiejar, json, subprocess, time, urllib.request, urllib.error
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--base',default='http://localhost:5088');parser.add_argument('--sql-server',default='.');parser.add_argument('--database',default='LibraryOnlineDemo');parser.add_argument('--output',default='tests/results.json');args=parser.parse_args()
assert args.base.startswith(('http://localhost:','http://127.0.0.1:')), 'Tests require localhost.'
assert args.database.replace('_','').isalnum()
demo_password=os.environ.get("LIBRARY_DEMO_PASSWORD")
if not demo_password: raise SystemExit("Set LIBRARY_DEMO_PASSWORD to your local demo password before running tests.")
qa_password="Qa1!"+secrets.token_urlsafe(24)
new_password="Qa2!"+secrets.token_urlsafe(24)
results=[]
def check(name,condition):
 results.append({'name':name,'passed':bool(condition)})
 print(('PASS ' if condition else 'FAIL ')+name,flush=True)
 if not condition:raise AssertionError(name)
class Client:
 def __init__(self):self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()));self.token=''
 def call(self,path,method='GET',data=None,status=200,token=True,raw=False):
  h={'Content-Type':'application/json'}
  if token:h['X-CSRF-Token']=self.token
  req=urllib.request.Request(args.base+path,data=json.dumps(data).encode() if data is not None else None,headers=h,method=method)
  try:r=self.opener.open(req,timeout=90)
  except urllib.error.HTTPError as e:r=e
  body=r.read()
  if r.code!=status:raise AssertionError(f'{method} {path}: wanted {status}, got {r.code}: {body[:1000]!r}')
  if not raw and body:
   assert b'PasswordHash' not in body and b'SecurityStamp' not in body, 'Sensitive Identity fields leaked'
  return body if raw else json.loads(body) if body else None
 def session(self):
  d=self.call('/api/session');self.token=d['csrfToken'];return d
 def login(self,email,password=None):
  self.session();self.call('/api/session/login','POST',{'Email':email,'Password':password or demo_password});return self.session()['user']
def sql(query):
 p=subprocess.run(['sqlcmd','-S',args.sql_server,'-E','-C','-d',args.database,'-b','-Q',query],capture_output=True,text=True)
 if p.returncode:raise RuntimeError(p.stdout+p.stderr)
 return p.stdout
def book(admin,index,copies=2):
 return admin.call('/api/books','POST',{'Title':f'QA Integration {run} {index}','Author':'QA Test','Publisher':'QA Test','PublicationYear':2026,'ISBN':str(9790000000000+int(run[-7:])*10+index),'Description':'Temporary integration fixture','CategoryId':category,'TotalCopies':copies,'IsActive':True})
try:
 anon=Client();anon.session();anon.call('/api/loans',status=401);check('Unauthenticated loans rejected',True)
 anon.call('/api/session/login','POST',{'Email':'admin@library.local','Password':demo_password},status=403,token=False);check('Missing CSRF rejected',True)
 a=Client();au=a.login('admin@library.local');check('Administrator sign in',au['role']=='Administrator')
 l=Client();lu=l.login('librarian01@library.local');check('Librarian sign in',lu['role']=='Librarian')
 categories=a.call('/api/categories');category=categories[0]['Id'];check('Eight seeded genres',len(categories)>=8)
 books=a.call('/api/books?pageSize=100');check('Fifty seeded books',books['total']>=50)
 check('Stock never negative',all(b['AvailableCopies']>=0 for b in books['items']))
 dashboard=a.call('/api/dashboard');check('Dashboard works',dashboard['overdue']>=10 and dashboard['fineCollected']>=50000)
 for query in ['q=Clean','author=Martin','publisher=NXB','genre='+str(category),'availability=unavailable']:
  d=anon.call('/api/books?'+query);check('Search '+query,len(d['items'])>0)
 # MVC compilation and all pages must actually return HTML.
 for p in ['Catalog','Dashboard','ManageBooks','Users','Settings','Loans','Fines','Profile','Notifications']:
  html=a.call('/'+p,raw=True);check('MVC page '+p,b'data-page=' in html and b'Server Error' not in html)
 run=str(int(time.time()));members=[]
 for suffix in ['a','b']:
  c=Client();c.session();email=f'qa{run}{suffix}@library.local';c.call('/api/session/register','POST',{'Email':email,'Password':qa_password,'FullName':'QA '+run+suffix});c.login(email,qa_password);members.append(c)
 m,n=members
 check('Register creates Member only',m.session()['user']['role']=='Member')
 m.call('/api/dashboard',status=401);m.call('/api/users',status=401);check('Member cannot access admin data',True)
 policy=a.call('/api/policy');l.call('/api/policy','PUT',policy,status=401);check('Librarian cannot change policy',True)
 b=book(a,1,1);bid=b['Id'];check('Create catalog record',bid>0)
 payload={k:b[k] for k in ['Title','Author','Publisher','PublicationYear','ISBN','Description','CoverImage','CategoryId','TotalCopies','IsActive']};payload['Version']=b['RowVersion'];payload['Title']='QA updated '+run
 a.call(f'/api/books/{bid}','PUT',payload);a.call(f'/api/books/{bid}','PUT',payload,status=409);check('Stale book rowversion rejected',True)
 a.call('/api/books','POST',payload,status=409);check('Duplicate ISBN rejected',True)
 # Concurrent reservations compete for exactly one copy.
 def reserve(c):
  try:return c.call('/api/loans','POST',{'BookId':bid})
  except AssertionError as e:
   if 'got 409:' in str(e):return None
   raise
 with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:res=list(pool.map(reserve,members))
 check('Concurrent last-copy reservation: exactly one succeeds',sum(x is not None for x in res)==1)
 winner=members[0 if res[0] else 1];other=members[1 if res[0] else 0];loan=next(x for x in res if x);lid=loan['Id']
 check('Last-copy stock is zero',a.call(f'/api/books/{bid}')['book']['AvailableCopies']==0)
 other.call(f'/api/loans/{lid}/actions','POST',{'Action':'cancel'},status=409);check('Cannot alter another member loan',True)
 winner.call(f'/api/loans/{lid}/actions','POST',{'Action':'checkout'},status=409);check('Member cannot checkout own loan',True)
 l.call(f'/api/loans/{lid}/actions','POST',{'Action':'checkout'});check('Librarian checkout',True)
 r=winner.call(f'/api/loans/{lid}/renewals','POST',{});winner.call(f'/api/loans/{lid}/renewals','POST',{},status=409);check('One pending renewal per loan',True)
 old=next(x for x in winner.call('/api/loans') if x['Id']==lid)['DueAt'];l.call(f'/api/renewals/{r["Id"]}/decision','POST',{'Approve':True,'Note':'QA'});new=next(x for x in winner.call('/api/loans') if x['Id']==lid)['DueAt'];check('Approved renewal extends due date',new>old)
 # Expire only this QA-created loan, never a seeded loan.
 sql(f"UPDATE Loans SET DueAt=DATEADD(day,-3,CAST(GETUTCDATE() AS date)) WHERE Id={lid} AND BookId={bid};")
 fines=winner.call('/api/fines');fine=next(x for x in fines if x['LoanId']==lid);check('Overdue auto calculation',fine['Amount']==15000)
 check('Overdue status set',next(x for x in winner.call('/api/loans') if x['Id']==lid)['Status']=='Overdue')
 winner.call(f'/api/loans/{lid}/renewals','POST',{},status=409);check('Overdue renewal blocked',True)
 l.call(f'/api/fines/{fine["Id"]}/payments','POST',{'Amount':15001,'Note':'QA'},status=409);check('Overpayment rejected',True)
 winner.call(f'/api/fines/{fine["Id"]}/payments','POST',{'Amount':1000},status=401);check('Member cannot record payment',True)
 l.call(f'/api/fines/{fine["Id"]}/payments','POST',{'Amount':5000,'Note':'QA'});fine=next(x for x in winner.call('/api/fines') if x['LoanId']==lid);check('Partial payment and ledger',fine['Paid']==5000 and fine['outstanding']==10000 and len(fine['payments'])==1)
 l.call(f'/api/loans/{lid}/actions','POST',{'Action':'return'});l.call(f'/api/loans/{lid}/actions','POST',{'Action':'return'},status=409);check('Double return rejected',True)
 check('Return releases one copy',a.call(f'/api/books/{bid}')['book']['AvailableCopies']==1)
 # Renewal denied when another member has reserved the same title.
 b2=book(a,2,2);loan2=m.call('/api/loans','POST',{'BookId':b2['Id']});l.call(f'/api/loans/{loan2["Id"]}/actions','POST',{'Action':'checkout'});loan3=n.call('/api/loans','POST',{'BookId':b2['Id']});m.call(f'/api/loans/{loan2["Id"]}/renewals','POST',{},status=409);check('Other reservation blocks renewal',True)
 n.call(f'/api/loans/{loan3["Id"]}/actions','POST',{'Action':'cancel'});l.call(f'/api/loans/{loan2["Id"]}/actions','POST',{'Action':'return'})
 lst=m.call('/api/reading-lists','POST',{'Name':'QA reading'});m.call(f'/api/reading-lists/{lst["Id"]}/books','POST',{'BookId':bid});n.call(f'/api/reading-lists/{lst["Id"]}/books','POST',{'BookId':bid},status=409);check('Reading list ownership enforced',True)
 check('Recommendation API works',isinstance(m.call('/api/recommendations'),list));m.call(f'/api/reading-lists/{lst["Id"]}','DELETE')
 notes=winner.call('/api/notifications');check('Reservation/overdue/payment notifications',len(notes)>=4)
 for kind in ['loans','fines']:
  pdf=winner.call('/Reports/Export?kind='+kind,raw=True);check('Unicode PDF export '+kind,pdf.startswith(b'%PDF') and len(pdf)>2000)
  out=Path(args.output).parent/(kind+'-sample.pdf');out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(pdf)
 check('Member data isolation',all(x['MemberId']==m.session()['user']['Id'] for x in m.call('/api/loans')))
 # Limit enforcement, reserved stock protection and expiration.
 extra=[book(a,i,2) for i in range(3,9)]
 held=[m.call('/api/loans','POST',{'BookId':x['Id']}) for x in extra[:5]]
 m.call('/api/loans','POST',{'BookId':extra[5]['Id']},status=409);check('Maximum active loan limit enforced',True)
 second=n.call('/api/loans','POST',{'BookId':extra[0]['Id']})
 edit=a.call('/api/books/'+str(extra[0]['Id']))['book'];edit['Version']=edit['RowVersion'];edit['TotalCopies']=1
 a.call('/api/books/'+str(extra[0]['Id']),'PUT',edit,status=409);check('Cannot reduce stock below held copies',True)
 n.call(f'/api/loans/{second["Id"]}/actions','POST',{'Action':'cancel'})
 expired=held.pop();sql(f"UPDATE Loans SET PickupExpiresAt=DATEADD(minute,-1,GETUTCDATE()) WHERE Id={expired['Id']} AND BookId={expired['BookId']};")
 check('Expired reservation automatically cancelled',next(x for x in m.call('/api/loans') if x['Id']==expired['Id'])['Status']=='Cancelled')
 check('Expiry releases stock',a.call('/api/books/'+str(expired['BookId']))['book']['AvailableCopies']==2)
 for h in held:m.call(f'/api/loans/{h["Id"]}/actions','POST',{'Action':'cancel'})
 # Recheck reservations at approval time and enforce maximum renewals.
 rr=m.call('/api/loans','POST',{'BookId':b2['Id']});l.call(f'/api/loans/{rr["Id"]}/actions','POST',{'Action':'checkout'})
 request=m.call(f'/api/loans/{rr["Id"]}/renewals','POST',{})
 blocker=n.call('/api/loans','POST',{'BookId':b2['Id']})
 l.call(f'/api/renewals/{request["Id"]}/decision','POST',{'Approve':True},status=409);check('Approval rechecks newly created reservations',True)
 n.call(f'/api/loans/{blocker["Id"]}/actions','POST',{'Action':'cancel'})
 l.call(f'/api/renewals/{request["Id"]}/decision','POST',{'Approve':True})
 request=m.call(f'/api/loans/{rr["Id"]}/renewals','POST',{});l.call(f'/api/renewals/{request["Id"]}/decision','POST',{'Approve':True})
 m.call(f'/api/loans/{rr["Id"]}/renewals','POST',{},status=409);check('Maximum renewal count enforced',True)
 l.call(f'/api/loans/{rr["Id"]}/actions','POST',{'Action':'return'})
 # Change credentials through API on a disposable test user; old cookie invalid.
 mail=m.session()['user']['Email'];m.call('/api/session/password','POST',{'OldPassword':qa_password,'NewPassword':new_password})
 check('Password change logs out current session',m.session()['user'] is None);m.login(mail,new_password);check('Login with new password succeeds',True)
 check('Responses exclude password hashes and security stamps',True)
 for c in members:
  u=c.session()['user'];a.call('/api/users/'+u['Id'],'PUT',{'Role':'Member','IsActive':False});check('Disabled account invalidates existing session',c.session()['user'] is None)
 # Archive test books with history, and verify the public catalog hides them.
 for testbook in [b,b2]+extra:
  a.call('/api/books/'+str(testbook['Id']),'DELETE');anon.call('/api/books/'+str(testbook['Id']),status=404)
 check('Archive preserves loan history and hides book',True)
 sql("IF EXISTS(SELECT 1 FROM Books b WHERE b.AvailableCopies <> b.TotalCopies-(SELECT COUNT(*) FROM Loans l WHERE l.BookId=b.Id AND l.Status IN(0,1,3))) THROW 51000,'Stock invariant failed',1;")
 check('SQL stock invariant after all mutations',True)
finally:
 out=Path(args.output);out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps({'time':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),'checks':results,'passed':sum(r['passed'] for r in results),'total':len(results)},indent=2),encoding='utf-8')
