# Diseñador de documentos y reportes

El diseñador del archivo `Diseñador de Reportes.zip` se adapta como experiencia Blazor nativa en `/app/reports/designer`. Se conservan el editor por paneles, el centro de reportes, el catálogo de componentes, la búsqueda, las pestañas de componentes/campos/capas/estilos, la configuración de página, las guías, el zoom y las propiedades contextuales. No se publica el runtime React ni sus datos simulados/localStorage.

## Capacidades integradas

- Centro de reportes con búsqueda, filtro de favoritos, vistas previas y apertura de plantillas guardadas.
- Diseños independientes por empresa, almacenados en MySQL mediante `ReportTemplate`, con revisiones y control de concurrencia.
- Edición del nombre y tipo de documento; formatos A4, Carta, Legal y térmico de 80 mm; orientación y márgenes.
- Componentes de texto, imagen por URL, logotipo/figura, tabla de productos, línea, gráfica de ventas, firma, QR, código de barras, numeración y marca de agua.
- Espacio de trabajo con búsqueda de herramientas, panel de capas seleccionables, temas de marca, campos dinámicos conectados, zoom y guías visuales; el inspector separa propiedades de diseño, fuentes de datos y ajustes de papel.
- Inserción de campos dinámicos de empresa y reporte, edición de ancho/tamaño/color, orden por arrastre, reordenamiento, duplicación, bloqueo, deshacer y rehacer.
- Vista previa, impresión del documento y exportación CSV de los totales diarios que entrega el reporte de ventas real.
- Datos de empresa, catálogo y ventas cargados desde servicios del tenant autenticado. La página requiere rol de Administrador o Encargado.

La publicación del prototipo React de origen era únicamente local y no compartía permisos ni estado. En OJunnySyS, guardar persiste una plantilla versionada en la empresa autenticada; los campos de empresa, catálogo y totales de venta se resuelven desde servicios del sistema. La impresión y exportación operan sobre los datos disponibles del sistema.
