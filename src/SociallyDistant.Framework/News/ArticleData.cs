using System.Text;
using SociallyDistant.Core.Core.Serialization.Binary;
using SociallyDistant.Core.Core.WorldData.Data;
using SociallyDistant.Core.Social;

namespace SociallyDistant.Core.News;

public sealed class ArticleData
{
    private readonly byte[]                identifier = Encoding.UTF8.GetBytes("3RITCHIE");
    private readonly ArticleInfo           info       = new();
    private readonly List<DocumentElement> document   = new();
    private          byte                  flags;

    public ArticleFlags Flags => (ArticleFlags)flags;
    public ArticleInfo Info => info;
    
    private ArticleData()
    {  }

    private ArticleData(ArticleInfo info)
    {
        this.info = info;
    }

    public IEnumerable<DocumentElement> GetDocument()
    {
        return document;
    }

    private void Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        var dataReader = new BinaryDataReader(reader);

        byte[] ritchie = reader.ReadBytes(identifier.Length);
        if (!ritchie.SequenceEqual(identifier))
            throw new FormatException("The given stream doesn't contain a Socially Distant news article.");

        info.Title = dataReader.Read_string();
        info.Author = dataReader.Read_string();
        info.Host = dataReader.Read_string();
        info.Topic = dataReader.Read_string();

        flags = dataReader.Read_byte();
        
        var documentCount = reader.ReadUInt32();

        for (var i = 0; i < documentCount; i++)
        {
            var doc = new DocumentElement();
            doc.Read(dataReader);
            this.document.Add(doc);
        }
    }

    private ArticleFlags GetFlags(string[] rawFlags)
    {
        var result = ArticleFlags.None;

        foreach (string flag in rawFlags)
        {
            if (Enum.TryParse(flag, true, out ArticleFlags articleFlag))
                result |= articleFlag;
        }

        return result;
    }

    public void SaveToStream(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        var dataWriter = new BinaryDataWriter(writer);
        
        writer.Write(identifier);
        
        dataWriter.Write(info.Title);
        dataWriter.Write(info.Author);
        dataWriter.Write(info.Host);
        dataWriter.Write(info.Topic);

        flags = (byte) GetFlags(info.Flags);
        dataWriter.Write(flags);
        
        dataWriter.Write(document.Count);
        foreach (DocumentElement element in document)
            element.Write(dataWriter);
    }
    
    public void SetDocument(IEnumerable<DocumentElement> source)
    {
        this.document.Clear();
        this.document.AddRange(source);
    }

    public static ArticleData CreateNew(ArticleInfo articleInfo)
    {
        return new ArticleData(articleInfo);
    }

    public static ArticleData? FromSTream(Stream stream)
    {
        var article = new ArticleData();
        article.Read(stream);
        return article;
    }
}