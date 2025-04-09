namespace InlineSqlSharp.Tests;

internal sealed class TestTableDto
{
    public TestTableDto(int code, string name, DateTime created_at)
    {
        Code = code;
        Name = name;
        CreatedAt = created_at;
    }

    public int Code { get; }

    public string Name { get; }

    public DateTime CreatedAt { get; }
}
