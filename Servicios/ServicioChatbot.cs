using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios
{
    public class ServicioChatbot : IServicioChatbot
    {
        private readonly IServicioArchivos _servicioArchivos;
        private readonly ILogger<ServicioChatbot> _logger;
        
        // Respuestas predeterminadas para el chatbot (simulación de IA)
        private readonly List<string> _respuestasSaludo = new()
        {
            "¡Hola! Soy el asistente de IA de GOMARCO. Estoy aquí para ayudarte con cualquier consulta relacionada con nuestros productos, procesos o documentos. ¿En qué puedo asistirte hoy?",
            "¡Buenos días! Bienvenido al chatbot corporativo de GOMARCO. Puedo ayudarte con consultas sobre colchones, documentos corporativos, o cualquier información que necesites. ¿Cómo puedo ayudarte?",
            "¡Hola! Soy tu asistente virtual de GOMARCO. Estoy capacitado para ayudarte con información sobre productos, políticas de la empresa y análisis de documentos. ¿Qué necesitas saber?"
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

        public ServicioChatbot(IServicioArchivos servicioArchivos, ILogger<ServicioChatbot> logger)
        {
            _servicioArchivos = servicioArchivos;
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
                
                // Verificar si hay archivos de contexto
                if (archivosContexto?.Any() == true)
                {
                    var resumenArchivos = await ProcesarMultiplesArchivosAsync(archivosContexto);
                    return GenerarRespuestaConContexto(mensaje, resumenArchivos);
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
                _logger.LogInformation("Analizando archivo: {Nombre}", archivo.NombreOriginal);

                // Obtener archivo temporal para análisis
                var rutaTemporal = await _servicioArchivos.DescargarArchivoTemporalAsync(archivo.Id);
                
                // Simular análisis del archivo basado en su tipo
                var tipoAnalisis = DeterminarTipoAnalisis(archivo.TipoContenido);
                await Task.Delay(2000); // Simular procesamiento

                var resultado = new StringBuilder();
                resultado.AppendLine($"📄 **Análisis del archivo: {archivo.NombreOriginal}**");
                resultado.AppendLine($"📅 Fecha de subida: {archivo.FechaSubida:dd/MM/yyyy HH:mm}");
                resultado.AppendLine($"📊 Tamaño: {FormatearTamaño(archivo.TamañoOriginal)}");
                resultado.AppendLine($"🔍 Tipo: {tipoAnalisis}");
                resultado.AppendLine();

                // Agregar análisis específico según tipo de archivo
                resultado.AppendLine(GenerarAnalisisEspecifico(archivo.TipoContenido, archivo.NombreOriginal));

                // Limpiar archivo temporal
                if (File.Exists(rutaTemporal))
                    File.Delete(rutaTemporal);

                return resultado.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar archivo: {Id}", archivo.Id);
                return $"❌ Error al analizar el archivo {archivo.NombreOriginal}. Por favor, verifica que el archivo no esté corrupto.";
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
                
                // Verificar tamaño máximo (100MB)
                if (infoArchivo.Length > 100 * 1024 * 1024)
                    return false;

                // Verificar extensiones permitidas
                var extensionesPermitidas = new[]
                {
                    ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
                    ".txt", ".csv", ".json", ".xml", ".jpg", ".jpeg", ".png", ".gif", ".bmp"
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
                        "Necesito ayuda con un documento",
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
                else if (contenido.Contains("documento"))
                {
                    sugerencias.AddRange(new[]
                    {
                        "¿Puedes resumir los puntos clave?",
                        "¿Hay información específica que deba revisar?",
                        "Ayúdame a entender este proceso"
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
                var resultado = new StringBuilder();
                resultado.AppendLine($"📁 **Procesando {archivos.Count} archivo(s) como contexto:**");
                resultado.AppendLine();

                foreach (var archivo in archivos.Take(5)) // Limitar a 5 archivos
                {
                    var resumen = await AnalizarArchivoAsync(archivo);
                    resultado.AppendLine(resumen);
                    resultado.AppendLine("---");
                }

                if (archivos.Count > 5)
                {
                    resultado.AppendLine($"⚠️ Se han procesado solo los primeros 5 archivos de {archivos.Count} total.");
                }

                return resultado.ToString();
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
                "application/pdf" => "Documento PDF",
                "application/msword" or "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "Documento de Word",
                "application/vnd.ms-excel" or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "Hoja de Cálculo",
                "application/vnd.ms-powerpoint" or "application/vnd.openxmlformats-officedocument.presentationml.presentation" => "Presentación",
                "text/plain" => "Archivo de Texto",
                "text/csv" => "Datos CSV",
                "application/json" => "Datos JSON",
                "image/jpeg" or "image/png" or "image/gif" or "image/bmp" => "Imagen",
                _ => "Archivo de Datos"
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
    }
} 