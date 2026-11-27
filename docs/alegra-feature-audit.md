# Auditoría funcional de Alegra

## Alcance y método

La revisión se hizo en la cuenta de prueba abierta por el propietario, leyendo pantallas, formularios, columnas, selectores y estados vacíos. No se copiaron datos privados de la cuenta. Se creó únicamente el artículo `TEST OJunnySyS - Producto demo POS` para verificar que el catálogo de Contabilidad se sincroniza con POS. Se preparó un carrito de RD$100 para leer el paso de cobro y se descartó antes de confirmar el pago. No se crearon facturas, pagos, movimientos de caja ni cierres.

La prueba está en República Dominicana y muestra numeración fiscal NCF, DOP, sucursal, almacén y cuentas. Los formularios fiscales y la facturación electrónica oficial necesitan reglas locales y validación DGII antes de ofrecerse como funcionalidad fiscal.

## Alegra Contabilidad: módulos observados

| Área | Funciones visibles observadas | Estado de OJunnySyS |
|---|---|---|
| Inicio | Tarjetas de cuentas por cobrar/pagar, impuestos, productos vendidos, devoluciones, clientes con ventas y ventas por período; selectores de período | Panel diario y ventas básicos; falta panel configurable y métricas fiscales/retornos |
| Ingresos | Facturas normales y rápidas, recurrentes, cobros, notas de crédito/débito, cotizaciones, conduces; cliente, NCF, vendedor, almacén, centro de costo, lista de precios, moneda, vencimiento, descuentos, impuestos, retenciones, notas, términos, pagos y formato de impresión | POS/ventas con cliente, vendedor, pagos, stock, impuestos por producto y ventas a crédito con abonos; faltan cotizaciones/conduces/facturas independientes, NCF/DGII, multimoneda, retenciones, términos, descuentos, devoluciones y notas de crédito |
| Compras | Facturas de proveedor, pagos/gastos, gastos menores y recurrentes, notas, órdenes, recepción de comprobantes | Proveedores, órdenes con impuesto desglosado por línea, recepción parcial/completa, saldos y pagos; faltan la factura de proveedor como documento legal, gastos libres/recurrentes y gestión de comprobantes |
| Contactos | Clientes y proveedores centralizados con historial de documentos | CRM de clientes, proveedores y ficha/cronología parcial; ampliar datos fiscales, múltiples contactos, actividad y documentos relacionados |
| Inventario | Productos/servicios, valoración por fecha/almacén, ajustes, importación/actualización masiva, listas de precios, almacenes/transferencias, categorías, variantes y atributos/campos personalizados | Catálogo, costo promedio ponderado y valor a costo conocido, stock por almacén y movimientos; los saldos históricos sin costo se identifican como no costeados. Faltan ajustes auditables, transferencias entre almacenes, listas, variantes/atributos e importación masiva |
| Bancos y cajas | Cuentas de efectivo/banco/tarjeta, movimientos, transferencias y conciliación bancaria con balance inicial, cargos/entradas y diferencia | Caja operativa, entradas/retiros, historial e informe por turnos; faltan cuentas bancarias, libro de movimientos y conciliación |
| Contabilidad | Catálogo de cuentas, asientos, activos, libro diario y complementos contables | Catálogo por empresa, cuentas base protegidas, asientos manuales de doble partida, libro diario y balance de comprobación; las ventas generan asiento por cuenta asignada al medio de pago, ingresos/impuestos y costo conocido de inventario. Pendiente: asientos desde compras, movimientos manuales/cierres de caja, conciliación bancaria, cierres de período, estados financieros, activos e informes fiscales. |
| Reportes | 7 reportes de ventas, 7 administrativos, 1 financiero, 9 contables, 8 fiscales, 2 para exportar, favoritos e historial de exportación. En Ventas generales se observaron rango de fecha, almacén, numeración y más filtros; tarjetas de ventas brutas, notas de crédito, antes/después de impuestos y total; gráfico Total ventas y descarga. | Ventas por día, método, producto y vendedor; rango rápido de fechas; filtros reales por almacén y vendedor; CSV con resúmenes; arqueos e informe de antigüedad de cuentas por pagar. La cuenta de prueba no tiene ventas, por lo que los reportes de Alegra aparecieron vacíos. Pendientes: ventas por cliente/almacén, rentabilidad completa, estado de cuenta por cliente, filtro por numeración, detalle completo exportable, más reportes administrativos/contables/fiscales, favoritos e historial. |
| Tareas | Bandeja de tareas y notificaciones | CRM tiene actividades y vencimientos; ampliar bandeja, asignación, alertas y filtros |
| Configuración | Empresa/usuarios/perfil, centros de costo, monedas; términos, NCF y vendedores; plantillas; impuestos/retenciones/categorías; configuración contable, cierre, tipos/numeraciones; notificaciones/correos; campos personalizados/variantes; integraciones e historial | Empresa/impuesto general/impresión base, identidad y usuarios/plataforma; faltan catálogo fiscal, numeraciones, centros de costo, usuarios por sucursal, auditoría y preferencias documentales |

