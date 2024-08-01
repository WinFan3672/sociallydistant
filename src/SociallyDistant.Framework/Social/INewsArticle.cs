#nullable enable
using SociallyDistant.Core.Core.WorldData.Data;

namespace SociallyDistant.Core.Social
{
	public interface INewsArticle
	{
		int Id { get; }
		string Topic { get; }
		string? HostName { get; }
		IProfile Author { get; }
		string Headline { get; }
		DateTime Date { get; }
		string Slug { get; }

		DocumentElement[] GetBody();
	}
}