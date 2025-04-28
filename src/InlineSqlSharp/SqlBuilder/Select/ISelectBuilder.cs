namespace InlineSqlSharp;

public interface ISelectBuilder
{
    ISelectBuilderSelect Select(
        params object[] selectItems);

    ISelectBuilderSelect Select(
        DistinctKeyword distinct,
        params object[] selectItems);

    ISelectBuilderSelect Select(
        SqlHints hints,
        params object[] selectItems);

    ISelectBuilderSelect Select(
        SqlHints hints,
        DistinctKeyword distinct,
        params object[] selectItems);
}
