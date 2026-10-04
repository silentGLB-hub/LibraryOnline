import os,json,urllib.request,urllib.error,http.cookiejar,uuid,secrets
base='http://localhost:5090'
class Client:
 def __init__(self):self.h=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()));self.token=''
 def call(self,path,method='GET',data=None,status=200):
  q=urllib.request.Request(base+path,data=json.dumps(data).encode() if data is not None else None,headers={'Content-Type':'application/json','X-CSRF-Token':self.token},method=method)
  try:r=self.h.open(q,timeout=60)
  except urllib.error.HTTPError as e:r=e
  body=r.read();assert r.status==status,(path,r.status,body[:200]);return json.loads(body)
 def session(self):
  d=self.call('/api/session');self.token=d['csrfToken'];return d
 def login(self,email,password,status=200):self.session();self.call('/api/session/login','POST',{'Email':email,'Password':password},status)
a=Client();a.login('admin@library.local',os.environ['LIBRARY_DEMO_PASSWORD']);a.session()
c=Client();c.session();email='qa-simple-'+uuid.uuid4().hex+'@library.local';password='Test1!'+secrets.token_hex(12)
c.call('/api/session/register','POST',{'Email':email,'Password':password,'FullName':'QA simplified accounts'})
user=next(x for x in a.call('/api/users') if x['Email']==email)
try:
 for i in range(5):c.login(email,'Wrong1!password',401)
 c.login(email,password,401);print('PASS lockout after five failures')
 a.call('/api/users/'+user['Id'],'PUT',{'Role':'Invalid','IsActive':True},409);print('PASS unknown role rejected')
 a.call('/api/users/'+user['Id'],'PUT',{'Role':'Librarian','IsActive':True})
 assert next(x for x in a.call('/api/users') if x['Id']==user['Id'])['role']=='Librarian';print('PASS single role updated')
finally:a.call('/api/users/'+user['Id'],'PUT',{'Role':'Member','IsActive':False})
