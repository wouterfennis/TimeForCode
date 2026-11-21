using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using TimeForCode.Reporting.Commands;

namespace TimeForCode.Reporting.Api.V1.Rendering
{
    /// <summary>
    /// Draws a flat badge in the style of shields.io: a grey "TimeForCode" label on the left and a coloured value with an icon on
    /// the right. The image is built as XML, so every text and attribute value is escaped and the output is always well-formed.
    /// It holds nothing but the label, the fixed texts and the text of the content: totals and tier names.
    /// </summary>
    public static class SvgBadgeRenderer
    {
        private const string Label = "TimeForCode";
        private const string LabelColour = "#555";
        private const string Font = "Verdana,Geneva,DejaVu Sans,sans-serif";
        private const int Height = 20;
        private const int Padding = 10;
        private const int IconWidth = 16;

        private const int MaxTextLength = 40;

        private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

        // Icons are white shapes in a 12 x 12 box: no text, no emoji, so they look the same in every viewer.
        // Parsed once; adding the elements of one to an image copies them.
        private static readonly Dictionary<string, XElement> Shapes = new()
        {
            ["clock"] = Shape("<circle cx=\"6\" cy=\"6\" r=\"4.8\" fill=\"none\" stroke=\"#fff\" stroke-width=\"1.4\"/><path d=\"M6 3.2V6l2 1.3\" fill=\"none\" stroke=\"#fff\" stroke-width=\"1.4\" stroke-linecap=\"round\"/>"),
            ["star"] = Shape("<circle cx=\"6\" cy=\"6\" r=\"4.6\" fill=\"#fff\" fill-opacity=\".25\" stroke=\"#fff\" stroke-width=\"1.4\"/><path d=\"M6 3.4l.8 1.7 1.8.2-1.3 1.2.4 1.8L6 7.4l-1.7.9.4-1.8-1.3-1.2 1.8-.2z\" fill=\"#fff\"/>"),
            ["empty"] = Shape("<circle cx=\"6\" cy=\"6\" r=\"4.6\" fill=\"none\" stroke=\"#fff\" stroke-width=\"1.4\" stroke-dasharray=\"2 1.6\"/>"),
            ["help"] = Shape("<circle cx=\"6\" cy=\"6\" r=\"4.8\" fill=\"none\" stroke=\"#fff\" stroke-width=\"1.4\"/><path d=\"M4.6 4.7c0-1.7 2.8-1.7 2.8 0 0 1.1-1.4 1.1-1.4 2.2\" fill=\"none\" stroke=\"#fff\" stroke-width=\"1.2\" stroke-linecap=\"round\"/><circle cx=\"6\" cy=\"8.7\" r=\".7\" fill=\"#fff\"/>"),
            ["warning"] = Shape("<path d=\"M6 1.2l5 9H1z\" fill=\"none\" stroke=\"#fff\" stroke-width=\"1.4\" stroke-linejoin=\"round\"/><path d=\"M6 4.6v2.6\" stroke=\"#fff\" stroke-width=\"1.4\" stroke-linecap=\"round\"/><circle cx=\"6\" cy=\"8.8\" r=\".8\" fill=\"#fff\"/>")
        };

        /// <summary>Renders the content as an SVG image.</summary>
        public static string Render(BadgeImageContent content)
        {
            (string text, string colour, string icon) = StyleOf(content);
            text = Shorten(Clean(text));

            int labelWidth = WidthOf(Label) + Padding;
            int valueWidth = WidthOf(text) + Padding + IconWidth;
            int width = labelWidth + valueWidth;
            string description = $"{Label}: {text}";

            // Prefixed, and the same for the same width (so the same width gives the same clip), so an image inlined into a page cannot clash with its ids.
            string clipId = $"tfc-clip-{width}";

            XElement svg = new(
                Svg + "svg",
                new XAttribute("width", width),
                new XAttribute("height", Height),
                new XAttribute("viewBox", $"0 0 {width} {Height}"),
                new XAttribute("role", "img"),
                new XAttribute("aria-label", description),
                new XElement(Svg + "title", description),
                new XElement(
                    Svg + "clipPath",
                    new XAttribute("id", clipId),
                    new XElement(Svg + "rect", new XAttribute("width", width), new XAttribute("height", Height), new XAttribute("rx", 3))),
                new XElement(
                    Svg + "g",
                    new XAttribute("clip-path", $"url(#{clipId})"),
                    Rect(0, labelWidth, LabelColour),
                    Rect(labelWidth, valueWidth, colour)),
                IconOf(icon, labelWidth + 6),
                new XElement(
                    Svg + "g",
                    new XAttribute("fill", "#fff"),
                    new XAttribute("text-anchor", "middle"),
                    new XAttribute("font-family", Font),
                    new XAttribute("font-size", 11),
                    Text(Label, labelWidth / 2.0),
                    Text(text, labelWidth + IconWidth + ((valueWidth - IconWidth) / 2.0))));

            return svg.ToString(SaveOptions.DisableFormatting);
        }

