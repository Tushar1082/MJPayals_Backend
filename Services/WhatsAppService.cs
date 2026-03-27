using Microsoft.Extensions.Configuration;

public interface IWhatsAppService
{
    Task SendInvoiceMessage(string phone, string message);
}

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public WhatsAppService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["WhatsApp:ApiKey"] ?? throw new Exception("WhatsApp ApiKey missing");
        _baseUrl = config["WhatsApp:BaseUrl"] ?? throw new Exception("WhatsApp BaseUrl missing");
    }

    public async Task SendInvoiceMessage(string phone, string message)
    {
        var encodedMessage = Uri.EscapeDataString(message);
        var url = $"{_baseUrl}?key={_apiKey}&mob=91+{phone}&msg={encodedMessage}";

        // 🔍 DEBUG: print full URL
        //Console.WriteLine("=== WhatsApp Debug ===");
        //Console.WriteLine($"URL: {url}");

        var response = await _http.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();

        // 🔍 DEBUG: print raw response
        //Console.WriteLine($"Status Code: {response.StatusCode}");
        //Console.WriteLine($"Status Code: {(int)response.StatusCode} {response.StatusCode}");
        //Console.WriteLine($"Raw Response: {json}");
        //Console.WriteLine("======================");

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var status = doc.RootElement.GetProperty("status").GetString();

        if (status == "error")
        {
            var msg = doc.RootElement.TryGetProperty("message", out var m)
                ? m.GetString()
                : "Unknown error";
            throw new Exception($"WhatsApp provider error: {msg}");
        }
    }

}