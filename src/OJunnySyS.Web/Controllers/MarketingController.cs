using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace OJunnySyS.Web.Controllers;

[Route("")]
public sealed class MarketingController(IConfiguration configuration) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var baseUrl = configuration["PublicBaseUrl"]?.TrimEnd('/');
        var canonical = string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl}/";
        var structuredData = JsonSerializer.Serialize(new
        {
            @context = "https://schema.org",
            @type = "SoftwareApplication",
            name = "OJunnySyS",
            applicationCategory = "BusinessApplication",
            operatingSystem = "Web, Windows, macOS, Linux, iOS, Android",
            description = "Plataforma web instalable para gestionar clientes, ventas, punto de venta, inventario, compras y reportes."
        });
        return View(new MarketingPage(canonical, structuredData));
    }

    [HttpGet("robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        var baseUrl = configuration["PublicBaseUrl"]?.TrimEnd('/');
        var sitemap = string.IsNullOrWhiteSpace(baseUrl) ? string.Empty : $"Sitemap: {baseUrl}/sitemap.xml\n";
        return Content($"User-agent: *\nAllow: /\nDisallow: /account/\nDisallow: /app/\nDisallow: /api/\nDisallow: /platform/\n{sitemap}", "text/plain", Encoding.UTF8);
    }

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Sitemap()
    {
        var baseUrl = configuration["PublicBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)) return NotFound();
        var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"><url><loc>{System.Security.SecurityElement.Escape(baseUrl + "/")}</loc><changefreq>weekly</changefreq><priority>1.0</priority></url></urlset>";
        return Content(xml, "application/xml", Encoding.UTF8);
    }
}

public sealed record MarketingPage(string? CanonicalUrl, string StructuredDataJson);
