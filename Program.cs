using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

class Program
{
    //Guild’s raid-history base URL
    private const string GuildRaidBaseUrl =
        "https://swgoh.gg/g/nA8P3kQJRMyA7VqglhVOqQ/raid-history/";

    //Folder where local HTML saves are stored
    private const string HtmlFolder = "html_files";

    static async Task Main()
    {
        //Find out the project root
        string projectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..")
        );

        //Load webhook URL from .env
        string? webhookUrl = LoadWebhookUrl(projectRoot);
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            Console.WriteLine("ERROR: Could not find DISCORD_WEBHOOK_URL in .env");
            Console.WriteLine($"Expected .env at: {Path.Combine(projectRoot, ".env")}");
            return;
        }

        Console.WriteLine("Enter the raid ID (the part after /raid-history/), e.g. bb0ea6749c:");
        string raidId = Console.ReadLine()?.Trim() ?? "";
        raidId = raidId.Trim('/');

        if (string.IsNullOrWhiteSpace(raidId))
        {
            Console.WriteLine("No raid ID entered. Exiting.");
            return;
        }

        //Build full SWGOH.GG URL
        string raidUrl = GuildRaidBaseUrl + raidId + "/";

        Console.WriteLine($"\nOpening raid page in your default browser:\n{raidUrl}\n");
        Process.Start(new ProcessStartInfo
        {
            FileName = raidUrl,
            UseShellExecute = true
        });

        //Ensure html_files exists at project root
        string htmlDir = Path.Combine(projectRoot, HtmlFolder);
        if (!Directory.Exists(htmlDir))
        {
            Directory.CreateDirectory(htmlDir);
            Console.WriteLine($"Created folder: {htmlDir}");
        }

        Console.WriteLine("When the page finishes loading:");
        Console.WriteLine($"  1) Press CTRL+S in your browser");
        Console.WriteLine($"  2) Save the page as: {raidId}.html");
        Console.WriteLine($"  3) Save it into this folder:\n     {htmlDir}");
        Console.WriteLine("\nPress ENTER here once you've saved the file...");
        Console.ReadLine();

        //Expected file path: <projectRoot>/html_files/<raidId>.html
        string filePath = Path.Combine(htmlDir, raidId + ".html");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"ERROR: File not found: {filePath}");
            Console.WriteLine("Did you save it with the correct name and in the correct folder?");
            return;
        }

        Console.WriteLine("\nParsing raid HTML...");

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
            discordMessage = $"**Raid `{raidId}` Report:**\nAll members contributed!";
        }
        else
        {
            discordMessage = $"**Raid `{raidId}` - Players with no raid score:**\n{missingList}";
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

        Console.WriteLine("\nDone. Press any key to exit.");
        Console.ReadKey();
    }

    private static string? LoadWebhookUrl(string projectRoot)
    {
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
