# Réplica en Python, línea por línea, de la lógica no visual de RipleyComprasMAUI
# (estructuras, InputHelper, carrito/deshacer, cola, importes, búsqueda y Dijkstra).
# Sirve para verificar los números del informe en un entorno sin .NET.
import unicodedata, math
from decimal import Decimal, ROUND_HALF_UP, getcontext
getcontext().prec = 28   # mismo número de dígitos significativos que System.Decimal

# ---------------- Structures ----------------
class Nodo:
    def __init__(s, d): s.dato=d; s.sig=None
class ListaEnlazada:
    def __init__(s): s.cab=None; s.ult=None; s.cant=0
    def agregar_final(s,d):
        n=Nodo(d)
        if s.ult is None: s.cab=n; s.ult=n
        else: s.ult.sig=n; s.ult=n
        s.cant+=1
    def agregar_inicio(s,d):
        n=Nodo(d); n.sig=s.cab; s.cab=n
        if s.ult is None: s.ult=n
        s.cant+=1
    def insertar_en(s,pos,d):
        if pos<=0 or s.cab is None: s.agregar_inicio(d); return
        if pos>=s.cant: s.agregar_final(d); return
        ant=s.cab
        for _ in range(pos-1): ant=ant.sig
        n=Nodo(d); n.sig=ant.sig; ant.sig=n; s.cant+=1
    def buscar(s,cond):
        a=s.cab
        while a:
            if cond(a.dato): return a.dato
            a=a.sig
        return None
    def eliminar(s,cond):
        ant=None; a=s.cab; pos=0
        while a:
            if cond(a.dato):
                if ant is None: s.cab=a.sig
                else: ant.sig=a.sig
                if a is s.ult: s.ult=ant
                s.cant-=1
                return True,a.dato,pos
            ant=a; a=a.sig; pos+=1
        return False,None,-1
    def limpiar(s): s.cab=None; s.ult=None; s.cant=0
    def __iter__(s):
        a=s.cab
        while a: yield a.dato; a=a.sig
class Pila:
    def __init__(s): s.cima=None; s.cant=0
    def apilar(s,d): n=Nodo(d); n.sig=s.cima; s.cima=n; s.cant+=1
    def desapilar(s):
        d=s.cima.dato; s.cima=s.cima.sig; s.cant-=1; return d
    def vacia(s): return s.cima is None
class Cola:
    def __init__(s): s.fr=None; s.fi=None; s.cant=0
    def encolar(s,d):
        n=Nodo(d)
        if s.fi is None: s.fr=n; s.fi=n
        else: s.fi.sig=n; s.fi=n
        s.cant+=1
    def desencolar(s):
        d=s.fr.dato; s.fr=s.fr.sig
        if s.fr is None: s.fi=None
        s.cant-=1; return d
    def vacia(s): return s.fr is None
    def __iter__(s):
        a=s.fr
        while a: yield a.dato; a=a.sig
class NA:
    def __init__(s,k,v): s.k=k; s.v=v; s.izq=None; s.der=None; s.alt=1
class AVL:
    def __init__(s): s.raiz=None; s.cant=0; s.rot=0; s.comp=0
    @staticmethod
    def h(n): return 0 if n is None else n.alt
    def act(s,n): n.alt=1+max(s.h(n.izq),s.h(n.der))
    def fe(s,n): return s.h(n.izq)-s.h(n.der)
    def rd(s,n):
        hijo=n.izq; n.izq=hijo.der; hijo.der=n; s.act(n); s.act(hijo); s.rot+=1; return hijo
    def ri(s,n):
        hijo=n.der; n.der=hijo.izq; hijo.izq=n; s.act(n); s.act(hijo); s.rot+=1; return hijo
    def bal(s,n):
        s.act(n); f=s.fe(n)
        if f>1:
            if s.fe(n.izq)<0: n.izq=s.ri(n.izq)
            return s.rd(n)
        if f<-1:
            if s.fe(n.der)>0: n.der=s.rd(n.der)
            return s.ri(n)
        return n
    def insertar(s,k,v):
        r=[False]
        def ins(n):
            if n is None: r[0]=True; return NA(k,v)
            if k<n.k: n.izq=ins(n.izq)
            elif k>n.k: n.der=ins(n.der)
            else: return n
            return s.bal(n)
        s.raiz=ins(s.raiz)
        if r[0]: s.cant+=1
        return r[0]
    def buscar(s,k):
        s.comp=0; a=s.raiz
        while a:
            s.comp+=1
            if k==a.k: return True,a.v
            a = a.izq if k<a.k else a.der
        return False,None
    def inorden(s):
        res=ListaEnlazada()
        def rec(n):
            if n is None: return
            rec(n.izq); res.agregar_final(n.v); rec(n.der)
        rec(s.raiz); return res
    def altura(s): return s.h(s.raiz)

