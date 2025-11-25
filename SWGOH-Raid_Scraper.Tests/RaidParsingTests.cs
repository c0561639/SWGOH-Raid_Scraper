using System;
using System.IO;
using Xunit;
using SWGOH_Raid_Scraper;

namespace SWGOH_Raid_Scraper.Tests
{
    public class RaidParsingTests
    {
        private string LoadSampleHtml()
        {
            var baseDir = AppContext.BaseDirectory;
            var path = Path.Combine(baseDir, "TestData", "sample_raid.html");

            Assert.True(File.Exists(path), $"Missing test file: {path}");

            return File.ReadAllText(path);
        }

        [Fact]
        public void ParseRaidTable_ExtractRows()
        {
            string html = LoadSampleHtml();
            var scraper = new RaidScraper();

            var players = scraper.ParseRaidTable(html);

            Assert.NotEmpty(players);
            Assert.Contains(players, p => p.Name == "Sean");
        }

        [Fact]
        public void GetNonContributors_ReturnCorrectNames()
        {
            string html = LoadSampleHtml();
            var scraper = new RaidScraper();

            var players = scraper.ParseRaidTable(html);
            var missing = scraper.GetNonContributors(players);

            Assert.Contains("Sean", missing);
        }

        [Fact]
        public void DiscordMessage_FormatNamesCorrectly()
        {
            var scraper = new RaidScraper();

            var message = scraper.BuildDiscordMessage(new() { "Sean", "Antonio" });

            Assert.Contains("Sean", message);
            Assert.Contains("Antonio", message);
            Assert.Contains("Players with no raid score", message);
        }

        [Fact]
        public void BuildDiscordMessage_NoMissingPlayers()
        {
            var scraper = new RaidScraper();

            var message = scraper.BuildDiscordMessage(new());

            Assert.Contains("All members contributed", message);
        }

        [Fact]
        public void ParseRaidTable_EmptyListOnInvalidHtml()
        {
            var scraper = new RaidScraper();

            var players = scraper.ParseRaidTable("<html></html>");

            Assert.Empty(players);
        }
    }
}
