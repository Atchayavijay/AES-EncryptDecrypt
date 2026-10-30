using Microsoft.AspNetCore.Mvc;
using SG.EncryptDecrypt.Models;
using System.Net.Http.Json;

namespace SG.EncryptDecrypt.IAPI.Controllers;

[ApiController]
[Route("api/crypto")]
public sealed class CryptoController : ControllerBase
{
    private readonly IHttpClientFactory _factory;

    public CryptoController(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    [HttpPost("encrypt")]
    public Task<IActionResult> Encrypt(
        [FromBody] CryptoRequest request,
        CancellationToken cancellationToken)
    {
        return Forward("encrypt", request, cancellationToken);
    }

    [HttpPost("decrypt")]
    public Task<IActionResult> Decrypt(
        [FromBody] CryptoRequest request,
        CancellationToken cancellationToken)
    {
        return Forward("decrypt", request, cancellationToken);
    }

    private async Task<IActionResult> Forward(
        string operation,
        CryptoRequest request,
        CancellationToken cancellationToken)
    {
        var client = _factory.CreateClient("CryptoApi");

        using var message = new HttpRequestMessage(
            HttpMethod.Post, $"api/crypto/{operation}")
        {
            Content = JsonContent.Create(request)
        };

        if (Request.Headers.TryGetValue(
                "Authorization", out var authorization))
        {
            message.Headers.TryAddWithoutValidation(
                "Authorization", authorization.ToArray());
        }

        try
        {
            using var response = await client.SendAsync(
                message, cancellationToken);

            string body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                ContentType =
                    response.Content.Headers.ContentType?.ToString()
                    ?? "application/json",
                Content = body
            };
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new
            {
                error = "Crypto API is unavailable."
            });
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                error = "Crypto API request timed out."
            });
        }
    }
}