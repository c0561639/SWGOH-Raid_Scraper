using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

class Program
{
    static async Task Main()
    {

        //Find out the project root
        //change path for docker vs. local
        string baseDir = AppContext.BaseDirectory;
        string htmlDir = Path.Combine(baseDir, "html");
        string projectRoot = baseDir;

        // If html doesn't exist in the app directory, try going up to project root (local dev)
        if (!Directory.Exists(htmlDir))
        {
            projectRoot = Path.GetFullPath(
                Path.Combine(baseDir, "..", "..", "..")
            );
            htmlDir = Path.Combine(projectRoot, "html");
        }

        string filePath = Path.Combine(htmlDir, "raid1.html");

        //Load webhook URL from .env
        string? webhookUrl = LoadWebhookUrl(projectRoot);
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            Console.WriteLine("ERROR: Could not find DISCORD_WEBHOOK_URL in .env");
            Console.WriteLine($"Expected .env at: {Path.Combine(projectRoot, ".env")}");
            return;
        }

        string html = File.ReadAllText(filePath);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var rows = doc.DocumentNode.SelectNodes("//table//tbody//tr");
        if (rows == null)
        {
            Console.WriteLine("ERROR: Could not find raid results table in the HTML file.");
            return;
        }

        Console.WriteLine("\n=== Players with no raid score ===\n");

        bool foundMissing = false;
        var missingList = new StringBuilder();

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("td");
            if (cells == null || cells.Count < 3)
                continue;

            string score = cells[1].InnerText.Trim();
            string name = cells[2].InnerText.Trim();

            if (score == "--")
            {
                Console.WriteLine(name);
                missingList.AppendLine($"- {name}");
                foundMissing = true;
            }
        }

        //Build Discord message
        string discordMessage;
        if (!foundMissing)
        {
            discordMessage = $"**Raid Report:**\nAll members contributed!";
        }
        else
        {
            discordMessage = $"**Raid - Players with no raid score:**\n{missingList}";
        }

        Console.WriteLine("\nSending report to Discord...");
        try
        {
            await SendDiscordMessageAsync(webhookUrl, discordMessage);
            Console.WriteLine("Posted to Discord!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to send message to Discord:");
            Console.WriteLine(ex.Message);
        }

        Console.WriteLine("\nDone.");
    }

    private static string? LoadWebhookUrl(string projectRoot)
    {
        // check env variable for docker
        string? webhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL");
        if (!string.IsNullOrWhiteSpace(webhookUrl))
        {
            Console.WriteLine("Loaded DISCORD_WEBHOOK_URL from environment variable");
            return webhookUrl;
        }

        // use .env file for local dev
        string envPath = Path.Combine(projectRoot, ".env");

        if (!File.Exists(envPath))
        {
            Console.WriteLine($"WARNING: .env file not found at: {envPath}");
            return null;
        }

        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();

            //Skip comments and empty lines
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                continue;

            int equalsIndex = trimmed.IndexOf('=');
            if (equalsIndex <= 0)
                continue;

            string key = trimmed[..equalsIndex].Trim();
            string value = trimmed[(equalsIndex + 1)..].Trim();

            if (key.Equals("DISCORD_WEBHOOK_URL", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }

    private static async Task SendDiscordMessageAsync(string webhookUrl, string text)
    {
        using var client = new HttpClient();
        var json = $"{{\"content\": \"{EscapeJson(text)}\"}}";
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(webhookUrl, data);
        response.EnsureSuccessStatusCode();
    }

    private static string EscapeJson(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "")
            .Replace("\n", "\\n");
    }
}
