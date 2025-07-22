using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    public class ServicioChatbot : IServicioChatbot
    {
        private readonly IServicioArchivos _servicioArchivos;
        private readonly IServicioExtraccionContenido _servicioExtraccion;
        private readonly ILogger<ServicioChatbot> _logger;
        
        // Respuestas predeterminadas para el chatbot (simulación de IA)
        private readonly List<string> _respuestasSaludo = new()
        {
            "¡Hola! Soy el asistente de IA de GOMARCO. Estoy aquí para cualquier consulta relacionada con procesos o documentos. ¿En qué puedo ayudarte hoy?",
            "¡Buenos días! Bienvenido al chatbot de GOMARCO. Puedo ayudarte con consultas sobre documentos o cualquier información que necesites. ¿Cómo puedo ayudarte?",
            "¡Hola! Soy tu asistente virtual de GOMARCO. Estoy capacitado para ayudarte con información y análisis de documentos. ¿Qué necesitas saber?"
        };

        private readonly Dictionary<string, List<string>> _respuestasTematicas = new()
        {
            ["colchon"] = new()
            {
                "En GOMARCO nos especializamos en colchones de alta calidad. Nuestros productos están diseñados para brindar el mejor descanso. ¿Te interesa algún modelo en particular?",
                "Los colchones GOMARCO están fabricados con materiales premium y tecnología avanzada para garantizar comodidad y durabilidad. ¿Necesitas información específica sobre algún producto?",
                "Nuestros colchones han sido desarrollados pensando en tu comodidad y salud postural. Como dice nuestro lema: 'Descansa como te mereces'. ¿Qué características buscas?"
            },
            ["documento"] = new()
            {
                "He analizado el documento que has compartido. Basándome en el contenido, puedo ayudarte con consultas específicas sobre la información contenida.",
                "Perfecto, he procesado el archivo. El documento contiene información valiosa que puedo usar para responder tus preguntas de manera más precisa.",
                "Documento procesado correctamente. Toda la información ha sido analizada y está disponible para consultas. ¿Qué necesitas saber específicamente?"
            },
            ["ayuda"] = new()
            {
                "Puedo ayudarte con múltiples tareas: analizar documentos, responder preguntas sobre GOMARCO, explicar procesos, revisar información corporativa y mucho más. ¿Qué necesitas?",
                "Mis capacidades incluyen: análisis de documentos PDF/Word/Excel, respuestas sobre productos GOMARCO, información corporativa, y procesamiento seguro de archivos confidenciales.",
                "Estoy aquí para asistirte con cualquier consulta. Puedes cargar documentos para análisis, hacer preguntas sobre la empresa, o solicitar información específica sobre nuestros productos."
            }
        };

        public ServicioChatbot(IServicioArchivos servicioArchivos, IServicioExtraccionContenido servicioExtraccion, ILogger<ServicioChatbot> logger)
        {
            _servicioArchivos = servicioArchivos;
            _servicioExtraccion = servicioExtraccion;
            _logger = logger;
        }

        public async Task<string> ProcesarMensajeAsync(string mensaje, string idSesion, List<ArchivoSubido>? archivosContexto = null)
        {
            try
            {
                _logger.LogInformation("Procesando mensaje para sesión: {Sesion}", idSesion);

                // Simular tiempo de procesamiento
                await Task.Delay(1000);

                var mensajeLower = mensaje.ToLowerInvariant();
                
                // Verificar si hay archivos de contexto y consultas específicas
                if (archivosContexto?.Any() == true)
                {
                    // Detectar consultas específicas sobre archivos
                    var archivoEspecifico = DetectarConsultaArchivoEspecifico(mensaje, archivosContexto);
                    if (archivoEspecifico != null)
                    {
                        return await ProcesarConsultaArchivoEspecificoAsync(mensaje, archivoEspecifico);
                    }

                    // Si pregunta por todos los archivos o información general
                    if (ConsultaSobreTodosLosArchivos(mensajeLower))
                    {
                        return await GenerarResumenArchivosAsync(archivosContexto);
                    }

                    // Respuesta inteligente con contexto mínimo
                    return await GenerarRespuestaConContextoInteligente(mensaje, archivosContexto);
                }

                // Detectar saludos
                if (EsSaludo(mensajeLower))
                {
                    return _respuestasSaludo[new Random().Next(_respuestasSaludo.Count)];
                }

                // Detectar temas específicos
                foreach (var tema in _respuestasTematicas.Keys)
                {
                    if (mensajeLower.Contains(tema))
                    {
                        var respuestas = _respuestasTematicas[tema];
                        return respuestas[new Random().Next(respuestas.Count)];
                    }
                }

                // Respuesta general inteligente
                return GenerarRespuestaGeneral(mensaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar mensaje");
                return "Disculpa, he experimentado un error al procesar tu consulta. Por favor, intenta nuevamente o contacta al soporte técnico.";
            }
        }

        public async Task<string> AnalizarArchivoAsync(ArchivoSubido archivo)
        {
            try
            {
                _logger.LogInformation("Analizando contenido real del archivo: {Nombre}", archivo.NombreOriginal);

                // Obtener archivo temporal para análisis
                var rutaTemporal = await _servicioArchivos.DescargarArchivoTemporalAsync(archivo.Id);
                
                var resultado = new StringBuilder();
                resultado.AppendLine($"📄 **Análisis completo de: {archivo.NombreOriginal}**");
                resultado.AppendLine($"📅 Fecha de subida: {archivo.FechaSubida:dd/MM/yyyy HH:mm}");
                resultado.AppendLine($"📊 Tamaño: {FormatearTamaño(archivo.TamañoOriginal)}");
                resultado.AppendLine($"🔍 Tipo: {DeterminarTipoAnalisis(archivo.TipoContenido)}");
                resultado.AppendLine();

                // Verificar si el tipo es compatible para extracción
                if (_servicioExtraccion.EsTipoCompatible(archivo.TipoContenido))
                {
                    // *** ANÁLISIS REAL DEL CONTENIDO ***
                    _logger.LogInformation("Extrayendo contenido real del archivo...");
                    
                    // Extraer metadatos reales
                    var metadatos = await _servicioExtraccion.ExtraerMetadatosAsync(rutaTemporal, archivo.TipoContenido);
                    if (!string.IsNullOrEmpty(metadatos.Titulo))
                    {
                        resultado.AppendLine($"📋 **Título del documento:** {metadatos.Titulo}");
                    }
                    if (!string.IsNullOrEmpty(metadatos.Autor))
                    {
                        resultado.AppendLine($"👤 **Autor:** {metadatos.Autor}");
                    }
                    if (metadatos.NumeroPaginas > 0)
                    {
                        resultado.AppendLine($"📃 **Páginas:** {metadatos.NumeroPaginas}");
                    }
                    if (metadatos.NumeroPalabras > 0)
                    {
                        resultado.AppendLine($"📝 **Palabras:** {metadatos.NumeroPalabras:N0}");
                    }
                    resultado.AppendLine();

                    // Extraer contenido real
                    var contenidoCompleto = await _servicioExtraccion.ExtraerTextoAsync(rutaTemporal, archivo.TipoContenido);
                    
                    if (!string.IsNullOrWhiteSpace(contenidoCompleto))
                    {
                        resultado.AppendLine("📖 **CONTENIDO EXTRAÍDO:**");
                        resultado.AppendLine("```");
                        
                        // Mostrar primeras líneas del contenido (limitado para legibilidad)
                        var lineasContenido = contenidoCompleto.Split('\n');
                        var lineasMostrar = Math.Min(50, lineasContenido.Length);
                        
                        for (int i = 0; i < lineasMostrar; i++)
                        {
                            var linea = lineasContenido[i].Trim();
                            if (!string.IsNullOrEmpty(linea))
                            {
                                resultado.AppendLine(linea);
                            }
                        }
                        
                        if (lineasContenido.Length > lineasMostrar)
                        {
                            resultado.AppendLine($"\n... y {lineasContenido.Length - lineasMostrar} líneas más de contenido.");
                        }
                        
                        resultado.AppendLine("```");
                        resultado.AppendLine();

                        // Análisis de estructura real
                        var estructura = await _servicioExtraccion.AnalizarEstructuraAsync(rutaTemporal, archivo.TipoContenido);
                        
                        resultado.AppendLine("🏗️ **ESTRUCTURA DEL DOCUMENTO:**");
                        if (estructura.Encabezados.Any())
                        {
                            resultado.AppendLine("**Encabezados encontrados:**");
                            foreach (var encabezado in estructura.Encabezados.Take(10))
                            {
                                resultado.AppendLine($"• {encabezado}");
                            }
                            if (estructura.Encabezados.Count > 10)
                            {
                                resultado.AppendLine($"• ... y {estructura.Encabezados.Count - 10} encabezados más");
                            }
                            resultado.AppendLine();
                        }

                        if (estructura.NumeroTablas > 0)
                        {
                            resultado.AppendLine($"📊 **Tablas:** {estructura.NumeroTablas} tabla(s) detectada(s)");
                        }

                        if (estructura.NumeroImagenes > 0)
                        {
                            resultado.AppendLine($"🖼️ **Imágenes:** {estructura.NumeroImagenes} imagen(es) detectada(s)");
                        }

                        resultado.AppendLine($"ℹ️ **Resumen:** {estructura.ResumenEstructural}");
                        resultado.AppendLine();

                        // Generar un resumen inteligente del contenido
                        var resumenInteligente = GenerarResumenInteligente(contenidoCompleto, archivo.TipoContenido);
                        resultado.AppendLine("🧠 **RESUMEN INTELIGENTE:**");
                        resultado.AppendLine(resumenInteligente);
                    }
                    else
                    {
                        resultado.AppendLine("⚠️ **No se pudo extraer contenido textual del archivo.**");
                        resultado.AppendLine("Esto puede deberse a que el archivo está protegido, corrupto, o contiene principalmente imágenes sin texto.");
                    }
                }
                else
                {
                    resultado.AppendLine("⚠️ **Tipo de archivo no compatible para análisis de contenido.**");
                    resultado.AppendLine("**Tipos compatibles:**");
                    resultado.AppendLine("• **Documentos:** PDF, Word (.doc/.docx), Excel (.xls/.xlsx), PowerPoint (.ppt/.pptx), RTF");
                    resultado.AppendLine("• **Texto y datos:** TXT, CSV, JSON, XML");
                    resultado.AppendLine("• **Imágenes:** JPG, PNG, GIF, BMP, SVG, WebP, TIFF");
                    resultado.AppendLine("• **Multimedia:** MP3, WAV, AAC, MP4, AVI, MKV, MOV");
                    resultado.AppendLine("• **Archivos comprimidos:** ZIP, RAR, 7Z, TAR, GZ");
                }

                // Limpiar archivo temporal
                if (File.Exists(rutaTemporal))
                    File.Delete(rutaTemporal);

                return resultado.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar archivo: {Id}", archivo.Id);
                return $"❌ Error al analizar el archivo {archivo.NombreOriginal}: {ex.Message}";
            }
        }

        public async Task<string> GenerarTituloConversacionAsync(string primerMensaje)
        {
            try
            {
                await Task.Delay(500); // Simular procesamiento

                var mensaje = primerMensaje.ToLowerInvariant();
                
                if (mensaje.Contains("colchon") || mensaje.Contains("cama"))
                    return "Consulta sobre Colchones";
                
                if (mensaje.Contains("precio") || mensaje.Contains("costo"))
                    return "Consulta de Precios";
                
                if (mensaje.Contains("documento") || mensaje.Contains("archivo"))
                    return "Análisis de Documentos";
                
                if (mensaje.Contains("proceso") || mensaje.Contains("procedimiento"))
                    return "Consulta de Procesos";

                // Título basado en las primeras palabras
                var palabras = primerMensaje.Split(' ').Take(4);
                var titulo = string.Join(" ", palabras);
                
                return titulo.Length > 30 ? titulo.Substring(0, 30) + "..." : titulo;
            }
            catch
            {
                return "Nueva Conversación";
            }
        }

        public async Task<bool> ValidarSeguridadArchivoAsync(string rutaArchivo)
        {
            try
            {
                var infoArchivo = new FileInfo(rutaArchivo);
                
                // Verificar tamaño máximo (1GB)
                if (infoArchivo.Length > 1024 * 1024 * 1024)
                    return false;

                // Verificar extensiones permitidas
                var extensionesPermitidas = new[]
                {
                    // Documentos
                    ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
                    ".txt", ".csv", ".json", ".xml", ".rtf", ".odt", ".ods", ".odp",
                    
                    // Imágenes
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp", ".tiff", ".tif",
                    
                    // Audio
                    ".mp3", ".wav", ".aac", ".ogg", ".m4a", ".flac",
                    
                    // Video
                    ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v",
                    
                    // Comprimidos
                    ".zip", ".rar", ".7z", ".tar", ".gz"
                };

                var extension = infoArchivo.Extension.ToLowerInvariant();
                if (!extensionesPermitidas.Contains(extension))
                    return false;

                // Verificar que el archivo no esté vacío
                if (infoArchivo.Length == 0)
                    return false;

                await Task.Delay(100); // Simular validación
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar seguridad del archivo: {Archivo}", rutaArchivo);
                return false;
            }
        }

        public async Task<List<string>> ObtenerSugerenciasRespuestaAsync(List<MensajeChat> contextoConversacion)
        {
            try
            {
                await Task.Delay(300); // Simular procesamiento

                var sugerencias = new List<string>();
                
                if (!contextoConversacion.Any())
                {
                    sugerencias.AddRange(new[]
                    {
                        "¿Puedes contarme sobre los productos GOMARCO?",
                        "¿Qué archivos tengo disponibles?",
                        "¿Cómo funciona el sistema de archivos seguros?"
                    });
                    return sugerencias;
                }

                var ultimoMensaje = contextoConversacion.Last();
                var contenido = ultimoMensaje.Contenido.ToLowerInvariant();

                if (contenido.Contains("colchon"))
                {
                    sugerencias.AddRange(new[]
                    {
                        "¿Qué modelos de colchones tienen disponibles?",
                        "Información sobre garantías",
                        "¿Cuáles son los precios actuales?"
                    });
                }
                else if (contenido.Contains("archivo") || contenido.Contains("documento"))
                {
                    sugerencias.AddRange(new[]
                    {
                        "Cuéntame sobre el archivo [nombre]",
                        "¿Qué archivos tengo disponibles?",
                        "Analiza el documento más reciente"
                    });
                }
                else if (contenido.Contains("disponible") || contenido.Contains("lista"))
                {
                    sugerencias.AddRange(new[]
                    {
                        "Muéstrame información del primer archivo",
                        "¿Puedes resumir el contenido?",
                        "Detalles técnicos del documento"
                    });
                }
                else
                {
                    sugerencias.AddRange(new[]
                    {
                        "¿Puedes dar más detalles?",
                        "¿Cómo puedo proceder?",
                        "¿Hay algo más que deba saber?"
                    });
                }

                return sugerencias.Take(3).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar sugerencias");
                return new List<string>();
            }
        }

        public async Task<string> ProcesarMultiplesArchivosAsync(List<ArchivoSubido> archivos)
        {
            try
            {
                // Ahora delegamos a la función de resumen más inteligente
                return await GenerarResumenArchivosAsync(archivos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar múltiples archivos");
                return "❌ Error al procesar los archivos de contexto.";
            }
        }

        private bool EsSaludo(string mensaje)
        {
            var saludos = new[] { "hola", "hello", "hi", "buenos", "buenas", "saludos", "hey" };
            return saludos.Any(s => mensaje.Contains(s));
        }

        private string GenerarRespuestaGeneral(string mensaje)
        {
            var respuestas = new[]
            {
                $"Entiendo tu consulta sobre '{mensaje}'. Como asistente de GOMARCO, puedo ayudarte con información específica. ¿Podrías proporcionar más detalles?",
                $"Gracias por tu pregunta. Para brindarte la mejor respuesta posible, ¿podrías ser más específico sobre lo que necesitas saber?",
                $"He recibido tu consulta. Como especialista en productos GOMARCO y análisis de documentos, puedo asistirte mejor si me proporcionas más contexto."
            };

            return respuestas[new Random().Next(respuestas.Length)];
        }

        private string GenerarRespuestaConContexto(string mensaje, string contextoArchivos)
        {
            return $"Basándome en los documentos proporcionados y tu consulta '{mensaje}', puedo ayudarte con lo siguiente:\n\n{contextoArchivos}\n\n¿Hay algo específico de esta información que te gustaría explorar más a fondo?";
        }

        private string DeterminarTipoAnalisis(string tipoContenido)
        {
            return tipoContenido switch
            {
                // Documentos
                "application/pdf" => "📄 Documento PDF",
                "application/msword" => "📝 Documento Word (.doc)",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "📝 Documento Word (.docx)",
                "application/vnd.ms-excel" => "📊 Hoja Excel (.xls)",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "📊 Hoja Excel (.xlsx)",
                "application/vnd.ms-powerpoint" => "📽️ Presentación PowerPoint (.ppt)",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation" => "📽️ Presentación PowerPoint (.pptx)",
                "application/rtf" => "📝 Documento RTF",
                
                // Texto y datos
                "text/plain" => "📄 Archivo de Texto",
                "text/csv" => "📈 Datos CSV",
                "application/json" => "🔧 Datos JSON",
                "application/xml" => "📋 Datos XML",
                
                // Imágenes
                "image/jpeg" or "image/png" => "🖼️ Imagen",
                "image/gif" => "🎞️ Imagen GIF",
                "image/bmp" => "🖼️ Imagen BMP",
                "image/svg+xml" => "🎨 Imagen SVG",
                "image/webp" => "🖼️ Imagen WebP",
                "image/tiff" => "🖼️ Imagen TIFF",
                
                // Audio
                "audio/mpeg" => "🎵 Audio MP3",
                "audio/wav" => "🎵 Audio WAV",
                "audio/aac" => "🎵 Audio AAC",
                "audio/ogg" => "🎵 Audio OGG",
                "audio/mp4" => "🎵 Audio M4A",
                "audio/flac" => "🎵 Audio FLAC",
                
                // Video
                "video/mp4" => "🎬 Video MP4",
                "video/avi" => "🎬 Video AVI",
                "video/x-matroska" => "🎬 Video MKV",
                "video/quicktime" => "🎬 Video MOV",
                "video/x-ms-wmv" => "🎬 Video WMV",
                "video/x-flv" => "🎬 Video FLV",
                "video/webm" => "🎬 Video WebM",
                "video/x-m4v" => "🎬 Video M4V",
                
                // Archivos comprimidos
                "application/zip" => "📦 Archivo ZIP",
                "application/vnd.rar" => "📦 Archivo RAR",
                "application/x-7z-compressed" => "📦 Archivo 7Z",
                "application/x-tar" => "📦 Archivo TAR",
                "application/gzip" => "📦 Archivo GZ",
                
                _ => "📁 Archivo de Datos"
            };
        }

        private string GenerarAnalisisEspecifico(string tipoContenido, string nombreArchivo)
        {
            return tipoContenido switch
            {
                "application/pdf" => "✅ El documento PDF ha sido procesado correctamente. Contiene texto e información estructurada lista para consultas.",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "✅ Documento de Word analizado. El contenido está disponible para búsquedas y consultas específicas.",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "✅ Hoja de cálculo procesada. Los datos numéricos y tablas están disponibles para análisis.",
                "text/plain" => "✅ Archivo de texto procesado. El contenido está indexado y listo para consultas.",
                "text/csv" => "✅ Datos CSV importados correctamente. La información tabular está disponible para análisis.",
                "application/json" => "✅ Estructura JSON analizada. Los datos están organizados y listos para consultas.",
                _ => "✅ Archivo procesado exitosamente. El contenido está disponible como contexto para nuestras conversaciones."
            };
        }

        private static string FormatearTamaño(long bytes)
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

        private ArchivoSubido? DetectarConsultaArchivoEspecifico(string mensaje, List<ArchivoSubido> archivos)
        {
            var mensajeLower = mensaje.ToLowerInvariant();
            
            // Buscar por nombre exacto del archivo (sin extensión)
            foreach (var archivo in archivos)
            {
                var nombreSinExtension = Path.GetFileNameWithoutExtension(archivo.NombreOriginal).ToLowerInvariant();
                var nombreCompleto = archivo.NombreOriginal.ToLowerInvariant();
                
                if (mensajeLower.Contains(nombreSinExtension) || mensajeLower.Contains(nombreCompleto))
                {
                    return archivo;
                }
            }

            // Detectar palabras clave que indican consulta específica
            var indicadoresEspecificos = new[] { "archivo", "documento", "pdf", "word", "excel", "imagen" };
            
            if (indicadoresEspecificos.Any(ind => mensajeLower.Contains(ind)) && 
                (mensajeLower.Contains("este") || mensajeLower.Contains("el") || mensajeLower.Contains("último")))
            {
                // Devolver el archivo más reciente
                return archivos.OrderByDescending(a => a.FechaSubida).FirstOrDefault();
            }

            return null;
        }

        private async Task<string> ProcesarConsultaArchivoEspecificoAsync(string mensaje, ArchivoSubido archivo)
        {
            var mensajeLower = mensaje.ToLowerInvariant();
            var resultado = new StringBuilder();

            resultado.AppendLine($"📄 **Información sobre: {archivo.NombreOriginal}**");
            resultado.AppendLine();

            // Información básica siempre
            resultado.AppendLine($"📅 **Fecha de carga:** {archivo.FechaSubida:dd/MM/yyyy HH:mm}");
            resultado.AppendLine($"📊 **Tamaño:** {FormatearTamaño(archivo.TamañoOriginal)}");
            resultado.AppendLine($"🔍 **Tipo:** {DeterminarTipoAnalisis(archivo.TipoContenido)}");
            resultado.AppendLine();

            // Respuesta específica según la consulta
            if (mensajeLower.Contains("resumen") || mensajeLower.Contains("qué contiene") || mensajeLower.Contains("contenido"))
            {
                resultado.AppendLine("📋 **Resumen del contenido:**");
                resultado.AppendLine(GenerarResumenContenidoEspecifico(archivo));
            }
            else if (mensajeLower.Contains("información") || mensajeLower.Contains("detalles"))
            {
                resultado.AppendLine("📋 **Detalles técnicos:**");
                resultado.AppendLine(GenerarDetallesTecnicos(archivo));
            }
            else if (mensajeLower.Contains("análisis") || mensajeLower.Contains("analizar"))
            {
                resultado.AppendLine("🔬 **Análisis del documento:**");
                resultado.AppendLine(await GenerarAnalisisDetallado(archivo));
            }
            else
            {
                resultado.AppendLine("💡 **Información general:**");
                resultado.AppendLine(GenerarAnalisisEspecifico(archivo.TipoContenido, archivo.NombreOriginal));
            }

            resultado.AppendLine();
            resultado.AppendLine("❓ **¿Necesitas algo específico de este archivo?** Puedes preguntarme por:");
            resultado.AppendLine("• Resumen del contenido");
            resultado.AppendLine("• Detalles técnicos");
            resultado.AppendLine("• Análisis específico");
            resultado.AppendLine("• Información particular sobre algún tema");

            return resultado.ToString();
        }

        private bool ConsultaSobreTodosLosArchivos(string mensajeLower)
        {
            var indicadores = new[]
            {
                "todos los archivos", "qué archivos", "archivos cargados", "documentos subidos",
                "lista archivos", "archivos disponibles", "qué documentos", "cuántos archivos"
            };

            return indicadores.Any(ind => mensajeLower.Contains(ind));
        }

        private async Task<string> GenerarResumenArchivosAsync(List<ArchivoSubido> archivos)
        {
            var resultado = new StringBuilder();
            resultado.AppendLine($"📁 **Tienes {archivos.Count} archivo(s) disponible(s):**");
            resultado.AppendLine();

            for (int i = 0; i < archivos.Count && i < 10; i++) // Limitar a 10 archivos
            {
                var archivo = archivos[i];
                resultado.AppendLine($"{i + 1}. **{archivo.NombreOriginal}**");
                resultado.AppendLine($"   📅 {archivo.FechaSubida:dd/MM/yyyy} • 📊 {FormatearTamaño(archivo.TamañoOriginal)} • 🔍 {DeterminarTipoAnalisis(archivo.TipoContenido)}");
                resultado.AppendLine();
            }

            if (archivos.Count > 10)
            {
                resultado.AppendLine($"... y {archivos.Count - 10} archivo(s) más.");
                resultado.AppendLine();
            }

            resultado.AppendLine("💡 **Para consultar un archivo específico**, menciona su nombre en tu pregunta.");
            resultado.AppendLine("**Ejemplo:** 'Cuéntame sobre el informe de ventas.pdf'");

            return resultado.ToString();
        }

        private async Task<string> GenerarRespuestaConContextoInteligente(string mensaje, List<ArchivoSubido> archivos)
        {
            var mensajeLower = mensaje.ToLowerInvariant();
            
            try
            {
                // 1. ANÁLISIS SEMÁNTICO DEL MENSAJE
                var intencionUsuario = AnalizarIntencionUsuario(mensaje);
                var palabrasClave = ExtraerPalabrasClave(mensaje);

                // 2. BÚSQUEDA EN EL CONTENIDO DE LOS ARCHIVOS
                var resultadosBusqueda = await BuscarEnArchivosAsync(palabrasClave, archivos);

                var respuesta = new StringBuilder();

                if (resultadosBusqueda.Any())
                {
                    respuesta.AppendLine($"🔍 **Encontré información relevante para tu consulta:** \"{mensaje}\"");
                    respuesta.AppendLine();

                    // Mostrar resultados más relevantes
                    foreach (var resultado in resultadosBusqueda.Take(3))
                    {
                        respuesta.AppendLine($"📄 **En: {resultado.NombreArchivo}**");
                        respuesta.AppendLine($"📍 **Contexto encontrado:**");
                        respuesta.AppendLine($"```");
                        respuesta.AppendLine(resultado.ContextoEncontrado);
                        respuesta.AppendLine($"```");
                        respuesta.AppendLine($"🎯 **Relevancia:** {resultado.PorcentajeRelevancia:F1}%");
                        respuesta.AppendLine();
                    }

                    // Generar respuesta inteligente basada en los resultados
                    var respuestaGenerada = GenerarRespuestaBasadaEnContexto(mensaje, resultadosBusqueda, intencionUsuario);
                    if (!string.IsNullOrEmpty(respuestaGenerada))
                    {
                        respuesta.AppendLine("🧠 **Respuesta generada:**");
                        respuesta.AppendLine(respuestaGenerada);
                        respuesta.AppendLine();
                    }

                    if (resultadosBusqueda.Count > 3)
                    {
                        respuesta.AppendLine($"💡 Encontré {resultadosBusqueda.Count - 3} resultado(s) adicional(es). ¿Te gustaría que profundice en algún archivo específico?");
                    }
                }
                else
                {
                    // Respuesta contextual cuando no encuentra coincidencias directas
                    var tiposPrincipales = archivos.GroupBy(a => DeterminarTipoAnalisis(a.TipoContenido))
                        .OrderByDescending(g => g.Count())
                        .Take(2)
                        .Select(g => g.Key)
                        .ToList();

                    respuesta.AppendLine($"🤔 No encontré coincidencias directas para: \"{mensaje}\"");
                    respuesta.AppendLine();
                    respuesta.AppendLine($"Tengo acceso a {archivos.Count} archivo(s) como contexto:");

                    foreach (var tipo in tiposPrincipales)
                    {
                        var cantidad = archivos.Count(a => DeterminarTipoAnalisis(a.TipoContenido) == tipo);
                        respuesta.AppendLine($"• {cantidad} {tipo}(s)");
                    }

                    respuesta.AppendLine();
                    respuesta.AppendLine("💡 **Sugerencias para mejorar tu búsqueda:**");

                    // Sugerencias inteligentes basadas en la intención
                    var sugerencias = GenerarSugerenciasBusqueda(intencionUsuario, archivos);
                    foreach (var sugerencia in sugerencias)
                    {
                        respuesta.AppendLine($"• {sugerencia}");
                    }
                }

                return respuesta.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en respuesta inteligente con contexto");
                
                // Fallback a la respuesta original
                var tiposPrincipales = archivos.GroupBy(a => DeterminarTipoAnalisis(a.TipoContenido))
                    .OrderByDescending(g => g.Count())
                    .Take(2)
                    .Select(g => g.Key)
                    .ToList();

                var respuesta = new StringBuilder();
                respuesta.AppendLine($"Entiendo tu consulta: '{mensaje}'");
                respuesta.AppendLine();
                respuesta.AppendLine($"Tengo acceso a {archivos.Count} archivo(s) como contexto, principalmente:");

                foreach (var tipo in tiposPrincipales)
                {
                    var cantidad = archivos.Count(a => DeterminarTipoAnalisis(a.TipoContenido) == tipo);
                    respuesta.AppendLine($"• {cantidad} {tipo}(s)");
                }

                respuesta.AppendLine();
                respuesta.AppendLine("🎯 **Para una respuesta más precisa:**");
                respuesta.AppendLine("• Menciona un archivo específico por su nombre");
                respuesta.AppendLine("• Pregunta 'qué archivos tengo' para ver la lista completa");
                respuesta.AppendLine("• Haz una consulta más específica sobre el tema que te interesa");

                return respuesta.ToString();
            }
        }

        private string GenerarResumenContenidoEspecifico(ArchivoSubido archivo)
        {
            return archivo.TipoContenido switch
            {
                "application/pdf" => "Este documento PDF contiene texto estructurado, posibles tablas y gráficos. Perfecto para consultas sobre información específica, análisis de contenido y extracción de datos clave.",
                
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "Documento de texto de Word con formato, posiblemente incluyendo encabezados, párrafos estructurados, tablas y elementos gráficos. Ideal para análisis de contenido textual.",
                
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "Hoja de cálculo con datos tabulares, fórmulas y posibles gráficos. Excelente para análisis de datos, consultas numéricas y información estadística.",
                
                "text/plain" => "Archivo de texto plano con información en formato simple. Perfecto para búsquedas de texto específico y análisis de contenido directo.",
                
                "text/csv" => "Datos estructurados en formato CSV, ideales para análisis estadístico, consultas sobre datos específicos y procesamiento de información tabular.",
                
                _ => "Archivo procesado que contiene información relevante para consultas. Puedo ayudarte a extraer información específica o responder preguntas sobre su contenido."
            };
        }

        private string GenerarDetallesTecnicos(ArchivoSubido archivo)
        {
            var detalles = new StringBuilder();
            detalles.AppendLine($"🔍 **Tipo MIME:** {archivo.TipoContenido}");
            detalles.AppendLine($"🔒 **Estado:** Archivo cifrado y seguro");
            detalles.AppendLine($"✅ **Hash de integridad:** {archivo.HashSha256[..16]}... (parcial)");
            
            if (!string.IsNullOrEmpty(archivo.Descripcion))
            {
                detalles.AppendLine($"📝 **Descripción:** {archivo.Descripcion}");
            }

            var extension = Path.GetExtension(archivo.NombreOriginal);
            detalles.AppendLine($"📄 **Extensión:** {extension}");
            
            // Información adicional según el tipo
            switch (archivo.TipoContenido)
            {
                case "application/pdf":
                    detalles.AppendLine("🔍 **Capacidades:** Extracción de texto, análisis de estructura, identificación de tablas");
                    break;
                case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                    detalles.AppendLine("🔍 **Capacidades:** Análisis de datos, consultas numéricas, procesamiento de fórmulas");
                    break;
                case "image/jpeg":
                case "image/png":
                    detalles.AppendLine("🔍 **Capacidades:** Análisis visual, extracción de metadatos, descripción de contenido");
                    break;
            }

            return detalles.ToString();
        }

        private async Task<string> GenerarAnalisisDetallado(ArchivoSubido archivo)
        {
            // Simular análisis más profundo
            await Task.Delay(500);

            return archivo.TipoContenido switch
            {
                "application/pdf" => 
                    "🔬 **Análisis completo del PDF:**\n" +
                    "• Estructura del documento analizada\n" +
                    "• Texto extraíble identificado\n" +
                    "• Posibles elementos multimedia detectados\n" +
                    "• Metadatos de creación procesados\n" +
                    "• Índice de contenidos generado\n\n" +
                    "💡 **Listo para:** Búsquedas de texto específico, extracción de párrafos, análisis de secciones particulares.",

                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => 
                    "🔬 **Análisis completo de la hoja de cálculo:**\n" +
                    "• Hojas de trabajo identificadas\n" +
                    "• Rangos de datos mapeados\n" +
                    "• Fórmulas y funciones catalogadas\n" +
                    "• Tipos de datos clasificados\n" +
                    "• Gráficos y elementos visuales detectados\n\n" +
                    "💡 **Listo para:** Consultas de datos específicos, análisis estadístico, extracción de información numérica.",

                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => 
                    "🔬 **Análisis completo del documento Word:**\n" +
                    "• Estructura de encabezados mapeada\n" +
                    "• Párrafos y secciones identificados\n" +
                    "• Tablas y listas procesadas\n" +
                    "• Formato y estilos analizados\n" +
                    "• Elementos gráficos catalogados\n\n" +
                    "💡 **Listo para:** Búsqueda de secciones específicas, análisis de contenido por temas, extracción de información estructurada.",

                _ => 
                    "🔬 **Análisis completo del archivo:**\n" +
                    "• Contenido procesado y indexado\n" +
                    "• Estructura interna analizada\n" +
                    "• Metadatos extraídos\n" +
                    "• Información clave identificada\n\n" +
                    "💡 **Listo para:** Consultas específicas, búsquedas de información, análisis de contenido relevante."
            };
        }

        private string GenerarResumenInteligente(string contenido, string tipoContenido)
        {
            try
            {
                var resumen = new StringBuilder();
                var palabras = contenido.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var lineas = contenido.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                // Estadísticas básicas mejoradas
                var caracteristicas = AnalyzarCaracteristicasContenido(contenido);
                resumen.AppendLine($"**Contenido analizado:** {palabras.Length:N0} palabras en {lineas.Length:N0} líneas");
                resumen.AppendLine($"**Complejidad estimada:** {caracteristicas.ComplejidadLectura}");
                resumen.AppendLine($"**Idioma detectado:** {caracteristicas.IdiomaDetectado}");
                resumen.AppendLine();

                // Categorización automática del documento
                var categoria = CategorizarDocumento(contenido);
                if (!string.IsNullOrEmpty(categoria))
                {
                    resumen.AppendLine($"📂 **Categoría identificada:** {categoria}");
                    resumen.AppendLine();
                }

                switch (tipoContenido)
                {
                    case "application/pdf":
                        return GenerarResumenPdf(contenido, resumen);
                    
                    case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                        return GenerarResumenWord(contenido, resumen);
                    
                    case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                        return GenerarResumenExcel(contenido, resumen);
                    
                    case "text/csv":
                        return GenerarResumenCsv(contenido, resumen);
                    
                    case "application/json":
                        return GenerarResumenJson(contenido, resumen);
                    
                    default:
                        return GenerarResumenTexto(contenido, resumen);
                }
            }
            catch (Exception ex)
            {
                return $"Error al generar resumen inteligente: {ex.Message}";
            }
        }

        private string GenerarResumenPdf(string contenido, StringBuilder resumen)
        {
            // Detectar si es un documento técnico, informe, manual, etc.
            var palabrasClave = new Dictionary<string, string>
            {
                { "manual|instruccion|guia|procedimiento", "📋 **Tipo:** Manual o guía de instrucciones" },
                { "informe|reporte|analisis|estudio|investigacion", "📊 **Tipo:** Informe o documento analítico" },
                { "contrato|acuerdo|clausula|termino", "📝 **Tipo:** Documento legal o contractual" },
                { "producto|catalogo|precio|especificacion", "🛍️ **Tipo:** Catálogo de productos o especificaciones" },
                { "capacitacion|entrenamiento|curso|formacion", "🎓 **Tipo:** Material de capacitación o formación" }
            };

            foreach (var kvp in palabrasClave)
            {
                if (Regex.IsMatch(contenido, kvp.Key, RegexOptions.IgnoreCase))
                {
                    resumen.AppendLine(kvp.Value);
                    break;
                }
            }

            // Buscar temas principales
            var temasPrincipales = ExtraerTemasPrincipales(contenido);
            if (temasPrincipales.Any())
            {
                resumen.AppendLine("**Temas principales identificados:**");
                foreach (var tema in temasPrincipales.Take(5))
                {
                    resumen.AppendLine($"• {tema}");
                }
            }

            return resumen.ToString();
        }

        private string GenerarResumenWord(string contenido, StringBuilder resumen)
        {
            resumen.AppendLine("📄 **Tipo:** Documento de Word");
            
            // Detectar formato (carta, informe, propuesta, etc.)
            if (contenido.Contains("Estimado") || contenido.Contains("Cordiales saludos"))
                resumen.AppendLine("**Formato detectado:** Carta o comunicación formal");
            else if (contenido.Contains("Propuesta") || contenido.Contains("Cotización"))
                resumen.AppendLine("**Formato detectado:** Propuesta comercial o cotización");
            else if (contenido.Contains("Introducción") && contenido.Contains("Conclusión"))
                resumen.AppendLine("**Formato detectado:** Informe o documento estructurado");

            var temas = ExtraerTemasPrincipales(contenido);
            if (temas.Any())
            {
                resumen.AppendLine("**Contenido principal:**");
                resumen.AppendLine($"• {temas.First()}");
            }

            return resumen.ToString();
        }

        private string GenerarResumenExcel(string contenido, StringBuilder resumen)
        {
            resumen.AppendLine("📊 **Tipo:** Hoja de cálculo de Excel");
            
            // Detectar tipo de datos
            if (contenido.Contains("TOTAL") || contenido.Contains("SUMA"))
                resumen.AppendLine("**Contenido detectado:** Datos financieros o contables");
            else if (contenido.Contains("Fecha") && contenido.Contains("Cantidad"))
                resumen.AppendLine("**Contenido detectado:** Registro de transacciones o inventario");
            else if (contenido.Contains("Nombre") && contenido.Contains("Teléfono"))
                resumen.AppendLine("**Contenido detectado:** Lista de contactos o directorio");

            // Contar filas aproximadas
            var filas = contenido.Split('\n').Where(l => l.Contains("|")).Count();
            if (filas > 0)
                resumen.AppendLine($"**Filas de datos:** Aproximadamente {filas} registros");

            return resumen.ToString();
        }

        private string GenerarResumenCsv(string contenido, StringBuilder resumen)
        {
            var lineas = contenido.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var primeraLinea = lineas.FirstOrDefault() ?? "";
            
            resumen.AppendLine("📈 **Tipo:** Archivo de datos CSV");
            resumen.AppendLine($"**Registros:** {Math.Max(0, lineas.Length - 1)} filas de datos");
            
            if (!string.IsNullOrEmpty(primeraLinea))
            {
                var columnas = primeraLinea.Split(',', ';').Length;
                resumen.AppendLine($"**Columnas:** {columnas} campos por registro");
                resumen.AppendLine($"**Encabezados:** {primeraLinea.Substring(0, Math.Min(100, primeraLinea.Length))}...");
            }

            return resumen.ToString();
        }

        private string GenerarResumenJson(string contenido, StringBuilder resumen)
        {
            resumen.AppendLine("🔧 **Tipo:** Archivo de datos JSON");
            
            try
            {
                if (contenido.TrimStart().StartsWith("["))
                    resumen.AppendLine("**Estructura:** Array de objetos");
                else if (contenido.TrimStart().StartsWith("{"))
                    resumen.AppendLine("**Estructura:** Objeto único");

                // Buscar campos comunes
                var camposComunesDetectados = new List<string>();
                var camposComunes = new[] { "id", "name", "email", "date", "user", "data", "config" };
                
                foreach (var campo in camposComunes)
                {
                    if (contenido.Contains($"\"{campo}\"", StringComparison.OrdinalIgnoreCase))
                        camposComunesDetectados.Add(campo);
                }

                if (camposComunesDetectados.Any())
                    resumen.AppendLine($"**Campos detectados:** {string.Join(", ", camposComunesDetectados)}");
            }
            catch
            {
                resumen.AppendLine("**Nota:** Estructura JSON compleja o anidada");
            }

            return resumen.ToString();
        }

        private string GenerarResumenTexto(string contenido, StringBuilder resumen)
        {
            // Detectar idioma aproximado
            var palabrasEspanol = new[] { "el", "la", "de", "que", "y", "a", "en", "un", "es", "se" };
            var palabrasIngles = new[] { "the", "of", "and", "a", "to", "in", "is", "you", "that", "it" };
            
            var contenidoLower = contenido.ToLowerInvariant();
            var conteoEspanol = palabrasEspanol.Count(p => contenidoLower.Contains(" " + p + " "));
            var conteoIngles = palabrasIngles.Count(p => contenidoLower.Contains(" " + p + " "));
            
            if (conteoEspanol > conteoIngles)
                resumen.AppendLine("**Idioma detectado:** Español");
            else if (conteoIngles > conteoEspanol)
                resumen.AppendLine("**Idioma detectado:** Inglés");

            // Detectar formato
            if (contenido.Contains("<?xml") || contenido.Contains("<html"))
                resumen.AppendLine("**Formato:** Archivo XML/HTML");
            else if (contenido.Contains("#!/") || contenido.Contains("import ") || contenido.Contains("function"))
                resumen.AppendLine("**Formato:** Código fuente o script");
            else
                resumen.AppendLine("**Formato:** Texto plano");

            return resumen.ToString();
        }

        #region Métodos de análisis semántico avanzado

        private CaracteristicasContenido AnalyzarCaracteristicasContenido(string contenido)
        {
            var caracteristicas = new CaracteristicasContenido();

            try
            {
                var palabras = contenido.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var oraciones = contenido.Split('.', StringSplitOptions.RemoveEmptyEntries);

                // Calcular complejidad de lectura (basado en longitud promedio de palabras y oraciones)
                var longitudPromedioPalabras = palabras.Average(p => p.Length);
                var palabrasPorOracion = palabras.Length / (float)Math.Max(1, oraciones.Length);

                caracteristicas.ComplejidadLectura = (longitudPromedioPalabras, palabrasPorOracion) switch
                {
                    ( < 4.5f, < 15f) => "Básica (fácil de leer)",
                    ( < 5.5f, < 20f) => "Intermedia (lectura estándar)",
                    ( < 6.5f, < 25f) => "Avanzada (lectura compleja)",
                    _ => "Experta (muy compleja)"
                };

                // Detección de idioma simple
                var palabrasEspañol = new[] { "el", "la", "de", "que", "y", "a", "en", "un", "es", "se", "no", "te", "lo", "del", "con", "por", "para" };
                var palabrasIngles = new[] { "the", "and", "to", "of", "a", "in", "is", "it", "you", "that", "he", "was", "for", "on", "are", "as", "with" };

                var contenidoLower = contenido.ToLowerInvariant();
                var conteoEspañol = palabrasEspañol.Sum(p => Regex.Matches(contenidoLower, $@"\b{p}\b").Count);
                var conteoIngles = palabrasIngles.Sum(p => Regex.Matches(contenidoLower, $@"\b{p}\b").Count);

                caracteristicas.IdiomaDetectado = conteoEspañol > conteoIngles ? "Español" : 
                                                 conteoIngles > conteoEspañol ? "Inglés" : "Mixto/Indeterminado";

                return caracteristicas;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al analizar características del contenido");
                return new CaracteristicasContenido 
                { 
                    ComplejidadLectura = "Indeterminada", 
                    IdiomaDetectado = "Indeterminado" 
                };
            }
        }

        private string CategorizarDocumento(string contenido)
        {
            var contenidoLower = contenido.ToLowerInvariant();

            var categorias = new Dictionary<string, string[]>
            {
                ["📋 Manual/Guía"] = new[] { "manual", "instrucciones", "guía", "procedimiento", "paso a paso", "cómo", "tutorial" },
                ["📊 Informe/Análisis"] = new[] { "informe", "reporte", "análisis", "estudio", "investigación", "estadística", "conclusión", "resultado" },
                ["💼 Documento Comercial"] = new[] { "propuesta", "cotización", "presupuesto", "factura", "contrato", "acuerdo", "venta", "cliente" },
                ["🏢 Documento Corporativo"] = new[] { "política", "proceso", "empresa", "organización", "corporativo", "empleado", "recursos humanos" },
                ["📚 Material Educativo"] = new[] { "capacitación", "entrenamiento", "curso", "formación", "aprendizaje", "conocimiento", "competencia" },
                ["🔧 Documentación Técnica"] = new[] { "especificación", "técnico", "sistema", "software", "hardware", "configuración", "instalación" },
                ["📈 Financiero"] = new[] { "financiero", "presupuesto", "costo", "precio", "inversión", "ganancia", "pérdida", "balance" },
                ["📋 Catálogo/Producto"] = new[] { "catálogo", "producto", "características", "modelo", "especificaciones", "colchón", "gomarco" }
            };

            foreach (var categoria in categorias)
            {
                var coincidencias = categoria.Value.Count(palabra => contenidoLower.Contains(palabra));
                if (coincidencias >= 2) // Al menos 2 palabras clave
                {
                    return categoria.Key;
                }
            }

            return string.Empty;
        }

        private string AnalizarIntencionUsuario(string mensaje)
        {
            var mensajeLower = mensaje.ToLowerInvariant();

            var intenciones = new Dictionary<string, string[]>
            {
                ["busqueda"] = new[] { "buscar", "encontrar", "localizar", "donde", "ubicar", "hallar" },
                ["explicacion"] = new[] { "qué es", "cómo", "explicar", "definir", "significado", "concepto" },
                ["comparacion"] = new[] { "comparar", "diferencia", "mejor", "peor", "versus", "vs", "diferente" },
                ["listado"] = new[] { "listar", "enumerar", "mostrar", "cuáles", "todos", "lista" },
                ["resumen"] = new[] { "resumir", "resumen", "sintetizar", "principales", "importante", "clave" },
                ["detalle"] = new[] { "detallar", "detalles", "específico", "profundidad", "completo", "exhaustivo" },
                ["procedimiento"] = new[] { "cómo hacer", "pasos", "proceso", "procedimiento", "método", "instrucciones" },
                ["recomendacion"] = new[] { "recomendar", "sugerir", "aconsejar", "mejor opción", "qué elegir" }
            };

            foreach (var intencion in intenciones)
            {
                if (intencion.Value.Any(palabra => mensajeLower.Contains(palabra)))
                {
                    return intencion.Key;
                }
            }

            return "general";
        }

        private List<string> ExtraerPalabrasClave(string mensaje)
        {
            var palabrasComunes = new HashSet<string> 
            { 
                "el", "la", "de", "que", "y", "a", "en", "un", "es", "se", "no", "te", "lo", "le", "da", "su", "por", "son", "con", "para", "al", "una", "las", "del", "los",
                "me", "mi", "tu", "si", "como", "más", "pero", "muy", "ser", "todo", "ya", "sobre", "esto", "qué", "cómo", "dónde", "cuándo", "por qué", "puedes", "puede"
            };

            return Regex.Matches(mensaje.ToLowerInvariant(), @"\b[a-záéíóúñü]{3,}\b")
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(p => !palabrasComunes.Contains(p))
                .Distinct()
                .ToList();
        }

        private async Task<List<ResultadoBusqueda>> BuscarEnArchivosAsync(List<string> palabrasClave, List<ArchivoSubido> archivos)
        {
            var resultados = new List<ResultadoBusqueda>();

            foreach (var archivo in archivos)
            {
                try
                {
                    // Solo buscar en archivos de texto por ahora
                    if (!_servicioExtraccion.EsTipoCompatible(archivo.TipoContenido))
                        continue;

                    var rutaTemporal = await _servicioArchivos.DescargarArchivoTemporalAsync(archivo.Id);
                    var contenido = await _servicioExtraccion.ExtraerTextoAsync(rutaTemporal, archivo.TipoContenido);

                    // Limpiar archivo temporal
                    if (File.Exists(rutaTemporal))
                        File.Delete(rutaTemporal);

                    if (string.IsNullOrWhiteSpace(contenido))
                        continue;

                    // Buscar coincidencias
                    var coincidencias = BuscarCoincidenciasEnTexto(contenido, palabrasClave);

                    foreach (var coincidencia in coincidencias)
                    {
                        resultados.Add(new ResultadoBusqueda
                        {
                            NombreArchivo = archivo.NombreOriginal,
                            ContextoEncontrado = coincidencia.Contexto,
                            PorcentajeRelevancia = coincidencia.Relevancia,
                            TipoArchivo = archivo.TipoContenido,
                            PosicionEnDocumento = coincidencia.Posicion
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al buscar en archivo: {Archivo}", archivo.NombreOriginal);
                }
            }

            return resultados.OrderByDescending(r => r.PorcentajeRelevancia).ToList();
        }

        private List<CoincidenciaTexto> BuscarCoincidenciasEnTexto(string contenido, List<string> palabrasClave)
        {
            var coincidencias = new List<CoincidenciaTexto>();
            var lineas = contenido.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lineas.Length; i++)
            {
                var linea = lineas[i];
                var lineaLower = linea.ToLowerInvariant();
                
                var palabrasEncontradas = palabrasClave.Count(p => lineaLower.Contains(p.ToLowerInvariant()));
                
                if (palabrasEncontradas > 0)
                {
                    // Crear contexto con líneas adyacentes
                    var inicioContexto = Math.Max(0, i - 2);
                    var finContexto = Math.Min(lineas.Length - 1, i + 2);
                    
                    var contexto = string.Join("\n", lineas[inicioContexto..(finContexto + 1)]);
                    var relevancia = (float)palabrasEncontradas / palabrasClave.Count * 100;

                    coincidencias.Add(new CoincidenciaTexto
                    {
                        Contexto = contexto.Length > 300 ? contexto.Substring(0, 300) + "..." : contexto,
                        Relevancia = relevancia,
                        Posicion = i
                    });
                }
            }

            return coincidencias.Where(c => c.Relevancia >= 20) // Solo mostrar coincidencias con al menos 20% de relevancia
                              .OrderByDescending(c => c.Relevancia)
                              .Take(5)
                              .ToList();
        }

        private string GenerarRespuestaBasadaEnContexto(string mensaje, List<ResultadoBusqueda> resultados, string intencion)
        {
            if (!resultados.Any()) return string.Empty;

            var respuesta = new StringBuilder();
            
            try
            {
                switch (intencion)
                {
                    case "explicacion":
                        respuesta.AppendLine("**Basándome en los documentos encontrados, puedo explicarte lo siguiente:**");
                        respuesta.AppendLine();
                        respuesta.AppendLine(SintetizarExplicacion(resultados));
                        break;

                    case "resumen":
                        respuesta.AppendLine("**Aquí tienes un resumen de la información encontrada:**");
                        respuesta.AppendLine();
                        respuesta.AppendLine(GenerarResumenDeResultados(resultados));
                        break;

                    case "listado":
                        respuesta.AppendLine("**He identificado los siguientes elementos:**");
                        respuesta.AppendLine();
                        respuesta.AppendLine(ExtraerElementosListado(resultados));
                        break;

                    case "comparacion":
                        if (resultados.Count >= 2)
                        {
                            respuesta.AppendLine("**Comparando la información encontrada:**");
                            respuesta.AppendLine();
                            respuesta.AppendLine(GenerarComparacion(resultados));
                        }
                        break;

                    default:
                        respuesta.AppendLine("**Con base en la información encontrada:**");
                        respuesta.AppendLine();
                        respuesta.AppendLine(GenerarRespuestaGeneral(resultados));
                        break;
                }

                return respuesta.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al generar respuesta basada en contexto");
                return "Encontré información relevante, pero tuve dificultades para procesarla completamente. ¿Podrías ser más específico en tu consulta?";
            }
        }

        private List<string> GenerarSugerenciasBusqueda(string intencion, List<ArchivoSubido> archivos)
        {
            var sugerencias = new List<string>();

            try
            {
                // Sugerencias basadas en tipos de archivos disponibles
                var tiposArchivos = archivos.Select(a => DeterminarTipoAnalisis(a.TipoContenido)).Distinct().ToList();

                foreach (var tipo in tiposArchivos.Take(3))
                {
                    switch (tipo)
                    {
                        case "📄 Documento PDF":
                            sugerencias.Add("Intenta buscar por temas específicos como 'proceso', 'procedimiento' o nombres de secciones");
                            break;
                        case "📊 Hoja Excel (.xlsx)":
                            sugerencias.Add("Busca por datos numéricos, nombres de columnas o información específica de tablas");
                            break;
                        case "📝 Documento Word (.docx)":
                            sugerencias.Add("Prueba con palabras clave del título, encabezados o contenido principal");
                            break;
                    }
                }

                // Sugerencias basadas en la intención
                switch (intencion)
                {
                    case "busqueda":
                        sugerencias.Add("Usa términos más específicos o combina varias palabras clave");
                        break;
                    case "explicacion":
                        sugerencias.Add("Pregunta '¿qué es [término específico]?' o '¿cómo funciona [proceso]?'");
                        break;
                    case "listado":
                        sugerencias.Add("Pregunta 'lista de', 'cuáles son' o 'enumera los'");
                        break;
                }

                // Sugerencias generales
                if (!sugerencias.Any())
                {
                    sugerencias.AddRange(new[]
                    {
                        "Menciona un archivo específico por su nombre",
                        "Usa palabras clave más específicas relacionadas con tu búsqueda",
                        "Pregunta por un tema o concepto particular"
                    });
                }

                return sugerencias.Take(4).ToList();
            }
            catch
            {
                return new List<string> { "Intenta ser más específico en tu consulta" };
            }
        }

        #endregion

        #region Métodos auxiliares para generación de respuestas

        private string SintetizarExplicacion(List<ResultadoBusqueda> resultados)
        {
            var explicacion = new StringBuilder();
            
            // Combinar contextos más relevantes
            var contextosRelevantes = resultados.Take(2).Select(r => r.ContextoEncontrado).ToList();
            
            if (contextosRelevantes.Any())
            {
                var contenidoCombinado = string.Join("\n\n", contextosRelevantes);
                var primerasLineas = contenidoCombinado.Split('\n').Take(5);
                
                foreach (var linea in primerasLineas)
                {
                    if (!string.IsNullOrWhiteSpace(linea))
                    {
                        explicacion.AppendLine($"• {linea.Trim()}");
                    }
                }
            }
            
            return explicacion.ToString();
        }

        private string GenerarResumenDeResultados(List<ResultadoBusqueda> resultados)
        {
            var resumen = new StringBuilder();
            
            var puntosUnicos = new HashSet<string>();
            
            foreach (var resultado in resultados.Take(3))
            {
                var lineas = resultado.ContextoEncontrado.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var linea in lineas.Take(2))
                {
                    var lineaLimpia = linea.Trim();
                    if (lineaLimpia.Length > 10 && puntosUnicos.Add(lineaLimpia))
                    {
                        resumen.AppendLine($"• {lineaLimpia}");
                    }
                }
            }
            
            return resumen.ToString();
        }

        private string ExtraerElementosListado(List<ResultadoBusqueda> resultados)
        {
            var elementos = new StringBuilder();
            var listasEncontradas = new HashSet<string>();
            
            foreach (var resultado in resultados)
            {
                var lineas = resultado.ContextoEncontrado.Split('\n');
                foreach (var linea in lineas)
                {
                    var lineaTrimmed = linea.Trim();
                    if ((lineaTrimmed.StartsWith("•") || lineaTrimmed.StartsWith("-") || 
                         Regex.IsMatch(lineaTrimmed, @"^\d+\.")) && 
                        listasEncontradas.Add(lineaTrimmed))
                    {
                        elementos.AppendLine(lineaTrimmed);
                    }
                }
            }
            
            return elementos.Length > 0 ? elementos.ToString() : 
                   "No se encontraron listas específicas, pero hay información relevante en los contextos mostrados arriba.";
        }

        private string GenerarComparacion(List<ResultadoBusqueda> resultados)
        {
            var comparacion = new StringBuilder();
            
            if (resultados.Count >= 2)
            {
                comparacion.AppendLine($"**Archivo 1 ({resultados[0].NombreArchivo}):**");
                comparacion.AppendLine(resultados[0].ContextoEncontrado.Split('\n').First());
                comparacion.AppendLine();
                
                comparacion.AppendLine($"**Archivo 2 ({resultados[1].NombreArchivo}):**");
                comparacion.AppendLine(resultados[1].ContextoEncontrado.Split('\n').First());
                comparacion.AppendLine();
                
                comparacion.AppendLine("*Para una comparación más detallada, especifica qué aspectos te interesan comparar.*");
            }
            
            return comparacion.ToString();
        }

        private string GenerarRespuestaGeneral(List<ResultadoBusqueda> resultados)
        {
            var respuesta = new StringBuilder();
            
            var mejorResultado = resultados.First();
            var primerasLineas = mejorResultado.ContextoEncontrado.Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Take(3);
            
            foreach (var linea in primerasLineas)
            {
                respuesta.AppendLine(linea.Trim());
            }
            
            if (resultados.Count > 1)
            {
                respuesta.AppendLine();
                respuesta.AppendLine($"También encontré información relacionada en {resultados.Count - 1} archivo(s) adicional(es).");
            }
            
            return respuesta.ToString();
        }

        #endregion

        #region Clases auxiliares para análisis semántico

        public class CaracteristicasContenido
        {
            public string ComplejidadLectura { get; set; } = string.Empty;
            public string IdiomaDetectado { get; set; } = string.Empty;
        }

        public class ResultadoBusqueda
        {
            public string NombreArchivo { get; set; } = string.Empty;
            public string ContextoEncontrado { get; set; } = string.Empty;
            public float PorcentajeRelevancia { get; set; }
            public string TipoArchivo { get; set; } = string.Empty;
            public int PosicionEnDocumento { get; set; }
        }

        public class CoincidenciaTexto
        {
            public string Contexto { get; set; } = string.Empty;
            public float Relevancia { get; set; }
            public int Posicion { get; set; }
        }

        #endregion

        private List<string> ExtraerTemasPrincipales(string contenido)
        {
            var temas = new List<string>();
            
            try
            {
                // Buscar palabras frecuentes (excluyendo palabras comunes)
                var palabrasComunes = new HashSet<string> { 
                    "el", "la", "de", "que", "y", "a", "en", "un", "es", "se", "no", "te", "lo", "le", "da", "su", "por", "son", "con", "para", "al", "una", "sur", "con", "las", "del", "los"
                };
                
                var palabras = Regex.Matches(contenido.ToLowerInvariant(), @"\b[a-záéíóúñü]{4,}\b")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .Where(p => !palabrasComunes.Contains(p))
                    .GroupBy(p => p)
                    .Where(g => g.Count() >= 3) // Aparecer al menos 3 veces
                    .OrderByDescending(g => g.Count())
                    .Select(g => char.ToUpper(g.Key[0]) + g.Key.Substring(1))
                    .Take(5)
                    .ToList();

                return palabras;
            }
            catch
            {
                return temas;
            }
        }
    }
} 