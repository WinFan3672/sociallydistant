using System.Collections.Immutable;
using System.Net;
using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Build.Framework;
using SociallyDistant.Core.Core.WorldData.Data;
using SociallyDistant.Core.News;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SociallyDistant.Core.BuildTasks;

public class MarkdownPreprocessorTask : Microsoft.Build.Utilities.Task
{
    [Required] 
    public string Source { get; set; } = string.Empty;
    
    [Required] 
    public string OutputPath { get; set; } = string.Empty;

    public override bool Execute()
    {
        var collectedDocuments = new List<string>();

        foreach (string file in Directory.EnumerateFiles(Source, "*.md", SearchOption.AllDirectories))
        {
            Log.LogMessage($"Found Markdown file: {file}");
            collectedDocuments.Add(file);
        }

        if (!Directory.Exists(OutputPath))
            Directory.CreateDirectory(OutputPath);
        
        foreach (string file in Directory.EnumerateFiles(OutputPath, "*.mdb", SearchOption.AllDirectories))
        {
            string relative = file.Substring(OutputPath.Length);
            string original = Source + relative.Substring(0, relative.Length - 1);
            
            if (collectedDocuments.Contains(original))
                continue;
            
            Log.LogMessage($"Deleting: {file}");
            File.Delete(file);
        }
        
        foreach (string file in collectedDocuments)
        {
            string relative = file.Substring(Source.Length);
            string destination = OutputPath + relative + "b";

            ProcessMarkdown(file, destination);
        }
        
        return true;
    }

    private bool ProcessMarkdown(string source, string destination)
    {
        string? directory = Path.GetDirectoryName(destination);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        
        using var sourceStream = File.OpenRead(source);
        using var sourceReader = new StreamReader(sourceStream);

        var isFirstLine = true;
        var parsingFrontmatter = false;
        var frontmatterBuilder = new StringBuilder();
        var bodyBuilder = new StringBuilder();

        while (!sourceReader.EndOfStream)
        {
            var wasFirstLIne = isFirstLine;
            isFirstLine = false;
            var line = sourceReader.ReadLine();

            if (line == "---")
            {
                if (parsingFrontmatter)
                {
                    parsingFrontmatter = false;
                    continue;
                }

                if (wasFirstLIne)
                {
                    parsingFrontmatter = true;
                    continue;
                }
            }

            if (parsingFrontmatter)
            {
                frontmatterBuilder.AppendLine(line);
            }
            else
            {
                bodyBuilder.AppendLine(line);
            }
        }

        string frontmatter = frontmatterBuilder.ToString();
        string markdown = bodyBuilder.ToString().Trim();
        
        // Try to deserialize the frontmatter as YAML to a news article header object.
        var yamlDeserializer = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
        var articleHeader = yamlDeserializer.Deserialize<ArticleInfo>(frontmatter);
        
        // Now we can parse the actual Markdown. We're going to serialize it as a list of DocumentElements that can be rendered by DocumentAdapter.
        var markdownParseTree = Markdown.Parse(markdown);
        var document = SerializeMarkdown(markdownParseTree);

        var article = ArticleData.CreateNew(articleHeader);
        article.SetDocument(document);

        using var stream = File.OpenWrite(destination);
        article.SaveToStream(stream);
        return true;
    }

    private DocumentElement[] SerializeMarkdown(MarkdownDocument document)
    {
        var result = new List<DocumentElement>();

        foreach (var block in document)
        {
            result.AddRange(SerializeMarkdownBlock(block));
        }
        
        return result.ToArray();
    }

