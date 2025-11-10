using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace GYM.Models
{
    // Atributo de validación personalizado para no permitir números en el nombre
    public class NoNumbersAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;

            var texto = value.ToString()!;
            if (Regex.IsMatch(texto, @"\d"))
            {
                return new ValidationResult(ErrorMessage ?? "El nombre no puede contener números.");
            }

            return ValidationResult.Success;
        }
    }

    public class MembresiaPlan
    {
        public int MembresiaPlanId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(80, ErrorMessage = "El nombre no puede exceder {1} caracteres.")]
        [NoNumbers(ErrorMessage = "El nombre no puede contener números.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(300, ErrorMessage = "La descripción no puede exceder {1} caracteres.")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, 99999, ErrorMessage = "Error Ingrese valores positivos")]
        public decimal Precio { get; set; }

        public bool PermiteRutina { get; set; }
        public bool PermiteAlimentacion { get; set; }
        public bool Activo { get; set; } = true;

        [Required(ErrorMessage = "La duración es obligatoria.")]
        [Range(1, 365, ErrorMessage = "La duración debe ser entre 1 y 365 días.")]
        public int DuracionDias { get; set; } = 30;

        // Beneficios como texto (separados por ';' o '\n')
        [StringLength(1000)]
        public string? BeneficiosTexto { get; set; }

        // Helper para mostrar/editar como lista en vistas
        [NotMapped]
        public List<string> Beneficios
        {
            get => string.IsNullOrWhiteSpace(BeneficiosTexto)
                ? new()
                : BeneficiosTexto.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            set => BeneficiosTexto = value != null && value.Any()
                ? string.Join("; ", value.Where(b => !string.IsNullOrWhiteSpace(b)))
                : null;
        }
    }
}