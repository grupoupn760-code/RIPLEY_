Sistema de Compra de Prendas "RipleyComprasMAUI" en C# (.NET MAUI)
Prototipo académico de escritorio para Windows que simula el proceso de compra de prendas en Ripley, dividido en Preventa (selección de productos), Venta (atención y pago simulado) y Postventa (boleta). Las estructuras de datos principales están implementadas manualmente en C#.

La aplicación también incluye una Bitácora de operaciones de la sesión y una Red de cajas simulada para calcular rutas mediante el algoritmo de Dijkstra. No procesa pagos reales ni emite comprobantes con validez tributaria.
Arquitectura del proyecto
RipleyComprasMAUI/
│
├── RipleyComprasMAUI/
│   ├── Models/
│   ├── Structures/
│   ├── Services/
│   ├── Data/
│   ├── Helpers/
│   ├── UI/
│   ├── Resources/
│   │   └── Raw/
│   │       └── productos.json
│   ├── Platforms/
│   │   └── Windows/
│   │
│   ├── MauiProgram.cs
│   ├── App.xaml
│   ├── App.xaml.cs
│   └── RipleyComprasMAUI.csproj
│
├── Comprobaciones/
│   ├── Program.cs
│   └── Comprobaciones.csproj
│
├── RipleyComprasMAUI.sln
├── RipleyComprasMAUI.slnx
└── README.md
Descripción de capas
Models (Modelos)
Representan las entidades del proceso de compra:
- Producto: código, nombre, precio regular, precio Tarjeta Ripley y stock.
- ItemCarrito: producto seleccionado, cantidad y precios de la compra.
- Venta: turno, datos opcionales del cliente, artículos, tarjeta e importes.
- Boleta: número, fecha y referencia a la venta para el comprobante simulado.
Structures (Estructuras)
Estructuras de datos implementadas manualmente:
- ListaEnlazada: administra los artículos del carrito y los historiales.
- PilaAcciones: pila LIFO para deshacer la última modificación del carrito.
- ColaCompras: cola FIFO para atender las compras por orden de llegada.
- ArbolBusqueda y NodoArbol: árbol AVL para buscar productos por código e indexar boletas por número.
- GrafoCajas: grafo ponderado que simula conexiones entre cajas y utiliza Dijkstra para hallar rutas mínimas.
- NodoSimple: nodo utilizado en las estructuras enlazadas.
Services (Servicios)
Contienen la lógica de los subprocesos:
- CatalogoService: carga el catálogo y busca prendas por código o nombre.
- CarritoService: agrega, modifica, quita y permite deshacer acciones del carrito.
- CompraService: organiza turnos, atiende clientes, aplica precios según la tarjeta y confirma pagos simulados.
- BoletaService: genera, almacena y consulta boletas por número, DNI o fecha.
Data (Persistencia)
La clase JsonDataManager centraliza:
- Lectura y escritura de archivos JSON.
- Serialización y deserialización de productos y ventas.
- Creación de los archivos de trabajo en el primer inicio.
- Conservación del catálogo y las ventas entre ejecuciones.
Helpers (Validaciones)
La clase InputHelper se encarga de:
- Validar códigos de producto de 13 dígitos y cantidades.
- Validar datos opcionales del cliente, como DNI y correo.
- Comprobar los tipos de tarjeta permitidos.
- Normalizar textos de búsqueda y dar formato a montos y fechas.
UI (Interfaz gráfica)
Contiene la interacción con el usuario mediante MainPage.xaml, MainPage.xaml.cs y EstilosNativos.cs:
- Preventa: catálogo, búsqueda de prendas y carrito.
- Venta: fila de clientes, atención y pago simulado.
- Boleta: consulta de comprobantes generados.
- Bitácora: visualización de operaciones de la sesión por subproceso.
- Red de cajas: enlaces activos o desactivados y cálculo de rutas.
Cómo ejecutar el proyecto
Requisitos
- Windows 10 compatible o Windows 11.
- .NET SDK 9 instalado.
- Visual Studio con la carga de trabajo de .NET MAUI.
Verificar el SDK instalado:
dotnet --version
Instalar la carga de trabajo de MAUI, si fuera necesario:
dotnet workload install maui
Ejecutar
Desde Visual Studio: abrir RipleyComprasMAUI.sln o RipleyComprasMAUI.slnx, establecer RipleyComprasMAUI como proyecto de inicio, seleccionar Windows Machine y presionar F5.

Desde la carpeta interna del proyecto (RipleyComprasMAUI/RipleyComprasMAUI):
dotnet build -t:Run -f net9.0-windows10.0.19041.0
La versión del código compartido está configurada solo para Windows; no incluye un destino Android en su archivo .csproj.

Ejecutar las comprobaciones
Desde la carpeta raíz de la solución:
dotnet run --project Comprobaciones/Comprobaciones.csproj
Este proyecto de consola permite verificar la lógica de las estructuras y operaciones sin abrir la interfaz MAUI.
Datos
- Resources/Raw/productos.json: catálogo inicial de siete prendas, incluido como recurso de la aplicación.
- productos.json de trabajo: copia del catálogo donde se actualiza el stock.
- ventas.json: historial persistente de ventas finalizadas y boletas simuladas.
Los archivos modificables se crean automáticamente en la carpeta privada de la aplicación, bajo FileSystem.AppDataDirectory/data. En los siguientes inicios se recuperan sin sobrescribirlos.

La cola de espera, la pila de deshacer y la bitácora de operaciones se mantienen en memoria durante la sesión; no se guardan en esos archivos JSON.
