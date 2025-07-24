# 🤖 Chatbot GOMARCO - Asistente Empresarial Inteligente

## 🌟 Características Principales

### 📄 Análisis Avanzado de Documentos con Claude 4
- **Análisis Profundo con IA**: Integración con Claude 4 Sonnet para análisis exhaustivo del contenido interno
- **Claude Vision**: Análisis completo de imágenes, extracción de texto, tablas y datos estructurados
- **PDFs**: Análisis página por página con resúmenes ejecutivos y extracción de datos clave
- **Word/Excel**: Identificación de estructura, tablas, tendencias y análisis estadístico
- **Extracción Inteligente**: No solo metadatos, sino comprensión completa del contenido

### 🔐 Seguridad y Privacidad
- **100% Local**: Todos los archivos se procesan y almacenan localmente
- **Encriptación AES**: Datos sensibles protegidos con encriptación de grado militar
- **Sin Datos en la Nube**: Tu información nunca sale de tu equipo (excepto al usar Claude API opcionalmente)

### 💼 Optimizado para Empresas
- **Multi-formato**: Soporta PDF, Word, Excel, PowerPoint, imágenes y más
- **Búsqueda Inteligente**: Encuentra información específica en todos tus documentos
- **Historial Completo**: Guarda y recupera conversaciones anteriores
- **Interfaz Profesional**: Diseño moderno con Material Design

### 🚀 Tecnología de Vanguardia
- **Claude 4 API**: El modelo más avanzado de Anthropic para análisis de contenido
- **OCR Avanzado**: Extracción de texto de imágenes y documentos escaneados
- **Procesamiento Natural del Lenguaje**: Comprende el contexto y la intención
- **Análisis de Metadatos**: Extrae información técnica y estructural

## 📋 Requisitos del Sistema

### Requisitos Mínimos
- **Sistema Operativo**: Windows 10/11 (64-bit)
- **Framework**: .NET 8.0 Runtime
- **RAM**: 4 GB mínimo (8 GB recomendado)
- **Almacenamiento**: 500 MB para la aplicación + espacio para archivos
- **Procesador**: Intel Core i3 o equivalente

## 🚀 Instalación

### Opción 1: Instalador Automático
1. Descarga el instalador desde la carpeta `Instalacion/`
2. Ejecuta `InstalarChatbotGomarco.bat` como administrador
3. Sigue las instrucciones en pantalla

### Opción 2: Instalación Manual
1. Instala .NET 8.0 Runtime desde [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Copia la carpeta `App/` a `C:\Program Files\GOMARCO\ChatbotGomarco\`
3. Crea un acceso directo del ejecutable en el escritorio

## ⚙️ Configuración

### Configuración de Claude 4 API (Opcional)
Para habilitar el análisis avanzado con IA:

1. **Obtener API Key**:
   - Visita [Anthropic Console](https://console.anthropic.com/api-keys)
   - Crea una cuenta y genera una API Key
   - Guarda la clave de forma segura

2. **Configurar en la Aplicación**:
   - Abre el Chatbot GOMARCO
   - Haz clic en el botón "🤖 Configurar IA"
   - Ingresa tu API Key cuando se solicite
   - La clave se mantendrá solo durante la sesión actual

### Primera Ejecución
1. Al iniciar por primera vez, se creará la estructura de carpetas necesaria
2. La base de datos SQLite se inicializará automáticamente
3. Los archivos de configuración se generarán en `%APPDATA%\GOMARCO\ChatbotGomarco\`

## 📖 Uso de la Aplicación

### Análisis de Documentos
1. **Cargar Archivos**:
   - Haz clic en "📎 Cargar archivos" o arrastra y suelta
   - Formatos soportados: PDF, Word, Excel, PowerPoint, imágenes, texto

2. **Análisis con Claude 4** (si está configurado):
   - Los documentos se analizarán automáticamente al cargarlos
   - PDFs: Análisis página por página con resúmenes
   - Imágenes: Claude Vision extrae TODO el contenido visible
   - Excel: Análisis estadístico y detección de tendencias

3. **Hacer Preguntas**:
   - Escribe tu pregunta en el campo de texto
   - El chatbot analizará el contenido de los archivos cargados
   - Recibirás respuestas contextuales basadas en el contenido

### Características Avanzadas
- **Búsqueda en Documentos**: Usa comandos como "busca información sobre..."
- **Análisis Comparativo**: "Compara estos documentos..."
- **Extracción de Datos**: "Extrae todas las fechas/números/nombres..."
- **Resúmenes**: "Resume este documento en 3 puntos"

## 🔒 Seguridad

- Todos los archivos se encriptan con AES-256
- Las claves de encriptación son únicas por máquina
- Los archivos temporales se eliminan automáticamente
- La API Key de Claude no se almacena permanentemente

## 📞 Soporte

Para soporte técnico o preguntas:
- **Email**: soporte@gomarco.com
- **Documentación Técnica**: Ver `README_TECNICO.md`

---

<div align="center">
  <strong>Desarrollado con ❤️ para GOMARCO</strong><br>
  <em>Versión 1.0.0 con Claude 4 Integration</em>
</div>
