# Hoja de ruta

## Producto objetivo

OJunnySyS se desarrolla en C#, ASP.NET Core .NET 10, Blazor Server interactivo, EF Core y MySQL. La solución es un monolito modular con Domain, Application, Infrastructure y Web; la misma aplicación entrega la experiencia web y PWA.

- [x] Solución .NET 10 dividida en proyectos Domain, Application, Infrastructure y Web.
- [x] Persistencia MySQL con EF Core y migraciones versionadas.
- [x] Landing en español con SEO básico, datos estructurados, sitemap y robots.
- [x] PWA instalable con manifest, iconos y página de reconexión.
- [x] Inicio local con Laragon y configuración fuera del repositorio.
- [x] Autenticación, roles de plataforma y negocio, bloqueo de acceso y cambio de contraseña inicial.
- [x] Separación de datos y autorización por empresa en CRM, ventas, compras, caja e inventario.
- [x] CRM de clientes, historial de actividad, oportunidades, tareas, etiquetas y segmentos.
- [x] Catálogo de productos, precios, código de barras y movimientos de inventario.
- [x] POS transaccional, pagos divididos, caja, recibos, devoluciones y crédito.
- [x] Administración multiempresa de terminales POS con caja por terminal y selección de caja en pagos en efectivo a proveedores.
- [x] Almacenes por empresa, asignación de terminal/compra, saldos por ubicación, transferencias auditables y checkout limitado al stock de la sucursal.
- [x] Proveedores, compras, recepciones parciales y completas, cuentas por pagar y antigüedad de saldos.
- [x] Reportes por empresa y vendedor, con exportación autorizada.
- [x] Administración inicial de tenants, planes, límites y facturación manual.
- [x] Configuración por empresa de zona horaria, moneda y tasa general de impuesto, aplicada en servidor y guardada como snapshot en la venta.
- [x] API versionada para los flujos que requieren integración externa.
- [x] Scaffold inicial para funcionalidades y componentes reutilizables.
- [x] Generador de clases C# desde el esquema MySQL con preview y salida por capas sin sobrescribir archivos.
- [x] Recorrido inicial por empresa con checklist basada en datos y panel de estado, prueba, plan y acceso administrador.
- [ ] Invitaciones y correo transaccional real con recuperación de acceso verificada en el entorno local.
- [ ] Pasarela de suscripciones con checkout y webhooks firmados e idempotentes.
- [ ] Integración y autorización de comprobantes electrónicos DGII/e-CF.
- [ ] Conciliación de pagos de tarjetas y transferencias.
- [ ] Impresión térmica integrada y manejo de conectividad intermitente.
- [ ] Offline de POS con cola local cifrada, idempotencia y resolución de conflictos. Hasta entonces solo se mantiene accesible la shell pública sin almacenar operaciones.

## Operación y producción

- [ ] Pruebas unitarias e integración contra MySQL para flujos de caja, ventas y compras.
- [ ] Observabilidad, límites de solicitudes, alertas y auditoría operativa completa.
- [ ] Copias de seguridad cifradas con restauración practicada.
- [ ] CI/CD, migración controlada, rollback y revisión de seguridad.
- [ ] Términos, privacidad, retención y borrado selectivo de datos por tenant.

No anunciar OJunnySyS como listo para producción hasta cerrar y verificar los pendientes fiscales, operativos, de seguridad y protección de datos para el mercado de despliegue.
