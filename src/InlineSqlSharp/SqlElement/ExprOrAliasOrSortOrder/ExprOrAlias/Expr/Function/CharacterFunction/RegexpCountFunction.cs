namespace InlineSqlSharp;

public sealed class RegexpCountFunction : NumericExpr
{
	private readonly CharacterExpr _source;
	private readonly CharacterExpr _pattern;
	private readonly NumericExpr? _position;
	private readonly RegexpOptions _options;

	internal static RegexpCountFunction Of(
		CharacterExpr source,
		CharacterExpr pattern) =>
		new(source, pattern, null, RegexpOptions.None);

	internal static RegexpCountFunction Of(
		CharacterExpr source,
		CharacterExpr pattern,
		NumericExpr position,
		RegexpOptions options) =>
		new(source, pattern, position, options);

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.REGEXP_COUNT)
		.OpenParenthesis()
		.Append(_source)
		.PrependComma(_pattern)
		.PrependCommaIfNotNull(_position)
		.PrependCommaIf(!_options.IsNone(), _options.ToSql())
		.CloseParenthesis();

	private RegexpCountFunction(
		CharacterExpr source,
		CharacterExpr pattern,
		NumericExpr? position,
		RegexpOptions options)
	{
		_source = source;
		_pattern = pattern;
		_position = position;
		_options = options;
	}
}
