# Auditoría funcional de Alegra

## Alcance y método

La revisión se hizo en la cuenta de prueba abierta por el propietario, leyendo pantallas, formularios, columnas, selectores y estados vacíos. No se copiaron datos privados de la cuenta. Se creó únicamente el artículo `TEST OJunnySyS - Producto demo POS` para verificar que el catálogo de Contabilidad se sincroniza con POS. Se preparó un carrito de RD$100 para leer el paso de cobro y se descartó antes de confirmar el pago. No se crearon facturas, pagos, movimientos de caja ni cierres.

La prueba está en República Dominicana y muestra numeración fiscal NCF, DOP, sucursal, almacén y cuentas. Los formularios fiscales y la facturación electrónica oficial necesitan reglas locales y validación DGII antes de ofrecerse como funcionalidad fiscal.

## Alegra Contabilidad: módulos observados

| Área | Funciones visibles observadas | Estado de OJunnySyS |
|---|---|---|
| Inicio | Tarjetas de cuentas por cobrar/pagar, impuestos, productos vendidos, devoluciones, clientes con ventas y ventas por período; selectores de período | Panel diario y ventas básicos; falta panel configurable y métricas fiscales/retornos |
| Ingresos | Facturas normales y rápidas, recurrentes, cobros, notas de crédito/débito, cotizaciones, conduces; cliente, NCF, vendedor, almacén, centro de costo, lista de precios, moneda, vencimiento, descuentos, impuestos, retenciones, notas, términos, pagos y formato de impresión | POS/ventas con cliente, vendedor, pagos, stock, impuesto general heredado o tasa por producto (incluido 0 %); faltan documentos comerciales, NCF/DGII, multimoneda, retenciones, términos, descuentos y asignaciones |
| Compras | Facturas de proveedor, pagos/gastos, gastos menores y recurrentes, notas, órdenes, recepción de comprobantes | Proveedores, órdenes con impuesto desglosado por línea, recepción parcial/completa, saldos y pagos; faltan la factura de proveedor como documento legal, gastos libres/recurrentes y gestión de comprobantes |
| Contactos | Clientes y proveedores centralizados con historial de documentos | CRM de clientes, proveedores y ficha/cronología parcial; ampliar datos fiscales, múltiples contactos, actividad y documentos relacionados |
| Inventario | Productos/servicios, valoración por fecha/almacén, ajustes, importación/actualización masiva, listas de precios, almacenes/transferencias, categorías, variantes y atributos/campos personalizados | Catálogo, costo promedio ponderado y valor a costo conocido, stock por almacén y movimientos; los saldos históricos sin costo se identifican como no costeados. Faltan ajustes auditables, transferencias entre almacenes, listas, variantes/atributos e importación masiva |
| Bancos y cajas | Cuentas de efectivo/banco/tarjeta, movimientos, transferencias y conciliación bancaria con balance inicial, cargos/entradas y diferencia | Caja operativa, entradas/retiros, historial e informe por turnos; faltan cuentas bancarias, libro de movimientos y conciliación |
| Contabilidad | Catálogo de cuentas, asientos, activos, libro diario y complementos contables | Catálogo por empresa, cuentas base protegidas, asientos manuales de doble partida, libro diario y balance de comprobación; las ventas generan asiento por cuenta asignada al medio de pago, ingresos/impuestos y costo conocido de inventario. Pendiente: asientos desde compras, movimientos manuales/cierres de caja, conciliación bancaria, cierres de período, estados financieros, activos e informes fiscales. |
| Reportes | Ventas (7), administrativos (7), flujo de caja, contables (9), fiscales (8), exportables (2), favoritos e historial de exportación | Ventas por día, método, producto y vendedor; rango rápido de fechas; filtros reales por almacén y vendedor; CSV con resúmenes; arqueos e informe de antigüedad de cuentas por pagar. La cuenta de prueba no tiene ventas, por lo que sus reportes aparecieron vacíos. Pendientes: filtro por numeración, exportar detalle completo, más reportes administrativos/contables/fiscales, favoritos e historial. |
| Tareas | Bandeja de tareas y notificaciones | CRM tiene actividades y vencimientos; ampliar bandeja, asignación, alertas y filtros |
| Configuración | Empresa/usuarios/perfil, centros de costo, monedas; términos, NCF y vendedores; plantillas; impuestos/retenciones/categorías; configuración contable, cierre, tipos/numeraciones; notificaciones/correos; campos personalizados/variantes; integraciones e historial | Empresa/impuesto general/impresión base, identidad y usuarios/plataforma; faltan catálogo fiscal, numeraciones, centros de costo, usuarios por sucursal, auditoría y preferencias documentales |

## POS: recorrido y hallazgos

### Pantalla principal y terminal

