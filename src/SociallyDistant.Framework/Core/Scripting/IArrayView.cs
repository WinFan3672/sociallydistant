#nullable enable
namespace SociallyDistant.Core.Core.Scripting
{
	public interface IArrayView<T>
	{
		bool EndOfArray { get; }
		T Current { get; }
		T? Previous { get; }
		T? Next { get; }
		int CurrentIndex { get; }

		void Advance();
		void GoToPrevious();
	}
}