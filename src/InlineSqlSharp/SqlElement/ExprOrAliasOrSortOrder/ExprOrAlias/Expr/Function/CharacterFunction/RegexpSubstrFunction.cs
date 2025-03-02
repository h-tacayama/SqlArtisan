namespace InlineSqlSharp;

public sealed class RegexpSubstrFunction : CharacterExpr
{
	private readonly CharacterExpr _source;
	private readonly CharacterExpr _pattern;
	private readonly NumericExpr? _position;
	private readonly NumericExpr? _occurrence;
	private readonly RegexpOptions _options;
	private readonly NumericExpr? _subPatternPos;

	public override void FormatSql(SqlBuildingBuffer buffer) => buffer
		.Append(Keywords.REGEXP_SUBSTR)
		.OpenParenthesis()
		.Append(_source)
		.PrependComma(_pattern)
		.PrependCommaIfNotNull(_position)
		.PrependCommaIfNotNull(_occurrence)
		.PrependCommaIf(!_options.IsNone() || _subPatternPos is not null, _options.ToSql())
		.PrependCommaIfNotNull(_subPatternPos)
		.CloseParenthesis();

	internal static RegexpSubstrFunction Of(
		CharacterExpr source,
		CharacterExpr pattern) =>
		new(source, pattern, null, null, RegexpOptions.None, null);

	internal static RegexpSubstrFunction Of(
		CharacterExpr source,
		CharacterExpr pattern,
		NumericExpr position) =>
		new(source, pattern, position, null, RegexpOptions.None, null);

	internal static RegexpSubstrFunction Of(
		CharacterExpr source,
		CharacterExpr pattern,
		NumericExpr position,
		NumericExpr occurrence) =>
		new(source, pattern, position, occurrence, RegexpOptions.None, null);

	internal static RegexpSubstrFunction Of(
		CharacterExpr source,
		CharacterExpr pattern,
		NumericExpr position,
		NumericExpr occurrence,
		RegexpOptions options) =>
		new(source, pattern, position, occurrence, options, null);

	internal static RegexpSubstrFunction Of(
		CharacterExpr source,
		CharacterExpr pattern,
		NumericExpr position,
		NumericExpr occurrence,
		RegexpOptions options,
		NumericExpr subPatternPos) =>
		new(source, pattern, position, occurrence, options, subPatternPos);

	private RegexpSubstrFunction(
		CharacterExpr source,
		CharacterExpr pattern,
		NumericExpr? position,
		NumericExpr? occurrence,
		RegexpOptions options,
		NumericExpr? subPatternPos)
	{
		_source = source;
		_pattern = pattern;
		_position = position;
		_occurrence = occurrence;
		_options = options;
		_subPatternPos = subPatternPos;
	}
}
