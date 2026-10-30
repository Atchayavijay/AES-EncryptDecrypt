
using SG.EncryptDecrypt.Models;
using SG.LoyaltyApps.API.Common;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SG.EncryptDecrypt.API.Services;

public sealed class JsonCryptoService
{
    private static readonly HashSet<string> SkipFields =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ENK_Key",
            "ENK_oldapikey",
            "ENK_olduikey"
        };

    public (JsonNode? Output, List<string> FailedFields) Process(
        CryptoRequest request,
        bool encrypt)
    {
        if (request.Input.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Input must be a JSON object.");

        // Team service requires the request key in its constructor.
        // Validate once before processing any fields.
        ValidateKey(request.Key);

        IEnService crypto = new AesGcmEnService(request.Key);

        var skippedSections = new HashSet<string>(
            request.SkipSections ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        var failedFields = new List<string>();
        var input = JsonNode.Parse(request.Input.GetRawText())!;

        var output = Transform(
            input,
            crypto,
            encrypt,
            "$",
            skippedSections,
            failedFields);

        return (output, failedFields);
    }

    private static void ValidateKey(string base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
            throw new ArgumentException("Key is required.");

        byte[] key;

        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Key must be valid Base64.");
        }

        try
        {
            if (key.Length != 32)
                throw new ArgumentException(
                    "Key must decode to exactly 32 bytes.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static JsonNode? Transform(
        JsonNode? node,
        IEnService crypto,
        bool encrypt,
        string path,
        HashSet<string> skippedSections,
        List<string> failedFields)
    {
        if (node is JsonObject obj)
        {
            var result = new JsonObject();

            foreach (var property in obj)
            {
                string fieldPath =
                    $"{path}[{JsonSerializer.Serialize(property.Key)}]";

                bool skip = SkipFields.Contains(property.Key)
                    || (path == "$"
                        && skippedSections.Contains(property.Key));

                result[property.Key] = skip
                    ? property.Value?.DeepClone()
                    : Transform(
                        property.Value,
                        crypto,
                        encrypt,
                        fieldPath,
                        skippedSections,
                        failedFields);
            }

            return result;
        }

        if (node is JsonArray array)
        {
            var result = new JsonArray();

            for (int i = 0; i < array.Count; i++)
            {
                result.Add(Transform(
                    array[i],
                    crypto,
                    encrypt,
                    $"{path}[{i}]",
                    skippedSections,
                    failedFields));
            }

            return result;
        }

        if (node is JsonValue value
            && value.TryGetValue<string>(out var text))
        {
            if (string.IsNullOrEmpty(text) || text == "*")
                return JsonValue.Create(text);

            // Previous AES format needs the previous service.
            // Report it instead of returning it as decrypted data.
            if (!encrypt &&
                text.StartsWith("gcm:v1:", StringComparison.Ordinal))
            {
                failedFields.Add(path);
                return null;
            }

            // Preserve existing plaintext fields.
            if (!encrypt &&
                !text.StartsWith("v1.", StringComparison.Ordinal))
            {
                return JsonValue.Create(text);
            }

            try
            {
                return JsonValue.Create(encrypt
                    ? crypto.ecr(text)
                    : crypto.dcr(text));
            }
            catch (CryptographicException)
            {
                failedFields.Add(path);
                return null;
            }
            catch (FormatException)
            {
                failedFields.Add(path);
                return null;
            }
        }

        return node?.DeepClone();
    }
}