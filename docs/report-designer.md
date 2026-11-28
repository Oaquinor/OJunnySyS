# Diseñador de documentos y reportes

La experiencia del archivo `Diseñador de Reportes.zip` se integra como una página Blazor nativa en `/app/reports/designer`. El ZIP es una referencia de interfaz React con datos de muestra y almacenamiento del navegador; no se publica su prototipo ni se agrega React al runtime de OJunnySyS.

## Capacidades integradas

- Centro de reportes con búsqueda, filtro de favoritos, vistas previas y apertura de plantillas guardadas.
- Diseños independientes por empresa, almacenados en MySQL mediante `ReportTemplate`, con revisiones y control de concurrencia.
- Edición del nombre y tipo de documento; formatos A4, Carta, Legal y térmico de 80 mm; orientación y márgenes.
- Componentes de texto, imagen por URL, logotipo/figura, tabla de productos, línea, gráfica de ventas, firma, QR, código de barras, numeración y marca de agua.
- Inserción de campos dinámicos de empresa y reporte, edición de ancho/tamaño/color, orden por arrastre, reordenamiento, duplicación, bloqueo, deshacer y rehacer.
- Vista previa, impresión del documento y exportación CSV de los totales diarios que entrega el reporte de ventas real.
- Datos de empresa, catálogo y ventas cargados desde servicios del tenant autenticado. La página requiere rol de Administrador o Encargado.

La publicación del prototipo React de origen era únicamente local y no compartía permisos ni estado. En OJunnySyS, guardar persiste una plantilla versionada en la empresa autenticada; la impresión y la exportación operan sobre los datos disponibles del sistema.
