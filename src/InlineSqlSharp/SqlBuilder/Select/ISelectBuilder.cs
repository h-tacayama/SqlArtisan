namespace InlineSqlSharp;

public interface ISelectBuilder
{
    ISelectBuilderSelect SELECT(
        params object[] selectItems);

    ISelectBuilderSelect SELECT(
        Distinct distinct,
        params object[] selectItems);

    ISelectBuilderSelect SELECT(
        Hints hints,
        params object[] selectItems);

    ISelectBuilderSelect SELECT(
        Hints hints,
        Distinct distinct,
        params object[] selectItems);
}
