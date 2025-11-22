using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public static class DiscordWebhook
{
    public static async Task SendMessageAsync(string webhookUrl, string text)
    {
        using var client = new HttpClient();

        var json = $"{{\"content\": \"{EscapeJson(text)}\"}}";
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        await client.PostAsync(webhookUrl, data);
    }

    private static string EscapeJson(string input)
    {
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
