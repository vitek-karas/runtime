// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Repro
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class Program
	{

		public static void Main ()
		{
			TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ();
		}

		[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Analyzer | ProducedBy.Trimmer)]
		static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
		{
			yield return 1;
			MethodWithGenericWhichRequiresMethods<TypeWithMethodWithRequires> ();
		}

		class TypeWithMethodWithRequires
		{
			[RequiresUnreferencedCode ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			[RequiresAssemblyFiles ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			[RequiresDynamicCode ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			static void MethodWithRequires ()
			{
			}
		}

		static void MethodWithGenericWhichRequiresMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] T> ()
		{
		}
	}
}
