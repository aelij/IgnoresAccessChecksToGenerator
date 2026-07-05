using IgnoresAccessChecksToGenerator.Tasks;

namespace IgnoresAccessChecksToGenerator.Tasks.Test;

public class PublicizeInternalsTests
{
    [Fact]
    public void BuildAttributeFileContent_DefaultBehavior_EmitsAttributeDefinition()
    {
        var content = PublicizeInternals.BuildAttributeFileContent(["TestAssembly"], omitAttributeDefinition: false);

        Assert.Contains("class IgnoresAccessChecksToAttribute", content);
        Assert.Contains("[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"TestAssembly\")]", content);
    }

    [Fact]
    public void BuildAttributeFileContent_OmitAttributeDefinition_DoesNotEmitAttributeClass()
    {
        var content = PublicizeInternals.BuildAttributeFileContent(["TestAssembly"], omitAttributeDefinition: true);

        Assert.DoesNotContain("class IgnoresAccessChecksToAttribute", content);
        Assert.Contains("[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"TestAssembly\")]", content);
    }

    [Fact]
    public void BuildAttributeFileContent_MultipleAssemblies_EmitsAllAssemblyAttributes()
    {
        var content = PublicizeInternals.BuildAttributeFileContent(["Assembly1", "Assembly2"], omitAttributeDefinition: false);

        Assert.Contains("[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"Assembly1\")]", content);
        Assert.Contains("[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"Assembly2\")]", content);
        Assert.Contains("class IgnoresAccessChecksToAttribute", content);
    }

    [Fact]
    public void BuildAttributeFileContent_MultipleAssembliesOmitDefinition_EmitsAllAssemblyAttributesWithoutClass()
    {
        var content = PublicizeInternals.BuildAttributeFileContent(["Assembly1", "Assembly2"], omitAttributeDefinition: true);

        Assert.Contains("[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"Assembly1\")]", content);
        Assert.Contains("[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"Assembly2\")]", content);
        Assert.DoesNotContain("class IgnoresAccessChecksToAttribute", content);
    }
}
