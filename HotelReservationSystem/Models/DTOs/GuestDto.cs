using System.ComponentModel.DataAnnotations;

namespace HotelReservationSystem.Models.DTOs;

/// <summary>
/// DTO de respuesta con datos completos del huésped
/// </summary>
public class GuestDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? DocumentNumber { get; set; }
    public string? DocumentType { get; set; }
    public string? Nationality { get; set; }
    public string? Company { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PreferredLanguage { get; set; }
    public bool IsVip { get; set; }
    public string? VipStatus { get; set; }
    public bool MarketingOptIn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request para crear un nuevo huésped con validaciones de formato
/// </summary>
public class CreateGuestRequest
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido")]
    [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Ingrese una dirección de correo válida")]
    [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Ingrese un número de teléfono válido")]
    [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string? Phone { get; set; }

    [StringLength(500, ErrorMessage = "La dirección no puede exceder 500 caracteres")]
    public string? Address { get; set; }

    [StringLength(50, ErrorMessage = "El número de documento no puede exceder 50 caracteres")]
    public string? DocumentNumber { get; set; }

    [StringLength(50, ErrorMessage = "El tipo de documento no puede exceder 50 caracteres")]
    public string? DocumentType { get; set; }

    [StringLength(100, ErrorMessage = "La nacionalidad no puede exceder 100 caracteres")]
    public string? Nationality { get; set; }

    [StringLength(200, ErrorMessage = "La empresa no puede exceder 200 caracteres")]
    public string? Company { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(10, ErrorMessage = "El idioma preferido no puede exceder 10 caracteres")]
    public string? PreferredLanguage { get; set; }

    public bool MarketingOptIn { get; set; }
}

/// <summary>
/// Request para actualizar datos de un huésped existente
/// </summary>
public class UpdateGuestRequest
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido")]
    [StringLength(100, ErrorMessage = "El apellido no puede exceder 100 caracteres")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Ingrese una dirección de correo válida")]
    [StringLength(100, ErrorMessage = "El correo no puede exceder 100 caracteres")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Ingrese un número de teléfono válido")]
    [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string? Phone { get; set; }

    [StringLength(500, ErrorMessage = "La dirección no puede exceder 500 caracteres")]
    public string? Address { get; set; }

    [StringLength(50, ErrorMessage = "El número de documento no puede exceder 50 caracteres")]
    public string? DocumentNumber { get; set; }

    [StringLength(50, ErrorMessage = "El tipo de documento no puede exceder 50 caracteres")]
    public string? DocumentType { get; set; }

    [StringLength(100, ErrorMessage = "La nacionalidad no puede exceder 100 caracteres")]
    public string? Nationality { get; set; }

    [StringLength(200, ErrorMessage = "La empresa no puede exceder 200 caracteres")]
    public string? Company { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [StringLength(10, ErrorMessage = "El idioma preferido no puede exceder 10 caracteres")]
    public string? PreferredLanguage { get; set; }

    public bool IsVip { get; set; }

    [StringLength(20, ErrorMessage = "El estado VIP no puede exceder 20 caracteres")]
    public string? VipStatus { get; set; }

    public bool MarketingOptIn { get; set; }
}