- El POS es una aplicación separada de Contabilidad y abre desde el selector de soluciones. La cabecera muestra disponibilidad, sucursal/terminal activa, sesión de caja, búsqueda por nombre/código de barras y selector de productos.
- La venta incluye lista de precios, numeración NCF, cliente (Consumidor final predeterminado), atajo de cliente nuevo, vista previa de impresión, preferencias de venta, carrito y botón de cobro.
- Un producto creado en el catálogo contable se sincronizó y apareció en POS con existencias y precio. Una búsqueda lenta mostró el estado de carga y después cargó el artículo.
- El carrito calcula el total y permite ir al paso de pago; los métodos disponibles son efectivo, tarjeta de crédito, transferencia, tarjeta débito, combinado y otros métodos. El importe preparado no fue cobrado.
- Existe venta simple para vender conceptos sin crear producto de inventario. No se activó ni se guardó preferencia alguna.
- El historial accesible desde la cabecera no presentó datos de venta en esta cuenta vacía durante la revisión.

### Turnos y caja

- El cierre detalla fecha inicial, total de ventas, fondo, efectivo, tarjeta de débito/crédito, transferencias, devoluciones, total de movimientos y efectivo esperado.
- Permite introducir dinero real contado, observaciones y registrar un nuevo movimiento antes del cierre. El aviso explica que el efectivo esperado se puede conciliar con ingresos/retiros.
- La pantalla advierte que se debe sincronizar la información y revisar pagos pendientes antes de cerrar. No se intentó guardar/cerrar el turno.
- La sucursal define banco de apertura y banco de cierre para el turno.

### Configuración POS

| Apartado | Campos y opciones observadas |
|---|---|
| Negocio | RNC, nombre, ciudad/provincia, dirección, precisión decimal, correo/teléfono/sitio, sector, logo; multimoneda; turnos; cuentas contables de entradas y salidas de efectivo |
| Sucursal actual | Nombre, numeraciones de ventas/devoluciones/recibos/gastos, almacén, lista de precios, nombre visible y contacto en tirilla, usuarios habilitados, banco por tarjeta débito/crédito/transferencia/efectivo, cuenta base y banco de cierre del turno, centro de costo y vendedor predeterminados, ventas pendientes/prefijo, venta simple |
| Sucursales | Lista de terminales con numeración, almacén y acciones; alta de sucursal |
| Vendedores | Alta/administración con nombre, identificación, observaciones y acciones |
| Métodos de pago | Activa efectivo, tarjeta de crédito, transferencia, tarjeta débito y combinado; permite añadir método con nombre, indicaciones, cuenta bancaria y QR |
| Preferencias de venta | Propina como impuesto legal local |
| Plantilla de impresión | Diseños clásico/minimalista/detallado, papel 80/57 mm, interlineado estándar/reducido, márgenes, fuente clásica/moderna/compacta, tamaño pequeño/mediano/grande, frase sugerida, vista previa, impresión de prueba y secciones configurables: logo, vendedor, descripción, referencia, precio unitario, unidad, impuestos incluidos, total de líneas/productos, pie y QR/imagen |
| Balanza | Perfil de balanza y conexión al puerto del dispositivo para leer peso del producto durante la venta. No se activó el hardware |
| Cuenta/plan | Estado del plan, facturación electrónica, perfil, consumo y accesos a POS Windows; no se cambiaron |

## Comparación con la implementación actual

OJunnySyS ya tiene catálogo con SKU/barcode, stock agregado y por almacén, búsqueda POS filtrada por el almacén de la terminal, cliente, vendedor asignable con nombre snapshot, cobro por medios configurables y combinado, cajas y arqueos por terminal, transferencias auditables, recepciones por almacén, movimiento de entrada/retiro, cierre con efectivo contado/diferencia, historial, ventas idempotentes y PWA. El impuesto de cada producto hereda la tasa de la empresa o define una propia (incluido 0 % exento); cada venta guarda la tasa usada por línea para preservar sus comprobantes históricos. Las órdenes de compra admiten una tasa por línea, conservan el subtotal/impuesto/total y guardan los importes históricos sin alterar órdenes ya existentes. Cada medio de pago tiene una cuenta contable de activo configurable; el asiento distribuye el débito entre esas cuentas y la línea de cobro guarda un snapshot para preservar el destino histórico. Cada venta conserva snapshots de método/importes; el arqueo suma únicamente las partes en efectivo y los reportes y recibos muestran cada medio. La migración convierte ventas existentes a un pago histórico preservando importes, método y asignación contable. La venta es transaccional: saldo del almacén, saldo global, ledger, venta y turno se escriben coordinadamente. El POS muestra un estado de carga explícito mientras consulta terminales e inventario. El recibo también permite configurar diseño clásico/minimalista/detallado, ancho de papel, tipografía, tamaño, márgenes, densidad de líneas y visibilidad del precio unitario/importe por artículo; estas preferencias se guardan por empresa y se aplican en la vista imprimible.

