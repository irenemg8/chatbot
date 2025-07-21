using System;
using System.ComponentModel.DataAnnotations;

namespace ChatbotGomarco.Modelos
{
    public class MensajeChat
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string IdSesionChat { get; set; } = string.Empty;

        [Required]
        public string Contenido { get; set; } = string.Empty;

        [Required]
        public TipoMensaje TipoMensaje { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public string? ArchivoAdjuntoRuta { get; set; }

        public string? NombreArchivoAdjunto { get; set; }

        public bool EsArchivoCifrado { get; set; } = false;
    }

    public enum TipoMensaje
    {
        Usuario = 0,
        Asistente = 1,
        Sistema = 2
    }
} 