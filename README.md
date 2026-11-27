# OJunnySyS

OJunnySyS es una plataforma web/PWA para CRM, POS, inventario, compras y operación multiempresa. El producto se desarrolla con **C#, ASP.NET Core .NET 10, Blazor Server interactivo y MySQL** en una arquitectura modular por capas y ejecuta localmente con Laragon.

## Estado actual

- La solución C# incluye landing, autenticación, espacio multiempresa, CRM, oportunidades, tareas, catálogo, inventario, POS, caja, compras y reportes.
- Blazor ofrece UI interactiva web y PWA instalable con `manifest`, iconos y pantalla sin conexión.
- La persistencia usa EF Core y MySQL, con migraciones versionadas y capas Domain, Application, Infrastructure y Web.
- Los módulos de negocio viven en la solución .NET; no se mantiene una segunda implementación del producto.

## Abrir localmente

En este equipo:

```powershell
.\tools\Start-OjunnyLocal.ps1
```

La aplicación Blazor se sirve en `http://localhost:5144`. El script lee las credenciales locales de `.env` y las pasa al proceso .NET sin versionarlas. La base de datos de Laragon se configura con `MYSQL_PORT`.

Para iniciar manualmente la aplicación desde la raíz del repositorio:

```powershell
dotnet restore OJunnySyS.slnx
dotnet build OJunnySyS.slnx
dotnet run --project src/OJunnySyS.Web --launch-profile http
```

Configura `.env` a partir de `.env.example`; no copies credenciales reales al repositorio.

## Arquitectura objetivo

Lee [`docs/architecture.md`](docs/architecture.md) y [`docs/roadmap.md`](docs/roadmap.md). Domain no depende de frameworks; Application coordina casos de uso y contratos; Infrastructure implementa persistencia MySQL; Blazor presenta los flujos.

`tools/New-OjunnyFeature.ps1` prepara un scaffold inicial de funcionalidad. El generador de clases basado en el estándar de OJunnySyS está documentado en [`docs/generador-de-clases.md`](docs/generador-de-clases.md) y todavía está pendiente de implementación.

## Ejecución y garantías

- El objetivo de producto es C#/.NET 10, ASP.NET Core, Blazor y MySQL.
- La PWA almacena solo recursos públicos y shell; jamás almacena páginas autenticadas, respuestas API ni operaciones de venta para replay offline.
- Las funciones aún pendientes se muestran como tales y se documentan en la hoja de ruta.

