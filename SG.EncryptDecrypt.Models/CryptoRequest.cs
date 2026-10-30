using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SG.EncryptDecrypt.Models;

public sealed class CryptoRequest
{
    [Required]
    public string Key { get; set; } = string.Empty;

    public JsonElement Input { get; set; }

    public string[] SkipSections { get; set; } =
        new[] { "Logging", "ENK" };
}