using System.Diagnostics.Metrics;
using System.Formats.Tar;
using System.Text;
using System.Xml.XPath;
using AcidicGUI.Layout;
using AcidicGUI.Rendering;
using AcidicGUI.TextRendering;
using Microsoft.Xna.Framework;

namespace AcidicGUI.Widgets;

public class TextWidget : Widget
{
    private readonly List<TextElement> textElements = new();
    private readonly StringBuilder stringBuilder = new();

    private Color? color;
    private FontInfo font;
    private string text = string.Empty;
    private bool useMarkup = false;
    private bool wordWrapping = false;
    private bool showMarkup;
    private TextAlignment textAlignment;
    private int? fontSize;
    private FontWeight fontWeight = FontWeight.Normal;

    public FontWeight FontWeight
    {
        get => fontWeight;
        set
        {
            fontWeight = value;
            InvalidateMeasurements();
            InvalidateLayout();
        }
    }

    public Color? TextColor
    {
        get => color;
        set
        {
            color = value;
            InvalidateGeometry();
        }
    }
    
    public int? FontSize
    {
        get => fontSize;
        set
        {
            fontSize = value;
            InvalidateMeasurements();
            InvalidateLayout();
        }
    }
    
    public bool ShowMarkup
    {
        get => showMarkup;
        set
        {
            showMarkup = value;
            RebuildText();
            InvalidateLayout();
        }
    }
    
    public TextAlignment TextAlignment
    {
        get => textAlignment;
        set
        {
            textAlignment = value;
            InvalidateLayout();
        }
    }
    
    public bool WordWrapping
    {
        get => wordWrapping;
        set
        {
            wordWrapping = value;
            InvalidateLayout();
        }
    }
    
    public bool UseMarkup
    {
        get => useMarkup;
        set
        {
            useMarkup = value;
            RebuildText();
            InvalidateLayout();
        }
    }
    
    public string Text
    {
        get => text;
        set
        {
            text = value;
            RebuildText();
            InvalidateLayout();
        }
    }
    
    public FontInfo Font
    {
        get => font;
        set
        {
            font = value;
            InvalidateMeasurements();
            InvalidateLayout();
        }
    }

    protected override Vector2 GetContentSize(Vector2 availableSize)
    {
        float wrapWidth = availableSize.X;
        
        // Measure text elements
        MeasureElements();

        Vector2 result = Vector2.Zero;
        float lineHeight = 0;
        float lineWidth = 0;

        for (var i = 0; i < textElements.Count; i++)
        {
            var newline = textElements[i].IsNewLine;
            var measurement = textElements[i].MeasuredSize.GetValueOrDefault();
            var wrap = wordWrapping && (lineWidth + measurement.X > wrapWidth) && wrapWidth > 0;
            
            if (newline || wrap)
            {
                result.X = Math.Max(result.X, lineWidth);
                result.Y += lineHeight;
                lineHeight = 0;
                lineWidth = 0;
            }

            lineWidth += measurement.X;
            lineHeight = Math.Max(lineHeight, measurement.Y);
        }
        
        result.Y += lineHeight;
        result.X = Math.Max(result.X, lineWidth);

        return result;
    }

    protected override void ArrangeChildren(IGuiContext context, LayoutRect availableSpace)
    {
        // Break words and figure out where lines start and end.
        var lines = BreakWords(availableSpace);

        var y = availableSpace.Top;
        foreach ((int start, int end, float lineWidth) in lines)
        {
            float lineHeight = 0;
            float offset = 0;
            float widgetX = availableSpace.Left;

            if (textAlignment == TextAlignment.Center)
            {
                widgetX += (availableSpace.Width - lineWidth) / 2;
            }
            else if (textAlignment == TextAlignment.Right)
            {
                widgetX += availableSpace.Width - lineWidth;
            }
            
            for (var i = start; i < end; i++)
            {
                lineHeight = Math.Max(lineHeight, textElements[i].MeasuredSize!.Value.Y);
                float x = widgetX + offset;
                textElements[i].Position = new Vector2(x, y);
                offset += textElements[i].MeasuredSize!.Value.X;
            }

            y += lineHeight;
        }
    }

