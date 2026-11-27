# Generador de clases de OJunnySyS

## Referencia recibida

`ClaseSys_VB-main` es una aplicación de escritorio VB.NET que consulta metadatos de tablas en MySQL o SQL Server, presenta las tablas y claves disponibles y genera código para las clases. Esa secuencia —conexión, selección de tabla, lectura del esquema, mapeo de columnas y generación— es la referencia funcional para el generador propio.

## Estándar de OJunnySyS

El generador propio será una herramienta C#/.NET que genere código para la solución actual. No generará clases de acceso a datos con SQL concatenado ni mantendrá conexiones compartidas. EF Core administra las consultas, parámetros y ciclo de vida de `DbContext`; las operaciones pasan por contratos y casos de uso.

Para cada tabla seleccionada, el flujo previsto es:

1. Leer columnas, tipos, nulabilidad, longitudes, claves e índices desde `INFORMATION_SCHEMA` usando una conexión de solo lectura.
2. Mostrar el mapeo antes de escribir archivos: tabla y esquema, nombre de clase, tipo C#, nulabilidad y clave primaria.
3. Generar una entidad Domain con constructores controlados, propiedades privadas y reglas explícitas. Las tablas multiempresa extienden `TenantEntity` y no aceptan un `TenantId` elegido por el navegador.
4. Generar DTOs/contratos en Application y configuración de EF Core en Infrastructure. Ninguna entidad persistida se expone directamente en la API.
5. Crear una guía de integración que señale los cambios deliberadamente manuales: DbSet, registro de servicios, autorización, filtros por tenant y migración.
6. Escribir archivos nuevos sin sobrescribir código existente. Una regeneración requiere comparación y revisión explícitas.

## Reglas para el código generado

- Identificadores y tipos válidos de C#; nombres SQL se escapan y nunca se interpolan como valores de consulta.
- Tipos de MySQL admitidos se mapean explícitamente; tipos desconocidos detienen la generación con un error claro.
- `decimal` conserva precisión y escala del esquema; columnas nullable producen tipos nullable.
- Claves primarias compuestas, vistas y tablas sin clave se detectan antes de generar y no se representan falsamente como una entidad editable.
- El código generado no inventa reglas de negocio, borrado lógico, permisos, índices únicos de tenant ni comportamiento fiscal. Esos requisitos necesitan decisión del módulo.
- Los secretos de conexión no se guardan en el proyecto ni en los archivos generados.

## Estado

`tools/New-OjunnyFeature.ps1` es hoy un scaffold inicial de archivos para módulos; no inspecciona tablas y no es todavía el generador de clases descrito aquí. La herramienta de clases queda pendiente hasta implementar la lectura del esquema MySQL, el preview y la generación no destructiva conforme a este documento.
