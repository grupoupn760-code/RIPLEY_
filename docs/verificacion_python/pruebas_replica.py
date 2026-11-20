# Pruebas de la réplica Python de la lógica de RipleyComprasMAUI (verificación hecha sin .NET).
# Ejecutar: python pruebas_replica.py
import json, sys, os, datetime
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from replica import *

ok=0; falla=0
def comprobar(nombre, cond, det=''):
    global ok, falla
    if cond: ok+=1; print('[OK]   ', nombre)
    else: falla+=1; print('[FALLA]', nombre, det)

semilla=json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "RipleyComprasMAUI", "Resources", "Raw", "productos.json"), encoding='utf-8'))
FEP='2015373474714'; ESTRE='2015373379330'; FALDA='2015372278467'; JEANS='2015372601050'
CUERNO='2072369066599'; DESTAR='2015371440865'; DETEJ='2015371441190'

print('== Estructuras ==')
c=Cola(); [c.encolar(i) for i in (1,2,3)]
comprobar('Cola FIFO', c.desencolar()==1 and c.fr.dato==2 and c.cant==2)
l=ListaEnlazada(); [l.agregar_final(x) for x in (10,20,30)]
_,_,pos=l.eliminar(lambda x:x==30); l.agregar_final(40); l.insertar_en(1,15)
comprobar('Lista: quitar último y reinsertar', pos==2 and list(l)==[10,15,20,40], list(l))
a=AVL(); [a.insertar(i,i) for i in range(1,101)]
comprobar('AVL 100 claves ascendentes: altura 7', a.altura()==7, a.altura())
comprobar('AVL clave repetida', not a.insertar(50,0) and a.cant==100)

print('== Árbol del catálogo ==')
cat=Catalogo(semilla)
comprobar('Orden de la semilla ascendente', [p['Codigo'] for p in semilla]==sorted(p['Codigo'] for p in semilla))
comprobar('Altura 3', cat.arbol.altura()==3, cat.arbol.altura())
comprobar('Raíz JEANS', cat.arbol.raiz.k==JEANS, cat.arbol.raiz.k)
comprobar('4 rotaciones', cat.arbol.rot==4, cat.arbol.rot)
okb,p=cat.obtener(FEP)
comprobar('FEPAPAYA en 2 comparaciones', okb and cat.arbol.comp==2 and p['PrecioTarjetaRipley']==39.99, cat.arbol.comp)
r=cat.arbol.raiz
print('   forma:', r.k, '| izq', r.izq.k, (r.izq.izq.k, r.izq.der.k), '| der', r.der.k, (r.der.izq.k, r.der.der.k))
okb,_=cat.obtener('9999999999999'); comprobar('9999999999999 no encontrado', not okb)
comprobar('20153734 inválido', not es_codigo('20153734'))

def buscar_nombre(cat,t):
    v,q=validar_busqueda(t)
    if not v: return None
    return [p['Codigo'] for p in cat.arbol.inorden() if nombre_contiene(p['Nombre'],q)]
for vte in ('POL MC FEPAPAYA INX','Pol Mc Fepapaya Inx','Pól MC Fepápaya Inx'):
    comprobar(f'Nombre "{vte}"', buscar_nombre(cat,vte)==[FEP], buscar_nombre(cat,vte))
comprobar('Parcial "pol mc"', buscar_nombre(cat,'pol mc')==[ESTRE,FEP])
comprobar('Consulta vacía', buscar_nombre(cat,'   ') is None)
comprobar('Sin coincidencias', buscar_nombre(cat,'zapatilla')==[])

print('== Polo Rojo (catálogo ficticio) ==')
polo=Catalogo([
 {'Codigo':'2000000000001','Nombre':'Polo Rojo','PrecioRegular':30.0,'PrecioTarjetaRipley':25.0,'Stock':5},
 {'Codigo':'2000000000002','Nombre':'POLO ROJO MANGA LARGA','PrecioRegular':35.0,'PrecioTarjetaRipley':35.0,'Stock':5},
 {'Codigo':'2000000000003','Nombre':'Polo Azul','PrecioRegular':30.0,'PrecioTarjetaRipley':30.0,'Stock':5},
 {'Codigo':'2000000000004','Nombre':'Pantalón rojo','PrecioRegular':70.0,'PrecioTarjetaRipley':60.0,'Stock':5}])
esperado=buscar_nombre(polo,'POLO ROJO')
for q in ('POLO ROJO','Polo Rojo','polo rojo','Poló rojo','  póló   RÓJO  ','Poló rojo','Poló rojo'):
    comprobar(f'"{q.encode("unicode_escape").decode()}" mismo resultado', buscar_nombre(polo,q)==esperado, buscar_nombre(polo,q))
comprobar('Esperado = 001,002', esperado==['2000000000001','2000000000002'], esperado)