    protected override void RebuildGeometry(GeometryHelper geometry)
    {
        foreach (TextElement element in textElements)
        {
            var fontInstance = (element.MarkupData.FontOverride ?? font).GetFont(this);
            
            // TODO: Color from a property or the Visual Style.
            var color = (element.MarkupData.ColorOverride ?? TextColor) ?? GetVisualStyle().GetTextColor(this);

            if (element.MeasuredSize.HasValue && element.MarkupData.Highlight.A > 0)
            {
                var highlightRect = new LayoutRect(
                    element.Position.X,
                    element.Position.Y,
                    element.MeasuredSize.Value.X,
                    element.MeasuredSize.Value.Y
                );

                geometry.AddQuad(highlightRect, element.MarkupData.Highlight);
            }

            fontInstance.Draw(geometry, element.Position, color, element.Text, element.MarkupData.FontSize ?? this.FontSize,
                element.MarkupData.Weight ?? FontWeight, element.MarkupData.Italic);

            var strikeLine = 1;
            var underLine = 2;

            if (element.MarkupData.Underline)
            {
                geometry.AddQuad(new LayoutRect(
                    element.Position.X,
                    element.Position.Y + element.MeasuredSize!.Value.Y - underLine,
                    element.MeasuredSize.Value.X,
                    underLine
                ), color);
            }
            else if (!string.IsNullOrWhiteSpace(element.MarkupData.Link))
            {
                geometry.AddQuad(new LayoutRect(
                    element.Position.X,
                    element.Position.Y + element.MeasuredSize!.Value.Y - strikeLine,
                    element.MeasuredSize.Value.X,
                    strikeLine
                ), color);
            }
            
            if (element.MarkupData.Strikethrough)
            {
                geometry.AddQuad(new LayoutRect(
                    element.Position.X,
                    element.Position.Y + ((element.MeasuredSize!.Value.Y - underLine)/2),
                    element.MeasuredSize.Value.X,
                    strikeLine
                ), color);
            }
        }
    }

    private (int start, int end, float size)[] BreakWords(LayoutRect availableSpace)
    {
        var lines = new List<(int, int, float)>();
        int start = 0;
        float lineHeight = 0;
        Vector2 offset = Vector2.Zero;

        for (var i = 0; i < textElements.Count; i++)
        {
            if (i == textElements.Count-1)
            {
                textElements[i].MeasuredSize = (textElements[i].MarkupData.FontOverride ?? font).GetFont(this)
                    .Measure(textElements[i].Text.TrimEnd(), textElements[i].MarkupData.FontSize ?? FontSize,
                        textElements[i].MarkupData.Weight ?? FontWeight, textElements[i].MarkupData.Italic);
            }
            
            var measurement = textElements[i].MeasuredSize.GetValueOrDefault();
            var isNewLine = textElements[i].IsNewLine;
            var wrap = wordWrapping && (offset.X + measurement.X > availableSpace.Width);

            if (isNewLine || wrap)
            {
                if (i > 0)
                {
                    offset.X -= textElements[i - 1].MeasuredSize!.Value.X;
                    textElements[i - 1].MeasuredSize = (textElements[i - 1].MarkupData.FontOverride ?? font)
                        .GetFont(this)
                        .Measure(textElements[i - 1].Text.TrimEnd(),
                            textElements[i - 1].MarkupData.FontSize ?? FontSize,
                            textElements[i - 1].MarkupData.Weight ?? FontWeight,
                            textElements[i - 1].MarkupData.Italic);
                    offset.X += textElements[i - 1].MeasuredSize!.Value.X;
                }

                lines.Add((start, i, offset.X));
                start = i;
                
                offset.X = 0;
                offset.Y += lineHeight;
                
                lineHeight = measurement.Y;
                textElements[i].IsNewLine = true;
            }

            offset.X += measurement.X;
            lineHeight = Math.Max(lineHeight, measurement.Y);
        }

        lines.Add((start, textElements.Count, offset.X));
        start = textElements.Count;
        
        return lines.ToArray();
    }

