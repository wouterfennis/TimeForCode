using AwesomeAssertions;
using System.Xml.Linq;
using TimeForCode.Reporting.Api.V1.Rendering;
using TimeForCode.Reporting.Commands;

namespace TimeForCode.Reporting.Api.Tests
{
    [TestClass]
    public class SvgBadgeRendererTests
    {
        private static XElement Parse(string svg) => XDocument.Parse(svg).Root!;

        private static IEnumerable<string> TextsOf(XElement svg) => svg.Descendants().Where(e => e.Name.LocalName == "text").Select(e => e.Value);

        private static IEnumerable<string> ColoursOf(XElement svg) => svg.Descendants().Where(e => e.Name.LocalName == "rect").Select(e => e.Attribute("fill")?.Value ?? string.Empty);

        private static string IconOf(XElement svg) => svg.Descendants().Single(e => e.Attribute("data-icon") != null).Attribute("data-icon")!.Value;

        [TestMethod]
        public void Render_Value_IsWellFormedSvgWithTheLabelAndTheValue()
        {
            XElement svg = Parse(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.HoursReceived, "120 hours")));

            svg.Name.LocalName.Should().Be("svg");
            svg.Attribute("role")!.Value.Should().Be("img");
            svg.Attribute("aria-label")!.Value.Should().Be("TimeForCode: 120 hours");
            TextsOf(svg).Should().Contain("TimeForCode").And.Contain("120 hours");
        }

        [TestMethod]
        public void Render_Value_HasATitleForAssistiveTechnology()
        {
            XElement svg = Parse(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, "Gold")));

            svg.Descendants().Single(e => e.Name.LocalName == "title").Value.Should().Be("TimeForCode: Gold");
        }

        [TestMethod]
        [DataRow(BadgeImageState.NoBadgeYet, "no badge yet", "#9f9f9f", "empty")]
        [DataRow(BadgeImageState.NotFound, "not found", "#9f9f9f", "help")]
        [DataRow(BadgeImageState.Unavailable, "unavailable", "#fe7d37", "warning")]
        public void Render_NeutralState_ShowsItsFixedTextColourAndIcon(BadgeImageState state, string text, string colour, string icon)
        {
            XElement svg = Parse(SvgBadgeRenderer.Render(new BadgeImageContent(state)));

            TextsOf(svg).Should().Contain(text);
            ColoursOf(svg).Should().Contain(colour);
            IconOf(svg).Should().Be(icon);
        }

        [TestMethod]
        [DataRow(BadgeImageState.HoursReceived, "120 hours", "#4c1", "clock")]
        [DataRow(BadgeImageState.Progress, "45 / 120 hours (38%)", "#007ec6", "clock")]
        public void Render_ProjectBadge_ShowsItsColourAndIcon(BadgeImageState state, string text, string colour, string icon)
        {
            XElement svg = Parse(SvgBadgeRenderer.Render(new BadgeImageContent(state, text)));

            ColoursOf(svg).Should().Contain(colour);
            IconOf(svg).Should().Be(icon);
        }

        [TestMethod]
        [DataRow("Bronze", "#cd7f32")]
        [DataRow("Silver", "#a8a8a8")]
        [DataRow("Gold", "#d4af37")]
        [DataRow("gold", "#d4af37")]
        [DataRow("Champion", "#007ec6")]
        public void Render_Tier_UsesTheColourOfTheTier(string tier, string colour)
        {
            XElement svg = Parse(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, tier)));

            ColoursOf(svg).Should().Contain(colour);
            IconOf(svg).Should().Be("star");
        }

        [TestMethod]
        public void Render_SpecialCharacters_AreEscapedAndSurviveAsPlainText()
        {
            string svg = SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, "<Gold & \"Co\">"));

            svg.Should().NotContain("<Gold");
            TextsOf(Parse(svg)).Should().Contain("<Gold & \"Co\">");
            Parse(svg).Attribute("aria-label")!.Value.Should().Be("TimeForCode: <Gold & \"Co\">");
        }

        [TestMethod]
        public void Render_LongerValue_IsWiderThanAShorterOne()
        {
            int shortWidth = int.Parse(Parse(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, "Gold"))).Attribute("width")!.Value);
            int longWidth = int.Parse(Parse(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, "A very long tier name"))).Attribute("width")!.Value);

            longWidth.Should().BeGreaterThan(shortWidth);
        }

        [TestMethod]
        public void Render_SameContent_GivesTheSameImage()
        {
            string first = SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.NoBadgeYet));
            string second = SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.NoBadgeYet));

            second.Should().Be(first);
        }
    }
}