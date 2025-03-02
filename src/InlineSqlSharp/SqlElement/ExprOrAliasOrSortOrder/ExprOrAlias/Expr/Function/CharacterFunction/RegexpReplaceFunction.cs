namespace InlineSqlSharp;

public sealed class RegexpReplaceFunction : CharacterExpr
{
	private readonly CharacterExpr _source;
	private readonly CharacterExpr _pattern;
	private readonly CharacterExpr _replacement;
	private readonly NumericExpr? _position;
	private readonly NumericExpr? _occurrence;
	private readonly RegexpOptions _options;

	public static RegexpReplaceFunction Of(
		CharacterExpr source,
		CharacterExpr pattern,
		CharacterExpr replacement) =>
		new(source, pattern, replacement, null, null, RegexpOptions.None);

	public static RegexpReplaceFunction Of(
		CharacterExpr source,
		CharacterExpr pattern,
		CharacterExpr replacement,
		NumericExpr position) =>
		new(source, pattern, replacement, position, null, RegexpOptions.None);

	public static RegexpReplaceFunction Of(
		CharacterExpr source,
		CharacterExpr pattern,
		CharacterExpr replacement,
		NumericExpr position,
		NumericExpr occurrence,
		RegexpOptions options = RegexpOptions.None) =>
		new(source, pattern, replacement, position, occurrence, options);

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.REGEXP_REPLACE)
		.OpenParenthesis()
		.Append(_source)
		.PrependComma(_pattern)
		.PrependComma(_replacement)
		.PrependCommaIfNotNull(_position)
		.PrependCommaIfNotNull(_occurrence)
		.PrependCommaIf(!_options.IsNone(), _options.ToSql())
		.CloseParenthesis();

	private RegexpReplaceFunction(
		CharacterExpr source,
		CharacterExpr pattern,
		CharacterExpr replacement,
		NumericExpr? position,
		NumericExpr? occurrence,
		RegexpOptions options)
	{
		_source = source;
		_pattern = pattern;
		_replacement = replacement;
		_position = position;
		_occurrence = occurrence;
		_options = options;
	}
}
