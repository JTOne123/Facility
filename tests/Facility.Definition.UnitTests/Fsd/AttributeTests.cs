using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Facility.Definition.UnitTests.Fsd
{
	public sealed class AttributeTests
	{
		[Test]
		public void NoParameters()
		{
			var service = TestUtility.ParseTestApi("[x] service TestApi{}");

			var attribute = service.Attributes.Single();
			attribute.Name.Should().Be("x");
			attribute.Parameters.Count.Should().Be(0);

			TestUtility.GenerateFsd(service).Should().Equal(
				"// DO NOT EDIT: generated by TestUtility",
				"",
				"[x]",
				"service TestApi",
				"{",
				"}",
				"");
		}

		[Test]
		public void ZeroParameter()
		{
			var service = TestUtility.ParseTestApi("[x(y:0)] service TestApi{}");

			var attribute = service.Attributes.Single();
			attribute.Name.Should().Be("x");
			var parameter = attribute.Parameters.Single();
			parameter.Name.Should().Be("y");
			parameter.Value.Should().Be("0");

			TestUtility.GenerateFsd(service).Should().Equal(
				"// DO NOT EDIT: generated by TestUtility",
				"",
				"[x(y: 0)]",
				"service TestApi",
				"{",
				"}",
				"");
		}

		[Test]
		public void TokenParameter()
		{
			var service = TestUtility.ParseTestApi("[x(y:1b-3D_5f.7H+9J)] service TestApi{}");

			var attribute = service.Attributes.Single();
			attribute.Name.Should().Be("x");
			var parameter = attribute.Parameters.Single();
			parameter.Name.Should().Be("y");
			parameter.Value.Should().Be("1b-3D_5f.7H+9J");

			TestUtility.GenerateFsd(service).Should().Equal(
				"// DO NOT EDIT: generated by TestUtility",
				"",
				"[x(y: 1b-3D_5f.7H+9J)]",
				"service TestApi",
				"{",
				"}",
				"");
		}

		[Test]
		public void EmptyStringParameter()
		{
			var service = TestUtility.ParseTestApi("[x(y:\"\")] service TestApi{}");

			var attribute = service.Attributes.Single();
			attribute.Name.Should().Be("x");
			var parameter = attribute.Parameters.Single();
			parameter.Name.Should().Be("y");
			parameter.Value.Should().Be("");

			TestUtility.GenerateFsd(service).Should().Equal(
				"// DO NOT EDIT: generated by TestUtility",
				"",
				"[x(y: \"\")]",
				"service TestApi",
				"{",
				"}",
				"");
		}

		[Test]
		public void JsonStringParameter()
		{
			var service = TestUtility.ParseTestApi(@"[x(y:""á\\\""\/\b\f\n\r\t\u0001\u1234!"")] service TestApi{}");

			var attribute = service.Attributes.Single();
			attribute.Name.Should().Be("x");
			var parameter = attribute.Parameters.Single();
			parameter.Name.Should().Be("y");
			parameter.Value.Should().Be("á\\\"/\b\f\n\r\t\u0001\u1234!");

			TestUtility.GenerateFsd(service).Should().Equal(
				"// DO NOT EDIT: generated by TestUtility",
				"",
				"[x(y: \"á\\\\\\\"/\\b\\f\\n\\r\\t\\u0001\u1234!\")]",
				"service TestApi",
				"{",
				"}",
				"");
		}

		[Test]
		public void ManyAttributesAndParameters()
		{
			var service = TestUtility.ParseTestApi("[x, x(x:0)] [x(x:0,y:1)] service TestApi{}");

			service.Attributes.Count.Should().Be(3);

			TestUtility.GenerateFsd(service).Should().Equal(
				"// DO NOT EDIT: generated by TestUtility",
				"",
				"[x]",
				"[x(x: 0)]",
				"[x(x: 0, y: 1)]",
				"service TestApi",
				"{",
				"}",
				"");
		}
	}
}
