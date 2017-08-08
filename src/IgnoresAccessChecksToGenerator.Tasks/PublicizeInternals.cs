using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace IgnoresAccessChecksToGenerator.Tasks
{
    public class PublicizeInternals : Task
    {
        private static readonly char[] Semicolon = { ';' };

        private readonly string _sourceDir = Directory.GetCurrentDirectory() + "\\";

        private readonly AssemblyResolver _resolver = new AssemblyResolver();

        [Required]
        public ITaskItem[] SourceReferences { get; set; }

        [Required]
        public string AssemblyNames { get; set; }

        [Output]
        public ITaskItem[] TargetReferences { get; set; }

        [Output]
        public ITaskItem[] RemovedReferences { get; set; }

        [Output]
        public ITaskItem[] GeneratedCodeFiles { get; set; }

        public override bool Execute()
        {
            if (SourceReferences == null) throw new ArgumentNullException(nameof(SourceReferences));

            var assemblyNames = new HashSet<string>(
                (AssemblyNames ?? string.Empty).Split(Semicolon, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);

            if (assemblyNames.Count == 0)
            {
                return true;
            }

            var targetPath = Path.Combine(_sourceDir, "obj", "GeneratedPublicizedAssemblies");
            Directory.CreateDirectory(targetPath);

            GenerateAttributes(targetPath, assemblyNames);

            foreach (var assemblyPath in SourceReferences
                .Select(a => Path.GetDirectoryName(GetFullFilePath(a.ItemSpec))))
            {
                _resolver.AddSearchDirectory(assemblyPath);
            }

            var targetReferences = new List<ITaskItem>();
            var removedReferences = new List<ITaskItem>();

            foreach (var assembly in SourceReferences)
            {
                var assemblyPath = GetFullFilePath(assembly.ItemSpec);
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                if (assemblyNames.Contains(assemblyName))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var targetAssemblyPath = Path.Combine(targetPath, Path.GetFileName(assemblyPath));

                    var targetAsemblyFileInfo = new FileInfo(targetAssemblyPath);
                    if (!targetAsemblyFileInfo.Exists || targetAsemblyFileInfo.Length == 0)
                    {
                        CreatePublicAssembly(assemblyPath, targetAssemblyPath);
                        Log.LogMessageFromText("Created publicized assembly at " + targetAssemblyPath, MessageImportance.Normal);
                    }
                    else
                    {
                        Log.LogMessageFromText("Publicized assembly already exists at " + targetAssemblyPath, MessageImportance.Low);
                    }

                    targetReferences.Add(new TaskItem(targetAssemblyPath));
                    removedReferences.Add(assembly);
                }
            }

            TargetReferences = targetReferences.ToArray();
            RemovedReferences = removedReferences.ToArray();

            return true;
        }

        private void GenerateAttributes(string path, IEnumerable<string> assemblyNames)
        {
            var attributes = string.Join(Environment.NewLine,
                assemblyNames.Select(a => $@"[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(""{a}"")]"));

            var content = attributes + @"

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
        }
    }
}";
            var filePath = Path.Combine(path, "IgnoresAccessChecksTo.cs");
            File.WriteAllText(filePath, content);

            GeneratedCodeFiles = new ITaskItem[] { new TaskItem(filePath) };

            Log.LogMessageFromText("Generated IgnoresAccessChecksTo attributes", MessageImportance.Low);
        }

        private void CreatePublicAssembly(string source, string target)
        {
            var assembly = AssemblyDefinition.ReadAssembly(source,
                new ReaderParameters { AssemblyResolver = _resolver });

            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.GetTypes())
                {
                    if (!type.IsNested && type.IsNotPublic)
                    {
                        type.IsPublic = true;
                    }
                    else if (type.IsNestedAssembly ||
                             type.IsNestedFamilyOrAssembly ||
                             type.IsNestedFamilyAndAssembly)
                    {
                        type.IsNestedPublic = true;
                    }

                    foreach (var field in type.Fields)
                    {
                        if (field.IsAssembly ||
                            field.IsFamilyOrAssembly ||
                            field.IsFamilyAndAssembly)
                        {
                            field.IsPublic = true;
                        }
                    }

                    foreach (var method in type.Methods)
                    {
                        method.Body?.Instructions?.Clear();
                        method.Body?.ExceptionHandlers?.Clear();

                        if (method.IsAssembly ||
                            method.IsFamilyOrAssembly ||
                            method.IsFamilyAndAssembly)
                        {
                            method.IsPublic = true;
                        }
                    }
                }
            }

            assembly.Write(target);
        }

        private string GetFullFilePath(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(_sourceDir, path);
            }

            path = Path.GetFullPath(path);
            return path;
        }

        private class AssemblyResolver : IAssemblyResolver
        {
            private readonly HashSet<string> _directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public void AddSearchDirectory(string directory)
            {
                _directories.Add(directory);
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                return Resolve(name, new ReaderParameters());
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                var assembly = SearchDirectory(name, _directories, parameters);
                if (assembly != null)
                {
                    return assembly;
                }

                throw new AssemblyResolutionException(name);
            }

            public void Dispose()
            {
            }

            private AssemblyDefinition SearchDirectory(AssemblyNameReference name, IEnumerable<string> directories, ReaderParameters parameters)
            {
                var extensions = name.IsWindowsRuntime ? new[] { ".winmd", ".dll" } : new[] { ".exe", ".dll" };
                foreach (var directory in directories)
                {
                    foreach (var extension in extensions)
                    {
                        var file = Path.Combine(directory, name.Name + extension);
                        if (!File.Exists(file))
                            continue;
                        try
                        {
                            return GetAssembly(file, parameters);
                        }
                        catch (BadImageFormatException)
                        {
                        }
                    }
                }

                return null;
            }

            private AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
            {
                if (parameters.AssemblyResolver == null)
                    parameters.AssemblyResolver = this;

                return ModuleDefinition.ReadModule(file, parameters).Assembly;
            }
        }
    }
}
