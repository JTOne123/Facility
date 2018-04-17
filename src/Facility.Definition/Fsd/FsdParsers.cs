using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Faithlife.Parsing;

namespace Facility.Definition.Fsd
{
	internal static class FsdParsers
	{
		public static ServiceInfo ParseDefinition(ServiceDefinitionText source, IReadOnlyDictionary<string, FsdRemarksSection> remarksSections) =>
			DefinitionParser(new Context(source, remarksSections)).Parse(source.Text);

		private static IParser<string> CommentParser { get; } =
			Parser.Regex(@"//([^\r\n]*)(\r\n?|\n|$)").Select(x => x.Groups[1].ToString());

		private static IParser<string> CommentOrWhiteSpaceParser { get; } =
			CommentParser.Or(Parser.WhiteSpace.AtLeastOnce().Success(""));

		private static IParser<T> CommentedToken<T>(this IParser<T> parser) =>
			parser.PrecededBy(CommentOrWhiteSpaceParser.Many()).TrimEnd();

		private static IParser<string> PunctuationParser(string token) =>
			Parser.String(token).CommentedToken().Named("'" + token + "'");

		private static IParser<string> NameParser { get; } =
			Parser.Regex(@"[a-zA-Z_][0-9a-zA-Z_]*").Select(x => x.ToString()).CommentedToken();

		private static IParser<string> KeywordParser(params string[] keywords) =>
			NameParser.Where(keywords.Contains).Named(string.Join(" or ", keywords.Select(x => "'" + x + "'")));

		private static IParser<IReadOnlyList<T>> Delimited<T>(this IParser<T> parser, string delimiter) =>
			parser.Delimited(PunctuationParser(delimiter));

		private static IParser<IReadOnlyList<T>> DelimitedAllowTrailing<T>(this IParser<T> parser, string delimiter) =>
			parser.DelimitedAllowTrailing(PunctuationParser(delimiter));

		private static IParser<T> Bracketed<T>(this IParser<T> parser, string openBracket, string closeBracket) =>
			parser.Bracketed(PunctuationParser(openBracket), PunctuationParser(closeBracket));