La revisión confirma que los siguientes trabajos son concretos y necesarios:

1. **Configuración POS por empresa y terminal**: gestión de terminales y almacenes, selección de almacén por terminal y stock por ubicación ya están implementados; faltan numeraciones, vendedor, centro de costo y destinos de pago, separando configuración global y configuración de sucursal.
2. **Medios de pago configurables**: implementados por empresa con tipo, instrucciones, datos de cuenta, payload QR opcional y cuenta contable de activo; medios configurables se pueden activar/desactivar, el POS divide el importe entre hasta diez medios y valida el total y la cuenta contable en servidor. Reportes, recibos, asientos y arqueo usan el detalle inmutable de cada medio; los históricos se migran preservando la asignación inicial. Pendiente: generar/mostrar QR gráfico y conciliar cuentas con bancos externos.
3. **Vendedores vinculados a ventas**: implementados como rol de usuario asignable, selección en POS, validación server-side del usuario activo dentro de la empresa y snapshot del nombre en venta/recibo/historial; el informe incluye ventas e ingresos por vendedor. Pendientes: comisiones, metas y filtros por vendedor.
4. **Preferencias de venta**: faltan descuento, venta rápida, propina/impuestos locales, monedas y ventas pendientes, siempre con validación transaccional.
5. **Diseñador de recibo**: presets, ancho térmico/A4, densidad/márgenes, tipografía, precio unitario/importe, cliente/SKU/impuesto y mensaje ya están en configuración; siguen pendientes orden/visibilidad de más campos, QR, vista previa en vivo y prueba de impresión.
6. **Caja por terminal**: implementado con turnos simultáneos por terminal, entradas/retiros, arqueo e informes; los pagos en efectivo a proveedores salen de la caja seleccionada.
7. **Dispositivos**: interfaz de extensión para balanza, lector y futuras impresoras; degradación limpia cuando el navegador/dispositivo no es compatible.
8. **Caja de auditoría/reportes**: cierres por operador/terminal, ventas por método y vendedor, conciliación y trazabilidad de cambios.
9. **Contabilidad de partida doble**: catálogo de cuentas con plantilla base por empresa, asientos manuales balanceados, libro diario y balance de comprobación. El checkout genera el asiento de venta y el costo de venta dentro de la transacción idempotente: débito a la cuenta de activo configurada para cada medio de pago, crédito a Ingresos por ventas/impuestos y, para el inventario con costo conocido, débito a Costo de ventas y crédito a Inventario. Cada producto mantiene costo promedio ponderado, actualizado al recibir compras, y la línea de venta conserva el costo usado en esa fecha; el informe de ventas y su CSV incluyen costo y margen bruto. Las ventas y pagos históricos conservan su cuenta y costo; los saldos anteriores a esta migración mantienen el único destino contable que existía entonces. Las ventas anteriores al registro de costo tienen costo histórico desconocido y no permiten reconstruir margen sin datos de costos. Todavía no hay asiento automático de recepción/compra e impuesto acreditable, pagos a proveedores, movimientos manuales/cierres de caja, anulaciones ni notas de crédito. Por eso la contabilidad sigue incompleta y no debe presentarse aún como libro contable integral.

## Revisión visual pendiente

La captura del área de Caja mostró controles pegados, un formulario de fondo inicial sin separación y el menú nativo del selector con esquinas cuadradas. Se corrigió la distribución de caja, la separación entre importe y acción, los estados responsivos y la apariencia redondeada de los selectores y campos. El popup de opciones sigue siendo el menú nativo del navegador/sistema y puede conservar sus esquinas propias. El resto de los módulos requiere una revisión visual pantalla por pantalla; la auditoría funcional anterior no implica que cada subapartado esté ya implementado.

Las siguientes entregas de código deben mantener el monolito modular .NET/Blazor/MySQL, tenant scoping en índices y relaciones, API/casos de uso separados del UI, migraciones compatibles, snapshots inmutables para datos históricos y operaciones financieras idempotentes. No se debe llamar “factura fiscal electrónica” a una impresión POS sin integración DGII validada.

## Fuera de alcance observado o sin acceso

El plan de prueba mostró módulos Nómina, portal de clientes y Alegra Ventas como soluciones adicionales. La opción de POS Restobar estaba disponible como activación y no se activó. Los reportes fiscales listaban 606, 607, 608, IT-1, impuestos/retenciones e IR-17, pero no se generó ni transmitió ninguna declaración. Facturación electrónica aparecía desactivada. No se hicieron cambios a la configuración del negocio, usuarios, pagos o plan.
