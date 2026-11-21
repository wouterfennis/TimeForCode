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
        [DataRow(BadgeImageState.HoursReceived, "120 hours")]
        [DataRow(BadgeImageState.Tier, "Gold")]
        [DataRow(BadgeImageState.NoBadgeYet, null)]
        [DataRow(BadgeImageState.Unavailable, null)]
        public void Render_Image_ShowsTheLogoMarkOnTheLeftOfTheLabel(BadgeImageState state, string? text)
        {
            XElement svg = Parse(SvgBadgeRenderer.Render(new BadgeImageContent(state, text)));

            XElement logo = svg.Descendants().Single(e => e.Attribute("data-logo") != null);
            double logoX = double.Parse(logo.Attribute("x")!.Value, System.Globalization.CultureInfo.InvariantCulture);
            double labelX = double.Parse(svg.Descendants().First(e => e.Name.LocalName == "text").Attribute("x")!.Value, System.Globalization.CultureInfo.InvariantCulture);
            logoX.Should().BeLessThan(labelX);
            logo.Descendants().Select(e => e.Attribute("stroke")?.Value).Should().Contain(new[] { "#2fc2ad", "#f7b733", "#e8f0ef" });
        }

        [TestMethod]
        public void Render_Logo_KeepsTheSettingsOfTheBrandMark()
        {
            XElement svg = Parse(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, "Gold")));

            XElement logo = svg.Descendants().Single(e => e.Attribute("data-logo") != null);
            logo.Attribute("viewBox")!.Value.Should().Be("0 0 48 48");
            logo.Attribute("fill")!.Value.Should().Be("none");
            logo.Attribute("stroke-linecap")!.Value.Should().Be("round");
            logo.Attribute("stroke-linejoin")!.Value.Should().Be("round");
            logo.Attribute("width")!.Value.Should().Be("14");
            logo.Attribute("height")!.Value.Should().Be("14");
        }

        [TestMethod]
        public void Render_Logo_HasTheShapesOfTheBrandMarkFile()
        {
            XElement svg = Parse(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, "Gold")));
            XElement brandMark = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "logo-mark-dark.svg")).Root!;

            XElement logo = svg.Descendants().Single(e => e.Attribute("data-logo") != null);
            logo.Elements().Select(e => e.ToString()).Should().Equal(brandMark.Elements().Where(e => e.Name.LocalName != "title").Select(e => e.ToString()));
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
        [DataRow('i')]
        [DataRow('l')]
        [DataRow('j')]
        [DataRow('I')]
        [DataRow('.')]
        [DataRow(',')]
        [DataRow(':')]
        [DataRow(';')]
        [DataRow('|')]
        [DataRow('!')]
        [DataRow('\'')]
        public void Render_NarrowCharacters_AreNarrowerThanOrdinaryOnes(char narrow)
        {
            WidthOfTier(new string(narrow, 10)).Should().BeLessThan(WidthOfTier("xxxxxxxxxx"));
        }

        [TestMethod]
        [DataRow('A')]
        [DataRow('Z')]
        [DataRow('%')]
        public void Render_CapitalsAndPercent_AreWiderThanOrdinaryOnes(char wide)
        {
            WidthOfTier(new string(wide, 10)).Should().BeGreaterThan(WidthOfTier("xxxxxxxxxx"));
        }

        [TestMethod]
        [DataRow('m')]
        [DataRow('w')]
        [DataRow('M')]
        [DataRow('W')]
        [DataRow('@')]
        public void Render_WideCharacters_AreWiderThanCapitals(char wide)
        {
            WidthOfTier(new string(wide, 10)).Should().BeGreaterThan(WidthOfTier("AAAAAAAAAA"));
        }

        [TestMethod]
        public void Render_Spaces_AreNarrowerThanOrdinaryCharacters()
        {
            WidthOfTier("          ").Should().BeLessThan(WidthOfTier("xxxxxxxxxx"));
        }

        [TestMethod]
        public void Render_Digits_AreWiderThanOrdinaryCharacters()
        {
            WidthOfTier("0000000000").Should().BeGreaterThan(WidthOfTier("xxxxxxxxxx"));
        }

        [TestMethod]
        public void Render_VeryLongText_IsShortenedWithAnEllipsisAndStaysBounded()
        {
            string svg = SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, new string('W', 500)));

            TextsOf(Parse(svg)).Should().Contain(t => t.EndsWith('…') && t.Length <= 40);
            int.Parse(Parse(svg).Attribute("width")!.Value).Should().BeLessThan(600);
        }

        [TestMethod]
        public void Render_TextOfTheMaximumLength_IsNotShortened()
        {
            string text = new('a', 40);

            TextsOf(Parse(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, text)))).Should().Contain(text);
        }

        private static int WidthOfTier(string tier) =>
            int.Parse(Parse(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, tier))).Attribute("width")!.Value);

        [TestMethod]
        [DataRow("\u0001")]
        [DataRow("\uD800")]
        [DataRow("Gold￾")]
        public void Render_TextWithCharactersXmlCannotHold_StillGivesWellFormedSvg(string tier)
        {
            string svg = SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, tier));

            Action parse = () => Parse(svg);
            parse.Should().NotThrow();
        }

        [TestMethod]
        public void Render_EmojiCutByTheLengthLimit_DoesNotLeaveAHalfSurrogate()
        {
            string svg = SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, new string('a', 38) + "😀😀"));

            Action parse = () => Parse(svg);
            parse.Should().NotThrow();
        }

        [TestMethod]
        public void Render_Image_HasAViewBoxOfItsOwnSize()
        {
            XElement svg = Parse(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, "Gold")));

            svg.Attribute("viewBox")!.Value.Should().Be($"0 0 {svg.Attribute("width")!.Value} 20");
        }

        [TestMethod]
        public void Render_ClipPathId_IsPrefixedAndMatchesItsReference()
        {
            XElement svg = Parse(SvgBadgeRenderer.Render(new BadgeImageContent(BadgeImageState.Tier, "Gold")));

            string id = svg.Descendants().Single(e => e.Name.LocalName == "clipPath").Attribute("id")!.Value;
            id.Should().StartWith("tfc-");
            svg.Descendants().Should().Contain(e => e.Attribute("clip-path") != null && e.Attribute("clip-path")!.Value == $"url(#{id})");
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