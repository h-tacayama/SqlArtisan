namespace InlineSqlSharp;

public interface IAliasable
{
    public ExprAlias AS(string alias);
}