        private static (string Text, string Colour, string Icon) StyleOf(BadgeImageContent content) => content.State switch
        {
            BadgeImageState.HoursReceived => (content.Text!, "#4c1", "clock"),
            BadgeImageState.Progress => (content.Text!, "#007ec6", "clock"),
            BadgeImageState.Tier => (content.Text!, TierColour(content.Text!), "star"),
            BadgeImageState.NoBadgeYet => ("no badge yet", "#9f9f9f", "empty"),
            BadgeImageState.NotFound => ("not found", "#9f9f9f", "help"),
            _ => ("unavailable", "#fe7d37", "warning")
        };

        private static string TierColour(string tier) => tier.ToLowerInvariant() switch
        {
            "bronze" => "#cd7f32",
            "silver" => "#a8a8a8",
            "gold" => "#d4af37",
            _ => "#007ec6"
        };

        private static XElement Rect(int x, int width, string colour) => new(
            Svg + "rect",
            new XAttribute("x", x),
            new XAttribute("width", width),
            new XAttribute("height", Height),
            new XAttribute("fill", colour));

        private static XElement Text(string text, double centre) => new(
            Svg + "text",
            new XAttribute("x", centre.ToString("0.#", CultureInfo.InvariantCulture)),
            new XAttribute("y", 14),
            text);

        // The width of a text at 11 px Verdana, estimated per character; a SVG cannot measure its own text.
        private static int WidthOf(string text) => (int)Math.Round(text.Sum(CharWidth));

        // A tier name is free text: a long one must not make the image arbitrarily wide. It is cut between text elements, never inside a surrogate pair.
        private static string Shorten(string text)
        {
            if (text.Length <= MaxTextLength)
            {
                return text;
            }

            int cut = char.IsLowSurrogate(text[MaxTextLength - 1]) ? MaxTextLength - 2 : MaxTextLength - 1;
            return string.Concat(text.AsSpan(0, cut), "…");
        }

        // XML cannot hold control characters or an unpaired surrogate, and the writer throws on them: they are left out.
        private static string Clean(string text)
        {
            // Enumerating runes keeps a surrogate pair together; an unpaired surrogate comes out as U+FFFD, which XML can hold.
            StringBuilder clean = new(text.Length);
            foreach (Rune rune in text.EnumerateRunes())
            {
                if (!rune.IsBmp || XmlConvert.IsXmlChar((char)rune.Value))
                {
                    clean.Append(rune.ToString());
                }
            }

            return clean.ToString();
        }

        private static double CharWidth(char c) => c switch
        {
            'i' or 'l' or 'j' or 'I' or '.' or ',' or ':' or ';' or '|' or '!' or '\'' => 3.5,
            ' ' => 3.8,
            'm' or 'w' or 'M' or 'W' or '@' => 10,
            >= 'A' and <= 'Z' or '%' => 7.8,
            >= '0' and <= '9' => 7,
            _ => 6.6
        };

        private static XElement IconOf(string icon, int x)
        {
            XElement group = new(
                Svg + "g",
                new XAttribute("data-icon", icon),
                new XAttribute("transform", $"translate({x} 4)"),
                Shapes[icon].Elements());

            return group;
        }

        private static XElement Shape(string elements) => XElement.Parse($"<g xmlns=\"{Svg.NamespaceName}\">{elements}</g>");
    }
}