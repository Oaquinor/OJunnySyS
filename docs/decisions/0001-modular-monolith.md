# ADR 0001: monolito modular web

- Estado: aceptada para la primera versión
- Fecha: 2026-11-26

## Contexto

CRM y POS comparten clientes, catálogo y ventas. El sistema anterior combina una app Windows/.NET Framework con APIs y servicios de varias generaciones, elevando los costos de compilación y de mantener estado coherente entre procesos. No se ha definido una necesidad para escalar módulos por separado.

## Decisión

Crear un monolito modular en .NET 10 con Blazor Server para pantallas, API HTTP versionada, capas Domain/Application/Infrastructure/Web y MySQL 8. Confirmar venta e inventario en una única transacción de base de datos.

## Consecuencias

- Un despliegue conserva una transacción sencilla y contratos internos reutilizables.
- Los módulos permanecen separables por dependencias y límites.
- Convenciones y dependencias estrictas evitan volver a mezclar UI, persistencia y reglas.
- Blazor Server requiere proteger circuitos y planificar su estado en despliegues con varias instancias.
