# Hoja de ruta

## Producto objetivo

OJunnySyS se desarrolla en C#, ASP.NET Core .NET 10, Blazor Server interactivo, EF Core y MySQL. La solución es un monolito modular con Domain, Application, Infrastructure y Web; la misma aplicación entrega la experiencia web y PWA.

- [x] Solución .NET 10 dividida en proyectos Domain, Application, Infrastructure y Web.
- [x] Persistencia MySQL con EF Core y migraciones versionadas.
- [x] Landing en español con SEO básico, datos estructurados, sitemap y robots.
- [x] PWA instalable con manifest, iconos y página de reconexión.
- [x] Cliente POS dedicado en Blazor WebAssembly, servido por el mismo host y API protegida; permite buscar artículos, seleccionar cliente/vendedor/terminal/medio de pago, confirmar el checkout con precios/impuestos/stock recalculados por el servidor, abrir el recibo y usar atajos F2/F3/F4/F6/F9.
- [x] Inicio local con Laragon y configuración fuera del repositorio.
- [x] Autenticación, roles de plataforma y negocio, bloqueo de acceso y cambio de contraseña inicial.
- [x] Separación de datos y autorización por empresa en CRM, ventas, compras, caja e inventario.
- [x] CRM de clientes, historial de actividad, oportunidades, tareas, etiquetas y segmentos.
- [x] Catálogo de productos, precios, código de barras y movimientos de inventario.
- [x] POS transaccional, pagos divididos, caja, recibos y ventas a crédito con abonos.
- [x] Descuentos porcentuales por línea autorizados en servidor solo para Administrador/Gerente; los importes e impuestos se calculan sobre el neto, se guardan como snapshot, se reflejan en recibos y se prorratean en devoluciones parciales.
- [x] Devoluciones parciales, reembolsos y notas de crédito con idempotencia, control de cantidades, restock, cartera, caja y reversos contables; integración MySQL de venta con descuento, cambio de tasa, reembolso en efectivo, reverso contable, reposición e idempotencia.
- [x] Reglas de dominio cubiertas por pruebas unitarias de venta a crédito, abonos parciales, límites de saldo, idempotencia y asociación de cobros en efectivo a caja.
- [x] Entradas/retiros manuales de caja con cuentas de contrapartida, clave idempotente y asiento de doble partida; el sobrante/faltante del arqueo se contabiliza atómicamente al cerrar.
- [x] Administración multiempresa de terminales POS con caja por terminal y selección de caja en pagos en efectivo a proveedores.
- [x] Almacenes por empresa, asignación de terminal/compra, saldos por ubicación, transferencias auditables y checkout limitado al stock de la sucursal.
- [x] Ajustes de inventario con aumento, merma y conciliación contra conteo físico autorizados por rol, motivo y usuario inmutables, saldo por almacén, costo promedio/unidades sin costo, idempotencia y asiento balanceado contra cuenta de ingreso/gasto; probado contra MySQL.
- [x] Proveedores, compras, recepciones parciales y completas, cuentas por pagar y antigüedad de saldos.
- [x] Contabilización de cada recepción de compra con inventario, impuesto acreditable y cuentas por pagar; abonos idempotentes reducen la cuenta por pagar y acreditan la cuenta de pago correspondiente.
- [x] Recepciones de compra con clave idempotente por lote, huella de líneas, protección contra reenvíos y vínculo tenant del asiento contable.
- [x] Reportes por empresa y vendedor, con exportación autorizada; detalle de ventas y notas de crédito por numeración, desglose de subtotales/impuestos/cobros/saldos y CSV coherente con filtros.
- [x] Diseñador Blazor de documentos portado desde la maqueta entregada: centro de reportes con búsqueda/favoritos, paleta de componentes/campos, edición de propiedades y orden, deshacer/rehacer, duplicado/bloqueo, vista previa, impresión y exportación CSV; plantillas versionadas por empresa en MySQL conectadas al catálogo y a los indicadores de ventas.
- [x] Estados financieros con comparación del período/corte anterior, análisis vertical por cuenta y vistas gráficas basadas en asientos publicados.
- [x] Administración inicial de tenants, planes, límites y facturación manual.
- [x] Configuración por empresa de zona horaria, moneda y tasa general de impuesto, aplicada en servidor y guardada como snapshot en la venta.
- [x] Catálogo de perfiles de impuesto por empresa, asignación exclusiva por producto y tasa histórica guardada por línea; probado con dos ventas POS MySQL antes y después de cambiar la tasa.
- [x] Cotizaciones multiempresa con precios/impuestos históricos, vigencia, estados y control de revisión; conversión idempotente a venta a crédito conserva los valores ofertados, descuenta inventario y contabiliza cartera en una sola transacción; vista imprimible por empresa con descarga PDF desde el navegador.
- [x] API versionada para los flujos que requieren integración externa.
- [x] Scaffold inicial para funcionalidades y componentes reutilizables.
- [x] Generador de clases C# desde el esquema MySQL con preview y salida por capas sin sobrescribir archivos.
- [x] Recorrido inicial por empresa con checklist basada en datos y panel de estado, prueba, plan y acceso administrador.
- [x] Solicitud y restablecimiento de contraseña mediante tokens de ASP.NET Identity, respuesta que no revela si el correo existe, límites por IP, confirmación previa del email y entrega SMTP reutilizable; enlace local solo en Development si SMTP no está configurado.
- [x] Invitaciones por empresa con enlace de aceptación, creación de contraseña personal, confirmación de correo, control por tenant, reenvío de invitación y enlace local Development; los miembros no confirmados no se ofrecen como responsables en CRM.
- [ ] Verificación extremo a extremo del correo transaccional con SMTP real en el entorno local/producción.
- [ ] Pasarela de suscripciones con checkout y webhooks firmados e idempotentes.
- [ ] Integración y autorización de comprobantes electrónicos DGII/e-CF.
- [x] Configuración de cuentas bancarias y conciliación de estados por CSV contra asientos publicados; control de diferencias, partidas pendientes, aislamiento por empresa e historial cerrado. Falta automatizar conciliación de pagos de tarjetas/transferencias y validar formatos de extracto de bancos locales.
- [ ] Impresión térmica integrada y manejo de conectividad intermitente.
- [x] Paridad operativa del cliente POS WebAssembly para ventas a crédito, vencimiento, pagos divididos, descuentos autorizados en servidor, estacionar/retomar/cancelar ventas, terminal/caja, atajos de teclado y recibo imprimible. Todos los cobros se envían online al mismo caso de uso transaccional del servidor; precios, impuestos e inventario son autoritativos en el servidor.
- [x] Acceso rápido a ajustes funcionales de impresión desde el POS y el recibo; catálogo con carga local segura PNG/JPEG/WebP por empresa o foto HTTP/HTTPS visible en ambas experiencias POS; existencias iniciales asignables a almacenes activos.
- [ ] POS realmente offline: el cliente WebAssembly ahora conserva borradores de carrito y un catálogo limitado cifrados con AES-GCM en IndexedDB, aislados por empresa/usuario; al reconectar refresca productos/caja y exige confirmación online en el caso de uso transaccional normal. No completa transacciones desconectadas. Sigue pendiente validar una identidad/dispositivo offline revocable, borradores multiorden, sincronización idempotente explícita, conflictos/auditoría, arranque frío del shell y pruebas de desconexión/reconexión. Ver ADR 0002.

## Operación y producción

- [ ] Ampliar pruebas MySQL a flujos autenticados completos de venta, devolución y cobro; ya se verifican en MySQL recepciones/pagos parciales de compra, movimientos de caja, idempotencia, cierre con diferencia, conciliación bancaria y aislamiento tenant.
- [ ] Observabilidad, límites de solicitudes, alertas y auditoría operativa completa.
- [ ] Copias de seguridad cifradas con restauración practicada.
- [ ] CI/CD, migración controlada, rollback y revisión de seguridad.
- [ ] Términos, privacidad, retención y borrado selectivo de datos por tenant.

No anunciar OJunnySyS como listo para producción hasta cerrar y verificar los pendientes fiscales, operativos, de seguridad y protección de datos para el mercado de despliegue.
