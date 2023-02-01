﻿// Copyright © 2012-2023 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Vlingo.Xoom.Common.Compiler.DynaFile;

namespace Vlingo.Xoom.Common.Compiler;

public class DynaCompiler
{
    public Type? Compile(Input input)
    {
        try
        {
            string sourceCode;
            using (var stream = input.SourceFile.OpenText())
            {
                sourceCode = stream.ReadToEnd();
            }

            var tree = SyntaxFactory.ParseSyntaxTree(sourceCode);

            var assembliesToLoad = new HashSet<Assembly>
            {
                typeof(object).GetTypeInfo().Assembly,
                input.Protocol.GetTypeInfo().Assembly,
                GetType().Assembly, // Adding Vlingo.Xoom.Common
                Assembly.GetCallingAssembly(),
                Assembly.Load(new AssemblyName("System.Runtime")),
                Assembly.Load(new AssemblyName("mscorlib")),
                Assembly.Load(new AssemblyName("netstandard")),
            };

            // Adding Vlingo.Xoom.Actors, if available
            TryLoadActorsAssembly(assembliesToLoad);

            input.Protocol.Assembly
                .GetReferencedAssemblies()
                .Select(x => Assembly.Load(x))
                .ToList()
                .ForEach(x => assembliesToLoad.Add(x));

            var metadataRefs = assembliesToLoad.Select(x => MetadataReference.CreateFromFile(x.Location));

            var compilation = CSharpCompilation
                .Create(input.FullyQualifiedClassName)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(metadataRefs)
                .AddSyntaxTrees(tree);

            byte[] byteCode;

            using (var ilStream = new MemoryStream())
            {
                var compilationResult = compilation.Emit(ilStream);
                if (!compilationResult.Success)
                {
                    throw new Exception(compilationResult.Diagnostics[0].GetMessage());
                }

                ilStream.Seek(0, SeekOrigin.Begin);
                byteCode = ilStream.ToArray();
            }

            Persist(input, byteCode);

            var lookupTypeName = input.FullyQualifiedNameForTypeLookup;

            // to prevent exception when trying to generate and load proxy of same interface in parallel from different places/thread
            return input.ClassLoader.LoadClass(lookupTypeName) ?? input.ClassLoader.AddDynaClass(lookupTypeName, byteCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dynamically generated class source for {input.FullyQualifiedClassName} did not compile because: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        throw new ArgumentException($"Dynamically generated class source did not compile: {input.FullyQualifiedClassName}");
    }

    private void TryLoadActorsAssembly(HashSet<Assembly> assembliesToLoad)
    {
        try
        {
            var assembly = Assembly.Load(new AssemblyName("Vlingo.Xoom.Actors"));
            assembliesToLoad.Add(assembly);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void Persist(Input input, byte[] byteCode)
    {
        if (!input.Persist)
        {
            return;
        }

        string relativePathToClass = ToFullPath(input.FullyQualifiedClassName);
        string pathToCompiledClass = ToNamespacePath(input.FullyQualifiedClassName);
        string rootOfGenerated = input.Type == DynaType.Main ? RootOfMainClasses : RootOfTestClasses;
        var directory = new DirectoryInfo(rootOfGenerated + pathToCompiledClass);
        if (!directory.Exists)
        {
            directory.Create();
        }
        string pathToClass = rootOfGenerated + relativePathToClass + ".dll";

        PersistDynaClass(pathToClass, byteCode);
    }
}