namespace InlineSqlSharp;

public sealed class AndCondition(ICondition[] conditions)
	: MultiLogicalCondition(Keywords.AND, conditions)
{
}
