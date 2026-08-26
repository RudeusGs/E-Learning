using Microsoft.AspNetCore.Mvc;

namespace Elearning.IntegrationTests;

[ApiController]
[Route("integration/unannotated")]
public sealed class UnannotatedTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