    public bool TryFindLink(Vector2 position, out string? linkId)
    {
        linkId = null;

        foreach (TextElement element in textElements)
        {
            if (element.MeasuredSize == null)
                continue;

            if (string.IsNullOrWhiteSpace(element.MarkupData.Link))
                continue;

            LayoutRect rect = new LayoutRect(
                element.Position.X,
                element.Position.Y,
                element.MeasuredSize.Value.X,
                element.MeasuredSize.Value.Y
            );

            if (!rect.Contains(position))
                continue;

            linkId = element.MarkupData.Link;
            return true;
        }

        return false;
    }
    
    private bool ParseMarkup(ReadOnlySpan<char> chars, int start, ref MarkupData markupData)
    {
        if (start < 0)
            return false;

        if (start >= chars.Length)
            return false;

        if (chars[start] != '<')
            return false;

        var end = start;
        
        for (var i = start; i <= chars.Length; i++)
        {
            if (i == chars.Length)
                return false;

            if (chars[i] == '>')
            {
                end = i + 1;
                break;
            }
        }

        var tag = chars.Slice(start, end - start);
        var tagWithoutAngles = tag.Slice(1, tag.Length - 2).ToString();

        markupData.Length = tag.Length;
        return ParseTag(tagWithoutAngles, ref markupData);
    }

    private bool ParseTag(string tag, ref MarkupData markupData)
    {
        var beforeEquals = tag;
        var afterEquals = string.Empty;

        var equalsIndex = tag.LastIndexOf("=", StringComparison.Ordinal);

        if (equalsIndex != -1)
        {
            beforeEquals = tag.Substring(0, equalsIndex);
            afterEquals = tag.Substring(equalsIndex + 1);
        }

        switch (beforeEquals)
        {
            case "size":
            {
                if (!int.TryParse(afterEquals, out int size) || size < 0)
                    return false;

                markupData.FontSize = size;
                return true;
            }
            case "/size":
            {
                markupData.FontSize = null;
                return true;
            }
            case "color":
            {
                if (ColorHelpers.ParseColor(afterEquals, out Color color))
                {
                    markupData.ColorOverride = color;
                    return true;
                }

                break;
            }
            case "/color":
            {
                markupData.ColorOverride = null;
                return true;
            }
            case "highlight":
            {
                if (ColorHelpers.ParseColor(afterEquals, out Color color))
                {
                    markupData.Highlight = color;
                    return true;
                }

                break;
            }
            case "/highlight":
            {
                markupData.Highlight = Color.Transparent;
                return true;
            }
            case "b":
                markupData.Weight = FontWeight.Bold;
                return true;
            case "/b":
                markupData.Weight = null;
                return true;
            case "i":
                markupData.Italic = true;
                return true;
            case "/i":
                markupData.Italic = false;
                return true;
            case "u":
                markupData.Underline = true;
                return true;
            case "/u":
                markupData.Underline = false;
                return true;
            case "s":
                markupData.Strikethrough = true;
                return true;
            case "/s":
                markupData.Strikethrough = false;
                return true;
            case "selected":
                markupData.Selected = true;
                return true;
            case "/selected":
                markupData.Selected = false;
                return true;
            case "link":
            {
                if (string.IsNullOrWhiteSpace(afterEquals))
                    return false;

                markupData.Link = afterEquals;
                return true;
            }
            case "/link":
                markupData.Link = null;
                return true;
        }

        return false;
    }
    
