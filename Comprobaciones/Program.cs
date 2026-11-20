using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RipleyComprasMAUI.Data;
using RipleyComprasMAUI.Helpers;
using RipleyComprasMAUI.Models;
using RipleyComprasMAUI.Services;
using RipleyComprasMAUI.Structures;

// Comprobaciones de la lógica de RipleyComprasMAUI, sin interfaz ni librerías de pruebas.
// Cada grupo usa su propia carpeta temporal: no se tocan los datos reales de la aplicación.

int correctas = 0;
int fallidas = 0;
string carpetaBase = Path.Combine(Path.GetTempPath(), "RipleyComprobaciones_" + Guid.NewGuid().ToString("N"));

const string Fepapaya = "2015373474714";
const string Estre = "2015373379330";
const string Falda = "2015372278467";
const string Jeans = "2015372601050";
const string Cuerno = "2072369066599";
const string Destar = "2015371440865";
const string Detej = "2015371441190";

const string SemillaRipley = """
[
  { "Codigo": "2015371440865", "Nombre": "SWE ML DESTAR INX CO", "PrecioRegular": 19.99, "PrecioTarjetaRipley": 19.99, "Stock": 20 },
  { "Codigo": "2015371441190", "Nombre": "SWE ML DETEJ INX CO",  "PrecioRegular": 19.99, "PrecioTarjetaRipley": 19.99, "Stock": 20 },
  { "Codigo": "2015372278467", "Nombre": "FALDA LARG FESTIDO I", "PrecioRegular": 89.99, "PrecioTarjetaRipley": 89.99, "Stock": 20 },
  { "Codigo": "2015372601050", "Nombre": "JEANS FITS ME WIDE L", "PrecioRegular": 89.99, "PrecioTarjetaRipley": 89.99, "Stock": 20 },
  { "Codigo": "2015373379330", "Nombre": "POL MC ESTRE INX COR", "PrecioRegular": 49.99, "PrecioTarjetaRipley": 49.99, "Stock": 20 },
  { "Codigo": "2015373474714", "Nombre": "POL MC FEPAPAYA INX",  "PrecioRegular": 59.99, "PrecioTarjetaRipley": 39.99, "Stock": 20 },
  { "Codigo": "2072369066599", "Nombre": "HI ML CUERNO AZI COR", "PrecioRegular": 19.99, "PrecioTarjetaRipley": 19.99, "Stock": 20 }
]
""";

// Catálogo FICTICIO solo para probar la búsqueda por nombre (no se agrega al catálogo base).
const string SemillaPoloRojo = """
[
  { "Codigo": "2000000000001", "Nombre": "Polo Rojo",            "PrecioRegular": 30.00, "PrecioTarjetaRipley": 25.00, "Stock": 5 },
  { "Codigo": "2000000000002", "Nombre": "POLO ROJO MANGA LARGA", "PrecioRegular": 35.00, "PrecioTarjetaRipley": 35.00, "Stock": 5 },
  { "Codigo": "2000000000003", "Nombre": "Polo Azul",            "PrecioRegular": 30.00, "PrecioTarjetaRipley": 30.00, "Stock": 5 },
  { "Codigo": "2000000000004", "Nombre": "Pantalón rojo",        "PrecioRegular": 70.00, "PrecioTarjetaRipley": 60.00, "Stock": 5 }
]
""";

void Comprobar(string nombre, bool condicion, string detalle = "")
{
    if (condicion)
    {
        correctas++;
        Console.WriteLine("[OK]    " + nombre);
    }
    else
    {
        fallidas++;
        Console.WriteLine("[FALLA] " + nombre + (detalle.Length > 0 ? "  -> " + detalle : ""));
    }
}

void Titulo(string texto)
{
    Console.WriteLine();
    Console.WriteLine("== " + texto + " ==");
}

async Task<Entorno> CrearEntornoAsync(string nombreCarpeta, string semilla)
{
    string carpeta = Path.Combine(carpetaBase, nombreCarpeta);
    JsonDataManager datos = new JsonDataManager(carpeta,
        () => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(semilla))));
    await datos.PrepararArchivosAsync();
    return Armar(datos);
}

Entorno Armar(JsonDataManager datos)
{
    CatalogoService catalogo = new CatalogoService(datos);
    catalogo.Cargar(out _);
    CarritoService carritos = new CarritoService(catalogo);
    BoletaService boletas = new BoletaService(datos);
    boletas.Cargar(out _);
    CompraService compras = new CompraService(carritos, catalogo, boletas);
    return new Entorno(datos, catalogo, carritos, boletas, compras);
}