		private static IParser<Match> AttributeParameterValueParser { get; } =
			Parser.Regex(@"""(([^""\\]+|\\[""\\/bfnrt]|\\u[0-9a-fA-f]{4})*)""|([0-9a-zA-Z.+_-]+)");

		private static IParser<ServiceAttributeParameterInfo> AttributeParameterParser(Context context) =>
			from name in NameParser.Named("parameter name").Positioned()
			from colon in PunctuationParser(":")
			from value in AttributeParameterValueParser.Named("parameter value").Positioned()
			select new ServiceAttributeParameterInfo(name.Value, TryParseAttributeParameterValue(value.Value),
				context.GetPart(ServicePartKind.Name, name),
				context.GetPart(ServicePartKind.Value, value));

		private static IParser<ServiceAttributeInfo> AttributeParser(Context context) =>
			from name in NameParser.Named("attribute name").Positioned()
			from parameters in AttributeParameterParser(context).Delimited(",").Bracketed("(", ")").OrDefault()
			select new ServiceAttributeInfo(name.Value, parameters,
				context.GetPart(ServicePartKind.Name, name));

		private static IParser<ServiceEnumValueInfo> EnumValueParser(Context context) =>
			from comments1 in CommentOrWhiteSpaceParser.Many()
			from attributes in AttributeParser(context).Delimited(",").Bracketed("[", "]").Many()
			from comments2 in CommentOrWhiteSpaceParser.Many()
			from name in NameParser.Named("value name").Positioned()
			select new ServiceEnumValueInfo(name.Value, attributes.SelectMany(x => x),
				BuildSummary(comments1, comments2),
				context.GetPart(ServicePartKind.Name, name));

		private static IParser<ServiceEnumInfo> EnumParser(Context context) =>
			from comments1 in CommentOrWhiteSpaceParser.Many()
			from attributes in AttributeParser(context).Delimited(",").Bracketed("[", "]").Many()
			from comments2 in CommentOrWhiteSpaceParser.Many()
			from keyword in KeywordParser("enum").Positioned()
			from name in NameParser.Named("enum name").Positioned()
			from values in EnumValueParser(context).DelimitedAllowTrailing(",").Bracketed("{", "}")
			select new ServiceEnumInfo(name.Value, values,
				attributes.SelectMany(x => x),
				BuildSummary(comments1, comments2),
				context.GetRemarksSection(name.Value)?.Lines,
				context.GetPart(ServicePartKind.Keyword, keyword),
				context.GetPart(ServicePartKind.Name, name));

		private static IParser<ServiceErrorInfo> ErrorParser(Context context) =>
			from comments1 in CommentOrWhiteSpaceParser.Many()
			from attributes in AttributeParser(context).Delimited(",").Bracketed("[", "]").Many()
			from comments2 in CommentOrWhiteSpaceParser.Many()
			from name in NameParser.Named("error name").Positioned()
			select new ServiceErrorInfo(name.Value,
				attributes.SelectMany(x => x),
				BuildSummary(comments1, comments2),
				context.GetPart(ServicePartKind.Name, name));

		private static IParser<ServiceErrorSetInfo> ErrorSetParser(Context context) =>
			from comments1 in CommentOrWhiteSpaceParser.Many()
			from attributes in AttributeParser(context).Delimited(",").Bracketed("[", "]").Many()
			from comments2 in CommentOrWhiteSpaceParser.Many()
			from keyword in KeywordParser("errors").Positioned()
			from name in NameParser.Named("errors name").Positioned()
			from errors in ErrorParser(context).DelimitedAllowTrailing(",").Bracketed("{", "}")
			select new ServiceErrorSetInfo(name.Value, errors,
				attributes.SelectMany(x => x),
				BuildSummary(comments1, comments2),
				context.GetRemarksSection(name.Value)?.Lines,
				context.GetPart(ServicePartKind.Keyword, keyword),
				context.GetPart(ServicePartKind.Name, name));

		private static IParser<string> TypeParser { get; } = Parser.Regex(@"[0-9a-zA-Z_<>[\]]+").Select(x => x.ToString()).CommentedToken();

		private static IParser<ServiceFieldInfo> FieldParser(Context context) =>
			from comments1 in CommentOrWhiteSpaceParser.Many()
			from attributes in AttributeParser(context).Delimited(",").Bracketed("[", "]").Many()
			from comments2 in CommentOrWhiteSpaceParser.Many()
			from name in NameParser.Named("field name").Positioned()
			from colon in PunctuationParser(":")
			from typeName in TypeParser.Named("field type name").Positioned()
			from semicolon in PunctuationParser(";")
			select new ServiceFieldInfo(name.Value, typeName.Value,
				attributes.SelectMany(x => x),
				BuildSummary(comments1, comments2),
				context.GetPart(ServicePartKind.Name, name),
				context.GetPart(ServicePartKind.TypeName, typeName));

		private static IParser<ServiceDtoInfo> DtoParser(Context context) =>
			from comments1 in CommentOrWhiteSpaceParser.Many()
			from attributes in AttributeParser(context).Delimited(",").Bracketed("[", "]").Many()
			from comments2 in CommentOrWhiteSpaceParser.Many()
			from keyword in KeywordParser("data").Positioned()
			from name in NameParser.Named("data name").Positioned()
			from fields in FieldParser(context).Many().Bracketed("{", "}")
			select new ServiceDtoInfo(name.Value, fields,
				attributes.SelectMany(x => x),
				BuildSummary(comments1, comments2),
				context.GetRemarksSection(name.Value)?.Lines,
				context.GetPart(ServicePartKind.Keyword, keyword),
				context.GetPart(ServicePartKind.Name, name));

		private static IParser<ServiceMethodInfo> MethodParser(Context context) =>
			from comments1 in CommentOrWhiteSpaceParser.Many()
			from attributes in AttributeParser(context).Delimited(",").Bracketed("[", "]").Many()
			from comments2 in CommentOrWhiteSpaceParser.Many()
			from keyword in KeywordParser("method").Positioned()
			from name in NameParser.Named("method name").Positioned()
			from requestFields in FieldParser(context).Many().Bracketed("{", "}")
			from colon in PunctuationParser(":")
			from responseFields in FieldParser(context).Many().Bracketed("{", "}")
			select new ServiceMethodInfo(name.Value, requestFields, responseFields,
				attributes.SelectMany(x => x),
				BuildSummary(comments1, comments2),
				context.GetRemarksSection(name.Value)?.Lines,
				context.GetPart(ServicePartKind.Keyword, keyword),
				context.GetPart(ServicePartKind.Name, name));

		private static IParser<ServiceMemberInfo> ServiceItemParser(Context context) =>
			Parser.Or<ServiceMemberInfo>(EnumParser(context), DtoParser(context), MethodParser(context), ErrorSetParser(context));

		private static IParser<ServiceInfo> ServiceParser(Context context) =>
			from comments1 in CommentOrWhiteSpaceParser.Many()
			from attributes in AttributeParser(context).Delimited(",").Bracketed("[", "]").Many()
			from comments2 in CommentOrWhiteSpaceParser.Many()
			from keyword in KeywordParser("service").Positioned()
			from name in NameParser.Named("service name").Positioned()
			from items in ServiceItemParser(context).Many().Bracketed("{", "}")
			select new ServiceInfo(name.Value, items,
				attributes.SelectMany(x => x),
				BuildSummary(comments1, comments2),
				context.GetRemarksSection(name.Value)?.Lines,
				context.GetPart(ServicePartKind.Keyword, keyword),
				context.GetPart(ServicePartKind.Name, name));

		private static IParser<ServiceInfo> DefinitionParser(Context context) =>
			from service in ServiceParser(context).FollowedBy(CommentOrWhiteSpaceParser.Many())
			from end in Parser.Success(true).End().Named("end")
			select service;

		private static string TryParseAttributeParameterValue(Match match)
		{
			return match.Groups[1].Success ?
				string.Concat(match.Groups[2].Captures.OfType<Capture>().Select(x => x.ToString()).Select(x => x[0] == '\\' ? DecodeBackslash(x) : x)) :
				match.Groups[3].ToString();
		}

		private static string DecodeBackslash(string text)
		{
			switch (text[1])
			{
			case 'b':
				return "\b";
			case 'f':
				return "\f";
			case 'n':
				return "\n";
			case 'r':
				return "\r";
			case 't':
				return "\t";
			case 'u':
				return new string((char) ushort.Parse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture), 1);
			default:
				return text.Substring(1);
			}
		}

		private static string BuildSummary(IEnumerable<string> comments1, IEnumerable<string> comments2)
		{
			return string.Join(" ", comments1.Concat(comments2).Where(x => x.Trim().Length != 0).Reverse().TakeWhile(x => x.Length > 2 && x[0] == '/' && x[1] == ' ').Reverse().Select(x => x.Substring(2).Trim()));
		}

		private sealed class Context
		{
			public Context(ServiceDefinitionText source, IReadOnlyDictionary<string, FsdRemarksSection> remarksSections)
			{
				m_source = source;
				m_remarksSections = remarksSections;
			}

			public ServicePart GetPart<T>(ServicePartKind kind, Positioned<T> positioned) => new ServicePart(kind, GetPosition(positioned.Position), GetPosition(positioned.Position.WithNextIndex(positioned.Length)));

			public FsdRemarksSection GetRemarksSection(string name)
			{
				m_remarksSections.TryGetValue(name, out var section);
				return section;
			}

			private ServiceDefinitionPosition GetPosition(TextPosition position)
			{
				var lineColumn = position.GetLineColumn();
				return new ServiceDefinitionPosition(m_source.Name, lineColumn.LineNumber, lineColumn.ColumnNumber);
			}

			readonly ServiceDefinitionText m_source;
			readonly IReadOnlyDictionary<string, FsdRemarksSection> m_remarksSections;
		}
	}
}
