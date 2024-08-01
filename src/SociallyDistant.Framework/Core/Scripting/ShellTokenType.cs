namespace SociallyDistant.Core.Core.Scripting
{
	public enum ShellTokenType
	{
		Text,
		Whitespace,
		Pipe,
		Overwrite,
		Append,
		FileInput,
		ParallelExecute,
		SequentialExecute,
		VariableAccess,
		AssignmentOperator,
		OpenParen,
		CloseParen,
		OpenCurly,
		CloseCurly,
		OpenSquare,
		ClosedSquare,
		Newline,
		LogicalAnd,
		LogicalOr,
		ExpansionString
	}
}