## POS: recorrido y hallazgos

### Reportes de ventas observados en Alegra

La categoría Ventas presenta estos siete reportes: Ventas generales, Ventas por producto/servicio, Ventas por cliente, Rentabilidad por producto, Ventas por vendedor, Estado de cuenta por cliente y Ventas por almacén. La vista general incluye período, almacén, numeración y filtros adicionales, además de totales antes/después de impuestos, notas de crédito, gráfico de ventas y descarga. En la inspección individual adicional se confirmaron: producto/servicio con cantidades y ventas antes/después de impuestos (incluye documentos pendientes de pago); cliente con período y descarga; rentabilidad con vendido, costo y total/porcentaje y botón Personalizar; vendedor con período, vendedor y almacén; estado de cuenta con selección obligatoria de cliente, fecha de corte y vencimiento, y envío del informe al cliente; almacén con filtros, ranking de cinco almacenes por ingresos y ventas, y detalle de subtotal, devoluciones e impuestos. La cuenta tiene pocos movimientos, por lo que la mayoría de vistas estaban vacías y no fue posible verificar todas las columnas de los seis informes.

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
9. **Contabilidad de partida doble**: catálogo de cuentas con plantilla base por empresa, asientos manuales balanceados, libro diario y balance de comprobación. El checkout genera el asiento de venta y el costo de venta dentro de la transacción idempotente: débito a la cuenta de activo configurada para cada medio de pago, crédito a Ingresos por ventas/impuestos y, para el inventario con costo conocido, débito a Costo de ventas y crédito a Inventario. Las recepciones parciales/completas generan asientos de inventario, impuesto acreditable y cuentas por pagar; los pagos a proveedores debitan cuentas por pagar y acreditan el activo elegido. Cada producto mantiene costo promedio ponderado, actualizado al recibir compras, y la línea de venta conserva el costo usado en esa fecha; el informe de ventas y su CSV incluyen costo y margen bruto. Las ventas y pagos históricos conservan su cuenta y costo; los saldos anteriores a esta migración mantienen el único destino contable que existía entonces. Las ventas anteriores al registro de costo tienen costo histórico desconocido y no permiten reconstruir margen sin datos de costos. Pendientes: movimientos manuales/cierres de caja contables, conciliación bancaria, anulaciones, notas de crédito y estados financieros. La contabilidad sigue incompleta y no debe presentarse aún como libro contable integral.

## Validación end-to-end local aislada (27/11/2026)

Se usó una base MySQL exclusiva de pruebas, sin tocar la empresa local principal ni generar pagos externos:

- Compra: 5 unidades a RD$100, ITBIS 18 %, total RD$590. La recepción de 2 y luego 3 unidades dejó stock en 5 y generó asientos balanceados por RD$236 y RD$354; el pago de proveedor por transferencia dejó cuentas por pagar en cero.
- POS: fondo de prueba RD$1,000, venta de una unidad a RD$250 + RD$45 de ITBIS y cobro efectivo total RD$295. La existencia bajó de 5 a 4; el efectivo esperado y contado fue RD$1,295 y la diferencia del cierre quedó en cero.
- Balance: se detectó y corrigió la orientación de saldos acreedores; el balance de comprobación quedó en RD$885 débito y RD$885 crédito.
- Defectos detectados y corregidos: enlaces numéricos que conservaban valores iniciales, `InputNumber` enlazado a un índice de diccionario que terminaba el circuito Blazor, lectura `DateOnly` incompatible con el lector MySQL y orientación invertida en el balance. Los valores numéricos se unificaron en `NumericInput`, y las fechas se convierten entre `DateOnly` y `DateTime` en el límite de persistencia.

Las ventas, órdenes, cierres, artículos y pagos descritos arriba pertenecen solo a la base de prueba local. La cuenta de Alegra se mantuvo en modo lectura durante esta revalidación.

