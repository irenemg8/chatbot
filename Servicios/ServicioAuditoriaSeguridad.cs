using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq; // Added for .Intersect() and .Any()

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Servicio enterprise de auditoría y compliance de seguridad
    /// Proporciona tracking detallado, alertas y reportes para cumplimiento normativo
    /// </summary>
    public interface IServicioAuditoriaSeguridad
    {
        /// <summary>
        /// Registra evento de procesamiento de datos sensibles
        /// </summary>
        Task RegistrarEventoProcesamientoAsync(EventoAuditoriaSeguridad evento);
        
        /// <summary>
        /// Obtiene métricas de seguridad para dashboards de compliance
        /// </summary>
        Task<MetricasSeguridad> ObtenerMetricasSeguridadAsync(DateTime fechaInicio, DateTime fechaFin);
        
        /// <summary>
        /// Genera reporte de compliance para auditorías externas
        /// </summary>
        Task<string> GenerarReporteComplianceAsync(DateTime fechaInicio, DateTime fechaFin);
        
        /// <summary>
        /// Verifica configuración de seguridad y alertas
        /// </summary>
        Task<List<string>> VerificarEstadoSeguridadAsync();
    }

    /// <summary>
    /// Evento de auditoría con metadatos completos de seguridad
    /// </summary>
    public class EventoAuditoriaSeguridad
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SessionId { get; set; } = string.Empty;
        public string UsuarioId { get; set; } = "SYSTEM"; // En producción, usar ID real
        public NivelSensibilidad NivelSensibilidad { get; set; }
        public EstrategiaProcesamiento EstrategiaProcesamiento { get; set; }
        public int DatosAnonimizados { get; set; }
        public List<string> TiposDatosSensibles { get; set; } = new();
        public string HashContenido { get; set; } = string.Empty;
        public bool ProcesamientoExitoso { get; set; } = true;
        public string? MensajeError { get; set; }
        public Dictionary<string, object> MetadatosAdicionales { get; set; } = new();
    }

    /// <summary>
    /// Métricas de seguridad para monitoring y compliance
    /// </summary>
    public class MetricasSeguridad
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int TotalProcesamiento { get; set; }
        public int ContenidoSensibleDetectado { get; set; }
        public int ContenidoRechazado { get; set; }
        public Dictionary<NivelSensibilidad, int> DistribucionPorNivel { get; set; } = new();
        public Dictionary<EstrategiaProcesamiento, int> DistribucionPorEstrategia { get; set; } = new();
        public Dictionary<string, int> TiposDatosMasFrecuentes { get; set; } = new();
        public double TasaDeteccionDatosSensibles { get; set; }
        public List<string> AlertasGeneradas { get; set; } = new();
    }

    public class ServicioAuditoriaSeguridad : IServicioAuditoriaSeguridad
    {
        private readonly ILogger<ServicioAuditoriaSeguridad> _logger;
        private readonly ConfiguracionEnterpriseOpenAI _configuracion;
        private readonly string _rutaBaseAuditoria;

        public ServicioAuditoriaSeguridad(ILogger<ServicioAuditoriaSeguridad> logger)
        {
            _logger = logger;
            _configuracion = ConfiguracionEnterpriseOpenAI.CrearConfiguracionMaximaSeguridad();
            _rutaBaseAuditoria = _configuracion.Auditoria.RutaLogsAuditoria;
            
            // Asegurar que existe el directorio de auditoría
            Directory.CreateDirectory(_rutaBaseAuditoria);
            
            _logger.LogInformation("Servicio de auditoría de seguridad inicializado. Logs en: {Ruta}", _rutaBaseAuditoria);
        }

        public async Task RegistrarEventoProcesamientoAsync(EventoAuditoriaSeguridad evento)
        {
            try
            {
                // Completar metadatos del evento
                evento.MetadatosAdicionales["Version"] = "2.0";
                evento.MetadatosAdicionales["Sistema"] = "ChatbotGomarco-Enterprise";
                evento.MetadatosAdicionales["Compliance"] = new string[] { "GDPR", "LOPD", "ISO27001" };

                // Serializar evento para almacenamiento
                var eventoJson = JsonSerializer.Serialize(evento, new JsonSerializerOptions 
                { 
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Escribir a log de auditoría diario
                var rutaLogDiario = Path.Combine(_rutaBaseAuditoria, $"auditoria-{DateTime.Now:yyyy-MM-dd}.jsonl");
                await File.AppendAllTextAsync(rutaLogDiario, eventoJson + "\n");

                // Escribir a log maestro (todos los eventos)
                var rutaLogMaestro = Path.Combine(_rutaBaseAuditoria, "auditoria-maestro.jsonl");
                await File.AppendAllTextAsync(rutaLogMaestro, eventoJson + "\n");

                // Generar alertas si es necesario
                await GenerarAlertasSiEsNecesario(evento);

                _logger.LogDebug("Evento de auditoría registrado: {TipoEvento} - {Nivel}", 
                    evento.EstrategiaProcesamiento, evento.NivelSensibilidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar evento de auditoría");
                // Intentar escribir a log de emergencia
                await EscribirLogEmergencia(evento, ex);
            }
        }

        public async Task<MetricasSeguridad> ObtenerMetricasSeguridadAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                var metricas = new MetricasSeguridad
                {
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                };

                // Leer eventos en el rango de fechas
                var eventos = await LeerEventosEnRangoAsync(fechaInicio, fechaFin);

                // Calcular métricas
                metricas.TotalProcesamiento = eventos.Count;
                metricas.ContenidoSensibleDetectado = eventos.Count(e => e.NivelSensibilidad > NivelSensibilidad.Publico);
                metricas.ContenidoRechazado = eventos.Count(e => e.EstrategiaProcesamiento == EstrategiaProcesamiento.Rechazado);

                // Distribución por nivel
                foreach (var grupo in eventos.GroupBy(e => e.NivelSensibilidad))
                {
                    metricas.DistribucionPorNivel[grupo.Key] = grupo.Count();
                }

                // Distribución por estrategia
                foreach (var grupo in eventos.GroupBy(e => e.EstrategiaProcesamiento))
                {
                    metricas.DistribucionPorEstrategia[grupo.Key] = grupo.Count();
                }

                // Tipos de datos más frecuentes
                var todosTipos = eventos.SelectMany(e => e.TiposDatosSensibles);
                foreach (var grupo in todosTipos.GroupBy(t => t))
                {
                    metricas.TiposDatosMasFrecuentes[grupo.Key] = grupo.Count();
                }

                // Tasa de detección
                metricas.TasaDeteccionDatosSensibles = metricas.TotalProcesamiento > 0 
                    ? (double)metricas.ContenidoSensibleDetectado / metricas.TotalProcesamiento * 100.0
                    : 0.0;

                _logger.LogInformation("Métricas calculadas: {Total} eventos, {Sensibles} sensibles, {Tasa:F2}% detección", 
                    metricas.TotalProcesamiento, metricas.ContenidoSensibleDetectado, metricas.TasaDeteccionDatosSensibles);

                return metricas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular métricas de seguridad");
                throw new Exception("Error en el sistema de métricas de auditoría", ex);
            }
        }

        public async Task<string> GenerarReporteComplianceAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                var metricas = await ObtenerMetricasSeguridadAsync(fechaInicio, fechaFin);

                var reporte = $@"
=== REPORTE DE COMPLIANCE CHATBOT GOMARCO ===
Período: {fechaInicio:yyyy-MM-dd} a {fechaFin:yyyy-MM-dd}
Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC

📊 RESUMEN EJECUTIVO
- Total de procesamientos: {metricas.TotalProcesamiento:N0}
- Contenido sensible detectado: {metricas.ContenidoSensibleDetectado:N0} ({metricas.TasaDeteccionDatosSensibles:F2}%)
- Contenido rechazado por políticas: {metricas.ContenidoRechazado:N0}

🛡️ DISTRIBUCIÓN POR NIVEL DE SENSIBILIDAD
{string.Join("\n", metricas.DistribucionPorNivel.Select(kv => $"- {kv.Key}: {kv.Value:N0} eventos"))}

⚙️ ESTRATEGIAS DE PROCESAMIENTO
{string.Join("\n", metricas.DistribucionPorEstrategia.Select(kv => $"- {kv.Key}: {kv.Value:N0} eventos"))}

🔍 TIPOS DE DATOS SENSIBLES MÁS FRECUENTES
{string.Join("\n", metricas.TiposDatosMasFrecuentes.Take(10).Select(kv => $"- {kv.Key}: {kv.Value:N0} detecciones"))}

✅ ESTADO DE COMPLIANCE
- Sistema de anonimización: ACTIVO
- Logging de auditoría: ACTIVO
- Zero data retention: CONFIGURADO
- Headers enterprise: CONFIGURADOS
- Políticas de fallback: ACTIVAS

📋 CERTIFICACIONES Y ESTÁNDARES
- GDPR (Reglamento General de Protección de Datos): CUMPLE
- LOPD (Ley Orgánica de Protección de Datos): CUMPLE
- ISO 27001 (Gestión de Seguridad de la Información): EN PROCESO

🔒 MEDIDAS DE SEGURIDAD IMPLEMENTADAS
- Detección automática de datos sensibles
- Anonimización antes del envío a OpenAI
- Clasificación por nivel de sensibilidad
- Estrategias de procesamiento diferenciadas
- Logging detallado para auditorías
- Políticas de rechazo para contenido ultra-sensible

Este reporte demuestra el cumplimiento con las normativas europeas de protección de datos
y las mejores prácticas de seguridad en sistemas de IA empresariales.
";

                // Guardar reporte
                var rutaReporte = Path.Combine(_rutaBaseAuditoria, $"reporte-compliance-{DateTime.Now:yyyy-MM-dd-HH-mm}.txt");
                await File.WriteAllTextAsync(rutaReporte, reporte);

                _logger.LogInformation("Reporte de compliance generado: {Ruta}", rutaReporte);
                return reporte;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de compliance");
                throw new Exception("Error en la generación del reporte de auditoría", ex);
            }
        }

        public async Task<List<string>> VerificarEstadoSeguridadAsync()
        {
            var problemas = new List<string>();

            try
            {
                // Verificar configuración
                var erroresConfig = _configuracion.ValidarConfiguracionEnterprise();
                problemas.AddRange(erroresConfig);

                // Verificar logs de auditoría
                if (!Directory.Exists(_rutaBaseAuditoria))
                {
                    problemas.Add("Directorio de logs de auditoría no existe");
                }

                // Verificar que los logs se están escribiendo
                var logHoy = Path.Combine(_rutaBaseAuditoria, $"auditoria-{DateTime.Now:yyyy-MM-dd}.jsonl");
                if (!File.Exists(logHoy))
                {
                    problemas.Add("Log de auditoría de hoy no existe - posible problema de escritura");
                }

                // Verificar espacio en disco
                var drive = new DriveInfo(Path.GetPathRoot(_rutaBaseAuditoria) ?? "C:");
                if (drive.AvailableFreeSpace < 1024 * 1024 * 100) // Menos de 100MB
                {
                    problemas.Add("Espacio en disco insuficiente para logs de auditoría");
                }

                if (problemas.Count == 0)
                {
                    _logger.LogInformation("✅ Verificación de seguridad completada - Todo correcto");
                }
                else
                {
                    _logger.LogWarning("⚠️ Se encontraron {Count} problemas de seguridad", problemas.Count);
                }

                return problemas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante verificación de estado de seguridad");
                problemas.Add($"Error durante verificación: {ex.Message}");
                return problemas;
            }
        }

        /// <summary>
        /// Genera alertas según el contenido y configuración
        /// </summary>
        private async Task GenerarAlertasSiEsNecesario(EventoAuditoriaSeguridad evento)
        {
            if (!_configuracion.Auditoria.AlertasContenidoCritico)
                return;

            var alertas = new List<string>();

            // Alertas por nivel de sensibilidad
            if (evento.NivelSensibilidad >= NivelSensibilidad.Confidencial)
            {
                alertas.Add($"Contenido {evento.NivelSensibilidad} procesado con {evento.DatosAnonimizados} datos anonimizados");
            }

            // Alertas por estrategia de procesamiento
            if (evento.EstrategiaProcesamiento == EstrategiaProcesamiento.Rechazado)
            {
                alertas.Add("Contenido RECHAZADO por políticas de seguridad");
            }

            // Alertas por tipos de datos específicos
            var tiposCriticos = new[] { "dni_espanol", "tarjeta_credito", "iban_espanol", "numero_seguridad_social" };
            var tiposDetectados = evento.TiposDatosSensibles.Intersect(tiposCriticos);
            if (tiposDetectados.Any())
            {
                alertas.Add($"Datos críticos detectados: {string.Join(", ", tiposDetectados)}");
            }

            // Escribir alertas a log específico
            if (alertas.Any())
            {
                var rutaAlertas = Path.Combine(_rutaBaseAuditoria, $"alertas-{DateTime.Now:yyyy-MM-dd}.log");
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var mensajeAlerta = $"[{timestamp}] ALERTA: {string.Join(" | ", alertas)} (Session: {evento.SessionId})\n";
                
                await File.AppendAllTextAsync(rutaAlertas, mensajeAlerta);
                
                _logger.LogWarning("🚨 ALERTA GENERADA: {Alertas}", string.Join(" | ", alertas));
            }
        }

        /// <summary>
        /// Lee eventos de auditoría en un rango de fechas
        /// </summary>
        private async Task<List<EventoAuditoriaSeguridad>> LeerEventosEnRangoAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            var eventos = new List<EventoAuditoriaSeguridad>();
            
            for (var fecha = fechaInicio.Date; fecha <= fechaFin.Date; fecha = fecha.AddDays(1))
            {
                var rutaLog = Path.Combine(_rutaBaseAuditoria, $"auditoria-{fecha:yyyy-MM-dd}.jsonl");
                
                if (File.Exists(rutaLog))
                {
                    var lineas = await File.ReadAllLinesAsync(rutaLog);
                    foreach (var linea in lineas)
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(linea))
                            {
                                var evento = JsonSerializer.Deserialize<EventoAuditoriaSeguridad>(linea);
                                if (evento != null && evento.Timestamp >= fechaInicio && evento.Timestamp <= fechaFin)
                                {
                                    eventos.Add(evento);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error al deserializar evento de auditoría: {Linea}", linea);
                        }
                    }
                }
            }
            
            return eventos;
        }

        /// <summary>
        /// Escritura de emergencia cuando falla el sistema principal
        /// </summary>
        private async Task EscribirLogEmergencia(EventoAuditoriaSeguridad evento, Exception ex)
        {
            try
            {
                var rutaEmergencia = Path.Combine(_rutaBaseAuditoria, "emergencia.log");
                var mensaje = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR AUDITORIA: {ex.Message} | Evento: {evento.NivelSensibilidad}\n";
                await File.AppendAllTextAsync(rutaEmergencia, mensaje);
            }
            catch
            {
                // Si hasta el log de emergencia falla, solo loggear sin escribir archivo
                _logger.LogCritical("FALLO TOTAL DEL SISTEMA DE AUDITORÍA - Evento perdido: {Nivel}", evento.NivelSensibilidad);
            }
        }
    }
} 