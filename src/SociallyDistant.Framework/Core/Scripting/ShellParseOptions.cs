namespace SociallyDistant.Core.Core.Scripting
{
	public sealed class ShellParseOptions
	{
		public bool ParseExpansionStrings { get; set; } = true;
		public bool ParseRedirection { get; set; } = true;
		public bool ParsePipes { get; set; } = true;

		public static readonly ShellParseOptions Default = new();
	}
}