## Revisión visual pendiente

La captura del área de Caja mostró controles pegados, un formulario de fondo inicial sin separación y el menú nativo del selector con esquinas cuadradas. Se corrigió la distribución de caja, la separación entre importe y acción, los estados responsivos y la apariencia redondeada de los selectores y campos. El popup de opciones sigue siendo el menú nativo del navegador/sistema y puede conservar sus esquinas propias. El resto de los módulos requiere una revisión visual pantalla por pantalla; la auditoría funcional anterior no implica que cada subapartado esté ya implementado.

Las siguientes entregas de código deben mantener el monolito modular .NET/Blazor/MySQL, tenant scoping en índices y relaciones, API/casos de uso separados del UI, migraciones compatibles, snapshots inmutables para datos históricos y operaciones financieras idempotentes. No se debe llamar “factura fiscal electrónica” a una impresión POS sin integración DGII validada.

## Fuera de alcance observado o sin acceso

El plan de prueba mostró módulos Nómina, portal de clientes y Alegra Ventas como soluciones adicionales. La opción de POS Restobar estaba disponible como activación y no se activó. Los reportes fiscales listaban 606, 607, 608, IT-1, impuestos/retenciones e IR-17, pero no se generó ni transmitió ninguna declaración. Facturación electrónica aparecía desactivada. No se hicieron cambios a la configuración del negocio, usuarios, pagos o plan.

## Ampliación de auditoría de reportes (27/11/2026)

Se recorrieron reportes contables y administrativos dentro de la cuenta de prueba, manteniendo la navegación en modo lectura:

- **Estado de situación financiera**: corte, comparación con período/año anterior, análisis vertical, variación absoluta y relativa, detalle por cuentas y enlace desde cada renglón al mayor filtrado por cuenta. Categorías observadas: efectivo, cuentas por cobrar, impuestos, inventario, activos fijos, cuentas por pagar, obligaciones, impuestos, capital, reservas, resultados y ajustes iniciales.
- **Estado de resultados**: filtro de período, centro de costos, vista detallada, descarga, comparativos y agrupación por conceptos de ingreso/gasto (ventas, costos, gastos operativos, financieros e impuestos).
- **Cuentas por cobrar y por pagar**: período requerido, bandas de vencimiento (hasta 30, 31–60, 61–90, más de 90 días y no vencidas), total pendiente, filtro y detalle por documento con fecha de creación/vencimiento, total, cobrado/pagado y saldo abierto. La cuenta no tenía documentos pendientes.
- **Valor de inventario**: fecha de corte y almacén, total valorizado y detalle de producto, referencia, cantidad, unidad, estado, costo promedio y total; incluye filtros, paginación y descarga. La cuenta presentaba el producto de prueba con 10 unidades y valor de RD$500.
- **Administrativos**: el catálogo contiene cuentas por cobrar/pagar, ingresos y compras, valor de inventario, transacciones, compras y reporte anual. Transacciones filtra período, muestra entradas/salidas y excluye transferencias entre bancos. Compras separa importes antes/después de impuestos, ofrece filtros y acceso a crear factura. El reporte anual permite seleccionar año y comparar con el anterior; resume ventas y rankings de productos, vendedores y clientes. No había transacciones de banco, compras ni ventas en el período seleccionado.
- **Financieros**: un reporte de flujo de caja con rango mensual y descarga. La vista previa no terminó de cargar durante la inspección; su desglose queda pendiente de observar.
- **Contables**: los nueve listados son estado de resultados, situación financiera, flujo de efectivo, libro diario, auxiliar por tercero, balanza de comprobación, movimientos por cuenta y movimientos agrupados. El libro diario filtra período y tipo, con asiento, estado, fecha, tercero, código, cuenta contable, centro de costo, débitos y créditos. Auxiliar por tercero pide tercero, cuenta y período; movimientos por cuenta pide cuenta y rango; el agrupado permite agrupar por cuenta y tercero. El flujo de efectivo tiene opciones tipo “Este mes” y “Generar vista previa”.
- **Fiscales**: el catálogo muestra IT-1/Anexo A, impuestos detallados, impuestos y retenciones, IR-17 y cartas de retención; 606/607/608 también se listan en el catálogo (no se generó ninguna declaración). IT-1 e IR-17 tienen “Nuevo reporte” e historial/estado; el detallado desglosa ventas, devoluciones, compras y devoluciones por tipo de impuesto, base imponible e importe; impuestos/retenciones filtra período, vista y tipo de impuesto; cartas de retención muestra estados listos/por completar y pide firma/sello para emitir. Son funciones fiscales dominicanas y requieren validación normativa y de la DGII antes de implementación o envío.
- **Para trabajar/Favoritos**: dos exportaciones: listado detallado de facturas de venta e informe para contador (ventas, gastos y otros); la categoría de favoritos estaba vacía.
- **Flujo de caja financiero**: periodo mensual y exportación. La vista previa no terminó de cargar durante la inspección, por lo que el desglose interno queda pendiente de una nueva revisión.
- Se detectó que el primer intento de abrir algunos reportes devolvió pantallas de carga; se esperó la carga completa antes de registrar su estructura. No se generaron documentos, pagos ni movimientos en Alegra.

