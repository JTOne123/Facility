using System.Collections.Generic;
using System.IO;
using System.Linq;
using Facility.Definition.Swagger;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Facility.Definition.UnitTests.Swagger
{
	[TestFixture]
	public class SimpleSwaggerTests
	{
		[Test]
		public void GenerateSimpleService()
		{
			var service = new SwaggerParser().ConvertSwaggerService(s_swaggerService);
			service.Summary.Should().Be("TestApi");
			service.Methods.Count.Should().Be(1);
		}

		[Test]
		public void GenerateSimpleServiceJson()
		{
			var generator = new SwaggerGenerator { GeneratorName = "tests" };
			var fsdService = TestUtility.ParseTestApi(s_fsdText);
			var file = generator.GenerateOutput(fsdService).Files.Single();
			file.Name.Should().Be("TestApi.json");
			var jToken = JToken.Parse(file.Text);
			var jTokenExpected = JToken.FromObject(s_swaggerService, JsonSerializer.Create(SwaggerUtility.JsonSerializerSettings));
			JToken.DeepEquals(jToken, jTokenExpected).Should().BeTrue("{0} should be {1}", jToken, jTokenExpected);

			var service = new SwaggerParser().ParseDefinition(new ServiceDefinitionText(name: file.Name, text: file.Text));
			service.Summary.Should().Be("TestApi");
			service.Methods.Count.Should().Be(fsdService.Methods.Count);

			service = new SwaggerParser().ConvertSwaggerService(s_swaggerService);
			service.Summary.Should().Be("TestApi");
			service.Methods.Count.Should().Be(fsdService.Methods.Count);
		}

		[Test]
		public void GenerateSimpleServiceYaml()
		{
			var generator = new SwaggerGenerator { Yaml = true, GeneratorName = "tests" };
			var fsdService = TestUtility.ParseTestApi(s_fsdText);
			var file = generator.GenerateOutput(fsdService).Files.Single();
			file.Name.Should().Be("TestApi.yaml");
			var jToken = JToken.FromObject(new YamlDotNet.Serialization.DeserializerBuilder().Build().Deserialize(new StringReader(file.Text)));
			var jTokenExpected = JToken.FromObject(s_swaggerService, JsonSerializer.Create(SwaggerUtility.JsonSerializerSettings));
			JToken.DeepEquals(jToken, jTokenExpected).Should().BeTrue("{0} should be {1}", jToken, jTokenExpected);

			var service = new SwaggerParser().ParseDefinition(new ServiceDefinitionText(name: file.Name, text: file.Text));
			service.Summary.Should().Be("TestApi");
			service.Methods.Count.Should().Be(fsdService.Methods.Count);
		}

		static readonly string s_fsdText = @"
			service TestApi
			{
				method do
				{
				}:
				{
				}
			}";

		static readonly SwaggerService s_swaggerService = new SwaggerService
		{
			Swagger = "2.0",
			Info = new SwaggerInfo
			{
				Identifier = "TestApi",
				Title = "TestApi",
				Version = "0.0.0",
				CodeGen = "DO NOT EDIT: generated by tests",
			},
			Paths = new Dictionary<string, SwaggerOperations>
			{
				["/do"] = new SwaggerOperations
				{
					Post = new SwaggerOperation
					{
						OperationId = "do",
						Responses = new Dictionary<string, SwaggerResponse>
						{
							["200"] = new SwaggerResponse
							{
								Description = "",
							},
						}
					}
				}
			}
		};
	}
}
