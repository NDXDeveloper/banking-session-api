using System.ComponentModel.DataAnnotations;

namespace BankingSessionAPI.Models.Requests;

public class CreateAccountRequest
{
    [Required(ErrorMessage = "Le prénom est requis")]
    [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom de famille est requis")]
    [MaxLength(100, ErrorMessage = "Le nom de famille ne peut pas dépasser 100 caractères")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
    [MinLength(3, ErrorMessage = "Le nom d'utilisateur doit contenir au moins 3 caractères")]
    [MaxLength(50, ErrorMessage = "Le nom d'utilisateur ne peut pas dépasser 50 caractères")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
        ErrorMessage = "Le mot de passe doit contenir au moins une minuscule, une majuscule, un chiffre et un caractère spécial")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare(nameof(Password), ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Format de téléphone invalide")]
    public string? PhoneNumber { get; set; }

    public List<string> Roles { get; set; } = new List<string> { "User" };
}