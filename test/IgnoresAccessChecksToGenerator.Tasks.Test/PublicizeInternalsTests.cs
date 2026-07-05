using IgnoresAccessChecksToGenerator.Tasks;

namespace IgnoresAccessChecksToGenerator.Tasks.Test;

[TestClass]
public class PublicizeInternalsTests
{
    [TestMethod]
    public void BuildAttributeFileContent_DefaultBehavior_EmitsAttributeDefinition()
    {
        var content = PublicizeInternals.BuildAttributeFileContent(["TestAssembly"], omitAttributeDefinition: false);

        StringAssert.Contains(content, "class IgnoresAccessChecksToAttribute");
        StringAssert.Contains(content, "[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"TestAssembly\")]");
    }

    [TestMethod]
    public void BuildAttributeFileContent_OmitAttributeDefinition_DoesNotEmitAttributeClass()
    {
        var content = PublicizeInternals.BuildAttributeFileContent(["TestAssembly"], omitAttributeDefinition: true);

        Assert.IsFalse(content.Contains("class IgnoresAccessChecksToAttribute"));
        StringAssert.Contains(content, "[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"TestAssembly\")]");
    }

    [TestMethod]
    public void BuildAttributeFileContent_MultipleAssemblies_EmitsAllAssemblyAttributes()
    {
        var content = PublicizeInternals.BuildAttributeFileContent(["Assembly1", "Assembly2"], omitAttributeDefinition: false);

        StringAssert.Contains(content, "[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"Assembly1\")]");
        StringAssert.Contains(content, "[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"Assembly2\")]");
        StringAssert.Contains(content, "class IgnoresAccessChecksToAttribute");
    }

    [TestMethod]
    public void BuildAttributeFileContent_MultipleAssembliesOmitDefinition_EmitsAllAssemblyAttributesWithoutClass()
    {
        var content = PublicizeInternals.BuildAttributeFileContent(["Assembly1", "Assembly2"], omitAttributeDefinition: true);

        StringAssert.Contains(content, "[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"Assembly1\")]");
        StringAssert.Contains(content, "[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"Assembly2\")]");
        Assert.IsFalse(content.Contains("class IgnoresAccessChecksToAttribute"));
    }
}

[TestClass]
public class TypeExclusionsTests
{
    [TestMethod]
    public void GlobalExclusion_AppliesToAllAssemblies()
    {
        var exclusions = new PublicizeInternals.TypeExclusions();
        exclusions.Add("Namespace.TypeName", assemblyName: "");

        Assert.IsTrue(exclusions.IsExcluded("AssemblyA", "Namespace.TypeName"));
        Assert.IsTrue(exclusions.IsExcluded("AssemblyB", "Namespace.TypeName"));
    }

    [TestMethod]
    public void AssemblyScopedExclusion_AppliesOnlyToThatAssembly()
    {
        var exclusions = new PublicizeInternals.TypeExclusions();
        exclusions.Add("Namespace.TypeName", assemblyName: "AssemblyA");

        Assert.IsTrue(exclusions.IsExcluded("AssemblyA", "Namespace.TypeName"));
        Assert.IsFalse(exclusions.IsExcluded("AssemblyB", "Namespace.TypeName"));
    }

    [TestMethod]
    public void AssemblyScopedExclusion_IsCaseInsensitive()
    {
        var exclusions = new PublicizeInternals.TypeExclusions();
        exclusions.Add("Namespace.TypeName", assemblyName: "AssemblyA");

        Assert.IsTrue(exclusions.IsExcluded("assemblya", "namespace.typename"));
    }

    [TestMethod]
    public void UnknownType_IsNotExcluded()
    {
        var exclusions = new PublicizeInternals.TypeExclusions();
        exclusions.Add("Namespace.TypeName", assemblyName: "AssemblyA");

        Assert.IsFalse(exclusions.IsExcluded("AssemblyA", "Namespace.OtherType"));
    }

    [TestMethod]
    public void EmptyTypeName_IsIgnored()
    {
        var exclusions = new PublicizeInternals.TypeExclusions();
        exclusions.Add("", assemblyName: "AssemblyA");

        Assert.IsFalse(exclusions.IsExcluded("AssemblyA", ""));
    }
}
