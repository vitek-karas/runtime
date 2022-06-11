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
			TestOtherMemberTypesWithRequires ();
		}

		[ExpectedWarning ("IL2026", "MemberTypesWithRequires.field")]
		[ExpectedWarning ("IL3050", "MemberTypesWithRequires.field", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Property.set")]
		[ExpectedWarning ("IL3050", "MemberTypesWithRequires.Property.set", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.remove")]
		[ExpectedWarning ("IL3050", "MemberTypesWithRequires.Event.remove", ProducedBy = ProducedBy.Analyzer | ProducedBy.NativeAot)]
		static void TestOtherMemberTypesWithRequires ()
		{
			MemberTypesWithRequires.field = 1;
			MemberTypesWithRequires.Property = 1;
			MemberTypesWithRequires.Event -= null;
		}

		[RequiresUnreferencedCode ("--MemberTypesWithRequires--")]
		[RequiresDynamicCode ("--MemberTypesWithRequires--")]
		class MemberTypesWithRequires
		{
			public static int field;
			public static int Property { get; set; }

			// These should not be reported https://github.com/mono/linker/issues/2218
			[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.add", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.add", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.remove", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "MemberTypesWithRequires.Event.remove", ProducedBy = ProducedBy.Trimmer)]
			public static event EventHandler Event;
		}
	}
}
