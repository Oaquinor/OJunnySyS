# Arquitectura de OJunnySyS

## Estilo

Monolito modular con capas limpias. CRM, catálogo, ventas y POS son áreas del mismo producto y despliegue. Se mantienen límites de módulo en Domain y Application antes de separar servicios: no se distribuyen transacciones de venta entre procesos.

    Browser
      └─ OJunnySyS.Web (Blazor Server + API /api/v1)
           └─ OJunnySyS.Application (casos de uso y puertos)
                └─ OJunnySyS.Domain (reglas, entidades y estados)
           └─ OJunnySyS.Infrastructure (EF Core, MySQL, integraciones)

Dependencias permitidas: Web → Application, Infrastructure; Infrastructure → Application, Domain; Application → Domain; Domain → ninguna capa externa. Los componentes Razor solo llaman a servicios de aplicación. Infrastructure crea un DbContext por operación mediante IDbContextFactory; así un circuito Blazor no conserva un contexto de larga vida.

## Módulos y responsabilidades

- **CRM**: perfil y contacto de clientes; ficha con cronología de ventas, oportunidades, actividades y tareas; oportunidades con etapas, valor, fecha esperada y responsable; actividades y tareas asociadas al cliente con vencimiento, asignación y cierre. Los responsables se validan como miembros activos del mismo tenant. Las oportunidades y actividades quedan aisladas por tenant y están expuestas como casos de uso, API versionada e interfaz.
- Los cambios de datos de cliente usan `Revision` como token de concurrencia optimista; la desactivación es lógica para preservar ventas y actividades históricas.
- **Catálogo**: SKU/código de barras, precio, categoría, estado y existencias.
- **Ventas/POS**: carrito, cliente opcional, pago, líneas y total; una transacción valida existencias, registra la venta, descuenta inventario y escribe el movimiento correspondiente.
- **Caja**: un turno abierto por empresa para la caja principal; operaciones bloquean el turno para coordinar ventas, entradas/retiros y cierre con arqueo. Cada venta conserva el usuario y el turno.
- **Compras**: proveedores por tenant, órdenes con costos capturados como snapshot, recepciones parciales y completas, e incrementos de inventario transaccionales. Cada movimiento conserva cantidad, balance posterior, tipo y referencia.
- Proveedores usan revisión optimista en edición/baja; cancelar órdenes bloquea la fila de compra y solo es válido antes de cualquier recepción o pago. Los abonos se guardan en un ledger inmutable por tenant y los pagos en efectivo se coordinan con el turno de caja en la misma transacción.
- La antigüedad de saldos se calcula por fecha de vencimiento y fecha de negocio de la empresa, no por la zona horaria del servidor.
- **Recibos**: vista imprimible derivada de la venta persistida, protegida por tenant; no acepta precios enviados por el navegador.
- **Plataforma**: empresa, usuarios, roles y ciclo de prueba. Una compuerta central revisa estado del tenant y del usuario en rutas web/API; los repositorios repiten la validación en cada operación para cubrir circuitos Blazor persistentes. Auditoría, configuración fiscal y salud operativa siguen pendientes antes de producción.

## Persistencia

MySQL 8 es el motor previsto. StoreDbContext configura índices únicos, precisión decimal y relaciones explícitas. Los cambios de esquema se conservan en Infrastructure/Persistence/Migrations. Configura la conexión con ConnectionStrings__OjunnySyS; no guardes credenciales reales en el código.

Para una venta, el repositorio bloquea los productos y cliente necesarios, valida existencias, crea Sale/SaleLine, ajusta StockOnHand y registra InventoryMovement en la misma transacción. Para recepción, bloquea orden y productos, valida la cantidad pendiente, actualiza el estado por línea y registra stock y movimiento en una sola transacción. Si falla una regla o escritura, nada de la operación se confirma. El cierre de caja y el checkout se serializan bloqueando la fila de sesión, de modo que el arqueo incluye todas las ventas confirmadas antes del cierre.

## Convenciones

- C# nullable habilitado; advertencias de compilación bloquean la integración.
- Nombres de tipos y miembros en inglés, texto visible en español.
- UTC al almacenar; la zona horaria del negocio se configura explícitamente para períodos y fechas de pantalla.
- Tipos de entrada separados de entidades persistidas; no enlazar JSON público directamente a entidades.
- Operaciones asíncronas aceptan CancellationToken.
- Errores esperados de dominio se presentan como validaciones; no exponer detalles internos al navegador.
- Los nuevos procesos comienzan como casos de uso en Application y reglas/invariantes en Domain.

## Componentes transversales necesarios

Autenticación, autorización por empresa/rol, auditoría, idempotencia de checkout, observabilidad, límites de API, copias de seguridad y estrategia de despliegue son requisitos para producción; están en roadmap.md.

## Caja y recibos

La caja principal admite un turno abierto por empresa. La apertura, cada entrada/retiro, checkout y cierre trabajan dentro de transacciones MySQL y bloquean las filas de tenant/sesión que serializan operaciones concurrentes. El efectivo esperado se calcula como fondo inicial + ventas completadas en efectivo + entradas − retiros; tarjetas/transferencias no afectan el efectivo del cajón. El cierre conserva valor esperado, conteo y diferencia, además de quién abrió/cerró.

Cada venta queda asociada al turno y al usuario autenticado. El recibo consulta el snapshot de líneas, impuestos y total guardado; sus preferencias por empresa incluyen diseño clásico/minimalista/detallado, papel térmico/A4, tipografía, tamaño, márgenes, densidad y visibilidad de datos. Estas preferencias afectan solo la representación impresa, nunca los importes históricos. La operación guarda los cambios junto a la configuración fiscal/empresarial mediante la capa de aplicación y una migración versionada.
# Estado de arquitectura

Esta es la arquitectura objetivo de OJunnySyS: ASP.NET Core .NET 10, Blazor Server interactivo y MySQL mediante EF Core. La interfaz Blazor usa la misma aplicación C# para renderizar flujos web y PWA instalables.

