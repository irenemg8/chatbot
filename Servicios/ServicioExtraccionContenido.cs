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

namespace ChatbotGomarco.Servicios
{
    public class ServicioExtraccionContenido : IServicioExtraccionContenido
    {
        private readonly ILogger<ServicioExtraccionContenido> _logger;

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

        public ServicioExtraccionContenido(ILogger<ServicioExtraccionContenido> logger)
        {
            _logger = logger;
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
            
            for (int pagina = 1; pagina <= pdfDocument.GetNumberOfPages(); pagina++)
            {
                var page = pdfDocument.GetPage(pagina);
                var textoPagina = PdfTextExtractor.GetTextFromPage(page);
                texto.AppendLine($"=== PÁGINA {pagina} ===");
                texto.AppendLine(textoPagina);
                texto.AppendLine();
            }

            return texto.ToString();
        }

        private async Task<string> ExtraerTextoWordAsync(string rutaArchivo)
        {
            var texto = new StringBuilder();

            using var documento = WordprocessingDocument.Open(rutaArchivo, false);
            var body = documento.MainDocumentPart.Document.Body;

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

            return texto.ToString();
        }

        private async Task<string> ExtraerTextoExcelAsync(string rutaArchivo)
        {
            var texto = new StringBuilder();

            using var documento = SpreadsheetDocument.Open(rutaArchivo, false);
            var workbookPart = documento.WorkbookPart;
            var shareStringPart = workbookPart.SharedStringTablePart;

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
                resultado.AppendLine("=== IMAGEN DETECTADA ===");

                // Usar MetadataExtractor para obtener información detallada
                var directorios = ImageMetadataReader.ReadMetadata(rutaArchivo);
                
                foreach (var directorio in directorios)
                {
                    resultado.AppendLine($"**{directorio.Name}:**");
                    foreach (var tag in directorio.Tags)
                    {
                        if (!tag.HasName || string.IsNullOrEmpty(tag.Description)) continue;
                        resultado.AppendLine($"• {tag.Name}: {tag.Description}");
                    }
                    resultado.AppendLine();
                }

                // Análisis básico adicional con System.Drawing
                try
                {
                    using var imagen = Image.FromFile(rutaArchivo);
                    resultado.AppendLine("**Propiedades Básicas:**");
                    resultado.AppendLine($"• Dimensiones: {imagen.Width} x {imagen.Height} píxeles");
                    resultado.AppendLine($"• Resolución: {imagen.HorizontalResolution} x {imagen.VerticalResolution} DPI");
                    resultado.AppendLine($"• Formato: {imagen.RawFormat}");
                }
                catch { /* Ignorar errores de System.Drawing si MetadataExtractor funcionó */ }

                return resultado.ToString();
            }
            catch (Exception ex)
            {
                return $"=== IMAGEN ===\nImagen detectada pero no se pudo analizar: {ex.Message}";
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
    }
} 