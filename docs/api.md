# API versionada

Base path: `/api/v1`. Todas las rutas requieren sesión autenticada. Las respuestas usan DTOs y los casos de uso comparten validaciones con la interfaz Blazor.

| Método | Ruta | Permiso | Uso |
|---|---|---|---|
| GET | `/products?search=&includeInactive=true` | Usuario autenticado | Buscar productos activos; solo Administrador/Gerente pueden incluir inactivos |
| GET | `/users` | Administrador de la empresa | Listar únicamente usuarios del tenant activo |
| POST | `/users` | Administrador de la empresa | Crear una cuenta y devolver su contraseña temporal una sola vez |
| POST | `/users/{id}/activation` | Administrador de la empresa | Activar/desactivar usuario del tenant; impide auto-desactivación y conservar cero administradores |
| POST | `/products` | Administrador, Gerente | Crear producto |
| PUT | `/products/{id}` | Administrador, Gerente | Editar SKU, descripción, precio y código con `expectedRevision`; el stock se cambia exclusivamente mediante movimientos |
| PATCH | `/products/{id}/activation` | Administrador, Gerente | Activar/desactivar lógicamente con `{ isActive, expectedRevision }`, sin borrar existencias ni historial |
| GET | `/customers?search=` | Usuario autenticado | Buscar clientes |
| GET | `/customers/{id}` | Administrador, Gerente, Vendedor | Consultar ficha de cliente de la empresa activa |
| GET | `/customers/{id}/tags` | Administrador, Gerente, Vendedor | Listar etiquetas asignadas al cliente |
| POST | `/customers/{id}/tags` | Administrador, Gerente | Asignar etiqueta existente o crearla en la empresa y asignarla; cuerpo `{ name, color }` |
| DELETE | `/customers/{id}/tags/{tagId}` | Administrador, Gerente | Retirar etiqueta del cliente sin borrar el catálogo de etiquetas de la empresa |
| POST | `/customers` | Administrador, Gerente | Crear cliente |
| PUT | `/customers/{id}` | Administrador, Gerente | Editar contacto con `expectedRevision` para concurrencia optimista |
| DELETE | `/customers/{id}?expectedRevision=0` | Administrador, Gerente | Desactivar sin borrar historial; devuelve 409 si el registro cambió |
| GET | `/crm/opportunities?customerId=` | Administrador, Gerente, Vendedor | Consultar embudo del tenant o filtrar oportunidades por cliente |
| GET | `/crm/tags` | Administrador, Gerente, Vendedor | Listar catálogo de etiquetas del tenant y cantidad de clientes por etiqueta |
| GET | `/crm/segments` | Administrador, Gerente | Listar definiciones de segmentos dinámicos del tenant |
| GET | `/crm/segments/{id}/customers?limit=200` | Administrador, Gerente | Recalcular miembros ahora; devuelve `totalCount`, `isTruncated` y clientes (límite 1–500) |
| POST | `/crm/segments` | Administrador, Gerente | Guardar criterios: etiqueta, valor histórico, compras mínimas, días de compra reciente y activos |
| PUT | `/crm/segments/{id}` | Administrador, Gerente | Modificar criterios con `expectedRevision` para concurrencia optimista |
| DELETE | `/crm/segments/{id}?expectedRevision=0` | Administrador, Gerente | Eliminar solo la definición guardada con verificación de revisión |
| POST | `/crm/opportunities` | Administrador, Gerente, Vendedor | Crear oportunidad para cliente activo; `assignedToUserId` es opcional y debe ser una cuenta activa del mismo tenant |
| PATCH | `/crm/opportunities/{id}/stage` | Administrador, Gerente | Avanzar una oportunidad abierta; las cerradas son inmutables |
| GET | `/crm/activities?customerId=&openTasksOnly=` | Administrador, Gerente | Consultar cronología y tareas |
| POST | `/crm/activities` | Administrador, Gerente, Vendedor | Registrar nota, llamada, correo, reunión o tarea con usuario actor y `assignedToUserId` opcional del mismo tenant |
| POST | `/crm/activities/{id}/complete` | Administrador, Gerente | Completar tarea pendiente |
| GET | `/sales?limit=50` | Administrador, Gerente, Cajero | Leer ventas recientes |
| GET | `/dashboard` | Usuario autenticado | Resumen del día comercial |
| GET | `/reports/sales?from=AAAA-MM-DD&to=AAAA-MM-DD` | Administrador, Gerente | Ventas, pagos, días y productos destacados |
| GET | `/reports/payables-aging?asOf=AAAA-MM-DD` | Administrador, Gerente | Saldos abiertos agrupados por vencimiento según la fecha y zona horaria comercial |
| POST | `/pos/checkout` | Administrador, Gerente, Cajero | Registrar cobro y salida de inventario; requiere `idempotencyKey` UUID estable por intento y devuelve la venta original al reintentar la misma solicitud |
| GET | `/pos/registers?includeInactive=false` | Administrador | Consultar terminales de la empresa |
| POST | `/pos/registers` | Administrador | Crear terminal con código permanente y nombre visible |
| PUT | `/pos/registers/{id}` | Administrador | Renombrar terminal con revisión optimista |
| PATCH | `/pos/registers/{id}/activation` | Administrador | Activar/desactivar; impide desactivar la última terminal o una con turno abierto |
| GET | `/inventory/warehouses?includeInactive=false` | Administrador | Consultar almacenes de la empresa |
| POST | `/inventory/warehouses` | Administrador | Crear almacén e inicializar saldos cero para el catálogo existente |
| PUT | `/inventory/warehouses/{id}` | Administrador | Renombrar almacén con revisión optimista |
| PATCH | `/inventory/warehouses/{id}/activation` | Administrador | Activar/desactivar; protege existencias, órdenes pendientes y terminales asignadas |
| POST | `/inventory/transfers` | Administrador, Gerente | Trasladar unidades entre almacenes con salida y entrada atómicas |
| GET | `/suppliers?search=` | Administrador, Gerente, Agente de compras | Buscar proveedores activos |
| POST | `/suppliers` | Administrador, Gerente, Agente de compras | Crear proveedor |
| PUT | `/suppliers/{id}` | Administrador, Gerente, Agente de compras | Editar proveedor con revisión optimista |
| DELETE | `/suppliers/{id}?expectedRevision=0` | Administrador, Gerente, Agente de compras | Desactivar sin borrar órdenes ni pagos históricos |
| GET | `/purchases` | Administrador, Gerente, Agente de compras | Consultar órdenes recientes con sus líneas |
| POST | `/purchases` | Administrador, Gerente, Agente de compras | Crear orden de compra (no altera existencias) |
| POST | `/purchases/{id}/receive` | Administrador, Gerente, Agente de compras | Recibir cantidades por línea; aumenta existencias y registra movimientos en una transacción |
| POST | `/purchases/{id}/payments` | Administrador, Gerente | Registrar abono con `idempotencyKey`, importe, método, referencia opcional y `registerCode`; los pagos en efectivo generan salida en el turno de esa terminal |
| POST | `/purchases/{id}/cancel` | Administrador, Gerente, Agente de compras | Cancelar una orden sin recepciones ni pagos; no modifica inventario |

