using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public static class TestHelpers
{
    public static async Task<Guid> CreatePaymentAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/checkout/create-session",
            new { amount = 1000, currency = "usd" });

        response.EnsureSuccessStatusCode();

        return Guid.NewGuid(); // PaymentId отримуємо окремо
    }

    public static async Task<HttpResponseMessage> SendWebhookAsync(
        HttpClient client,
        object payload,
        string signature = "test_signature")
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("Stripe-Signature", signature);

        return await client.PostAsync("/api/webhook", content);
    }
}
