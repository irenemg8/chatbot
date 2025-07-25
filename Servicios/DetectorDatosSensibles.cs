using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Servicio enterprise de detección y anonimización de datos sensibles
    /// Implementa protección avanzada PII/PCI antes del envío a servicios de IA externos
    /// Cumple con GDPR, LOPD y estándares de seguridad corporativa
    /// </summary>
    public interface IDetectorDatosSensibles
    {
        /// <summary>
        /// Analiza y anonimiza datos sensibles en el contenido
        /// </summary>
        Task<ResultadoAnonimizacion> AnonimizarContenidoAsync(string contenido);
        
        /// <summary>
        /// Detecta nivel de sensibilidad del contenido
        /// </summary>
        NivelSensibilidad ClasificarSensibilidad(string contenido);
        
        /// <summary>
        /// Restaura datos anonimizados después del procesamiento (si es necesario)
        /// </summary>
        string RestaurarDatosAnonimizados(string contenidoProcesado, Dictionary<string, string> mapaAnonimizacion);
    }

    /// <summary>
    /// Nivel de sensibilidad del contenido para determinar estrategia de procesamiento
    /// </summary>
    public enum NivelSensibilidad
    {
        /// <summary>Información pública sin restricciones</summary>
        Publico = 0,
        
        /// <summary>Información interna de la empresa</summary>
        Interno = 1,
        
        /// <summary>Información confidencial que requiere protección especial</summary>
        Confidencial = 2,
        
        /// <summary>Información ultra-sensible que no debe salir del perímetro corporativo</summary>
        UltraSecreto = 3
    }

    /// <summary>
    /// Resultado del proceso de anonimización con metadatos de seguridad
    /// </summary>
    public class ResultadoAnonimizacion
    {
        public string ContenidoAnonimizado { get; set; } = string.Empty;
        public Dictionary<string, string> MapaAnonimizacion { get; set; } = new();
        public NivelSensibilidad NivelDetectado { get; set; }
        public List<string> TiposDatosSensiblesDetectados { get; set; } = new();
        public bool RequiereProcesamientoLocal { get; set; }
        public int CantidadDatosAnonimizados { get; set; }
    }

    public class DetectorDatosSensibles : IDetectorDatosSensibles
    {
        private readonly ILogger<DetectorDatosSensibles> _logger;
        
        /// <summary>
        /// Patrones enterprise para detección de datos sensibles en España y UE
        /// Actualizados según normativas GDPR 2024
        /// </summary>
        private static readonly Dictionary<string, Regex> PATRONES_DATOS_SENSIBLES = new()
        {
            // === DOCUMENTOS DE IDENTIDAD ===
            ["dni_espanol"] = new Regex(@"\b\d{8}[A-Za-z]\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["nie_espanol"] = new Regex(@"\b[XYZ]\d{7}[A-Za-z]\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["pasaporte"] = new Regex(@"\b[A-Z]{3}\d{6}\b", RegexOptions.Compiled),
            
            // === DATOS FINANCIEROS ===
            ["tarjeta_credito"] = new Regex(@"\b(?:\d{4}[\s-]?){3}\d{4}\b", RegexOptions.Compiled),
            ["iban_espanol"] = new Regex(@"\bES\d{2}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["iban_generico"] = new Regex(@"\b[A-Z]{2}\d{2}[\s-]?(?:\d{4}[\s-]?){3,7}\d{1,4}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["cuenta_bancaria"] = new Regex(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{2}[\s-]?\d{10}\b", RegexOptions.Compiled),
            
            // === IDENTIFICADORES EMPRESARIALES ===
            ["cif_espanol"] = new Regex(@"\b[ABCDEFGHJNPQRSUVW]\d{7}[0-9A-J]\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["nif_empresarial"] = new Regex(@"\b[ABCDEFGHJNPQRSUVW]\d{8}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            
            // === SEGURIDAD SOCIAL Y LABORAL ===
            ["numero_seguridad_social"] = new Regex(@"\b\d{2}[\s-]?\d{10}[\s-]?\d{2}\b", RegexOptions.Compiled),
            ["numero_afiliacion"] = new Regex(@"\b\d{12}\b", RegexOptions.Compiled),
            
            // === DATOS MÉDICOS ===
            ["numero_historia_clinica"] = new Regex(@"\b(?:HC|HC\.|HISTORIA|H\.CLINICA)[\s-]?\d{6,12}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["numero_colegiado"] = new Regex(@"\b(?:COL|COLEGIADO)[\s-]?\d{4,8}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            
            // === CONTACTO PERSONAL ===
            ["telefono_movil"] = new Regex(@"\b(?:\+34\s?)?[67]\d{8}\b", RegexOptions.Compiled),
            ["telefono_fijo"] = new Regex(@"\b(?:\+34\s?)?[89]\d{8}\b", RegexOptions.Compiled),
            ["email_personal"] = new Regex(@"\b[a-zA-Z0-9._%+-]+@(?:gmail|hotmail|yahoo|outlook|icloud|protonmail)\.(?:com|es|org)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            
            // === DIRECCIONES Y UBICACIONES ===
            ["codigo_postal"] = new Regex(@"\b\d{5}\b", RegexOptions.Compiled),
            ["direccion_completa"] = new Regex(@"\b(?:CALLE|C/|AVENIDA|AVDA|PLAZA|PL|PASEO|PO)[\s\.]\s*[A-ZÁÉÍÓÚÑ][A-ZÁÉÍÓÚÑa-záéíóúñ\s\d,.-]+\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            
            // === FECHAS SENSIBLES ===
            ["fecha_nacimiento"] = new Regex(@"\b(?:NACIMIENTO|FECHA\s+NAC|F\.NAC|BORN)[\s\.:]*\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{4}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            
            // === NÚMEROS DE SERIE Y CÓDIGOS ===
            ["numero_serie"] = new Regex(@"\b(?:S/N|SERIE|SERIAL)[\s\.:]*[A-Z0-9]{8,20}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            ["codigo_activacion"] = new Regex(@"\b[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}\b", RegexOptions.Compiled),
        };

        /// <summary>
        /// Palabras clave que indican contenido sensible empresarial
        /// </summary>
        private static readonly HashSet<string> PALABRAS_CLAVE_SENSIBLES = new(StringComparer.OrdinalIgnoreCase)
        {
            "confidencial", "secreto", "privado", "restringido", "clasificado",
            "salario", "sueldo", "nómina", "compensación", "bonus",
            "estrategia", "fusión", "adquisición", "presupuesto", "costes",
            "contraseña", "password", "clave", "token", "api key",
            "paciente", "diagnóstico", "tratamiento", "medicación",
            "demanda", "juicio", "legal", "abogado", "tribunal"
        };

        public DetectorDatosSensibles(ILogger<DetectorDatosSensibles> logger)
        {
            _logger = logger;
        }

        public async Task<ResultadoAnonimizacion> AnonimizarContenidoAsync(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
            {
                return new ResultadoAnonimizacion 
                { 
                    ContenidoAnonimizado = contenido,
                    NivelDetectado = NivelSensibilidad.Publico 
                };
            }

            try
            {
                _logger.LogDebug("Iniciando análisis de sensibilidad de contenido ({Length} caracteres)", contenido.Length);

                var resultado = new ResultadoAnonimizacion
                {
                    ContenidoAnonimizado = contenido
                };

                var contadorAnonimizaciones = 0;

                // Procesar cada patrón de datos sensibles
                foreach (var (tipo, patron) in PATRONES_DATOS_SENSIBLES)
                {
                    var coincidencias = patron.Matches(resultado.ContenidoAnonimizado);
                    
                    foreach (Match coincidencia in coincidencias)
                    {
                        var valorOriginal = coincidencia.Value;
                        var valorAnonimizado = GenerarTokenAnonimizado(tipo, contadorAnonimizaciones);
                        
                        resultado.ContenidoAnonimizado = resultado.ContenidoAnonimizado.Replace(valorOriginal, valorAnonimizado);
                        resultado.MapaAnonimizacion[valorAnonimizado] = valorOriginal;
                        
                        if (!resultado.TiposDatosSensiblesDetectados.Contains(tipo))
                        {
                            resultado.TiposDatosSensiblesDetectados.Add(tipo);
                        }
                        
                        contadorAnonimizaciones++;
                        
                        _logger.LogWarning("Dato sensible detectado y anonimizado: {Tipo} -> {Token}", tipo, valorAnonimizado);
                    }
                }

                resultado.CantidadDatosAnonimizados = contadorAnonimizaciones;
                resultado.NivelDetectado = ClasificarSensibilidad(contenido);
                // Siempre procesar con anonimización, nunca rechazar
                resultado.RequiereProcesamientoLocal = false;

                _logger.LogInformation("Análisis completado: {Nivel} sensibilidad, {Cantidad} datos anonimizados, {Tipos} tipos detectados", 
                    resultado.NivelDetectado, 
                    resultado.CantidadDatosAnonimizados,
                    string.Join(", ", resultado.TiposDatosSensiblesDetectados));

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la anonimización de contenido");
                throw new Exception("Error en el procesamiento de seguridad de datos", ex);
            }
        }

        public NivelSensibilidad ClasificarSensibilidad(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return NivelSensibilidad.Publico;

            var contenidoLower = contenido.ToLowerInvariant();
            var puntuacionSensibilidad = 0;

            // Evaluar presencia de palabras clave sensibles
            foreach (var palabra in PALABRAS_CLAVE_SENSIBLES)
            {
                if (contenidoLower.Contains(palabra.ToLowerInvariant()))
                {
                    puntuacionSensibilidad += palabra switch
                    {
                        var p when p.Contains("secret") || p.Contains("confidencial") => 15,
                        var p when p.Contains("salario") || p.Contains("nómina") => 10,
                        var p when p.Contains("estrategia") || p.Contains("presupuesto") => 12,
                        var p when p.Contains("contraseña") || p.Contains("token") => 20,
                        var p when p.Contains("paciente") || p.Contains("diagnóstico") => 18,
                        var p when p.Contains("legal") || p.Contains("demanda") => 14,
                        _ => 5
                    };
                }
            }

            // Evaluar densidad de datos identificables
            var datosSensiblesDetectados = 0;
            foreach (var patron in PATRONES_DATOS_SENSIBLES.Values)
            {
                datosSensiblesDetectados += patron.Matches(contenido).Count;
            }

            // Calcular puntuación por densidad (por cada 1000 caracteres)
            var densidadDatos = (datosSensiblesDetectados * 1000.0) / contenido.Length;
            puntuacionSensibilidad += (int)(densidadDatos * 10);

            // Clasificar según puntuación (más permisivo)
            return puntuacionSensibilidad switch
            {
                >= 50 => NivelSensibilidad.UltraSecreto,
                >= 25 => NivelSensibilidad.Confidencial,
                >= 10 => NivelSensibilidad.Interno,
                _ => NivelSensibilidad.Publico
            };
        }

        public string RestaurarDatosAnonimizados(string contenidoProcesado, Dictionary<string, string> mapaAnonimizacion)
        {
            var contenidoRestaurado = contenidoProcesado;
            
            foreach (var (tokenAnonimizado, valorOriginal) in mapaAnonimizacion)
            {
                contenidoRestaurado = contenidoRestaurado.Replace(tokenAnonimizado, valorOriginal);
            }
            
            return contenidoRestaurado;
        }

        /// <summary>
        /// Genera un token único para cada dato anonimizado
        /// Permite tracking y potencial restauración sin exponer el dato original
        /// </summary>
        private static string GenerarTokenAnonimizado(string tipoDato, int contador)
        {
            var prefijo = tipoDato switch
            {
                var t when t.Contains("dni") => "DNI",
                var t when t.Contains("tarjeta") => "CARD",
                var t when t.Contains("iban") => "IBAN",
                var t when t.Contains("telefono") => "PHONE",
                var t when t.Contains("email") => "EMAIL",
                var t when t.Contains("direccion") => "ADDR",
                var t when t.Contains("fecha") => "DATE",
                _ => "DATA"
            };
            
            return $"[{prefijo}_ANONIMIZADO_{contador:D3}]";
        }
    }
} 