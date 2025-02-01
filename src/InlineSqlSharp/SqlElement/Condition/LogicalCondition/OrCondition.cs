namespace InlineSqlSharp;

public sealed class OrCondition(ICondition[] conditions)
	: MultiLogicalCondition(Keywords.OR, conditions)
{
}
