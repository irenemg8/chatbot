using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Datos
{
    public class ContextoBaseDatos : DbContext
    {
        private readonly ILogger<ContextoBaseDatos> _logger;
        private static readonly string RutaBaseDatos = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GOMARCO",
            "ChatbotGomarco",
            "chatbot.db"
        );

        public ContextoBaseDatos(ILogger<ContextoBaseDatos> logger)
        {
            _logger = logger;
        }

        public DbSet<SesionChat> SesionesChat { get; set; }
        public DbSet<MensajeChat> MensajesChat { get; set; }
        public DbSet<ArchivoSubido> ArchivosSubidos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Crear directorio si no existe
            var directorio = Path.GetDirectoryName(RutaBaseDatos);
            if (!Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio!);
            }

            optionsBuilder
                .UseSqlite($"Data Source={RutaBaseDatos}")
                .EnableSensitiveDataLogging(false)
                .LogTo(mensaje => _logger.LogDebug(mensaje));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de SesionChat
            modelBuilder.Entity<SesionChat>(entidad =>
            {
                entidad.HasKey(s => s.Id);
                entidad.Property(s => s.Titulo).HasMaxLength(200);
                entidad.HasIndex(s => s.FechaCreacion);
                entidad.HasIndex(s => s.FechaUltimaActividad);
            });

            // Configuración de MensajeChat
            modelBuilder.Entity<MensajeChat>(entidad =>
            {
                entidad.HasKey(m => m.Id);
                entidad.Property(m => m.Contenido).HasMaxLength(10000);
                entidad.Property(m => m.NombreArchivoAdjunto).HasMaxLength(500);
                entidad.Property(m => m.ArchivoAdjuntoRuta).HasMaxLength(1000);
                entidad.HasIndex(m => m.IdSesionChat);
                entidad.HasIndex(m => m.FechaCreacion);
                
                // Relación con SesionChat
                entidad.HasOne<SesionChat>()
                      .WithMany(s => s.Mensajes)
                      .HasForeignKey(m => m.IdSesionChat)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de ArchivoSubido
            modelBuilder.Entity<ArchivoSubido>(entidad =>
            {
                entidad.HasKey(a => a.Id);
                entidad.Property(a => a.NombreOriginal).HasMaxLength(500);
                entidad.Property(a => a.RutaArchivoCifrado).HasMaxLength(1000);
                entidad.Property(a => a.HashSha256).HasMaxLength(64);
                entidad.Property(a => a.TipoContenido).HasMaxLength(100);
                entidad.Property(a => a.Descripcion).HasMaxLength(1000);
                entidad.Property(a => a.VectorInicializacion).HasMaxLength(500);
                entidad.HasIndex(a => a.IdSesionChat);
                entidad.HasIndex(a => a.FechaSubida);
                entidad.HasIndex(a => a.HashSha256);

                // Relación con SesionChat
                entidad.HasOne(a => a.SesionChat)
                      .WithMany(s => s.ArchivosSubidos)
                      .HasForeignKey(a => a.IdSesionChat)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public void InicializarBaseDatos()
        {
            try
            {
                _logger.LogInformation("Inicializando base de datos en: {RutaBaseDatos}", RutaBaseDatos);
                Database.EnsureCreated();
                _logger.LogInformation("Base de datos inicializada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al inicializar la base de datos");
                throw;
            }
        }
    }
} 