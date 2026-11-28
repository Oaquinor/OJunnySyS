# Alta y espacio de empresa

## Registro público

1. Desde la landing, la persona abre `/account/register` y proporciona empresa, responsable, correo, zona horaria, moneda y contraseña.
2. El servidor valida los datos y crea en una transacción el tenant con 14 días de prueba, la cuenta administradora pendiente de confirmar, el almacén principal y la terminal POS inicial.
3. Si SMTP está configurado, envía un enlace de confirmación. En Development sin SMTP muestra el enlace local; en otros entornos no revela si una cuenta existe y permite pedir un nuevo correo.
4. Identity exige correo confirmado antes de iniciar sesión. El tenant y cada cuenta siguen aislados por empresa.

## Puesta en marcha

El administrador encuentra `Mi empresa` en la navegación. El panel lee el tenant activo y sus datos, muestra estado, plan/prueba, fecha de vencimiento en la zona horaria del tenant, usuarios habilitados y actividad. Su checklist comprueba identidad comercial, productos activos, clientes activos y ventas guardadas, y enlaza cada paso al módulo real.

La administración central de tenants y suspensión/activación se mantiene en `/platform/tenants`, protegida por el rol `PlatformAdministrator`. No es la vía de registro normal del cliente.

## Límites explícitos actuales

- El plan y el período de prueba se controlan en el servidor; al vencer la prueba, el guard de tenant bloquea operaciones.
- El cliente puede consultar su plan y vencimiento, pero aún no puede elegir ni pagar una suscripción online.
- El conteo de usuarios considera cuentas con correo confirmado, no bloqueadas y filtradas por el `TenantId` del usuario actual; invitaciones pendientes no cuentan como accesos habilitados.
- El progreso comprueba datos existentes; abrir un enlace no marca el paso como completado.
- La prueba local debe configurarse y renovarse desde la administración de plataforma; SMTP sigue siendo requisito operativo para confirmar correo fuera de Development.