    private IEnumerable<DocumentElement> SerializeMarkdownBlock(Block block)
    {
        if (block is HeadingBlock heading)
        {
            var documentType = (DocumentElementType)((int)DocumentElementType.Heading1 + heading.Level - 1);
            var markup = GetLeafMarkup(heading);
            yield return new DocumentElement { ElementType = documentType, Data = markup };
        }
        else if (block is ParagraphBlock paragraph)
        {
            var markup = GetLeafMarkup(paragraph);
            yield return new DocumentElement { ElementType = DocumentElementType.Text, Data = markup };
        }
        else if (block is ListItemBlock listItem)
        {
            var children = new List<DocumentElement>();
            foreach (var child in listItem)
                children.AddRange(SerializeMarkdownBlock(child));

            yield return new DocumentElement { ElementType = DocumentElementType.ListItem, Data = listItem.Order.ToString(), Children = children };
        }
        else if (block is ListBlock list)
        {
            var type = list.IsOrdered
                ? DocumentElementType.OrderedList
                : DocumentElementType.UnorderedList;

            var children = new List<DocumentElement>();
            
            foreach (var child in list)
                children.AddRange(SerializeMarkdownBlock(child));

            yield return new DocumentElement { ElementType = type, Data = list.OrderedStart ?? list.DefaultOrderedStart ?? "1", Children = children };
        }
        else if (block is QuoteBlock quote)
        {
            var children = new List<DocumentElement>();
            foreach (var childBlock in quote)
                children.AddRange(SerializeMarkdownBlock(childBlock));
            
            var document = new DocumentElement { ElementType = DocumentElementType.Quote, Children = children };
            yield return document;
        }
        else if (block is ThematicBreakBlock breakblock)
        {
            // No.
            yield break;
        }
        else if (block is CodeBlock code)
        {
            var markupBuilder = new StringBuilder();

            foreach (var line in code.Lines)
            {
                markupBuilder.AppendLine(line.ToString());
            }
            
            var markup = markupBuilder.ToString();
            yield return new DocumentElement { ElementType = DocumentElementType.Code, Data = markup };
        }
        else
        {
            Log.LogError(block.ToString());
        }

        yield break;
    }

    private string GetLeafMarkup(LeafBlock leaf)
    {
        return GetMarkup(leaf.Inline);
    }

    private string GetMarkup(ContainerInline? inline)
    {
        if (inline == null)
            return string.Empty;
        
        var result = new StringBuilder();

        // Opening link tags
        var wasImage = false;
        if (inline is LinkInline link)
        {
            wasImage = link.IsImage;
            
            if (wasImage)
                result.Append("<img=\"");
            else 
                result.Append("<link=\"");
            result.Append(link.Url);
            result.Append("\">");
        }
        
        // Opening formatter tags
        {
            if (inline is EmphasisInline emphasis)
            {
                switch (emphasis.DelimiterChar)
                {
                    // bold
                    case '*' when emphasis.DelimiterCount == 2:
                        result.Append("<b>");
                        break;
                    // italic 1
                    case '*' when emphasis.DelimiterCount == 1:
                        result.Append("<i>");
                        break;
                    // underline
                    case '_' when emphasis.DelimiterCount == 2:
                        result.Append("<u>");
                        break;
                    // italic 2
                    case '_' when emphasis.DelimiterCount == 1:
                        result.Append("<i>");
                        break;
                    // strikethrough
                    case '~' when emphasis.DelimiterCount == 2:
                        result.Append("<s>");
                        break;
                }
            }
        }

        foreach (var childInline in inline)
        {
            if (childInline is ContainerInline container)
            {
                result.Append(GetMarkup(container));
            }
            else if (childInline is LineBreakInline lineBreak)
            {
                if (lineBreak.IsHard)
                    result.AppendLine();
                else result.Append(' ');
            }
            else if (childInline is CodeInline code)
            {
                result.Append("<font=monospace><color=magenta>");
                result.Append(code.Content);
                result.Append("</color></font>");
            }
            else
            {
                result.Append(childInline);
            }
        }
        
        // Closing formatter tags
        {
            if (inline is EmphasisInline emphasis)
            {
                switch (emphasis.DelimiterChar)
                {
                    // bold
                    case '*' when emphasis.DelimiterCount == 2:
                        result.Append("</b>");
                        break;
                    // italic 1
                    case '*' when emphasis.DelimiterCount == 1:
                        result.Append("</i>");
                        break;
                    // underline
                    case '_' when emphasis.DelimiterCount == 2:
                        result.Append("</u>");
                        break;
                    // italic 2
                    case '_' when emphasis.DelimiterCount == 1:
                        result.Append("</i>");
                        break;
                    // strikethrough
                    case '~' when emphasis.DelimiterCount == 2:
                        result.Append("</s>");
                        break;
                }
            }
        }
        
        // Closing link tag
        if (inline is LinkInline)
        {
            if (wasImage)
                result.Append("</img>");
            else 
                result.Append("</link>");
        }

        return result.ToString();
    }
}