# ---------------- InputHelper ----------------
def es_digito(c): return '0'<=c<='9'
def solo_digitos(t): return all(es_digito(c) for c in t)
def entero_con_signo(t):
    ini=1 if t[0] in '+-' else 0
    if ini==len(t): return False
    return all(es_digito(c) for c in t[ini:])
def try_leer_entero(texto,mn,mx):
    l=(texto or '').strip()
    if not l: return False,None
    if not entero_con_signo(l): return False,None
    v=int(l)
    if v<-2**31 or v>2**31-1: return False,None
    if v<mn or v>mx: return False,None
    return True,v
def preparar_numero(texto,maxdec):
    l=(texto or '').strip()
    if not l: return False,None
    res=''; sep=0; ent=0; dec=0
    for i,c in enumerate(l):
        if c in '+-' and i==0: res+=c; continue
        if c in '.,':
            sep+=1
            if sep>1: return False,None
            res+='.'; continue
        if not es_digito(c): return False,None
        if sep==0: ent+=1
        else: dec+=1
        res+=c
    if ent==0: return False,None
    if sep==1 and dec==0: return False,None
    if dec>maxdec: return False,None
    return True,res
def try_leer_decimal(t,mn,mx):
    ok,n=preparar_numero(t,2)
    if not ok: return False,None
    v=Decimal(n)
    if v<mn or v>mx: return False,None
    return True,v
def try_leer_double(t,mn,mx,maxdec):
    ok,n=preparar_numero(t,maxdec)
    if not ok: return False,None
    v=float(n)
    if not math.isfinite(v) or v<mn or v>mx: return False,None
    return True,v
def es_codigo(c): return c is not None and len(c)==13 and solo_digitos(c)
def es_dni(d): return d is not None and len(d)==8 and solo_digitos(d)
def es_marca(c): return unicodedata.category(c)=='Mn'
def unir_espacios(t):
    r=''; p=False
    for c in (t or ''):
        if c.isspace(): p=len(r)>0; continue
        if p: r+=' '; p=False
        r+=c
    return r
def normalizar(t):
    d=unicodedata.normalize('NFD', t or '')
    r=''; p=False
    for c in d:
        if es_marca(c): continue
        if c.isspace(): p=len(r)>0; continue
        if p: r+=' '; p=False
        r+=c.upper()
    return r
def nombre_contiene(nombre,consulta):
    q=normalizar(consulta)
    return len(q)>0 and q in normalizar(nombre)
def validar_busqueda(t):
    q=unir_espacios(t)
    if not q or len(q)>40: return False,q
    for c in q:
        if not (c.isalnum() or c==' ' or es_marca(c) or c in "-./&'()#"): return False,q
    return True,q
def validar_nombre_persona(t):
    l=unir_espacios(t)
    if not l: return True,None
    if len(l)<2 or len(l)>60 or not l[0].isalpha(): return False,None
    for c in l:
        if not (c.isalpha() or c in " -'’" or es_marca(c)): return False,None
    return True,l
def validar_correo(t):
    l=(t or '').strip()
    if not l: return True,None
    if len(l)>80: return False,None
    a=l.find('@'); una = a>0 and a==l.rfind('@')
    dom = l[a+1:] if una else ''
    domok = '.' in dom and not dom.startswith('.') and not dom.endswith('.') and '..' not in dom
    okc = all((c.isascii() and c.isalnum()) or c in '@._-+' for c in l)
    return (una and domok and okc), (l if (una and domok and okc) else None)

# ---------------- Servicios (lógica) ----------------
def redondear(x): return x.quantize(Decimal('0.01'), rounding=ROUND_HALF_UP)
class Item:
    def __init__(s,cod,nom,cant,pr,prr): s.cod=cod; s.nom=nom; s.cant=cant; s.pr=pr; s.prr=prr; s.pa=Decimal('0')
    def copiar(s):
        i=Item(s.cod,s.nom,s.cant,s.pr,s.prr); i.pa=s.pa; return i
class Contexto:
    def __init__(s,ref): s.ref=ref; s.lista=ListaEnlazada(); s.pila=Pila()
class Catalogo:
    def __init__(s,prods):
        s.arbol=AVL()
        for p in prods: s.arbol.insertar(p['Codigo'],dict(p))
    def obtener(s,cod): return s.arbol.buscar(cod)
    def stock(s,cod):
        ok,p=s.obtener(cod); return p['Stock'] if ok else 0
