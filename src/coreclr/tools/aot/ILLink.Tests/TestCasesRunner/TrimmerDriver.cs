// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class TrimmerDriver
    {
        public void Trim (string rspFilePath)
        {
            Assembly ilcAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("ilc"));
            Type programType = ilcAssembly.GetType("ILCompiler.Program")!;
            object programObject = Activator.CreateInstance(programType, nonPublic: true)!;
            MethodInfo runMethod = programType.GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Instance)!;
            runMethod.Invoke(programObject, new object[] { new string[] { "@" + rspFilePath } });
        }
    }
}
