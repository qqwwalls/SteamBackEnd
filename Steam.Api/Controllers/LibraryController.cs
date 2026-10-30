using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Steam.Application.DTOs.LibraryDTOs;
using Steam.Application.Interfaces.Services;
using Steam.Application.Configuration;
using Microsoft.Extensions.Options;

namespace Steam.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LibraryController : ControllerBase
{
    private readonly ILibraryService _service;
    private readonly CheckoutSettings _checkoutSettings;

    public LibraryController(ILibraryService service, IOptions<CheckoutSettings> checkoutSettings)
    {
        _service = service;
        _checkoutSettings = checkoutSettings.Value;
    }

    [AllowAnonymous]
    [HttpGet("checkout-options")]
    public IActionResult GetCheckoutOptions() => Ok(new { demoPurchasesEnabled = _checkoutSettings.EnableDemoPurchases });

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await _service.GetAsync(ct));

    [HttpPost("acquire")]
    public async Task<IActionResult> Acquire([FromBody] AcquireGamesDTO dto, CancellationToken ct)
        => Ok(await _service.AcquireAsync(dto, ct));
}
