using System;
using Microsoft.CodeAnalysis.CSharp;

namespace IgnoresAccessChecksToGenerator.Test
{
    public class Program
    {
        static void Main(string[] args)
        {
            var c = CSharpCompilation.Create("X");
            Console.WriteLine(c.HasSubmissionResult());
        }
    }
}
