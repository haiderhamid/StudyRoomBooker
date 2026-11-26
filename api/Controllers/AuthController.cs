using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AuthUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(UserManager<AuthUser> userManager, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        // Login accepts username or email, so a username can't be another user's email,
        // and the other way round. Otherwise login could find the wrong account.
        if (await _userManager.FindByEmailAsync(registerDto.Username) != null)
        {
            ModelState.AddModelError("Username", $"The username {registerDto.Username} is already taken.");
        }
        if (await _userManager.FindByNameAsync(registerDto.Email) != null)
        {
            ModelState.AddModelError("Email", $"Email '{registerDto.Email}' is already taken.");
        }
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("[AuthController] User registration failed for {Username}, the username or email is used by another account.", registerDto.Username);
            return ValidationProblem(ModelState);
        }

        var user = new AuthUser { UserName = registerDto.Username, Email = registerDto.Email };
        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (result.Succeeded)
        {
            // New users are always students.
            await _userManager.AddToRoleAsync(user, "Student");
            _logger.LogInformation("[AuthController] User {Username} registered successfully.", registerDto.Username);
            return Ok(new { Message = "User registered successfully" });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }
        _logger.LogWarning("[AuthController] User registration failed for {Username}.", registerDto.Username);
        return ValidationProblem(ModelState);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var user = await _userManager.FindByNameAsync(loginDto.Username)
            ?? await _userManager.FindByEmailAsync(loginDto.Username);

        // 404, so React can show a link to register
        if (user == null)
        {
            _logger.LogWarning("[AuthController] Login attempt for unknown user {Username}.", loginDto.Username);
            return NotFound("This username or email is not registered.");
        }

        if (await _userManager.CheckPasswordAsync(user, loginDto.Password))
        {
            _logger.LogInformation("[AuthController] User {Username} logged in.", loginDto.Username);
            var token = await GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        _logger.LogWarning("[AuthController] Failed login attempt for {Username}.", loginDto.Username);
        return Unauthorized("Wrong password. Please try again.");
    }

    // Token with the username and roles, valid for 2 hours
    private async Task<string> GenerateJwtToken(AuthUser user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            _logger.LogError("[AuthController] JWT key is missing from configuration.");
            throw new InvalidOperationException("JWT key is missing from configuration.");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Needed for [Authorize(Roles = "Admin")]
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        _logger.LogInformation("[AuthController] JWT token created for {Username}.", user.UserName);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}