using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace Focus.Admin.Pages.ApiLab;

public class IndexModel : PageModel
{
    private readonly IConfiguration _configuration;

    public IndexModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string WebApiBaseUrl { get; set; } = "http://localhost:5000";

    public void OnGet()
    {
        WebApiBaseUrl = _configuration["WebApiBaseUrl"] ?? "http://localhost:5000";
    }
}
