using Microsoft.AspNetCore.Mvc;
using SG.EncryptDecrypt.API.Services;

using SG.EncryptDecrypt.Models;
using System.Text.Json;

namespace SG.EncryptDecrypt.API.Controllers;

[ApiController]
[Route("api/crypto")]
public sealed class CryptoController : ControllerBase
{
    private readonly JsonCryptoService _service;

    public CryptoController(JsonCryptoService service)
    {
        _service = service;
    }

    [HttpPost("encrypt")]
    public IActionResult Encrypt([FromBody] CryptoRequest request)
    {
        return Process(request, true);
    }

    [HttpPost("decrypt")]
    public IActionResult Decrypt([FromBody] CryptoRequest request)
    {
        return Process(request, false);
    }

    private IActionResult Process(CryptoRequest request, bool encrypt)
    {
        try
        {
            var result = _service.Process(request, encrypt);

            if (result.FailedFields.Count > 0)
            {
                return BadRequest(new
                {
                    error = encrypt
                        ? "Encryption failed."
                        : "Decryption failed. Check the key and value format.",
                    failedFields = result.FailedFields
                });
            }

            return Ok(result.Output);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                error = "Invalid JSON. Ensure property names are unique."
            });
        }
    }
}