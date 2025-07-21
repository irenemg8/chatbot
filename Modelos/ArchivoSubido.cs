using System;
using System.ComponentModel.DataAnnotations;

namespace ChatbotGomarco.Modelos
{
    public class ArchivoSubido
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string IdSesionChat { get; set; } = string.Empty;

        [Required]
        public string NombreOriginal { get; set; } = string.Empty;

        [Required]
        public string RutaArchivoCifrado { get; set; } = string.Empty;

        [Required]
        public string HashSha256 { get; set; } = string.Empty;

        [Required]
        public long TamañoOriginal { get; set; }

        [Required]
        public DateTime FechaSubida { get; set; } = DateTime.Now;

        [Required]
        public string TipoContenido { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        // Metadatos de cifrado (IV almacenado por separado por seguridad)
        public string VectorInicializacion { get; set; } = string.Empty;

        public bool EstaCifrado { get; set; } = true;

        // Navegación
        public virtual SesionChat? SesionChat { get; set; }
    }
} 