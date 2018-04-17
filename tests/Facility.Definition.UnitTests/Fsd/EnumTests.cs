using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Facility.Definition.UnitTests.Fsd
{
	public sealed class EnumTests
	{
		[Test]
		public void OneMinimalEnum()
		{
			var service = TestUtility.ParseTestApi("service TestApi { enum One { X } }");

			var enumInfo = service.Enums.Single();
			enumInfo.Name.Should().Be("One");
			enumInfo.Attributes.Count.Should().Be(0);
			enumInfo.Summary.Should().Be("");
			enumInfo.Remarks.Count.Should().Be(0);
			var enumValue = enumInfo.Values.Single();
			enumValue.Name.Should().Be("X");
			enumValue.Attributes.Count.Should().Be(0);
			enumValue.Summary.Should().Be("");
			service.Enums.FirstOrDefault(x => x.Name == "One").Should().Be(enumInfo);

			TestUtility.GenerateFsd(service).Should().Equal(
				"// DO NOT EDIT: generated by TestUtility",
				"",
				"service TestApi",
				"{",
				"\tenum One",
				"\t{",
				"\t\tX,",
				"\t}",
				"}",
				"");
		}
	}
}