Checkout recibe `customerId` opcional, `paymentMethod` (`Cash`, `Card` o `Transfer`), `registerCode`, `idempotencyKey` UUID y `lines` con `{ productId, quantity }`. Reutiliza la clave solo para reintentar el mismo intento; si se envía con otros datos devuelve error. El servidor toma precios, descripciones y existencias de MySQL; no acepta esos valores desde el cliente.

Para una compra se envía `supplierId`, `warehouseId`, referencia opcional, `dueDate` opcional (`AAAA-MM-DD`) y `lines` con `{ productId, quantity, unitCost }`. Las recepciones envían una colección de `{ lineId, quantity }`, permitiendo recepciones parciales repetidas hasta completar cada línea. Un mismo producto debe consolidarse en una línea por orden. Las salidas del POS, las entradas por recepción y los traslados se reflejan por almacén en `InventoryMovements`; el saldo global del producto equivale a la suma de sus almacenes.

Errores de dominio se devuelven como Problem Details de validación. La plataforma protege la API con cookie `HttpOnly`; fuera de desarrollo usa HTTPS. La limitación de solicitudes, idempotencia de pagos y credenciales de integración aún deben completarse antes de publicar endpoints en Internet.

## Caja y recibos

| Método | Ruta | Permiso | Uso |
|---|---|---|---|
| GET | `/cash/session?registerCode=MAIN` | Administrador, Gerente, Cajero | Consultar el turno abierto de una terminal y su arqueo esperado |
| GET | `/cash/sessions?limit=10` | Administrador, Gerente, Cajero | Consultar turnos recientes y cierres |
| POST | `/cash/sessions` | Administrador, Gerente, Cajero | Abrir turno; cuerpo `{ registerCode, openingFloat }` para una terminal activa |
| POST | `/cash/sessions/{id}/movements` | Administrador, Gerente, Cajero | Registrar `CashIn` o `CashOut` con importe y motivo |
| POST | `/cash/sessions/{id}/close` | Administrador, Gerente, Cajero | Cerrar turno y guardar `{ countedCash }` |
| GET | `/sales/{id}/receipt` | Administrador, Gerente, Cajero | Consultar recibo de una venta del tenant |

El arqueo esperado suma fondo inicial, ventas completadas en efectivo y movimientos de entrada; resta retiros. Una venta nueva requiere un turno abierto para el `registerCode` indicado; el terminal determina el almacén y el checkout descuenta sus existencias sin consumir stock de otras ubicaciones. El recibo imprimible se presenta en `/app/receipts/{id}`.

El módulo actual registra un método de pago por venta. Pagos divididos, idempotencia, conciliación bancaria y recibos fiscales oficiales siguen pendientes.
