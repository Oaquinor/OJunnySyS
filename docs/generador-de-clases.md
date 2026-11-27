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

## Uso implementado

La herramienta está en `tools/OJunnySyS.ClassGenerator` como utilidad .NET 10 incluida en la solución. Utiliza una cuenta de base de datos separada y de solo lectura mediante la variable `OJUNNYSYS_GENERATOR_CONNECTION`; no guarda secretos ni necesita usar la cuenta de escritura de la aplicación.

```powershell
$env:OJUNNYSYS_GENERATOR_CONNECTION = 'Server=127.0.0.1;Database=mi_esquema;User ID=generador_lectura;Password=...;'
dotnet run --project tools/OJunnySyS.ClassGenerator -- tables mi_esquema
dotnet run --project tools/OJunnySyS.ClassGenerator -- preview mi_esquema Customers
dotnet run --project tools/OJunnySyS.ClassGenerator -- generate mi_esquema Customers --output ./.generated
```

La vista previa enumera columnas, tipo MySQL, nulabilidad, clave primaria, índices y claves foráneas, y muestra los archivos completos por capas. La salida contiene Domain, DTO de Application, configuración EF Core de Infrastructure y una guía de integración. Los tipos desconocidos, vistas, tablas sin clave y decimales que excedan el rango fiable de .NET se rechazan con un mensaje. El generador detecta colisiones de nombres de propiedad y protege las rutas de salida. Cada archivo se crea con semántica `CreateNew`; no reemplaza código y limpia los archivos que alcanzó a crear si una escritura falla.

El mapeo de una columna `TenantId` por sí solo no hace una tabla segura para multiempresa. La guía exige agregar aislamiento tenant-scoped, relaciones compuestas, autorización, validaciones y reglas de negocio manualmente antes de integrar el código. La configuración generada registra nombres SQL originales, precisión decimal, longitudes e índices; las relaciones detectadas quedan en la vista previa y se modelan conscientemente durante la integración.

`tools/New-OjunnyFeature.ps1` sigue siendo el scaffold de archivos para módulos nuevos; este generador tiene un objetivo distinto: inspeccionar el esquema MySQL y producir una primera versión revisable de clases alineadas con las capas del sistema.
