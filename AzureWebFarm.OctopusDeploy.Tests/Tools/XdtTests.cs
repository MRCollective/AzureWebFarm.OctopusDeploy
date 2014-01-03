using System.IO;
using System.Runtime.CompilerServices;
using ApprovalTests;
using ApprovalTests.Reporters;
using Microsoft.Web.XmlTransform;
using Xunit;
using Shouldly;

namespace AzureWebFarm.OctopusDeploy.Tests.Tools
{
    [UseReporter(typeof(DiffReporter))]
    public class XdtTests
    {
        private static readonly string TestPath = Path.GetDirectoryName(typeof (XdtTests).Assembly.CodeBase.Replace("file:///", ""));
        private static readonly string ToolsPath = Path.Combine(TestPath, "Tools");
        private static readonly string ExamplePath = Path.Combine(ToolsPath, "ExampleFiles");

        private static readonly string ExampleCloudProject = Path.Combine(ExamplePath, "CloudProject.ccproj");
        private static readonly string ExampleServiceDefinition = Path.Combine(ExamplePath, "ServiceDefinition.csdef");
        private static readonly string ExampleServiceConfiguration = Path.Combine(ExamplePath, "ServiceConfiguration.cscfg");

        private static readonly string AlreadyTransformedCloudProject = Path.Combine(ExamplePath, "CloudProject.transformed.ccproj");
        private static readonly string AlreadyTransformedServiceDefinition = Path.Combine(ExamplePath, "ServiceDefinition.transformed.csdef");
        private static readonly string AlreadyTransformedServiceConfiguration = Path.Combine(ExamplePath, "ServiceConfiguration.transformed.cscfg");

        private static readonly string XdtCloudProject = Path.Combine(ToolsPath, "CloudProject.ccproj.xdt.xml");
        private static readonly string XdtServiceDefinition = Path.Combine(ToolsPath, "ServiceDefinition.csdef.xdt.xml");
        private static readonly string XdtServiceConfiguration = Path.Combine(ToolsPath, "ServiceConfiguration.cscfg.xdt.xml");

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WhenExecutingCloudProjectTransform_ThenCorrectlyTransformExampleFile()
        {
            var result = PerformXdtTransform(ExampleCloudProject, XdtCloudProject);

            Approvals.VerifyXml(result);
        }

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WhenExecutingCloudProjectTransform_ThenItShouldBeIdempotent()
        {
            var result = PerformXdtTransform(AlreadyTransformedCloudProject, XdtCloudProject);

            result.ShouldBe(File.ReadAllText(AlreadyTransformedCloudProject));
        }

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WhenExecutingServiceDefinitionTransform_ThenCorrectlyTransformExampleFile()
        {
            var result = PerformXdtTransform(ExampleServiceDefinition, XdtServiceDefinition);

            Approvals.VerifyXml(result);
        }

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WhenExecutingServiceDefinitionTransform_ThenItShouldBeIdempotent()
        {
            var result = PerformXdtTransform(AlreadyTransformedServiceDefinition, XdtServiceDefinition);

            result.ShouldBe(File.ReadAllText(AlreadyTransformedServiceDefinition));
        }

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WhenExecutingServiceConfigurationTransform_ThenCorrectlyTransformExampleFile()
        {
            var result = PerformXdtTransform(ExampleServiceConfiguration, XdtServiceConfiguration);

            Approvals.VerifyXml(result);
        }

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void WhenExecutingServiceConfigurationTransform_ThenItShouldBeIdempotent()
        {
            var result = PerformXdtTransform(AlreadyTransformedServiceConfiguration, XdtServiceConfiguration);

            result.ShouldBe(File.ReadAllText(AlreadyTransformedServiceConfiguration));
        }

        private static string PerformXdtTransform(string exampleFile, string xdtFile)
        {
            var doc = new XmlTransformableDocument {PreserveWhitespace = true};
            doc.Load(exampleFile);
            var xdt = new XmlTransformation(xdtFile);

            var result = xdt.Apply(doc);

            result.ShouldBe(true);
            using (var stream = new MemoryStream())
            {
                doc.Save(stream);
                stream.Position = 0;
                var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }
    }
}