## Entrega de esta revisión

OJunnySyS ahora presenta **Estados financieros** en `/app/reports/financial-statements`: estado de resultados por rango de hasta 366 días y situación financiera a la fecha final, ambos obtenidos exclusivamente de asientos publicados y cuentas de la empresa activa. Calcula saldos normales deudores/acreedores y agrega el resultado acumulado de las cuentas de ingreso y gasto al patrimonio para comprobar la ecuación contable. El reporte muestra el estado cuadrado o una diferencia, estados vacíos explícitos y enlaces desde Contabilidad y la navegación principal. Se validó manualmente en `localhost:5144`: el módulo carga, el botón actualiza sin errores y el libro local vacío muestra importes cero y balance cuadrado. El reporte operativo también agrupa ventas completas por cliente y almacén, con subtotal, impuesto, total y número de ventas a partir del mismo conjunto filtrado por fecha, vendedor y almacén.

Los reportes de cuentas por pagar ya existen en Compras con bandas y saldos; el reporte de valoración está en Inventario. Se agregó el flujo de venta a crédito con cliente/vencimiento, saldo derivado de cobros persistidos, antigüedad de cartera, recibos parciales idempotentes, pólizas a cuentas por cobrar y movimiento de caja solo cuando el abono es en efectivo. La pantalla POS evita tratar el crédito como efectivo y el módulo Cuentas por cobrar permite registrar abonos y consultar saldos por antigüedad. **La migración y compilación están verificadas; no se ha ejecutado todavía un ciclo completo de venta-abono con una sesión de usuario autenticada ni se ofrece aún una factura fiscal/e-CF/NCF.** La situación financiera y el resultado se respaldan ahora en el mayor; todavía no se ofrece comparación interanual, análisis vertical, filtros por centro de costo, navegación de detalle por renglón ni descarga propia de estados. También siguen pendientes flujo de caja contable, reportes fiscales dominicanos y el catálogo individual de filtros/columnas aún no observados. El flujo financiero y los formularios DGII requieren revisión adicional antes de replicar cálculos o declarar conformidad fiscal.

## Referencias fiscales oficiales consultadas

## Estado de cuenta por cliente

Se agregó una consulta de estado de cuenta por cliente con rango de hasta 366 días, saldo anterior al rango, ventas y cobros ordenados por fecha, vencimiento, balance corrido y saldo al corte. La misma fuente sirve a la pantalla, endpoint protegido y exportación CSV; las fechas del CSV usan la zona horaria comercial. El saldo se calcula desde ventas completadas, pagos iniciales y recibos persistidos; no sustituye una factura fiscal ni incluye notas de crédito/devoluciones, que siguen pendientes.

- [DGII: Formatos de envío de datos](https://dgii.gov.do/cicloContribuyente/obligacionesTributarias/remisionInformacion/Paginas/formatoEnvioDatos.aspx): propósito de 606/607/608/609 y guías vigentes.
- [DGII: Biblioteca de instructivos para formatos](https://dgii.gov.do/publicacionesOficiales/bibliotecaVirtual/contribuyentes/formatoEnvioDatos/Paginas/default.aspx): la DGII publica instructivos revisados en 2025–2026; el generador debe versionar el formato y sus reglas por período.
- [DGII: Declaraciones juradas](https://dgii.gov.do/cicloContribuyente/obligacionesTributarias/declaracionPagoImpuestos/declaracionesJuradasPFyPJ/Paginas/default.aspx): IT-1 e IR-17 tienen obligaciones, periodicidad y reglas distintas; no basta con copiar la interfaz del competidor.
