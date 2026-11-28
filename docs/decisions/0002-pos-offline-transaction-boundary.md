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
4. Los borradores offline se guardarán localmente cifrados y con alcance a tenant/usuario/dispositivo. El cliente no conservará secretos de autenticación reutilizables ni persistirá clientes, crédito o medios/importes de pago. La política de eliminación y revocación del dispositivo debe estar definida antes de habilitar ventas completas offline.
5. Al sincronizar, el servidor revalida permisos, vigencia de caja, stock, catálogo, impuestos y pagos. La misma clave con el mismo contenido devuelve el resultado anterior; la misma clave con contenido distinto se rechaza. Las diferencias requieren resolución explícita y registro auditable; no se corrigen silenciosamente.
6. El servidor asigna numeración definitiva y genera recibos solo después de aceptar el checkout. El efectivo de una venta pendiente no altera el esperado de caja hasta la sincronización confirmada; el flujo de arqueo debe mostrar las operaciones locales todavía no conciliadas.

## Consecuencias

- El cliente WebAssembly conserva un borrador de carrito y hasta 100 productos del almacén seleccionado cifrados con AES-GCM en IndexedDB. Antes de confirmar desde el cliente POS, el navegador debe inscribir una clave ECDSA P-256; la clave privada no exportable se conserva en IndexedDB y el servidor guarda solo la clave pública, su huella y su estado revocable por tenant/usuario. El cliente firma una representación canónica que incluye la idempotencia, el terminal, cliente, vendedor, productos, cantidades, descuentos y pagos; el servidor reconstruye la representación, verifica firma e identidad, y bloquea el registro del dispositivo en la misma transacción que el checkout para serializar cobro y revocación. Una revocación impide nuevos cobros firmados desde esa clave y la interfaz permite revocar los dispositivos propios.
- El borrador local sigue siendo pendiente: no se altera caja ni se genera recibo hasta que el servidor confirma el checkout online con precios, impuestos, pagos e inventario revalidados. Esta etapa no habilita ventas sin conexión, arranque frío offline, varias órdenes pendientes, una cola de sincronización explícita, resolución de conflictos o impresión sin conexión. Tampoco sustituye las pruebas de concurrencia y de desconexión/reconexión requeridas antes de declarar terminado el POS offline.
- El service worker nunca cachea rutas autenticadas `/media/`; las imágenes de catálogo privadas se sirven con `Cache-Control: private, no-store`.
- La interfaz debe comunicar esta limitación si pierde la conexión durante el uso.
- Una futura implementación necesita primero una capa POS cliente que use endpoints versionados para catálogo, sesión de terminal, cobro y sincronización. Ya existen lecturas `/api/v1/pos/client/*` protegidas por el permiso Checkout para terminales, métodos activos, vendedores confirmados, configuración comercial y existencias por almacén; falta construir el cliente y el protocolo de sincronización. La aplicación no debe declararse POS offline hasta superar pruebas de reconexión, reintentos, doble envío, cierre concurrente, stock cambiado, impuesto cambiado, logout y cambio de tenant en un mismo dispositivo.
- Impresión térmica desconectada y cola de impresión también quedan después de confirmar qué recibos son ventas registradas y cuáles son comprobantes pendientes.
