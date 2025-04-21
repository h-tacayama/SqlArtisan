namespace InlineSqlSharp;

public interface ISelectBuilder
{
    ISelectBuilderSelect SELECT(
        params object[] selectItems);

    ISelectBuilderSelect SELECT(
        AllOrDistinct allOrDistinct,
        params object[] selectItems);

    ISelectBuilderSelect SELECT(
        Hints hints,
        params object[] selectItems);

    ISelectBuilderSelect SELECT(
        Hints hints,
        AllOrDistinct allOrDistinct,
        params object[] selectItems);
}
