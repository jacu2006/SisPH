using System.ComponentModel.DataAnnotations;

namespace SisPH.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [DataType(DataType.Password)]
    public string Contrasena { get; set; } = string.Empty;
}

public class CambiarPasswordViewModel
{
    [Required(ErrorMessage = "Ingresa tu contraseña actual.")]
    [DataType(DataType.Password)]
    public string ClaveActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa la nueva contraseña.")]
    [MinLength(8, ErrorMessage = "Debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    public string ClaveNueva { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la nueva contraseña.")]
    [Compare(nameof(ClaveNueva), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmarClave { get; set; } = string.Empty;
}