    private void RebuildText()
    {
        var markupData = new MarkupData();
        var newMarkupData = new MarkupData();
        
        var sourceStart = 0;
        
        textElements.Clear();

        ReadOnlySpan<char> chars = text.AsSpan();
        
        for (var i = 0; i <= chars.Length; i++)
        {
            char? character = i < chars.Length ? chars[i] : null;

            // End of text.
            if (!character.HasValue)
            {
                if (stringBuilder.Length > 0)
                {
                    textElements.Add(new TextElement
                    {
                        Text = stringBuilder.ToString().TrimEnd(),
                        SourceStart = sourceStart,
                        SourceEnd = i,
                        MarkupData = markupData
                    });
                    markupData.Length = 0;
                    sourceStart = i;
                }

                stringBuilder.Length = 0;
                break;
            }

            switch (character.Value)
            {
                case '<' when this.useMarkup:
                {
                    if (!ParseMarkup(chars, i, ref newMarkupData))
                    {
                        goto default;
                        break;
                    }

                    textElements.Add(new TextElement
                    {
                        Text = stringBuilder.ToString(),
                        SourceStart = sourceStart,
                        SourceEnd = i,
                        MarkupData = markupData
                    });

                    int markupLength = newMarkupData.Length;
                    
                    if (showMarkup)
                    {
                        textElements.Add(new TextElement
                        {
                            Text = chars.Slice(i, newMarkupData.Length).ToString(),
                            SourceStart = i,
                            SourceEnd = i + newMarkupData.Length,
                            MarkupData = markupData
                        });
                        
                        newMarkupData.Length = 0;
                    }
                    
                    markupData = newMarkupData;
                    
                    stringBuilder.Length = 0;
                    sourceStart = i;
                    
                    i += markupLength - 1;
                    break;
                }
                case '\r':
                    continue;
                case '\n':
                {
                    textElements.Add(new TextElement
                    {
                        Text = stringBuilder.ToString().TrimEnd(),
                        SourceStart = sourceStart,
                        SourceEnd = i,
                        MarkupData = markupData
                    });

                    markupData.Length = 0;
                    
                    stringBuilder.Length = 0;
                    sourceStart = i;
                    
                    textElements.Add(new TextElement
                    {
                        Text = stringBuilder.ToString(),
                        IsNewLine = true,
                        SourceStart = sourceStart,
                        SourceEnd = i + 1,
                        MarkupData = markupData
                    });

                    sourceStart = i + 1;
                    break;
                }
                default:
                {
                    stringBuilder.Append(character.Value);
                    
                    if (char.IsWhiteSpace(character.Value))
                    {
                        textElements.Add(new TextElement
                        {
                            Text = stringBuilder.ToString(),
                            SourceStart = sourceStart,
                            SourceEnd = i + 1,
                            MarkupData = markupData
                        });

                        markupData.Length = 0;
                        sourceStart = i + 1;
                        stringBuilder.Length = 0;
                    }
                    break;
                }
            }
        }
    }

    public int GetLineCount()
    {
        return GetLineAtIndex(text.Length) + 1;
    }

    public int GetLineLength(int line)
    {
        var lineStart = GetLineStartElement(line);
        var length = 0;

        for (var i = lineStart; i < textElements.Count; i++)
        {
            if (textElements[i].IsNewLine)
                break;

            length += (textElements[i].SourceEnd - textElements[i].SourceStart);
        }

        return length;
    }
    
    public int GetLineStart(int line)
    {
        var lineStartElement = GetLineStartElement(line);
        return textElements[lineStartElement].SourceStart;
    }
    
    public int GetLineAtIndex(int characterIndex)
    {
        int line = 0;
        var wasNewLine = false;
        foreach (TextElement element in textElements)
        {
            if (element.SourceStart >= characterIndex)
                break;

            if (wasNewLine)
            {
                line++;
                wasNewLine = false;
            }

            if (element.IsNewLine)
                wasNewLine = true;
        }
     
        if (wasNewLine)
        {
            line++;
            wasNewLine = false;
        }
        
        return line;
    }
    
