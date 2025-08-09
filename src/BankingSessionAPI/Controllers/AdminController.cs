using BankingSessionAPI.Constants;
using BankingSessionAPI.Models.DTOs;
using BankingSessionAPI.Models.Requests;
using BankingSessionAPI.Models.Responses;
using BankingSessionAPI.Models.Entities;
using BankingSessionAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;

namespace BankingSessionAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.SuperAdmin}")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminController> _logger;
    private readonly IAuditService _auditService;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IMapper mapper,
        ILogger<AdminController> logger,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _mapper = mapper;
        _logger = logger;
        _auditService = auditService;
    }

    /// <summary>
    /// Crée un nouvel utilisateur (Admin uniquement)
    /// </summary>
    /// <param name="request">Informations du nouvel utilisateur</param>
    /// <returns>Informations de l'utilisateur créé</returns>
    [HttpPost("users")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            _logger.LogInformation("Tentative de création d'utilisateur par admin {AdminId} pour email {Email}", 
                User.FindFirst(SecurityConstants.UserIdClaim)?.Value, request.Email);

            // Vérifier si l'utilisateur existe déjà (email ou username)
            var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingUserByEmail != null)
            {
                _logger.LogWarning("Tentative de création d'utilisateur avec email existant: {Email}", request.Email);
                return Conflict(new { message = "Un utilisateur avec cet email existe déjà" });
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(request.UserName);
            if (existingUserByUsername != null)
            {
                _logger.LogWarning("Tentative de création d'utilisateur avec nom d'utilisateur existant: {UserName}", request.UserName);
                return Conflict(new { message = "Un utilisateur avec ce nom d'utilisateur existe déjà" });
            }

            // Créer le nouvel utilisateur
            var newUser = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                EmailConfirmed = request.EmailConfirmed,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                PhoneNumberConfirmed = !string.IsNullOrEmpty(request.PhoneNumber),
                TwoFactorEnabled = request.TwoFactorEnabled,
                IsActive = request.IsActive,
                Notes = request.Notes,
                Address = request.Address,
                CreatedAt = DateTime.UtcNow,
                PasswordChangedAt = DateTime.UtcNow
            };

            // Créer l'utilisateur avec le mot de passe
            var result = await _userManager.CreateAsync(newUser, request.Password);
            
            if (!result.Succeeded)
            {
                var adminId = User.FindFirst(SecurityConstants.UserIdClaim)?.Value ?? "Unknown";
                var adminName = User.Identity?.Name ?? "Unknown";
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                
                _logger.LogWarning("Échec de création d'utilisateur {Email}: {Errors}", request.Email, errors);
                
                // Audit de l'échec
                await _auditService.LogUserActionAsync(
                    AuditActions.UserCreated,
                    adminId,
                    adminName,
                    GetClientIpAddress(),
                    Request.Headers.UserAgent.ToString(),
                    GetSessionToken(),
                    new { Email = request.Email, Errors = errors, Success = false });
                
                return BadRequest(new { message = "Erreur lors de la création de l'utilisateur", errors = result.Errors.Select(e => e.Description).ToList() });
            }

            // Assigner les rôles si spécifiés
            if (request.Roles?.Any() == true)
            {
                foreach (var roleName in request.Roles)
                {
                    // Vérifier que le rôle existe
                    if (await _roleManager.RoleExistsAsync(roleName))
                    {
                        await _userManager.AddToRoleAsync(newUser, roleName);
                        _logger.LogInformation("Rôle {Role} assigné à l'utilisateur {UserId}", roleName, newUser.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Tentative d'assignation d'un rôle inexistant {Role} pour l'utilisateur {UserId}", 
                            roleName, newUser.Id);
                    }
                }
            }
            else
            {
                // Assigner le rôle User par défaut
                if (await _roleManager.RoleExistsAsync(UserRoles.User))
                {
                    await _userManager.AddToRoleAsync(newUser, UserRoles.User);
                    _logger.LogInformation("Rôle User par défaut assigné à l'utilisateur {UserId}", newUser.Id);
                }
            }

            // Récupérer l'utilisateur créé avec ses rôles
            var createdUser = await _userManager.FindByIdAsync(newUser.Id);
            var userRoles = await _userManager.GetRolesAsync(createdUser!);

            // Mapper vers le DTO de réponse  
            var userDto = _mapper.Map<UserDto>(createdUser);
            userDto.Roles = userRoles.ToList();

            // Variables pour l'audit
            var currentAdminId = User.FindFirst(SecurityConstants.UserIdClaim)?.Value ?? "Unknown";
            var currentAdminName = User.Identity?.Name ?? "Unknown";

            // Audit de succès
            await _auditService.LogUserActionAsync(
                AuditActions.UserCreated,
                currentAdminId,
                currentAdminName,
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                GetSessionToken(),
                new { 
                    Email = request.Email, 
                    UserId = newUser.Id,
                    Roles = userRoles.ToList(),
                    Success = true 
                });

            _logger.LogInformation("Utilisateur {UserId} créé avec succès par admin {AdminId}", 
                newUser.Id, currentAdminId);

            return CreatedAtAction(nameof(GetUser), new { userId = newUser.Id }, 
                new { 
                    message = "Utilisateur créé avec succès", 
                    userId = newUser.Id,
                    email = newUser.Email,
                    userName = newUser.UserName
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création d'utilisateur pour {Email}", request.Email);
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    private string GetClientIpAddress()
    {
        return Request.Headers.ContainsKey("X-Forwarded-For") 
            ? Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ?? Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            : Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string? GetSessionToken()
    {
        if (Request.Headers.ContainsKey(SecurityConstants.SessionTokenHeader))
        {
            return Request.Headers[SecurityConstants.SessionTokenHeader].FirstOrDefault();
        }
        return User.FindFirst(SecurityConstants.SessionIdClaim)?.Value;
    }

    /// <summary>
    /// Récupère les informations d'un utilisateur (Admin uniquement)
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Informations de l'utilisateur</returns>
    [HttpGet("users/{userId}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé" });
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var userDto = _mapper.Map<UserDto>(user);
            userDto.Roles = userRoles.ToList();

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de l'utilisateur {UserId}", userId);
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }

    /// <summary>
    /// Liste tous les utilisateurs (Admin uniquement)
    /// </summary>
    /// <param name="page">Numéro de page (défaut: 1)</param>
    /// <param name="pageSize">Taille de page (défaut: 10)</param>
    /// <returns>Liste paginée des utilisateurs</returns>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            // Limiter la taille de page
            pageSize = Math.Min(Math.Max(pageSize, 1), 100);
            page = Math.Max(page, 1);

            var users = _userManager.Users
                .OrderBy(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var userDtos = new List<UserDto>();
            
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var userDto = _mapper.Map<UserDto>(user);
                userDto.Roles = userRoles.ToList();
                userDtos.Add(userDto);
            }

            var totalCount = _userManager.Users.Count();
            
            var response = new
            {
                users = userDtos,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de la liste des utilisateurs");
            return StatusCode(500, new { message = "Erreur interne du serveur" });
        }
    }
}