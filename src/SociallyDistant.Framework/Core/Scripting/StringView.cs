#nullable enable
namespace SociallyDistant.Core.Core.Scripting
{
	public sealed class StringView : IArrayView<char>
	{
		private readonly string underlyingString;
		private int index;

		public StringView(string underlyingString)
		{
			this.underlyingString = underlyingString;
		}
		
		private void ThrowIfBeginOfCollection()
		{
			if (index == 0)
				throw new InvalidOperationException("Beginning of collection");
		}
		
		private void ThrowIfEndOfCollection()
		{
			if (EndOfArray)
				throw new InvalidOperationException("End of collection");
		}

		/// <inheritdoc />
		public bool EndOfArray => index >= underlyingString.Length;

		/// <inheritdoc />
		public char Current => underlyingString[index];

		/// <inheritdoc />
		public char Previous
		{
			get
			{
				if (index == 0)
					return default;

				return underlyingString[index - 1];
			}
		}

		/// <inheritdoc />
		public char Next
		{
			get
			{
				if (index >= underlyingString.Length - 1)
					return default;

				return underlyingString[index + 1];
			}
		}

		/// <inheritdoc />
		public int CurrentIndex => index;

		/// <inheritdoc />
		public void Advance()
		{
			ThrowIfEndOfCollection();
			index++;
		}

		/// <inheritdoc />
		public void GoToPrevious()
		{
			ThrowIfBeginOfCollection();
			index--;
		}
	}
}