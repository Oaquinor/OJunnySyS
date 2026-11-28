# ADR 0002: límites de operación offline del POS

- Estado: aceptada
- Fecha: 2026-11-28

## Contexto

OJunnySyS es una PWA Blazor Server. El service worker conserva recursos estáticos y muestra `offline.html` cuando una navegación no puede llegar al servidor. La pantalla POS usa servicios de servidor en su circuito Blazor para buscar el catálogo, leer caja y confirmar checkout. Una desconexión corta ese circuito: la pantalla instalable actual no puede confirmar ventas sin conexión.

Checkout valida en MySQL el tenant, la sesión de caja, los precios e impuestos vigentes, el inventario por almacén, los pagos y la clave de idempotencia. El cierre de caja coordina concurrentemente con ventas y otros movimientos. Por tanto, un formulario offline que presente una venta como completada y la envíe después podría mostrar precios obsoletos, vender stock no disponible o dejar el efectivo fuera del arqueo.

## Decisión

1. El POS solo considera una venta completada cuando el servidor confirma la transacción. Una operación creada sin conexión se identifica como borrador o pendiente de sincronización, nunca como venta final, recibo fiscal ni efectivo asentado en caja.
2. La pantalla Blazor Server existente seguirá siendo online-first. No se añadirá una cola de checkout en el service worker ni se guardarán credenciales, datos de clientes o métodos de pago en Cache Storage.
3. La operación POS offline requerirá una interfaz cliente que pueda ejecutarse sin circuito SignalR y una API de sincronización del servidor. El contrato debe enlazar cada operación a tenant, usuario, terminal, almacén, sesión de caja, versión de precios/impuestos y clave de idempotencia.
4. Los borradores offline se guardarán localmente cifrados y con alcance a una sesión/dispositivo. El cliente no conservará secretos de autenticación reutilizables. La política de eliminación y revocación del dispositivo debe estar definida antes de habilitar ventas completas offline.
5. Al sincronizar, el servidor revalida permisos, vigencia de caja, stock, catálogo, impuestos y pagos. La misma clave con el mismo contenido devuelve el resultado anterior; la misma clave con contenido distinto se rechaza. Las diferencias requieren resolución explícita y registro auditable; no se corrigen silenciosamente.
6. El servidor asigna numeración definitiva y genera recibos solo después de aceptar el checkout. El efectivo de una venta pendiente no altera el esperado de caja hasta la sincronización confirmada; el flujo de arqueo debe mostrar las operaciones locales todavía no conciliadas.

## Consecuencias

- La PWA instalada actualmente ofrece shell/reconexión offline, no venta offline. La interfaz debe comunicar esta limitación si pierde la conexión durante el uso.
- Una futura implementación necesita primero una capa POS cliente que use endpoints versionados para catálogo, sesión de terminal, cobro y sincronización. La aplicación no debe declararse POS offline hasta superar pruebas de reconexión, reintentos, doble envío, cierre concurrente, stock cambiado, impuesto cambiado, logout y cambio de tenant en un mismo dispositivo.
- Impresión térmica desconectada y cola de impresión también quedan después de confirmar qué recibos son ventas registradas y cuáles son comprobantes pendientes.
