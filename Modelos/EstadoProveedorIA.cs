using System;
using System.Collections.Generic;

namespace ChatbotGomarco.Modelos
{
    /// <summary>
    /// Representa el estado actual de un proveedor de IA
    /// Incluye información sobre disponibilidad, configuración y rendimiento
    /// </summary>
    public class EstadoProveedorIA
    {
        /// <summary>
        /// Indica si el proveedor está operativo y listo para uso
        /// </summary>
        public bool EstaDisponible { get; set; }
        
        /// <summary>
        /// Indica si la configuración es válida
        /// </summary>
        public bool ConfiguracionValida { get; set; }
        
        /// <summary>
        /// Mensaje descriptivo del estado actual
        /// </summary>
        public string MensajeEstado { get; set; } = string.Empty;
        
        /// <summary>
        /// Nivel de estado: Success, Warning, Error, Info
        /// </summary>
        public string Nivel { get; set; } = "Info";
        
        /// <summary>
        /// Versión del modelo o servicio
        /// </summary>
        public string Version { get; set; } = string.Empty;
        
        /// <summary>
        /// Información sobre el modelo cargado (para proveedores locales)
        /// </summary>
        public string ModeloCargado { get; set; } = string.Empty;
        
        /// <summary>
        /// Latencia promedio en milisegundos (si está disponible)
        /// </summary>
        public double LatenciaPromedio { get; set; }
        
        /// <summary>
        /// Timestamp de la última verificación
        /// </summary>
        public DateTime UltimaVerificacion { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Información adicional específica del proveedor
        /// </summary>
        public Dictionary<string, string> InformacionAdicional { get; set; } = new();
        
        /// <summary>
        /// Lista de capacidades soportadas por este proveedor
        /// </summary>
        public HashSet<string> CapacidadesSoportadas { get; set; } = new();
        
        /// <summary>
        /// Indica si el proveedor necesita instalación o configuración adicional
        /// </summary>
        public bool RequiereInstalacion { get; set; }
        
        /// <summary>
        /// Pasos recomendados para completar la configuración
        /// </summary>
        public List<string> PasosConfiguracion { get; set; } = new();
        
        /// <summary>
        /// Crea un estado indicando que el proveedor está disponible
        /// </summary>
        public static EstadoProveedorIA Disponible(string mensaje = "Proveedor disponible y configurado correctamente")
        {
            return new EstadoProveedorIA
            {
                EstaDisponible = true,
                ConfiguracionValida = true,
                MensajeEstado = mensaje,
                Nivel = "Success"
            };
        }
        
        /// <summary>
        /// Crea un estado indicando que el proveedor no está disponible
        /// </summary>
        public static EstadoProveedorIA NoDisponible(string mensaje, bool requiereInstalacion = false)
        {
            return new EstadoProveedorIA
            {
                EstaDisponible = false,
                ConfiguracionValida = false,
                MensajeEstado = mensaje,
                Nivel = "Error",
                RequiereInstalacion = requiereInstalacion
            };
        }
        
        /// <summary>
        /// Crea un estado de advertencia
        /// </summary>
        public static EstadoProveedorIA Advertencia(string mensaje)
        {
            return new EstadoProveedorIA
            {
                EstaDisponible = true,
                ConfiguracionValida = true,
                MensajeEstado = mensaje,
                Nivel = "Warning"
            };
        }
    }
} 