bool RegistrarCompra(Entorno e, (string Codigo, int Cantidad)[] lineas, string? nombre = null, string? dni = null, string? correo = null)
{
    foreach ((string codigo, int cantidad) in lineas)
    {
        if (!e.Carritos.AgregarPrenda(e.Carritos.Actual, codigo, cantidad, out string mensaje))
        {
            Console.WriteLine("        (no se pudo agregar: " + mensaje + ")");
            return false;
        }
    }
    return e.Compras.RegistrarLlegada(nombre, dni, correo, out _, out _);
}

string Orden(ContextoCarrito carrito)
{
    List<string> codigos = new List<string>();
    foreach (ItemCarrito articulo in carrito.Articulos)
    {
        codigos.Add(articulo.Codigo);
    }
    return string.Join(",", codigos);
}

string CodigosDe(ListaEnlazada<Producto> productos)
{
    List<string> codigos = new List<string>();
    foreach (Producto producto in productos)
    {
        codigos.Add(producto.Codigo);
    }
    return string.Join(",", codigos);
}

try
{
    // ------------------------------------------------------------------
    Titulo("Estructuras básicas");
    ColaCompras<int> cola = new ColaCompras<int>();
    cola.Encolar(1); cola.Encolar(2); cola.Encolar(3);
    Comprobar("Cola FIFO: sale primero el 1", cola.Desencolar() == 1 && cola.VerFrente() == 2 && cola.Cantidad == 2);
    PilaAcciones<string> pila = new PilaAcciones<string>();
    pila.Apilar("a"); pila.Apilar("b");
    Comprobar("Pila LIFO: sale primero la última", pila.Desapilar() == "b" && pila.Cantidad == 1);
    ListaEnlazada<int> lista = new ListaEnlazada<int>();
    lista.AgregarAlFinal(10); lista.AgregarAlFinal(20); lista.AgregarAlFinal(30);
    lista.Eliminar(x => x == 30, out _, out int posicionQuitada);
    lista.AgregarAlFinal(40);                       // "ultimo" debe haberse actualizado al quitar el 30
    lista.InsertarEn(1, 15);
    Comprobar("Lista: quitar el último y reinsertar en posición", posicionQuitada == 2 && string.Join(",", lista) == "10,15,20,40");

    ArbolBusqueda<int, int> arbolNumeros = new ArbolBusqueda<int, int>();
    for (int i = 1; i <= 100; i++)
    {
        arbolNumeros.Insertar(i, i);
    }
    Comprobar("AVL con 100 claves ascendentes: altura 7", arbolNumeros.Altura == 7, $"altura {arbolNumeros.Altura}");
    Comprobar("AVL: clave repetida no se inserta", !arbolNumeros.Insertar(50, 0) && arbolNumeros.Cantidad == 100);

    // ------------------------------------------------------------------
    Titulo("Preventa: árbol y búsquedas sobre el catálogo real");
    Entorno a = await CrearEntornoAsync("arbol", SemillaRipley);
    Comprobar("Catálogo con 7 prendas", a.Catalogo.Cantidad == 7, $"{a.Catalogo.Cantidad}");
    Comprobar("AVL: altura 3", a.Catalogo.Altura == 3, $"altura {a.Catalogo.Altura}");
    Comprobar("AVL: raíz 2015372601050 (JEANS)", a.Catalogo.CodigoRaiz == Jeans, $"raíz {a.Catalogo.CodigoRaiz}");
    Comprobar("AVL: 4 rotaciones al insertar en orden ascendente", a.Catalogo.Rotaciones == 4, $"{a.Catalogo.Rotaciones}");

    bool encontrada = a.Catalogo.BuscarPorCodigo(Fepapaya, out Producto? fepapaya, out _);
    Comprobar("2015373474714 encontrada en 2 comparaciones con S/ 59.99 y S/ 39.99",
        encontrada && fepapaya != null && fepapaya.PrecioRegular == 59.99m && fepapaya.PrecioTarjetaRipley == 39.99m
        && a.Catalogo.UltimasComparaciones == 2, $"comparaciones {a.Catalogo.UltimasComparaciones}");
    Comprobar("9999999999999: Prenda no encontrada",
        !a.Catalogo.BuscarPorCodigo("9999999999999", out _, out string mensajeInexistente) && mensajeInexistente.StartsWith("Prenda no encontrada"));
    Comprobar("20153734 (8 dígitos): Código inválido",
        !a.Catalogo.BuscarPorCodigo("20153734", out _, out string mensajeCorto) && mensajeCorto.StartsWith("Código inválido"));

    foreach (string variante in new[] { "POL MC FEPAPAYA INX", "Pol Mc Fepapaya Inx", "Pól MC Fepápaya Inx" })
    {
        ListaEnlazada<Producto> resultado = a.Catalogo.BuscarPorNombre(variante, out _);
        Comprobar($"Nombre \"{variante}\" encuentra solo 2015373474714", CodigosDe(resultado) == Fepapaya, CodigosDe(resultado));
    }
    ListaEnlazada<Producto> parcial = a.Catalogo.BuscarPorNombre("pol mc", out _);
    Comprobar("Búsqueda parcial \"pol mc\": 2 prendas en orden de código", CodigosDe(parcial) == Estre + "," + Fepapaya, CodigosDe(parcial));
    ListaEnlazada<Producto> vacia = a.Catalogo.BuscarPorNombre("   ", out string mensajeVacio);
    Comprobar("Consulta vacía: mensaje y ningún resultado", vacia.EstaVacia && mensajeVacio.StartsWith("Escriba"));
    ListaEnlazada<Producto> sinCoincidencias = a.Catalogo.BuscarPorNombre("zapatilla", out string mensajeSin);
    Comprobar("Sin coincidencias: Prenda no encontrada", sinCoincidencias.EstaVacia && mensajeSin == "Prenda no encontrada.");

    // ------------------------------------------------------------------
    Titulo("Búsqueda por nombre sin mayúsculas ni tildes (catálogo ficticio \"Polo Rojo\")");
    Entorno polo = await CrearEntornoAsync("polo", SemillaPoloRojo);
    string esperado = CodigosDe(polo.Catalogo.BuscarPorNombre("POLO ROJO", out _));
    string[] consultas = { "POLO ROJO", "Polo Rojo", "polo rojo", "Poló rojo", "  póló   RÓJO  ", "Poló rojo", "Poló rojo" };
    foreach (string consulta in consultas)
    {
        string obtenido = CodigosDe(polo.Catalogo.BuscarPorNombre(consulta, out _));
        Comprobar($"\"{consulta.Replace("́", "(tilde separada)")}\" → mismos códigos y mismo orden", obtenido == esperado, obtenido);
    }
    Comprobar("La lista esperada es 2000000000001,2000000000002", esperado == "2000000000001,2000000000002", esperado);
    Comprobar("Normalización equivalente para acento compuesto y separado",
        InputHelper.NormalizarParaBusqueda("Poló") == InputHelper.NormalizarParaBusqueda("Poló"));
    Producto? primero = polo.Catalogo.Listar().Buscar(prenda => prenda.Codigo == "2000000000001");
    Comprobar("El nombre guardado no cambia (\"Polo Rojo\")", primero != null && primero.Nombre == "Polo Rojo");

    // ------------------------------------------------------------------
    Titulo("Carrito: lista enlazada y pila de deshacer");
    Entorno c = await CrearEntornoAsync("carrito", SemillaRipley);
    ContextoCarrito k = c.Carritos.Actual;
    c.Carritos.AgregarPrenda(k, Fepapaya, 1, out _);
    c.Carritos.AgregarPrenda(k, Estre, 1, out _);
    c.Carritos.AgregarPrenda(k, Falda, 1, out _);
    c.Carritos.AgregarPrenda(k, Falda, 1, out _);
    Comprobar("Misma prenda dos veces: 3 líneas y FALDA x 2", k.CantidadLineas == 3 && c.Carritos.CantidadEnCarrito(k, Falda) == 2);
    Comprobar("Total regular 289.96", c.Carritos.CalcularTotalRegular(k) == 289.96m, $"{c.Carritos.CalcularTotalRegular(k)}");
    c.Carritos.Deshacer(k, out _);
    Comprobar("Deshacer el aumento: FALDA vuelve a 1 y el total a 199.97",
        c.Carritos.CantidadEnCarrito(k, Falda) == 1 && c.Carritos.CalcularTotalRegular(k) == 199.97m);
    c.Carritos.CambiarCantidad(k, Falda, 2, out _);
    c.Carritos.QuitarPrenda(k, Estre, out _);
    Comprobar("FEPAPAYA x 1 y FALDA x 2: total regular 239.97",
        c.Carritos.CalcularTotalRegular(k) == 239.97m && Orden(k) == Fepapaya + "," + Falda, Orden(k));
    c.Carritos.Deshacer(k, out _);
    Comprobar("Deshacer quitar: ESTRE vuelve a su posición original", Orden(k) == Fepapaya + "," + Estre + "," + Falda, Orden(k));
    c.Carritos.Deshacer(k, out _);
    Comprobar("Deshacer cambio de cantidad: FALDA vuelve a 1", c.Carritos.CantidadEnCarrito(k, Falda) == 1);
    c.Carritos.Deshacer(k, out _);
    c.Carritos.Deshacer(k, out _);
    c.Carritos.Deshacer(k, out _);
    Comprobar("Tras deshacer todo: carrito y pila vacíos", k.EstaVacio && k.AccionesParaDeshacer == 0);
    Comprobar("Con la pila vacía: \"No hay acciones para deshacer.\"",
        !c.Carritos.Deshacer(k, out string mensajeDeshacer) && mensajeDeshacer == "No hay acciones para deshacer.");

    // ------------------------------------------------------------------
    Titulo("Entradas inválidas");
    foreach (string texto in new[] { "0", "-1", "1.5", "100", "abc", "", "99999999999", "1 2" })
    {
        Comprobar($"Cantidad \"{texto}\" rechazada", !InputHelper.TryLeerEntero(texto, "Cantidad", 1, 99, out _, out _));
    }
    Comprobar("Cantidad \"7\" aceptada", InputHelper.TryLeerEntero(" 7 ", "Cantidad", 1, 99, out int siete, out _) && siete == 7);
    Comprobar("Decimal \"12,50\" se lee como 12.50", InputHelper.TryLeerDecimal("12,50", "Monto", 0.01m, 1000m, out decimal monto, out _) && monto == 12.50m);
    Comprobar("Decimal \"1,234\" (miles ambiguos) rechazado", !InputHelper.TryLeerDecimal("1,234", "Monto", 0.01m, 100000m, out _, out _));
    Comprobar("Decimal \"1.234,56\" rechazado", !InputHelper.TryLeerDecimal("1.234,56", "Monto", 0.01m, 100000m, out _, out _));
    Comprobar("Double \"NaN\" rechazado", !InputHelper.TryLeerDouble("NaN", "Peso", 0.1, 1000, 2, out _, out _));
    Comprobar("Double \"4.5\" aceptado", InputHelper.TryLeerDouble("4.5", "Peso", 0.1, 1000, 2, out double peso, out _) && peso == 4.5);
    Comprobar("DNI \"01234567\" válido y conserva el cero", InputHelper.TryValidarDni("01234567", out string dniCero, out _) && dniCero == "01234567");
    Comprobar("DNI \"1234\" inválido", !InputHelper.TryValidarDni("1234", out _, out _));
    Comprobar("Correo \"abc\" inválido", !InputHelper.TryValidarCorreo("abc", out _, out _));
    Comprobar("Correo vacío permitido (opcional)", InputHelper.TryValidarCorreo("  ", out string? sinCorreo, out _) && sinCorreo == null);
    Comprobar("Nombre con tilde, ñ, guion y apóstrofo aceptado",
        InputHelper.TryValidarNombrePersona("  María-José   O'Higgins Ñuñez ", out string? nombreLimpio, out _) && nombreLimpio == "María-José O'Higgins Ñuñez");

    ContextoCarrito k2 = c.Carritos.Actual;
    c.Carritos.AgregarPrenda(k2, Cuerno, 1, out _);
    int accionesAntes = k2.AccionesParaDeshacer;
    int filaAntes = c.Compras.ClientesEnFila;
    bool aceptoExceso = c.Carritos.AgregarPrenda(k2, Cuerno, 20, out _);        // 1 + 20 = 21 > stock 20
    bool aceptoCero = c.Carritos.AgregarPrenda(k2, Cuerno, 0, out _);
    bool aceptoCambioCero = c.Carritos.CambiarCantidad(k2, Cuerno, 0, out _);
    bool aceptoMismaCantidad = c.Carritos.CambiarCantidad(k2, Cuerno, 1, out _);
    Comprobar("Mayor al stock, 0 y cantidad sin cambios: rechazados sin tocar lista ni pila",
        !aceptoExceso && !aceptoCero && !aceptoCambioCero && !aceptoMismaCantidad
        && k2.AccionesParaDeshacer == accionesAntes && c.Carritos.CantidadEnCarrito(k2, Cuerno) == 1);
    Comprobar("DNI inválido: no se encola",
        !c.Compras.RegistrarLlegada("Ana Prueba", "1234", null, out _, out string mensajeDni) && mensajeDni.StartsWith("DNI inválido") && c.Compras.ClientesEnFila == filaAntes);
    Comprobar("Correo inválido: no se encola",
        !c.Compras.RegistrarLlegada("Ana Prueba", "70000021", "abc", out _, out string mensajeCorreo) && mensajeCorreo.StartsWith("Correo inválido") && c.Compras.ClientesEnFila == filaAntes);
    c.Carritos.Vaciar(k2, out _);
    Comprobar("Carrito vacío: no se envía a la fila", !c.Compras.RegistrarLlegada(null, null, null, out _, out _) && c.Compras.ClientesEnFila == filaAntes);

    // ------------------------------------------------------------------
    Titulo("Venta y Postventa: escenarios A y B, DNI, número y fecha");
    Entorno v = await CrearEntornoAsync("ventas", SemillaRipley);
    RegistrarCompra(v, new[] { (Fepapaya, 1), (Estre, 1), (Falda, 1) }, "Cliente Prueba", "70000021", "cliente.prueba@correo.com");
    v.Compras.AtenderSiguiente(out _);
    Venta compraA = v.Compras.CompraEnAtencion!;
    v.Compras.SeleccionarTarjeta(TipoTarjeta.OtraTarjeta, out _);
    Comprobar("A con Otra tarjeta: final 199.97 y ahorro 0.00", compraA.TotalFinal == 199.97m && compraA.Ahorro == 0.00m);
    v.Compras.SeleccionarTarjeta(TipoTarjeta.TarjetaRipley, out _);      // cambio de tarjeta antes de confirmar
    Comprobar("A con Tarjeta Ripley: regular 199.97, final 179.97, ahorro 20.00",
        compraA.TotalRegular == 199.97m && compraA.TotalFinal == 179.97m && compraA.Ahorro == 20.00m);
    Comprobar("A: operación gravada 152.52 e IGV 27.45", compraA.OperacionGravada == 152.52m && compraA.Igv == 27.45m,
        $"{compraA.OperacionGravada} / {compraA.Igv}");
    bool confirmadaA = v.Compras.ConfirmarPagoSimulado(out Venta? ventaA, out _);
    Comprobar("A confirmada: boleta N° 1 asociada a la venta", confirmadaA && ventaA?.Boleta?.Numero == 1 && ventaA.Boleta.IdVenta == ventaA.Id);
    Comprobar("Confirmar de nuevo no duplica la boleta", !v.Compras.ConfirmarPagoSimulado(out _, out _) && v.Boletas.CantidadVentas == 1);
    Comprobar("Stock descontado una sola vez (FEPAPAYA 19)", v.Catalogo.StockDisponible(Fepapaya) == 19);

    RegistrarCompra(v, new[] { (Jeans, 1), (Cuerno, 1), (Destar, 1), (Detej, 1) }, "Cliente Prueba", "70000021");
    v.Compras.AtenderSiguiente(out _);
    Venta compraB = v.Compras.CompraEnAtencion!;
    v.Compras.SeleccionarTarjeta(TipoTarjeta.TarjetaRipley, out _);
    Comprobar("B con Tarjeta Ripley: 149.96 y ahorro 0.00", compraB.TotalFinal == 149.96m && compraB.Ahorro == 0.00m);
    v.Compras.SeleccionarTarjeta(TipoTarjeta.OtraTarjeta, out _);
    Comprobar("B con Otra tarjeta: 149.96, op. gravada 127.08, IGV 22.88",
        compraB.TotalFinal == 149.96m && compraB.Ahorro == 0.00m && compraB.OperacionGravada == 127.08m && compraB.Igv == 22.88m,
        $"{compraB.OperacionGravada} / {compraB.Igv}");
    v.Compras.ConfirmarPagoSimulado(out _, out _);

    RegistrarCompra(v, new[] { (Cuerno, 1) });                                    // compra anónima
    RegistrarCompra(v, new[] { (Detej, 2) }, "Otra Clienta", "70000099");
    v.Compras.AtenderSiguiente(out _);
    Comprobar("No se atiende otra compra mientras hay una activa",
        !v.Compras.AtenderSiguiente(out _) && v.Compras.CompraEnAtencion?.Turno == 3);
    v.Compras.SeleccionarTarjeta(TipoTarjeta.OtraTarjeta, out _);
    v.Compras.ConfirmarPagoSimulado(out Venta? anonima, out _);
    Comprobar("Compra anónima: boleta sin nombre ni DNI", anonima?.Boleta?.Numero == 3 && anonima.Dni == null && anonima.NombreCliente == null);
    v.Compras.AtenderSiguiente(out _);
    v.Compras.SeleccionarTarjeta(TipoTarjeta.TarjetaRipley, out _);
    v.Compras.ConfirmarPagoSimulado(out _, out _);

    ListaEnlazada<Venta> delDni = v.Boletas.BuscarPorDni("70000021", out _);
    List<int> numerosDelDni = new List<int>();
    foreach (Venta venta in delDni)
    {
        numerosDelDni.Add(venta.Boleta!.Numero);
    }
    Comprobar("DNI 70000021: devuelve sus dos boletas (1 y 2)", string.Join(",", numerosDelDni) == "1,2", string.Join(",", numerosDelDni));
    Comprobar("DNI 70000099: solo su boleta", v.Boletas.BuscarPorDni("70000099", out _).Cantidad == 1);
    Comprobar("DNI sin compras: ninguna boleta", v.Boletas.BuscarPorDni("12345678", out _).Cantidad == 0);
    Comprobar("Boleta N° 2 encontrada en el árbol", v.Boletas.BuscarPorNumero(2, out Venta? b2, out _) && b2 != null && b2.TotalFinal == 149.96m);
    Comprobar("Boleta N° 99: no se encontró", !v.Boletas.BuscarPorNumero(99, out _, out string mensaje99) && mensaje99.StartsWith("No se encontró"));
    Comprobar("Fecha de hoy: 4 boletas", v.Boletas.BuscarPorFecha(DateTime.Today, out _).Cantidad == 4);
    Comprobar("Fecha de ayer: ninguna boleta", v.Boletas.BuscarPorFecha(DateTime.Today.AddDays(-1), out _).Cantidad == 0);
    string comprobante = v.Boletas.ObtenerComprobante(ventaA!);
    Comprobar("Comprobante con leyenda, tarjeta, ahorro, IGV y total", comprobante.Contains(Boleta.LeyendaSimulada)
        && comprobante.Contains("Tarjeta Ripley") && comprobante.Contains("S/ 20.00") && comprobante.Contains("S/ 27.45") && comprobante.Contains("S/ 179.97"));

    Comprobar("Bitácoras con registros de los tres subprocesos",
        !v.Carritos.Bitacora.EstaVacia && !v.Compras.Bitacora.EstaVacia && !v.Boletas.Bitacora.EstaVacia);

    // ------------------------------------------------------------------
    Titulo("Cerrar y reabrir (persistencia JSON)");
    Entorno r = Armar(v.Datos);           // nuevas instancias que leen los mismos archivos
    Comprobar("Se recargan las 4 ventas", r.Boletas.CantidadVentas == 4, $"{r.Boletas.CantidadVentas}");
    Comprobar("Siguiente número = máximo + 1 = 5", r.Boletas.SiguienteNumero == 5, $"{r.Boletas.SiguienteNumero}");
    r.Boletas.BuscarPorNumero(1, out Venta? recargada, out _);
    Comprobar("Boleta 1 conserva tarjeta, ahorro, precio aplicado y fecha de emisión",
        recargada != null && recargada.Tarjeta == TipoTarjeta.TarjetaRipley && recargada.Ahorro == 20.00m
        && recargada.Articulos[0].PrecioAplicado == 39.99m && recargada.Boleta!.FechaEmision == ventaA!.Boleta!.FechaEmision);
    Comprobar("Stock guardado (FEPAPAYA 19)", r.Catalogo.StockDisponible(Fepapaya) == 19);

    using (JsonDocument documento = JsonDocument.Parse(File.ReadAllText(v.Datos.RutaVentas)))
    {
        bool coherente = true;
        foreach (JsonElement elemento in documento.RootElement.EnumerateArray())
        {
            JsonElement boleta = elemento.GetProperty("Boleta");
            bool idCoincide = boleta.GetProperty("IdVenta").GetString() == elemento.GetProperty("Id").GetString();
            bool sinDuplicar = !boleta.TryGetProperty("Articulos", out _) && !boleta.TryGetProperty("TotalFinal", out _)
                               && !boleta.TryGetProperty("Venta", out _);
            coherente = coherente && idCoincide && sinDuplicar;
        }
        Comprobar("ventas.json: Boleta.IdVenta = Venta.Id, sin ciclos ni importes duplicados en Boleta", coherente);
    }

    // ------------------------------------------------------------------
    Titulo("Falta de stock al finalizar una compra que esperaba");
    Entorno s = await CrearEntornoAsync("stock", SemillaRipley);
    RegistrarCompra(s, new[] { (Jeans, 15) });
    RegistrarCompra(s, new[] { (Jeans, 10) });
    s.Compras.AtenderSiguiente(out _);
    s.Compras.SeleccionarTarjeta(TipoTarjeta.TarjetaRipley, out _);
    s.Compras.ConfirmarPagoSimulado(out _, out _);
    s.Compras.AtenderSiguiente(out _);
    s.Compras.SeleccionarTarjeta(TipoTarjeta.OtraTarjeta, out _);
    bool sinStock = s.Compras.ConfirmarPagoSimulado(out _, out string mensajeStock);
    Comprobar("Conflicto detectado y la compra sigue en atención",
        !sinStock && mensajeStock.StartsWith("Falta stock") && s.Compras.CompraEnAtencion?.Turno == 2 && s.Boletas.CantidadVentas == 1);
    s.Compras.CorregirCantidad(Jeans, 5, out _);
    Comprobar("Tras corregir a 5 unidades se confirma y el stock queda en 0",
        s.Compras.ConfirmarPagoSimulado(out _, out _) && s.Catalogo.StockDisponible(Jeans) == 0);

    // ------------------------------------------------------------------
    Titulo("Pico de 12 clientes (FIFO)");
    Entorno p = await CrearEntornoAsync("pico", SemillaRipley);
    string[] codigos = { Fepapaya, Estre, Falda, Jeans, Cuerno, Destar, Detej };
    bool pilasNuevas = true;
    for (int i = 1; i <= 12; i++)
    {
        RegistrarCompra(p, new[] { (codigos[(i - 1) % 7], 1) });
        pilasNuevas = pilasNuevas && p.Carritos.Actual.AccionesParaDeshacer == 0 && p.Carritos.Actual.EstaVacio;
    }
    Comprobar("12 clientes en la fila", p.Compras.ClientesEnFila == 12);
    Comprobar("Cada cliente nuevo empieza con carrito y pila vacíos", pilasNuevas);
    bool enOrden = true;
    bool sinMezcla = true;
    for (int i = 1; i <= 12; i++)
    {
        p.Compras.AtenderSiguiente(out _);
        Venta? atendida = p.Compras.CompraEnAtencion;
        enOrden = enOrden && atendida != null && atendida.Turno == i;
        sinMezcla = sinMezcla && atendida != null && atendida.Articulos.Count == 1
                    && atendida.Articulos[0].Codigo == codigos[(i - 1) % 7] && atendida.Articulos[0].Cantidad == 1;
        p.Compras.SeleccionarTarjeta(TipoTarjeta.OtraTarjeta, out _);
        p.Compras.ConfirmarPagoSimulado(out _, out _);
    }
    Comprobar("Turnos 1 a 12 atendidos en orden de llegada", enOrden);
    Comprobar("Ninguna compra mezcla artículos de otra", sinMezcla);
    Comprobar("Fila vacía y 12 boletas emitidas", p.Compras.ClientesEnFila == 0 && p.Boletas.CantidadVentas == 12);

    // ------------------------------------------------------------------
    Titulo("Archivos: inexistente, vacío válido, dañado y error de escritura");
    Entorno nuevo = await CrearEntornoAsync("archivos", SemillaRipley);
    Comprobar("JSON inexistente: se crean productos.json y ventas.json = []",
        File.Exists(nuevo.Datos.RutaProductos) && File.ReadAllText(nuevo.Datos.RutaVentas).Trim() == "[]");
    Comprobar("[] es una colección vacía válida", nuevo.Boletas.ArchivoDisponible && nuevo.Boletas.CantidadVentas == 0);

    File.WriteAllText(nuevo.Datos.RutaVentas, "{ esto no es JSON");
    BoletaService danada = new BoletaService(nuevo.Datos);
    bool cargoDanada = danada.Cargar(out _);
    Comprobar("JSON dañado: se informa el error y el archivo no se modifica",
        !cargoDanada && !danada.ArchivoDisponible && File.ReadAllText(nuevo.Datos.RutaVentas) == "{ esto no es JSON");
    File.WriteAllText(nuevo.Datos.RutaVentas, "");
    Comprobar("Archivo vacío (sin JSON) no se toma como []", !new BoletaService(nuevo.Datos).Cargar(out _));

    if (OperatingSystem.IsWindows())
    {
        Entorno w = await CrearEntornoAsync("escritura", SemillaRipley);
        RegistrarCompra(w, new[] { (Fepapaya, 1) }, "Cliente Prueba");
        w.Compras.AtenderSiguiente(out _);
        w.Compras.SeleccionarTarjeta(TipoTarjeta.TarjetaRipley, out _);

        File.SetAttributes(w.Datos.RutaVentas, FileAttributes.ReadOnly);       // simula que ventas.json no se puede escribir
        bool conVentasBloqueado = w.Compras.ConfirmarPagoSimulado(out _, out _);
        File.SetAttributes(w.Datos.RutaVentas, FileAttributes.Normal);
        Comprobar("Error al guardar ventas.json: no se anuncia venta y el cliente sigue en atención",
            !conVentasBloqueado && w.Compras.CompraEnAtencion != null && w.Boletas.CantidadVentas == 0 && w.Boletas.SiguienteNumero == 1);

        File.SetAttributes(w.Datos.RutaProductos, FileAttributes.ReadOnly);    // ventas se guarda, productos falla
        bool conProductosBloqueado = w.Compras.ConfirmarPagoSimulado(out _, out _);
        File.SetAttributes(w.Datos.RutaProductos, FileAttributes.Normal);
        BoletaService enDisco = new BoletaService(w.Datos);
        enDisco.Cargar(out _);
        Comprobar("Error al guardar productos.json: se restaura ventas.json y no se descuenta stock",
            !conProductosBloqueado && enDisco.CantidadVentas == 0 && w.Catalogo.StockDisponible(Fepapaya) == 20
            && w.Compras.CompraEnAtencion?.Boleta == null);

        bool reintento = w.Compras.ConfirmarPagoSimulado(out Venta? reintentada, out _);
        Comprobar("Reintento: una sola boleta (N° 1) y stock descontado una vez",
            reintento && reintentada?.Boleta?.Numero == 1 && w.Boletas.CantidadVentas == 1 && w.Catalogo.StockDisponible(Fepapaya) == 19);
    }
    else
    {
        Console.WriteLine("[OMITIDA] Error de escritura: la simulación con archivos de solo lectura es para Windows.");
    }

    // ------------------------------------------------------------------
    Titulo("Grafo (complemento del desafío)");
    GrafoCajas grafo = GrafoCajas.CrearEjemplo();
    bool hayRuta = grafo.BuscarRutaMinima(0, 5, out ListaEnlazada<int> ruta, out double distancia);
    Comprobar("Ruta mínima Caja 511 → Central: 26 ms por 512, 513 y servidor",
        hayRuta && distancia == 26 && grafo.DescribirRuta(ruta) == "Caja 511 → Caja 512 → Caja 513 → Servidor de tienda → Central (simulada)",
        $"{distancia} {grafo.DescribirRuta(ruta)}");
    grafo.CambiarEstadoEnlace(3, 5, false, out _);
    hayRuta = grafo.BuscarRutaMinima(0, 5, out ruta, out distancia);
    Comprobar("Enlace Servidor–Central caído: ruta por el Almacén, 34 ms", hayRuta && distancia == 34, $"{distancia}");
    grafo.CambiarEstadoEnlace(4, 5, false, out _);
    Comprobar("Sin enlaces activos hacia Central: no existe ruta", !grafo.BuscarRutaMinima(0, 5, out _, out _));
}
finally
{
    try
    {
        Directory.Delete(carpetaBase, true);
    }
    catch (Exception)
    {
        Console.WriteLine("(No se pudo borrar la carpeta temporal " + carpetaBase + ")");
    }
}

Console.WriteLine();
Console.WriteLine($"Resultado: {correctas} correctas, {fallidas} con falla. Ejecutado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}.");
return fallidas == 0 ? 0 : 1;

/// <summary>Servicios de una ejecución de prueba, conectados como en MauiProgram.</summary>
record Entorno(JsonDataManager Datos, CatalogoService Catalogo, CarritoService Carritos, BoletaService Boletas, CompraService Compras);
