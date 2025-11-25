using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
/*
 * 
 * 
 * 
 * THIS IS A TESTING CLASS. IT USES THE SAME LOGIC AS PROGRAM.CS
 * 
 * 
 * 
 */
namespace SWGOH_Raid_Scraper
{
    public class RaidScraper
    {
        public List<(string Name, string Score)> ParseRaidTable(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var rows = doc.DocumentNode.SelectNodes("//table//tbody//tr");
            if (rows == null)
                return new List<(string, string)>();

            var result = new List<(string Name, string Score)>();

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells == null || cells.Count < 3)
                    continue;

                string score = cells[1].InnerText.Trim();
                string name = cells[2].InnerText.Trim();

                result.Add((Name: name, Score: score));
            }

            return result;
        }

        public List<string> GetNonContributors(List<(string Name, string Score)> players)
        {
            return players
                .Where(p => p.Score == "--")
                .Select(p => p.Name)
                .ToList();
        }

        public string BuildDiscordMessage(List<string> nonContributors)
        {
            if (nonContributors == null || nonContributors.Count == 0)
                return "**Raid Report:**\nAll members contributed!";

            var names = string.Join("\n", nonContributors.Select(n => $"- {n}"));
            return $"**Raid - Players with no raid score:**\n{names}";
        }
    }
}