class Carrito:
    def __init__(s,cat): s.cat=cat
    def agregar(s,c,cod,cant):
        if not es_codigo(cod) or cant<1 or cant>99: return False
        ok,p=s.cat.obtener(cod)
        if not ok: return False
        ex=c.lista.buscar(lambda a: a.cod==cod)
        ant=0 if ex is None else ex.cant; nueva=ant+cant
        if nueva>99 or nueva>p['Stock']: return False
        if ex is None:
            c.lista.agregar_final(Item(cod,p['Nombre'],cant,Decimal(str(p['PrecioRegular'])),Decimal(str(p['PrecioTarjetaRipley']))))
            c.pila.apilar(('AgregarLinea',cod,0,0,None))
        else:
            c.pila.apilar(('CambiarCantidad',cod,ant,0,None)); ex.cant=nueva
        return True
    def cambiar(s,c,cod,nueva):
        a=c.lista.buscar(lambda x: x.cod==cod)
        if a is None or nueva<1 or nueva>99 or nueva>s.cat.stock(cod) or nueva==a.cant: return False
        c.pila.apilar(('CambiarCantidad',cod,a.cant,0,None)); a.cant=nueva; return True
    def quitar(s,c,cod):
        ok,q,pos=c.lista.eliminar(lambda x: x.cod==cod)
        if not ok: return False
        c.pila.apilar(('QuitarLinea',cod,0,pos,q.copiar())); return True
    def deshacer(s,c):
        if c.pila.vacia(): return False
        t,cod,ant,pos,art=c.pila.desapilar()
        if t=='AgregarLinea': c.lista.eliminar(lambda x: x.cod==cod)
        elif t=='CambiarCantidad':
            a=c.lista.buscar(lambda x: x.cod==cod)
            if a: a.cant=ant
        else: c.lista.insertar_en(pos, art.copiar())
        return True
    def total_regular(s,c): return sum((a.pr*a.cant for a in c.lista), Decimal('0'))
def calcular_importes(articulos,tarjeta):
    tr=Decimal('0'); tf=Decimal('0')
    for a in articulos:
        a.pa = a.prr if tarjeta=='TarjetaRipley' else a.pr
        tr+=a.pr*a.cant; tf+=a.pa*a.cant
    og=redondear(tf/Decimal('1.18'))
    return dict(TotalRegular=tr,TotalFinal=tf,Ahorro=tr-tf,OperacionGravada=og,Igv=tf-og)

# ---------------- Grafo ----------------
class Grafo:
    def __init__(s): s.nombres=[]; s.ady=[]
    def agregar(s,n): s.nombres.append(n); s.ady.append(ListaEnlazada()); return len(s.nombres)-1
    def conectar(s,a,b,p):
        s.ady[a].agregar_final({'d':b,'p':p,'act':True}); s.ady[b].agregar_final({'d':a,'p':p,'act':True})
    def estado(s,a,b,act):
        for x,y in ((a,b),(b,a)):
            e=s.ady[x].buscar(lambda e: e['d']==y); e['act']=act
    def ruta(s,o,d):
        n=len(s.nombres); dist=[math.inf]*n; prev=[-1]*n; vis=[False]*n; dist[o]=0
        for _ in range(n):
            u=-1
            for i in range(n):
                if not vis[i] and (u==-1 or dist[i]<dist[u]): u=i
            if u==-1 or math.isinf(dist[u]): break
            vis[u]=True
            if u==d: break
            for e in s.ady[u]:
                if not e['act'] or vis[e['d']]: continue
                c=dist[u]+e['p']
                if c<dist[e['d']]: dist[e['d']]=c; prev[e['d']]=u
        if math.isinf(dist[d]): return None,None
        r=ListaEnlazada(); x=d
        while x!=-1: r.agregar_inicio(x); x=prev[x]
        return [s.nombres[i] for i in r], dist[d]
def grafo_ejemplo():
    g=Grafo()
    c511=g.agregar('Caja 511'); c512=g.agregar('Caja 512'); c513=g.agregar('Caja 513')
    srv=g.agregar('Servidor de tienda'); alm=g.agregar('Almacén'); cen=g.agregar('Central (simulada)')
    g.conectar(c511,c512,4); g.conectar(c511,c513,9); g.conectar(c512,c513,3); g.conectar(c512,srv,10)
    g.conectar(c513,srv,4); g.conectar(c513,alm,7); g.conectar(srv,cen,15); g.conectar(alm,cen,20)
    return g
