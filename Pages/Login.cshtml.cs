using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HorarioDocentes.Pages;

public class LoginModel : PageModel
{
    private readonly IConfiguration _config;

    public LoginModel(IConfiguration config)
    {
        _config = config;
    }

    [BindProperty]
    public string? Password { get; set; }

    public string? Error { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var claveCorrecta = _config["SitePassword"];

        if (string.IsNullOrWhiteSpace(claveCorrecta))
        {
            Error = "El sistema no tiene una clave de acceso configurada. Contacta al administrador.";
            return Page();
        }

        if (string.IsNullOrEmpty(Password) || Password != claveCorrecta)
        {
            Error = "Clave incorrecta.";
            return Page();
        }

        var claims = new List<Claim> { new(ClaimTypes.Name, "Usuario IE") };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }
}