    public int GetLineStartElement(int line)
    {
        var currentLine = 0;
        var i = 0;
        var lineStartElement = 0;
        var wasNewLine = false;
        foreach (TextElement element in textElements)
        {
            if (currentLine == line)
                return lineStartElement;

            if (wasNewLine)
            {
                currentLine++;
                lineStartElement = i;
                wasNewLine = false;
            }

            if (element.IsNewLine)
                wasNewLine = true;

            i++;
        }
     
        if (wasNewLine)
        {
            currentLine++;
            wasNewLine = false;
            lineStartElement = textElements.Count - i;
        }
        
        return lineStartElement;
    }

    
    private void InvalidateMeasurements()
    {
        for (var i = 0; i < textElements.Count; i++)
        {
            textElements[i].MeasuredSize = null;
        }
    }
    
    private void MeasureElements()
    {
        for (var i = 0; i < textElements.Count; i++)
        {
            if (textElements[i].MeasuredSize != null)
                continue;
            
            var fontInstance = (textElements[i].MarkupData.FontOverride ?? font).GetFont(this);

            textElements[i].MeasuredSize = fontInstance.Measure(textElements[i].Text,
                textElements[i].MarkupData.FontSize ?? FontSize, textElements[i].MarkupData.Weight ?? FontWeight,
                textElements[i].MarkupData.Italic);
        }
    }

    public LayoutRect GetPositionOfCharacter(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex > text.Length)
            throw new ArgumentOutOfRangeException(nameof(characterIndex));
        
        var i = 0;
        foreach (TextElement element in textElements)
        {
            if (characterIndex < element.SourceStart)
                break;

            if (i == textElements.Count - 1 || characterIndex <= element.SourceEnd)
            {
                var fontInstance = (element.MarkupData.FontOverride ?? font).GetFont(this);
                
                if (characterIndex == element.SourceStart)
                {
                    string singleChar = element.Text.Substring(0, Math.Min(1, element.Text.Length));
                    Vector2 charMeasure = fontInstance.Measure(singleChar, element.MarkupData.FontSize ?? FontSize, element.MarkupData.Weight ?? FontWeight, element.MarkupData.Italic);

                    return new LayoutRect(
                        element.Position.X,
                        element.Position.Y,
                        charMeasure.X,
                        charMeasure.Y
                    );
                }

                string textToMeasure =
                    element.Text.Substring(0, Math.Min(element.Text.Length, characterIndex - element.SourceStart));
                
                Vector2 measurement = fontInstance.Measure(textToMeasure, element.MarkupData.FontSize ?? FontSize, element.MarkupData.Weight ?? FontWeight, element.MarkupData.Italic);

                string charAfterMeasure =
                    element.Text.Substring(textToMeasure.Length, Math.Min(1, element.Text.Length - textToMeasure.Length));

                var singleCharMeasure = fontInstance.Measure(charAfterMeasure, element.MarkupData.FontSize ?? FontSize, element.MarkupData.Weight ?? FontWeight, element.MarkupData.Italic);

                return new LayoutRect(
                    element.Position.X + measurement.X,
                    element.Position.Y,
                    singleCharMeasure.X,
                    singleCharMeasure.Y
                );
            }
            
            i++;
        }
        
        Vector2 lineMeasurement = font.GetFont(this).Measure(text);
        return new LayoutRect(
            ContentArea.Left,
            ContentArea.Top,
            lineMeasurement.X,
            lineMeasurement.Y
        );
    }

    private struct MarkupData
    {
        public int Length;
        public Color? ColorOverride;
        public Color Highlight;
        public FontInfo? FontOverride;
        public FontWeight? Weight;
        public int? FontSize;
        public bool Italic;
        public bool Underline;
        public bool Strikethrough;
        public bool Selected;
        public string? Link;
    }
    
    private class TextElement
    {
        public string Text;
        public Vector2 Position;
        public Vector2? MeasuredSize;
        public bool IsNewLine;
        public int SourceStart;
        public int SourceEnd;
        public MarkupData MarkupData;
    }
}