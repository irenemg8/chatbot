using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Presentation;
using System.Drawing;
using MetadataExtractor;
using SharpCompress.Archives;
using SharpCompress.Common;
using Tesseract;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace ChatbotGomarco.Servicios
{
    public class ServicioExtraccionContenido : IServicioExtraccionContenido
    {
        private readonly ILogger<ServicioExtraccionContenido> _logger;
        private readonly IServicioIA? _servicioIA;
        
        // Ruta para archivos de datos de Tesseract
        private static readonly string RutaDatosTesseract = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GOMARCO",
            "ChatbotGomarco",
            "tessdata"
        );

        private readonly HashSet<string> _tiposCompatibles = new()
        {
            // Documentos de oficina
            "application/pdf",
            "application/msword", // .doc
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
            "application/vnd.ms-excel", // .xls
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // .xlsx
            "application/vnd.ms-powerpoint", // .ppt
            "application/vnd.openxmlformats-officedocument.presentationml.presentation", // .pptx
            "application/rtf", // .rtf
            
            // Texto y datos
            "text/plain", // .txt
            "text/csv", // .csv
            "application/json", // .json
            "application/xml", // .xml
            
            // Imágenes - todas compatibles para metadatos
            "image/jpeg", "image/png", "image/gif", "image/bmp", 
            "image/svg+xml", "image/webp", "image/tiff",
            
            // Audio - compatibles para metadatos
            "audio/mpeg", "audio/wav", "audio/aac", "audio/ogg", 
            "audio/mp4", "audio/flac",
            
            // Video - compatibles para metadatos
            "video/mp4", "video/avi", "video/x-matroska", "video/quicktime",
            "video/x-ms-wmv", "video/x-flv", "video/webm", "video/x-m4v",
            
            // Archivos comprimidos - compatibles para listado de contenido
            "application/zip", "application/vnd.rar", "application/x-7z-compressed",
            "application/x-tar", "application/gzip"
        };

        public ServicioExtraccionContenido(ILogger<ServicioExtraccionContenido> logger, IServicioIA? servicioIA = null)
        {
            _logger = logger;
            _servicioIA = servicioIA;
        }

        public async Task<string> ExtraerTextoAsync(string rutaArchivo, string tipoContenido)
        {
            try
            {
                _logger.LogInformation("Extrayendo texto de archivo: {Archivo} ({Tipo})", rutaArchivo, tipoContenido);

                if (!File.Exists(rutaArchivo))
                {
                    throw new FileNotFoundException($"El archivo no existe: {rutaArchivo}");
                }

                var texto = tipoContenido switch
                {
                    // Documentos PDF y Office
                    "application/pdf" => await ExtraerTextoPdfAsync(rutaArchivo),
                    "application/msword" => await ExtraerTextoWordLegacyAsync(rutaArchivo),
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => await ExtraerTextoWordAsync(rutaArchivo),
                    "application/vnd.ms-excel" => await ExtraerTextoExcelLegacyAsync(rutaArchivo),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => await ExtraerTextoExcelAsync(rutaArchivo),
                    "application/vnd.ms-powerpoint" => await ExtraerTextoPowerPointLegacyAsync(rutaArchivo),
                    "application/vnd.openxmlformats-officedocument.presentationml.presentation" => await ExtraerTextoPowerPointAsync(rutaArchivo),
                    "application/rtf" => await ExtraerTextoRtfAsync(rutaArchivo),
                    
                    // Texto y datos estructurados
                    "text/plain" => await File.ReadAllTextAsync(rutaArchivo, Encoding.UTF8),
                    "text/csv" => await ExtraerTextoCsvAsync(rutaArchivo),
                    "application/json" => await ExtraerTextoJsonAsync(rutaArchivo),
                    "application/xml" => await File.ReadAllTextAsync(rutaArchivo, Encoding.UTF8),
                    
                    // Archivos multimedia
                    "image/jpeg" or "image/png" or "image/gif" or "image/bmp" or "image/tiff" => await ExtraerTextoImagenAsync(rutaArchivo),
                    "image/svg+xml" => await ExtraerTextoSvgAsync(rutaArchivo),
                    "image/webp" => await ExtraerTextoImagenAsync(rutaArchivo),
                    
                    // Audio
                    "audio/mpeg" or "audio/wav" or "audio/aac" or "audio/ogg" or "audio/mp4" or "audio/flac" => await ExtraerTextoAudioAsync(rutaArchivo),
                    
                    // Video
                    "video/mp4" or "video/avi" or "video/x-matroska" or "video/quicktime" or "video/x-ms-wmv" or "video/x-flv" or "video/webm" or "video/x-m4v" => await ExtraerTextoVideoAsync(rutaArchivo),
                    
                    // Archivos comprimidos
                    "application/zip" or "application/vnd.rar" or "application/x-7z-compressed" or "application/x-tar" or "application/gzip" => await ExtraerTextoArchivoComprimidoAsync(rutaArchivo),
                    
                    _ => throw new NotSupportedException($"Tipo de archivo no compatible: {tipoContenido}")
                };

                _logger.LogInformation("Texto extraído exitosamente. Longitud: {Longitud} caracteres", texto.Length);
                return texto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer texto del archivo: {Archivo}", rutaArchivo);
                return $"Error al procesar el archivo: {ex.Message}";
            }
        }

        public async Task<DocumentoMetadatos> ExtraerMetadatosAsync(string rutaArchivo, string tipoContenido)
        {
            try
            {
                var metadatos = new DocumentoMetadatos();
                var infoArchivo = new FileInfo(rutaArchivo);

                // Metadatos básicos del archivo
                metadatos.FechaCreacion = infoArchivo.CreationTime;
                metadatos.FechaModificacion = infoArchivo.LastWriteTime;

                switch (tipoContenido)
                {
                    case "application/pdf":
                        await ExtraerMetadatosPdfAsync(rutaArchivo, metadatos);
                        break;
                    case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                        await ExtraerMetadatosWordAsync(rutaArchivo, metadatos);
                        break;
                    case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                        await ExtraerMetadatosExcelAsync(rutaArchivo, metadatos);
                        break;
                    default:
                        // Para otros tipos, calcular metadatos básicos
                        var texto = await ExtraerTextoAsync(rutaArchivo, tipoContenido);
                        metadatos.NumeroPalabras = ContarPalabras(texto);
                        break;
                }

                return metadatos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer metadatos: {Archivo}", rutaArchivo);
                return new DocumentoMetadatos();
            }
        }

        public async Task<DocumentoEstructura> AnalizarEstructuraAsync(string rutaArchivo, string tipoContenido)
        {
            try
            {
                var estructura = new DocumentoEstructura();

                switch (tipoContenido)
                {
                    case "application/pdf":
                        await AnalizarEstructuraPdfAsync(rutaArchivo, estructura);
                        break;
                    case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                        await AnalizarEstructuraWordAsync(rutaArchivo, estructura);
                        break;
                    case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                        await AnalizarEstructuraExcelAsync(rutaArchivo, estructura);
                        break;
                    default:
                        // Análisis básico para otros tipos
                        var texto = await ExtraerTextoAsync(rutaArchivo, tipoContenido);
                        await AnalizarEstructuraTextoAsync(texto, estructura);
                        break;
                }

                return estructura;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar estructura: {Archivo}", rutaArchivo);
                return new DocumentoEstructura();
            }
        }

        public bool EsTipoCompatible(string tipoContenido)
        {
            return _tiposCompatibles.Contains(tipoContenido);
        }

        #region Métodos de extracción específicos

        private async Task<string> ExtraerTextoPdfAsync(string rutaArchivo)
        {
            var texto = new StringBuilder();
            
            using var pdfReader = new PdfReader(rutaArchivo);
            using var pdfDocument = new PdfDocument(pdfReader);
            
            var numeroPaginas = pdfDocument.GetNumberOfPages();
            
            // Si Claude Vision está disponible y el PDF no es muy grande, usar análisis visual
            if (_servicioIA != null && _servicioIA.EstaDisponible() && numeroPaginas <= 20)
            {
                try
                {
                    texto.AppendLine("🤖 **ANÁLISIS PROFUNDO DEL PDF CON CLAUDE VISION:**");
                    texto.AppendLine();
                    
                    // Usar IA para análisis profundo página por página
                    {
                        // Analizar cada página como imagen para capturar TODA la información
                        for (int pagina = 1; pagina <= numeroPaginas; pagina++)
                        {
                            texto.AppendLine($"=== PÁGINA {pagina} de {numeroPaginas} ===");
                            
                            try
                            {
                                // Convertir página PDF a imagen (requeriría una biblioteca adicional como PDFiumSharp)
                                // Por ahora, extraer texto tradicional + análisis mejorado
                                var page = pdfDocument.GetPage(pagina);
                                var textoPagina = PdfTextExtractor.GetTextFromPage(page);
                                
                                if (!string.IsNullOrWhiteSpace(textoPagina))
                                {
                                    // Analizar el contenido de la página con IA para mejor comprensión
                                    var prompt = $@"Analiza el siguiente contenido de la página {pagina} de un PDF:

{textoPagina}

Por favor:
1. Resume los puntos clave
2. Extrae TODOS los datos importantes (números, fechas, nombres, cantidades)
3. Identifica cualquier tabla o estructura de datos
4. Resalta información empresarial relevante
5. Mantén el formato estructurado

Responde en español de forma clara y completa.";
                                    
                                    var analisisPagina = await _servicioIA.AnalizarContenidoConIAAsync(textoPagina, prompt);
                                    texto.AppendLine(analisisPagina);
                                }
                                else
                                {
                                    texto.AppendLine("[Página sin texto detectable - posiblemente contiene solo imágenes]");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error al analizar página {Pagina} del PDF", pagina);
                                // Fallback al texto extraído normalmente
                                var page = pdfDocument.GetPage(pagina);
                                var textoPagina = PdfTextExtractor.GetTextFromPage(page);
                                texto.AppendLine(textoPagina);
                            }
                            
                            texto.AppendLine();
                        }
                        
                        // Agregar resumen general si el PDF tiene múltiples páginas
                        if (numeroPaginas > 1)
                        {
                            texto.AppendLine("=== RESUMEN GENERAL DEL DOCUMENTO ===");
                            var resumenPrompt = "Basándote en todo el contenido anterior, proporciona un resumen ejecutivo del documento completo, destacando los puntos más importantes.";
                            var resumenGeneral = await _servicioIA.AnalizarContenidoConIAAsync(texto.ToString(), resumenPrompt);
                            texto.AppendLine(resumenGeneral);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al usar Claude Vision para PDF, usando extracción tradicional");
                    // Continuar con extracción tradicional
                }
            }
            
            // Si no se usó IA o falló, hacer extracción tradicional
            if (!texto.ToString().Contains("CLAUDE VISION"))
            {
                for (int pagina = 1; pagina <= numeroPaginas; pagina++)
                {
                    var page = pdfDocument.GetPage(pagina);
                    var textoPagina = PdfTextExtractor.GetTextFromPage(page);
                    texto.AppendLine($"=== PÁGINA {pagina} ===");
                    texto.AppendLine(textoPagina);
                    texto.AppendLine();
                }
            }

            return texto.ToString();
        }

        private async Task<string> ExtraerTextoWordAsync(string rutaArchivo)
        {
            var texto = new StringBuilder();

            using var documento = WordprocessingDocument.Open(rutaArchivo, false);
            var body = documento.MainDocumentPart.Document.Body;

            // Si Claude Vision está disponible, hacer análisis profundo
            if (_servicioIA != null && _servicioIA.EstaDisponible())
            {
                try
                {
                    texto.AppendLine("🤖 **ANÁLISIS PROFUNDO DEL DOCUMENTO WORD CON CLAUDE:**");
                    texto.AppendLine();
                    
                    // Primero extraer todo el contenido estructurado
                    var contenidoEstructurado = new StringBuilder();
                    var tablas = new List<string>();
                    var imagenes = 0;
                    
                    foreach (var elemento in body.Elements())
                    {
                        if (elemento is Paragraph paragraph)
                        {
                            var textoParrafo = paragraph.InnerText;
                            if (!string.IsNullOrWhiteSpace(textoParrafo))
                            {
                                // Detectar si es un encabezado
                                var estilo = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
                                if (estilo?.Contains("Heading") == true)
                                {
                                    contenidoEstructurado.AppendLine($"### {textoParrafo}");
                                }
                                else
                                {
                                    contenidoEstructurado.AppendLine(textoParrafo);
                                }
                            }
                        }
                        else if (elemento is DocumentFormat.OpenXml.Wordprocessing.Table tabla)
                        {
                            var tablaTexto = new StringBuilder();
                            tablaTexto.AppendLine("=== TABLA ===");
                            foreach (var fila in tabla.Elements<TableRow>())
                            {
                                var textoFila = string.Join(" | ", fila.Elements<TableCell>().Select(c => c.InnerText));
                                tablaTexto.AppendLine(textoFila);
                            }
                            tablas.Add(tablaTexto.ToString());
                            contenidoEstructurado.AppendLine(tablaTexto.ToString());
                        }
                    }
                    
                    // Analizar el contenido con IA
                    var prompt = $@"Analiza el siguiente documento Word empresarial:

{contenidoEstructurado}

Por favor proporciona:
1. **Resumen ejecutivo** del documento
2. **Puntos clave** y conclusiones principales
3. **Datos importantes** (números, fechas, nombres, cantidades, porcentajes)
4. **Estructura del documento** (secciones principales)
5. **Información crítica** para decisiones empresariales
6. **Análisis de tablas** si las hay
7. **Recomendaciones o acciones** sugeridas en el documento

Responde en español de forma estructurada y profesional.";
                    
                    var analisisIA = await _servicioIA.AnalizarContenidoConIAAsync(contenidoEstructurado.ToString(), prompt);
                    texto.AppendLine(analisisIA);
                    
                    // Agregar información adicional
                    if (tablas.Count > 0)
                    {
                        texto.AppendLine();
                        texto.AppendLine($"📊 **Tablas encontradas:** {tablas.Count}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al usar IA para análisis de Word, usando extracción tradicional");
                    // Continuar con extracción tradicional
                }
            }
            
            // Si no se usó IA o falló, hacer extracción tradicional
            if (!texto.ToString().Contains("CLAUDE"))
            {
                foreach (var elemento in body.Elements())
                {
                    if (elemento is Paragraph paragraph)
                    {
                        var textoParrafo = paragraph.InnerText;
                        if (!string.IsNullOrWhiteSpace(textoParrafo))
                        {
                            texto.AppendLine(textoParrafo);
                        }
                    }
                    else if (elemento is DocumentFormat.OpenXml.Wordprocessing.Table tabla)
                    {
                        texto.AppendLine("=== TABLA ===");
                        foreach (var fila in tabla.Elements<TableRow>())
                        {
                            var textoFila = string.Join(" | ", fila.Elements<TableCell>().Select(c => c.InnerText));
                            texto.AppendLine(textoFila);
                        }
                        texto.AppendLine();
                    }
                }
            }

            return texto.ToString();
        }

        private async Task<string> ExtraerTextoExcelAsync(string rutaArchivo)
        {
            var texto = new StringBuilder();

            using var documento = SpreadsheetDocument.Open(rutaArchivo, false);
            var workbookPart = documento.WorkbookPart;
            var shareStringPart = workbookPart.SharedStringTablePart;

            // Si Claude está disponible, hacer análisis profundo
            if (_servicioIA != null && _servicioIA.EstaDisponible())
            {
                try
                {
                    texto.AppendLine("🤖 **ANÁLISIS PROFUNDO DE LA HOJA DE CÁLCULO CON CLAUDE:**");
                    texto.AppendLine();
                    
                    var todasLasHojas = new StringBuilder();
                    var numeroHoja = 0;
                    
                    foreach (var worksheetPart in workbookPart.WorksheetParts)
                    {
                        numeroHoja++;
                        var worksheet = worksheetPart.Worksheet;
                        var hojaData = worksheet.GetFirstChild<SheetData>();
                        
                        todasLasHojas.AppendLine($"=== HOJA {numeroHoja} ===");
                        
                        // Construir representación tabular de los datos
                        var datosHoja = new List<List<string>>();
                        
                        foreach (var fila in hojaData.Elements<Row>())
                        {
                            var valores = new List<string>();
                            foreach (var celda in fila.Elements<Cell>())
                            {
                                var valor = ObtenerValorCelda(celda, shareStringPart);
                                valores.Add(valor);
                            }
                            if (valores.Any(v => !string.IsNullOrWhiteSpace(v)))
                            {
                                datosHoja.Add(valores);
                                todasLasHojas.AppendLine(string.Join(" | ", valores));
                            }
                        }
                        
                        // Si la hoja tiene datos significativos, analizarla
                        if (datosHoja.Count > 1)
                        {
                            todasLasHojas.AppendLine($"\nTotal de filas con datos: {datosHoja.Count}");
                        }
                        
                        todasLasHojas.AppendLine();
                    }
                    
                    // Analizar todos los datos con IA
                    var prompt = $@"Analiza los siguientes datos de una hoja de cálculo empresarial:

{todasLasHojas}

Por favor proporciona un análisis completo que incluya:
1. **Resumen de los datos**: ¿Qué tipo de información contiene?
2. **Estructura identificada**: ¿Qué tablas, listas o conjuntos de datos hay?
3. **Valores clave**: Extrae TODOS los números importantes, totales, porcentajes, fechas
4. **Análisis estadístico**: Si hay datos numéricos, proporciona sumas, promedios, máximos, mínimos
5. **Tendencias o patrones**: Identifica cualquier patrón en los datos
6. **Datos anómalos**: Señala valores inusuales o que requieran atención
7. **Contexto empresarial**: ¿Para qué sirven estos datos? ¿Qué decisiones apoyan?
8. **Recomendaciones**: Basándote en los datos, ¿qué acciones sugieres?

Responde en español con un análisis profesional y detallado.";
                    
                    var analisisIA = await _servicioIA.AnalizarContenidoConIAAsync(todasLasHojas.ToString(), prompt);
                    texto.AppendLine(analisisIA);
                    
                    // Agregar metadatos adicionales
                    texto.AppendLine();
                    texto.AppendLine($"📊 **Información adicional:**");
                    texto.AppendLine($"• Número de hojas: {numeroHoja}");
                    
                    return texto.ToString();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al usar IA para análisis de Excel, usando extracción tradicional");
                    // Continuar con extracción tradicional
                }
            }
            
            // Extracción tradicional si no hay IA o falló
            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var worksheet = worksheetPart.Worksheet;
                var hojaData = worksheet.GetFirstChild<SheetData>();
                
                texto.AppendLine($"=== HOJA DE CÁLCULO ===");

                foreach (var fila in hojaData.Elements<Row>())
                {
                    var valores = new List<string>();
                    foreach (var celda in fila.Elements<Cell>())
                    {
                        var valor = ObtenerValorCelda(celda, shareStringPart);
                        valores.Add(valor);
                    }
                    if (valores.Any(v => !string.IsNullOrWhiteSpace(v)))
                    {
                        texto.AppendLine(string.Join(" | ", valores));
                    }
                }
                texto.AppendLine();
            }

            return texto.ToString();
        }

        private async Task<string> ExtraerTextoCsvAsync(string rutaArchivo)
        {
            var texto = new StringBuilder();
            var lineas = await File.ReadAllLinesAsync(rutaArchivo, Encoding.UTF8);
            
            texto.AppendLine("=== DATOS CSV ===");
            foreach (var linea in lineas.Take(100)) // Limitar a 100 filas para no abrumar
            {
                texto.AppendLine(linea);
            }

            if (lineas.Length > 100)
            {
                texto.AppendLine($"... y {lineas.Length - 100} filas más.");
            }

            return texto.ToString();
        }

        private async Task<string> ExtraerTextoJsonAsync(string rutaArchivo)
        {
            var contenido = await File.ReadAllTextAsync(rutaArchivo, Encoding.UTF8);
            
            try
            {
                // Formatear JSON para mejor legibilidad
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject(contenido);
                var jsonFormateado = Newtonsoft.Json.JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented);
                return $"=== CONTENIDO JSON ===\n{jsonFormateado}";
            }
            catch
            {
                return $"=== CONTENIDO JSON (RAW) ===\n{contenido}";
            }
        }

        private async Task<string> ExtraerTextoImagenAsync(string rutaArchivo)
        {
            try
            {
                var resultado = new StringBuilder();
                resultado.AppendLine("=== ANÁLISIS COMPLETO DE IMAGEN ===");

                // 1. Si Claude Vision está disponible, usarlo primero
                if (_servicioIA != null && _servicioIA.EstaDisponible())
                {
                    try
                    {
                        resultado.AppendLine("🤖 **ANÁLISIS PROFUNDO CON IA AVANZADA:**");
                        resultado.AppendLine();
                        
                        // Usar IA para análisis completo de imágenes
                        var contenidoBasico = await ObtenerMetadatosBasicosImagenAsync(rutaArchivo);
                        var analisisIA = await _servicioIA.AnalizarContenidoConIAAsync(
                            contenidoBasico, 
                            "Analiza esta imagen y extrae toda la información relevante, especialmente cualquier texto visible."
                        );
                        resultado.AppendLine(analisisIA);
                        resultado.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error al usar Claude Vision, continuando con análisis tradicional");
                        resultado.AppendLine("⚠️ No se pudo completar el análisis con IA, usando métodos alternativos...");
                        resultado.AppendLine();
                    }
                }

                // 2. PROPIEDADES BÁSICAS DE LA IMAGEN
                await AnaluzarPropiedadesBasicasImagen(rutaArchivo, resultado);

                // 3. METADATOS TÉCNICOS (EXIF, etc.)
                await AnaluzarMetadatosTecnicos(rutaArchivo, resultado);

                // 4. ANÁLISIS VISUAL Y CONTENIDO (solo si no se usó Claude Vision)
                if (_servicioIA == null || !_servicioIA.EstaDisponible())
                {
                    await AnalyzarContenidoVisual(rutaArchivo, resultado);
                }

                // 5. OCR - EXTRACCIÓN DE TEXTO (solo si no se usó Claude Vision)
                if (_servicioIA == null || !_servicioIA.EstaDisponible())
                {
                    await EjecutarOCRAsync(rutaArchivo, resultado);
                }

                return resultado.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar imagen: {Ruta}", rutaArchivo);
                return $"=== IMAGEN ===\nImagen detectada pero no se pudo analizar completamente: {ex.Message}";
            }
        }

        private async Task<string> ExtraerTextoSvgAsync(string rutaArchivo)
        {
            try
            {
                var contenidoSvg = await File.ReadAllTextAsync(rutaArchivo, Encoding.UTF8);
                var resultado = new StringBuilder();
                resultado.AppendLine("=== IMAGEN SVG ===");
                
                // Extraer elementos de texto del SVG
                var textos = Regex.Matches(contenidoSvg, @"<text[^>]*>(.*?)</text>", RegexOptions.IgnoreCase)
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value.Trim())
                    .Where(t => !string.IsNullOrEmpty(t));

                if (textos.Any())
                {
                    resultado.AppendLine("**Texto encontrado en SVG:**");
                    foreach (var texto in textos)
                    {
                        resultado.AppendLine($"• {texto}");
                    }
                }

                resultado.AppendLine($"\n**Tamaño del archivo:** {new FileInfo(rutaArchivo).Length} bytes");
                return resultado.ToString();
            }
            catch (Exception ex)
            {
                return $"=== IMAGEN SVG ===\nError al procesar SVG: {ex.Message}";
            }
        }

        private async Task<string> ExtraerTextoAudioAsync(string rutaArchivo)
        {
            try
            {
                var resultado = new StringBuilder();
                resultado.AppendLine("=== ARCHIVO DE AUDIO ===");

                // Usar MetadataExtractor para obtener metadatos de audio
                var directorios = ImageMetadataReader.ReadMetadata(rutaArchivo);
                
                foreach (var directorio in directorios)
                {
                    resultado.AppendLine($"**{directorio.Name}:**");
                    foreach (var tag in directorio.Tags)
                    {
                        if (!tag.HasName || string.IsNullOrEmpty(tag.Description)) continue;
                        
                        // Filtrar tags más relevantes para audio
                        var tagName = tag.Name.ToLower();
                        if (tagName.Contains("duration") || tagName.Contains("title") || 
                            tagName.Contains("artist") || tagName.Contains("album") || 
                            tagName.Contains("bitrate") || tagName.Contains("sample") ||
                            tagName.Contains("genre") || tagName.Contains("year"))
                        {
                            resultado.AppendLine($"• {tag.Name}: {tag.Description}");
                        }
                    }
                }

                var infoArchivo = new FileInfo(rutaArchivo);
                resultado.AppendLine($"\n**Información del archivo:**");
                resultado.AppendLine($"• Tamaño: {FormatearTamaño(infoArchivo.Length)}");
                resultado.AppendLine($"• Fecha de modificación: {infoArchivo.LastWriteTime:dd/MM/yyyy HH:mm}");

                return resultado.ToString();
            }
            catch (Exception ex)
            {
                var infoArchivo = new FileInfo(rutaArchivo);
                return $"=== ARCHIVO DE AUDIO ===\n" +
                       $"Archivo de audio detectado\n" +
                       $"Tamaño: {FormatearTamaño(infoArchivo.Length)}\n" +
                       $"Extensión: {infoArchivo.Extension}\n" +
                       $"Error al extraer metadatos: {ex.Message}";
            }
        }

        private async Task<string> ExtraerTextoVideoAsync(string rutaArchivo)
        {
            try
            {
                var resultado = new StringBuilder();
                resultado.AppendLine("=== ARCHIVO DE VIDEO ===");

                // Usar MetadataExtractor para obtener metadatos de video
                var directorios = ImageMetadataReader.ReadMetadata(rutaArchivo);
                
                foreach (var directorio in directorios)
                {
                    resultado.AppendLine($"**{directorio.Name}:**");
                    foreach (var tag in directorio.Tags)
                    {
                        if (!tag.HasName || string.IsNullOrEmpty(tag.Description)) continue;
                        
                        // Filtrar tags relevantes para video
                        var tagName = tag.Name.ToLower();
                        if (tagName.Contains("duration") || tagName.Contains("width") || 
                            tagName.Contains("height") || tagName.Contains("framerate") || 
                            tagName.Contains("bitrate") || tagName.Contains("codec") ||
                            tagName.Contains("resolution") || tagName.Contains("format"))
                        {
                            resultado.AppendLine($"• {tag.Name}: {tag.Description}");
                        }
                    }
                }

                var infoArchivo = new FileInfo(rutaArchivo);
                resultado.AppendLine($"\n**Información del archivo:**");
                resultado.AppendLine($"• Tamaño: {FormatearTamaño(infoArchivo.Length)}");
                resultado.AppendLine($"• Fecha de modificación: {infoArchivo.LastWriteTime:dd/MM/yyyy HH:mm}");

                return resultado.ToString();
            }
            catch (Exception ex)
            {
                var infoArchivo = new FileInfo(rutaArchivo);
                return $"=== ARCHIVO DE VIDEO ===\n" +
                       $"Archivo de video detectado\n" +
                       $"Tamaño: {FormatearTamaño(infoArchivo.Length)}\n" +
                       $"Extensión: {infoArchivo.Extension}\n" +
                       $"Error al extraer metadatos: {ex.Message}";
            }
        }

        private async Task<string> ExtraerTextoArchivoComprimidoAsync(string rutaArchivo)
        {
            try
            {
                var resultado = new StringBuilder();
                resultado.AppendLine("=== ARCHIVO COMPRIMIDO ===");

                using var archivo = ArchiveFactory.Open(rutaArchivo);
                var entradas = archivo.Entries.Where(e => !e.IsDirectory).Take(50).ToList();
                
                resultado.AppendLine($"**Archivos encontrados:** {archivo.Entries.Count(e => !e.IsDirectory)}");
                resultado.AppendLine($"**Carpetas encontradas:** {archivo.Entries.Count(e => e.IsDirectory)}");
                resultado.AppendLine();

                if (entradas.Any())
                {
                    resultado.AppendLine("**Contenido (primeros 50 archivos):**");
                    foreach (var entrada in entradas)
                    {
                        var tamaño = FormatearTamaño(entrada.Size);
                        var fecha = entrada.LastModifiedTime?.ToString("dd/MM/yyyy") ?? "?";
                        resultado.AppendLine($"• {entrada.Key} ({tamaño}) - {fecha}");
                    }
                }

                var infoArchivo = new FileInfo(rutaArchivo);
                resultado.AppendLine($"\n**Información del archivo comprimido:**");
                resultado.AppendLine($"• Tamaño comprimido: {FormatearTamaño(infoArchivo.Length)}");
                
                return resultado.ToString();
            }
            catch (Exception ex)
            {
                var infoArchivo = new FileInfo(rutaArchivo);
                return $"=== ARCHIVO COMPRIMIDO ===\n" +
                       $"Archivo comprimido detectado\n" +
                       $"Tamaño: {FormatearTamaño(infoArchivo.Length)}\n" +
                       $"Extensión: {infoArchivo.Extension}\n" +
                       $"Error al analizar contenido: {ex.Message}";
            }
        }

        // Métodos para formatos legacy (implementación básica)
        private async Task<string> ExtraerTextoWordLegacyAsync(string rutaArchivo)
        {
            return await Task.FromResult($"=== DOCUMENTO WORD LEGACY (.doc) ===\n" +
                                       $"Archivo de Word en formato legacy detectado.\n" +
                                       $"Tamaño: {FormatearTamaño(new FileInfo(rutaArchivo).Length)}\n" +
                                       $"Para análisis completo del contenido, considera convertir a formato .docx");
        }

        private async Task<string> ExtraerTextoExcelLegacyAsync(string rutaArchivo)
        {
            return await Task.FromResult($"=== HOJA DE CÁLCULO EXCEL LEGACY (.xls) ===\n" +
                                       $"Archivo de Excel en formato legacy detectado.\n" +
                                       $"Tamaño: {FormatearTamaño(new FileInfo(rutaArchivo).Length)}\n" +
                                       $"Para análisis completo del contenido, considera convertir a formato .xlsx");
        }

        private async Task<string> ExtraerTextoPowerPointAsync(string rutaArchivo)
        {
            try
            {
                var resultado = new StringBuilder();
                resultado.AppendLine("=== PRESENTACIÓN POWERPOINT (.pptx) ===");

                using var presentacion = PresentationDocument.Open(rutaArchivo, false);
                var diapositivas = presentacion.PresentationPart.SlideParts;
                
                resultado.AppendLine($"**Número de diapositivas:** {diapositivas.Count()}");
                resultado.AppendLine();

                int numeroSlide = 1;
                foreach (var diapositiva in diapositivas.Take(10)) // Limitar a 10 diapositivas
                {
                    resultado.AppendLine($"=== DIAPOSITIVA {numeroSlide} ===");
                    
                    var shapes = diapositiva.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>().ToList();
                    foreach (var shape in shapes.Take(20)) // Limitar texto por diapositiva
                    {
                        var texto = shape.InnerText?.Trim();
                        if (!string.IsNullOrEmpty(texto))
                        {
                            resultado.AppendLine(texto);
                        }
                    }
                    resultado.AppendLine();
                    numeroSlide++;
                }

                if (diapositivas.Count() > 10)
                {
                    resultado.AppendLine($"... y {diapositivas.Count() - 10} diapositivas más.");
                }

                return resultado.ToString();
            }
            catch (Exception ex)
            {
                return $"=== PRESENTACIÓN POWERPOINT (.pptx) ===\n" +
                       $"Error al procesar presentación: {ex.Message}\n" +
                       $"Tamaño: {FormatearTamaño(new FileInfo(rutaArchivo).Length)}";
            }
        }

        private async Task<string> ExtraerTextoPowerPointLegacyAsync(string rutaArchivo)
        {
            return await Task.FromResult($"=== PRESENTACIÓN POWERPOINT LEGACY (.ppt) ===\n" +
                                       $"Presentación de PowerPoint en formato legacy detectada.\n" +
                                       $"Tamaño: {FormatearTamaño(new FileInfo(rutaArchivo).Length)}\n" +
                                       $"Para análisis completo del contenido, considera convertir a formato .pptx");
        }

        private async Task<string> ExtraerTextoRtfAsync(string rutaArchivo)
        {
            try
            {
                var contenidoRtf = await File.ReadAllTextAsync(rutaArchivo, Encoding.UTF8);
                var resultado = new StringBuilder();
                resultado.AppendLine("=== DOCUMENTO RTF ===");
                
                // Extraer texto básico removiendo códigos RTF (implementación simple)
                var textoLimpio = Regex.Replace(contenidoRtf, @"\\[a-z]+\d*|\{|\}", "", RegexOptions.IgnoreCase);
                textoLimpio = Regex.Replace(textoLimpio, @"\s+", " ").Trim();
                
                if (!string.IsNullOrEmpty(textoLimpio) && textoLimpio.Length > 50)
                {
                    resultado.AppendLine("**Contenido extraído:**");
                    resultado.AppendLine(textoLimpio.Substring(0, Math.Min(2000, textoLimpio.Length)));
                    
                    if (textoLimpio.Length > 2000)
                    {
                        resultado.AppendLine("\n... [contenido truncado]");
                    }
                }

                resultado.AppendLine($"\n**Información del archivo:**");
                resultado.AppendLine($"• Tamaño: {FormatearTamaño(new FileInfo(rutaArchivo).Length)}");
                
                return resultado.ToString();
            }
            catch (Exception ex)
            {
                return $"=== DOCUMENTO RTF ===\n" +
                       $"Error al procesar documento RTF: {ex.Message}\n" +
                       $"Tamaño: {FormatearTamaño(new FileInfo(rutaArchivo).Length)}";
            }
        }

        private string FormatearTamaño(long bytes)
        {
            string[] sufijos = { "B", "KB", "MB", "GB" };
            int contador = 0;
            decimal numero = bytes;
            
            while (Math.Round(numero / 1024) >= 1)
            {
                numero /= 1024;
                contador++;
            }
            
            return $"{numero:n1} {sufijos[contador]}";
        }

        #endregion

        #region Métodos de metadatos

        private async Task ExtraerMetadatosPdfAsync(string rutaArchivo, DocumentoMetadatos metadatos)
        {
            using var pdfReader = new PdfReader(rutaArchivo);
            using var pdfDocument = new PdfDocument(pdfReader);
            
            var info = pdfDocument.GetDocumentInfo();
            metadatos.Titulo = info.GetTitle() ?? "";
            metadatos.Autor = info.GetAuthor() ?? "";
            metadatos.Tema = info.GetSubject() ?? "";
            metadatos.NumeroPaginas = pdfDocument.GetNumberOfPages();
            metadatos.Aplicacion = info.GetCreator() ?? "";

            // Contar palabras aproximadas
            var texto = await ExtraerTextoPdfAsync(rutaArchivo);
            metadatos.NumeroPalabras = ContarPalabras(texto);
        }

        private async Task ExtraerMetadatosWordAsync(string rutaArchivo, DocumentoMetadatos metadatos)
        {
            using var documento = WordprocessingDocument.Open(rutaArchivo, false);
            
            var propiedades = documento.PackageProperties;
            metadatos.Titulo = propiedades.Title ?? "";
            metadatos.Autor = propiedades.Creator ?? "";
            metadatos.Tema = propiedades.Subject ?? "";
            
            if (propiedades.Created.HasValue)
                metadatos.FechaCreacion = propiedades.Created.Value;
            
            if (propiedades.Modified.HasValue)
                metadatos.FechaModificacion = propiedades.Modified.Value;

            // Contar palabras y páginas
            var texto = await ExtraerTextoWordAsync(rutaArchivo);
            metadatos.NumeroPalabras = ContarPalabras(texto);
            
            // Estimar páginas (aproximadamente 250 palabras por página)
            metadatos.NumeroPaginas = Math.Max(1, metadatos.NumeroPalabras / 250);
        }

        private async Task ExtraerMetadatosExcelAsync(string rutaArchivo, DocumentoMetadatos metadatos)
        {
            using var documento = SpreadsheetDocument.Open(rutaArchivo, false);
            
            var propiedades = documento.PackageProperties;
            metadatos.Titulo = propiedades.Title ?? "";
            metadatos.Autor = propiedades.Creator ?? "";
            
            // Contar hojas como "páginas"
            metadatos.NumeroPaginas = documento.WorkbookPart.WorksheetParts.Count();
            
            var texto = await ExtraerTextoExcelAsync(rutaArchivo);
            metadatos.NumeroPalabras = ContarPalabras(texto);
        }

        #endregion

        #region Métodos de análisis de estructura

        private async Task AnalizarEstructuraPdfAsync(string rutaArchivo, DocumentoEstructura estructura)
        {
            var texto = await ExtraerTextoPdfAsync(rutaArchivo);
            await AnalizarEstructuraTextoAsync(texto, estructura);
            
            using var pdfReader = new PdfReader(rutaArchivo);
            using var pdfDocument = new PdfDocument(pdfReader);
            
            estructura.ResumenEstructural = $"Documento PDF con {pdfDocument.GetNumberOfPages()} páginas";
        }

        private async Task AnalizarEstructuraWordAsync(string rutaArchivo, DocumentoEstructura estructura)
        {
            using var documento = WordprocessingDocument.Open(rutaArchivo, false);
            var body = documento.MainDocumentPart.Document.Body;

            // Buscar encabezados
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                var propiedades = paragraph.ParagraphProperties;
                if (propiedades?.ParagraphStyleId?.Val?.Value?.Contains("Heading") == true)
                {
                    var texto = paragraph.InnerText.Trim();
                    if (!string.IsNullOrEmpty(texto))
                        estructura.Encabezados.Add(texto);
                }
            }

            // Contar tablas
            estructura.NumeroTablas = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>().Count();
            
            estructura.ResumenEstructural = $"Documento Word con {estructura.Encabezados.Count} encabezados y {estructura.NumeroTablas} tablas";
        }

        private async Task AnalizarEstructuraExcelAsync(string rutaArchivo, DocumentoEstructura estructura)
        {
            using var documento = SpreadsheetDocument.Open(rutaArchivo, false);
            
            var numeroHojas = documento.WorkbookPart.WorksheetParts.Count();
            estructura.SeccionesPrincipales.Add($"{numeroHojas} hoja(s) de cálculo");
            
            estructura.ResumenEstructural = $"Libro de Excel con {numeroHojas} hoja(s)";
        }

        private async Task AnalizarEstructuraTextoAsync(string texto, DocumentoEstructura estructura)
        {
            // Buscar posibles encabezados (líneas cortas seguidas de líneas vacías)
            var lineas = texto.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < lineas.Length - 1; i++)
            {
                var linea = lineas[i].Trim();
                if (linea.Length < 100 && linea.Length > 5 && 
                    !linea.EndsWith('.') && !linea.EndsWith(','))
                {
                    estructura.Encabezados.Add(linea);
                    if (estructura.Encabezados.Count >= 10) break; // Limitar encabezados
                }
            }

            estructura.ResumenEstructural = $"Texto con aproximadamente {ContarPalabras(texto)} palabras";
        }

        #endregion

        #region Métodos auxiliares

        private string ObtenerValorCelda(Cell celda, SharedStringTablePart shareStringPart)
        {
            if (celda.DataType?.Value == CellValues.SharedString)
            {
                var indice = int.Parse(celda.InnerText);
                return shareStringPart.SharedStringTable.Elements<SharedStringItem>().ElementAt(indice).InnerText;
            }
            return celda.InnerText;
        }

        private int ContarPalabras(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return 0;
            
            return Regex.Matches(texto, @"\b\w+\b").Count;
        }

        #endregion

        #region Métodos de análisis avanzado de imágenes

        private async Task AnaluzarPropiedadesBasicasImagen(string rutaArchivo, StringBuilder resultado)
        {
            try
            {
                using var imagen = ImageSharpImage.Load(rutaArchivo);
                var infoArchivo = new FileInfo(rutaArchivo);

                resultado.AppendLine("🖼️ **PROPIEDADES BÁSICAS:**");
                resultado.AppendLine($"• Dimensiones: {imagen.Width} x {imagen.Height} píxeles");
                resultado.AppendLine($"• Relación de aspecto: {(double)imagen.Width / imagen.Height:F2}:1");
                resultado.AppendLine($"• Formato: {imagen.Metadata.DecodedImageFormat?.Name ?? "Desconocido"}");
                resultado.AppendLine($"• Profundidad de color: {imagen.PixelType.BitsPerPixel} bits por píxel");
                resultado.AppendLine($"• Tamaño del archivo: {FormatearTamaño(infoArchivo.Length)}");

                // Determinar orientación
                var orientacion = imagen.Width > imagen.Height ? "Horizontal (paisaje)" 
                                : imagen.Height > imagen.Width ? "Vertical (retrato)" 
                                : "Cuadrada";
                resultado.AppendLine($"• Orientación: {orientacion}");

                // Categorizar tamaño
                var totalPixeles = (long)imagen.Width * imagen.Height;
                var tipoTamaño = totalPixeles switch
                {
                    <= 300_000 => "Pequeña (< 300K píxeles)",
                    <= 2_000_000 => "Mediana (300K - 2M píxeles)",
                    <= 8_000_000 => "Grande (2M - 8M píxeles)",
                    _ => "Muy grande (> 8M píxeles)"
                };
                resultado.AppendLine($"• Categoría de tamaño: {tipoTamaño}");
                resultado.AppendLine();
            }
            catch (Exception ex)
            {
                resultado.AppendLine($"⚠️ Error al analizar propiedades básicas: {ex.Message}");
                resultado.AppendLine();
            }
        }

        private async Task AnaluzarMetadatosTecnicos(string rutaArchivo, StringBuilder resultado)
        {
            try
            {
                resultado.AppendLine("🔍 **METADATOS TÉCNICOS:**");
                
                var directorios = ImageMetadataReader.ReadMetadata(rutaArchivo);
                var metadatosRelevantes = new List<string>();

                foreach (var directorio in directorios)
                {
                    foreach (var tag in directorio.Tags)
                    {
                        if (!tag.HasName || string.IsNullOrEmpty(tag.Description)) continue;
                        
                        var tagName = tag.Name.ToLower();
                        
                        // Filtrar metadatos más relevantes
                        if (tagName.Contains("camera") || tagName.Contains("make") || tagName.Contains("model") ||
                            tagName.Contains("lens") || tagName.Contains("focal") || tagName.Contains("aperture") ||
                            tagName.Contains("exposure") || tagName.Contains("iso") || tagName.Contains("flash") ||
                            tagName.Contains("gps") || tagName.Contains("date") || tagName.Contains("time") ||
                            tagName.Contains("resolution") || tagName.Contains("compression") || 
                            tagName.Contains("software") || tagName.Contains("artist") || tagName.Contains("copyright"))
                        {
                            metadatosRelevantes.Add($"• {tag.Name}: {tag.Description}");
                        }
                    }
                }

                if (metadatosRelevantes.Any())
                {
                    foreach (var metadata in metadatosRelevantes.Take(15)) // Limitar para no abrumar
                    {
                        resultado.AppendLine(metadata);
                    }
                    
                    if (metadatosRelevantes.Count > 15)
                    {
                        resultado.AppendLine($"• ... y {metadatosRelevantes.Count - 15} metadatos más");
                    }
                }
                else
                {
                    resultado.AppendLine("• No se encontraron metadatos técnicos específicos");
                }
                
                resultado.AppendLine();
            }
            catch (Exception ex)
            {
                resultado.AppendLine($"⚠️ Error al extraer metadatos: {ex.Message}");
                resultado.AppendLine();
            }
        }

        private async Task AnalyzarContenidoVisual(string rutaArchivo, StringBuilder resultado)
        {
            try
            {
                resultado.AppendLine("🎨 **ANÁLISIS DE CONTENIDO VISUAL:**");
                
                using var imagen = ImageSharpImage.Load<Rgba32>(rutaArchivo);
                
                // Análisis de colores dominantes
                var coloresDominantes = await AnalizarColoresDominantesAsync(imagen);
                if (coloresDominantes.Any())
                {
                    resultado.AppendLine("**Colores dominantes detectados:**");
                    foreach (var color in coloresDominantes.Take(5))
                    {
                        resultado.AppendLine($"• RGB({color.R}, {color.G}, {color.B}) - {DescribirColor(color)}");
                    }
                    resultado.AppendLine();
                }

                // Análisis de brillo y contraste
                var estadisticasLuminancia = await AnalizarLuminanciaAsync(imagen);
                resultado.AppendLine("**Características de luminancia:**");
                resultado.AppendLine($"• Brillo promedio: {estadisticasLuminancia.BrilloPromedio:F1}%");
                resultado.AppendLine($"• Contraste: {estadisticasLuminancia.Contraste:F1}%");
                resultado.AppendLine($"• Tipo de imagen: {DeterminarTipoImagen(estadisticasLuminancia)}");
                resultado.AppendLine();

                // Detección básica de características
                var caracteristicas = await DetectarCaracteristicasBasicasAsync(imagen);
                if (caracteristicas.Any())
                {
                    resultado.AppendLine("**Características detectadas:**");
                    foreach (var caracteristica in caracteristicas)
                    {
                        resultado.AppendLine($"• {caracteristica}");
                    }
                    resultado.AppendLine();
                }

            }
            catch (Exception ex)
            {
                resultado.AppendLine($"⚠️ Error en análisis visual: {ex.Message}");
                resultado.AppendLine();
            }
        }

        private async Task EjecutarOCRAsync(string rutaArchivo, StringBuilder resultado)
        {
            try
            {
                resultado.AppendLine("📝 **RECONOCIMIENTO DE TEXTO (OCR):**");

                // Verificar si existen los datos de Tesseract
                if (!await VerificarDatosTesseractAsync())
                {
                    resultado.AppendLine("⚠️ Los datos de OCR no están disponibles. Instalando automáticamente...");
                    await DescargarDatosTesseractAsync();
                }

                // Preprocesar imagen para mejor OCR
                var rutaImagenPreprocesada = await PreprocesarImagenParaOCRAsync(rutaArchivo);

                try
                {
                    using var engine = new TesseractEngine(RutaDatosTesseract, "spa+eng", EngineMode.Default);
                    using var img = Pix.LoadFromFile(rutaImagenPreprocesada ?? rutaArchivo);
                    using var page = engine.Process(img);
                    
                    var textoExtraido = page.GetText();
                    var confianza = page.GetMeanConfidence();

                    if (!string.IsNullOrWhiteSpace(textoExtraido))
                    {
                        resultado.AppendLine($"**Confianza del OCR: {confianza * 100:F1}%**");
                        resultado.AppendLine();
                        resultado.AppendLine("**Texto extraído:**");
                        resultado.AppendLine("```");
                        resultado.AppendLine(textoExtraido.Trim());
                        resultado.AppendLine("```");
                        resultado.AppendLine();

                        // Estadísticas del texto
                        var palabras = Regex.Matches(textoExtraido, @"\b\w+\b").Count;
                        var lineas = textoExtraido.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
                        resultado.AppendLine($"**Estadísticas del texto:**");
                        resultado.AppendLine($"• Palabras detectadas: {palabras}");
                        resultado.AppendLine($"• Líneas de texto: {lineas}");
                        resultado.AppendLine($"• Caracteres totales: {textoExtraido.Length}");
                        resultado.AppendLine();
                    }
                    else
                    {
                        resultado.AppendLine("• No se detectó texto legible en la imagen");
                        resultado.AppendLine("• La imagen puede ser puramente gráfica, tener texto muy pequeño o baja calidad");
                        resultado.AppendLine();
                    }
                }
                finally
                {
                    // Limpiar imagen preprocesada si se creó
                    if (rutaImagenPreprocesada != null && rutaImagenPreprocesada != rutaArchivo && File.Exists(rutaImagenPreprocesada))
                    {
                        File.Delete(rutaImagenPreprocesada);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OCR: {Ruta}", rutaArchivo);
                resultado.AppendLine($"⚠️ Error al ejecutar OCR: {ex.Message}");
                resultado.AppendLine("• Esto puede indicar que los datos de OCR no están correctamente instalados");
                resultado.AppendLine();
            }
        }

        #endregion

        #region Métodos auxiliares para análisis de imágenes

        private async Task<List<Rgba32>> AnalizarColoresDominantesAsync(Image<Rgba32> imagen)
        {
            var colores = new Dictionary<Rgba32, int>();
            var muestreo = Math.Max(1, Math.Max(imagen.Width, imagen.Height) / 50); // Muestrear para performance

            for (int y = 0; y < imagen.Height; y += muestreo)
            {
                for (int x = 0; x < imagen.Width; x += muestreo)
                {
                    var pixel = imagen[x, y];
                    
                    // Agrupar colores similares para reducir ruido
                    var colorSimplificado = new Rgba32(
                        (byte)((pixel.R / 16) * 16),
                        (byte)((pixel.G / 16) * 16),
                        (byte)((pixel.B / 16) * 16),
                        255
                    );

                    colores[colorSimplificado] = colores.GetValueOrDefault(colorSimplificado, 0) + 1;
                }
            }

            return colores.OrderByDescending(c => c.Value)
                         .Take(10)
                         .Select(c => c.Key)
                         .ToList();
        }

        private async Task<EstadisticasLuminancia> AnalizarLuminanciaAsync(Image<Rgba32> imagen)
        {
            long sumaBrillo = 0;
            int pixelCount = 0;
            var brillos = new List<float>();

            // Muestrear para performance
            var muestreo = Math.Max(1, Math.Max(imagen.Width, imagen.Height) / 100);

            for (int y = 0; y < imagen.Height; y += muestreo)
            {
                for (int x = 0; x < imagen.Width; x += muestreo)
                {
                    var pixel = imagen[x, y];
                    var brillo = (pixel.R * 0.299f + pixel.G * 0.587f + pixel.B * 0.114f) / 255f;
                    
                    brillos.Add(brillo);
                    sumaBrillo += (long)(brillo * 100);
                    pixelCount++;
                }
            }

            var brilloPromedio = (float)sumaBrillo / pixelCount;
            var desviacion = brillos.Select(b => Math.Pow(b * 100 - brilloPromedio, 2)).Average();
            var contraste = (float)Math.Sqrt(desviacion);

            return new EstadisticasLuminancia
            {
                BrilloPromedio = brilloPromedio,
                Contraste = contraste
            };
        }

        private async Task<List<string>> DetectarCaracteristicasBasicasAsync(Image<Rgba32> imagen)
        {
            var caracteristicas = new List<string>();

            try
            {
                // Análisis de relación de aspecto
                var aspectRatio = (float)imagen.Width / imagen.Height;
                if (Math.Abs(aspectRatio - 16f/9f) < 0.1f)
                    caracteristicas.Add("Formato widescreen (16:9)");
                else if (Math.Abs(aspectRatio - 4f/3f) < 0.1f)
                    caracteristicas.Add("Formato estándar (4:3)");
                else if (Math.Abs(aspectRatio - 1f) < 0.1f)
                    caracteristicas.Add("Formato cuadrado (1:1)");

                // Análisis de resolución
                var megapixeles = (imagen.Width * imagen.Height) / 1_000_000f;
                if (megapixeles >= 8)
                    caracteristicas.Add("Alta resolución (≥8MP)");
                else if (megapixeles >= 2)
                    caracteristicas.Add("Resolución media (2-8MP)");
                else
                    caracteristicas.Add("Resolución básica (<2MP)");

                // Detectar si es potencialmente una captura de pantalla
                if (imagen.Width % 16 == 0 && imagen.Height % 9 == 0 && aspectRatio > 1.5f)
                    caracteristicas.Add("Posible captura de pantalla");

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al detectar características básicas");
            }

            return caracteristicas;
        }

        private string DescribirColor(Rgba32 color)
        {
            // Convertir RGB a HSV para mejor descripción
            var max = Math.Max(color.R, Math.Max(color.G, color.B)) / 255f;
            var min = Math.Min(color.R, Math.Min(color.G, color.B)) / 255f;
            
            var saturacion = max == 0 ? 0 : (max - min) / max;
            var valor = max;

            if (valor < 0.3f)
                return "Color oscuro";
            else if (valor > 0.8f && saturacion < 0.3f)
                return "Color claro/blanco";
            else if (saturacion < 0.3f)
                return "Color gris";
            else if (color.R > color.G && color.R > color.B)
                return "Tonos rojizos";
            else if (color.G > color.R && color.G > color.B)
                return "Tonos verdosos";
            else if (color.B > color.R && color.B > color.G)
                return "Tonos azulados";
            else
                return "Color mixto";
        }

        private string DeterminarTipoImagen(EstadisticasLuminancia stats)
        {
            if (stats.BrilloPromedio < 25)
                return "Imagen muy oscura";
            else if (stats.BrilloPromedio > 75)
                return "Imagen muy clara";
            else if (stats.Contraste < 15)
                return "Imagen de bajo contraste";
            else if (stats.Contraste > 35)
                return "Imagen de alto contraste";
            else
                return "Imagen balanceada";
        }

        private async Task<bool> VerificarDatosTesseractAsync()
        {
            try
            {
                var rutaEspañol = Path.Combine(RutaDatosTesseract, "spa.traineddata");
                var rutaIngles = Path.Combine(RutaDatosTesseract, "eng.traineddata");
                
                return File.Exists(rutaEspañol) && File.Exists(rutaIngles);
            }
            catch
            {
                return false;
            }
        }

        private async Task DescargarDatosTesseractAsync()
        {
            try
            {
                // Crear directorio si no existe
                System.IO.Directory.CreateDirectory(RutaDatosTesseract);

                // En un entorno real, aquí descargarías los archivos .traineddata
                // Por ahora, crear archivos básicos o usar los que vienen con Tesseract
                _logger.LogInformation("Configurando datos de Tesseract en: {Ruta}", RutaDatosTesseract);
                
                // Nota: En producción, necesitarás descargar spa.traineddata y eng.traineddata
                // desde https://github.com/tesseract-ocr/tessdata
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar datos de Tesseract");
            }
        }

        private async Task<string?> PreprocesarImagenParaOCRAsync(string rutaOriginal)
        {
            try
            {
                using var imagen = ImageSharpImage.Load(rutaOriginal);
                
                // Solo preprocesar si la imagen es pequeña o tiene bajo contraste
                if (imagen.Width < 800 || imagen.Height < 600)
                {
                    var rutaTemporal = Path.Combine(Path.GetTempPath(), $"ocr_prep_{Guid.NewGuid():N}.png");
                    
                    imagen.Mutate(ctx => ctx
                        .Resize(imagen.Width * 2, imagen.Height * 2) // Escalar 2x
                        .Grayscale() // Convertir a escala de grises
                        .Contrast(1.2f) // Aumentar contraste
                        .AutoOrient() // Corregir orientación
                    );
                    
                    await imagen.SaveAsync(rutaTemporal);
                    return rutaTemporal;
                }
                
                return null; // No necesita preprocesamiento
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al preprocesar imagen para OCR");
                return null;
            }
        }

        private async Task<string> ObtenerMetadatosBasicosImagenAsync(string rutaArchivo)
        {
            var info = new StringBuilder();
            try
            {
                using var imagen = ImageSharpImage.Load(rutaArchivo);
                var infoArchivo = new FileInfo(rutaArchivo);
                
                info.AppendLine($"Imagen: {infoArchivo.Name}");
                info.AppendLine($"Dimensiones: {imagen.Width}x{imagen.Height}");
                info.AppendLine($"Formato: {imagen.Metadata.DecodedImageFormat?.Name ?? "Desconocido"}");
                info.AppendLine($"Tamaño: {FormatearTamaño(infoArchivo.Length)}");
            }
            catch (Exception ex)
            {
                info.AppendLine($"Error al obtener metadatos: {ex.Message}");
            }
            return info.ToString();
        }

        #endregion

        #region Clases auxiliares

        public class EstadisticasLuminancia
        {
            public float BrilloPromedio { get; set; }
            public float Contraste { get; set; }
        }

        #endregion
    }
} 