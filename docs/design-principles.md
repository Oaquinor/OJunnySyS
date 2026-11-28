# Principios de diseño

La arquitectura de esta aplicación toma como referencia los retos observados al revisar un sistema comercial previo. Este documento conserva únicamente las decisiones aplicables al proyecto nuevo.

## Retos que guiaron el diseño

- Un sistema de escritorio y varios servicios compartían solución, nombres, contratos y requisitos de compilación diferentes.
- Parte del escritorio dependía de .NET Framework, VB.NET, referencias COM y CefSharp, con compilación atada a parámetros particulares de Visual Studio.
- El arranque de una API usaba un callback estático global para comunicar el usuario activo a código de negocio.
- La configuración JWT deshabilitaba la validación de issuer y audience.
- Se debilitaba la validación implícita de campos requeridos y se aplicaba una convención global de JSON.

## Reglas adoptadas aquí

1. Blazor web y la API comparten contratos de Application; no dependen de UI de escritorio o componentes del sistema operativo.
2. No mantener usuario, empresa, transacción ni contexto HTTP en estado estático.
3. Mantener Domain independiente de ASP.NET, EF y MySQL; poner persistencia en Infrastructure.
4. Validar entradas en los casos de uso y mantener invariantes en entidades de dominio.
5. Versionar contratos HTTP y aceptar DTOs de entrada separados de entidades persistidas.
6. Diseñar autenticación y autorización seguras antes de publicar: validar emisores y audiencias, guardar secretos fuera del código y autorizar por rol/empresa.
7. Versionar cambios de datos con migraciones y guardar operaciones POS en transacciones consistentes.
8. Mantener SDK, dependencias y estilo de código definidos para permitir compilación repetible.

La autenticación, la confirmación de correo, la recuperación de contraseña y los permisos por rol están implementados con ASP.NET Identity y alcance por tenant. Las invitaciones crean cuentas pendientes sin compartir contraseñas temporales; la persona invitada confirma su correo y crea su propia contraseña mediante un token de un solo uso. La entrega SMTP real aún requiere validación en el entorno de despliegue; consulta la hoja de ruta.
