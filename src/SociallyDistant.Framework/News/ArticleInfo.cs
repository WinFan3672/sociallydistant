namespace SociallyDistant.Core.News;

/// <summary>
///     Represents the metadata for an in-game news article.
/// </summary>
[Serializable]
public sealed class ArticleInfo
{
    /// <summary>
    ///     Gets or sets the article's headline.
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    ///     Gets or sets the domain name of the in-game website that'll display the article.
    /// </summary>
    public string Host { get; set; } = string.Empty;
    
    /// <summary>
    ///     Gets or sets the Narrative ID of the NPC who wrote the article.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the topic text of the article, such as "Politics" or "Crime," usually displayed near the headline.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    public string[] Flags { get; set; } = Array.Empty<string>();
}