print('== Carrito y deshacer ==')
car=Carrito(cat); k=Contexto('Carrito 1')
car.agregar(k,FEP,1); car.agregar(k,ESTRE,1); car.agregar(k,FALDA,1); car.agregar(k,FALDA,1)
comprobar('3 líneas, FALDA x2', k.lista.cant==3 and k.lista.buscar(lambda a:a.cod==FALDA).cant==2)
comprobar('Total 289.96', car.total_regular(k)==Decimal('289.96'), car.total_regular(k))
car.deshacer(k); comprobar('Deshacer aumento → 199.97', car.total_regular(k)==Decimal('199.97'))
car.cambiar(k,FALDA,2); car.quitar(k,ESTRE)
comprobar('FEP1+FALDA2 = 239.97', car.total_regular(k)==Decimal('239.97') and [a.cod for a in k.lista]==[FEP,FALDA])
car.deshacer(k); comprobar('ESTRE vuelve a su posición', [a.cod for a in k.lista]==[FEP,ESTRE,FALDA])
car.deshacer(k); comprobar('FALDA vuelve a 1', k.lista.buscar(lambda a:a.cod==FALDA).cant==1)
car.deshacer(k); car.deshacer(k); car.deshacer(k)
comprobar('Todo deshecho: lista y pila vacías', k.lista.cant==0 and k.pila.vacia())
comprobar('Pila vacía: Deshacer falla', not car.deshacer(k))
car.agregar(k,CUERNO,1); n=k.pila.cant
comprobar('Exceso de stock / 0 / misma cantidad rechazados sin apilar',
  not car.agregar(k,CUERNO,20) and not car.agregar(k,CUERNO,0) and not car.cambiar(k,CUERNO,0) and not car.cambiar(k,CUERNO,1) and k.pila.cant==n)

print('== Validaciones ==')
for t in ('0','-1','1.5','100','abc','','99999999999','1 2'):
    comprobar(f'Cantidad "{t}" rechazada', not try_leer_entero(t,1,99)[0])
comprobar('" 7 " aceptado', try_leer_entero(' 7 ',1,99)==(True,7))
comprobar('12,50 → 12.50', try_leer_decimal('12,50',Decimal('0.01'),Decimal('1000'))==(True,Decimal('12.50')))
comprobar('1,234 rechazado', not try_leer_decimal('1,234',Decimal('0.01'),Decimal('100000'))[0])
comprobar('1.234,56 rechazado', not try_leer_decimal('1.234,56',Decimal('0.01'),Decimal('100000'))[0])
comprobar('NaN rechazado', not try_leer_double('NaN',0.1,1000,2)[0])
comprobar('4.5 aceptado', try_leer_double('4.5',0.1,1000,2)==(True,4.5))
comprobar('DNI 01234567 válido', es_dni('01234567')); comprobar('DNI 1234 inválido', not es_dni('1234'))
comprobar('Correo abc inválido', not validar_correo('abc')[0]); comprobar('Correo de prueba válido', validar_correo('cliente.prueba@correo.com')[0])
comprobar('Nombre con tildes/ñ/guion/apóstrofo', validar_nombre_persona("  María-José   O'Higgins Ñuñez ")==(True,"María-José O'Higgins Ñuñez"))

print('== Importes ==')
def articulos(*pares):
    res=[]
    for cod,cant in pares:
        _,p=cat.obtener(cod); res.append(Item(cod,p['Nombre'],cant,Decimal(str(p['PrecioRegular'])),Decimal(str(p['PrecioTarjetaRipley']))))
    return res
A=articulos((FEP,1),(ESTRE,1),(FALDA,1))
o=calcular_importes(A,'OtraTarjeta'); comprobar('A Otra: 199.97 / 0.00', o['TotalFinal']==Decimal('199.97') and o['Ahorro']==0)
rp=calcular_importes(A,'TarjetaRipley')
comprobar('A Ripley: 199.97 / 179.97 / 20.00', (rp['TotalRegular'],rp['TotalFinal'],rp['Ahorro'])==(Decimal('199.97'),Decimal('179.97'),Decimal('20.00')))
comprobar('A Ripley: OG 152.52 IGV 27.45', (rp['OperacionGravada'],rp['Igv'])==(Decimal('152.52'),Decimal('27.45')), (rp['OperacionGravada'],rp['Igv']))
B=articulos((JEANS,1),(CUERNO,1),(DESTAR,1),(DETEJ,1))
for t in ('TarjetaRipley','OtraTarjeta'):
    b=calcular_importes(B,t)
    comprobar(f'B {t}: 149.96, 0.00, 127.08, 22.88', (b['TotalFinal'],b['Ahorro'],b['OperacionGravada'],b['Igv'])==(Decimal('149.96'),Decimal('0.00'),Decimal('127.08'),Decimal('22.88')), b)

print('== Cola: pico de 12 ==')
fila=Cola(); cods=[FEP,ESTRE,FALDA,JEANS,CUERNO,DESTAR,DETEJ]
for i in range(1,13):
    ctx=Contexto(f'Carrito {i}'); Carrito(cat).agregar(ctx,cods[(i-1)%7],1)
    fila.encolar({'turno':i,'art':[x.copiar() for x in ctx.lista]})
orden=[]; mezcla=False
while not fila.vacia():
    v=fila.desencolar(); orden.append(v['turno'])
    if len(v['art'])!=1 or v['art'][0].cod!=cods[(v['turno']-1)%7]: mezcla=True
comprobar('Turnos 1..12 en orden', orden==list(range(1,13)))
comprobar('Sin mezcla de artículos', not mezcla)

print('== Grafo ==')
g=grafo_ejemplo()
ruta,d=g.ruta(0,5); comprobar('511→Central 26', d==26 and ruta==['Caja 511','Caja 512','Caja 513','Servidor de tienda','Central (simulada)'], (ruta,d))
g.estado(3,5,False); ruta,d=g.ruta(0,5); comprobar('Servidor–Central caído: 34 por Almacén', d==34, (ruta,d))
g.estado(4,5,False); ruta,d=g.ruta(0,5); comprobar('Sin enlaces: no hay ruta', ruta is None)

print()
print(f'Réplica Python: {ok} correctas, {falla} con falla. Ejecutado el {datetime.datetime.now():%d/%m/%Y %H:%M:%S} (hora del entorno).')
