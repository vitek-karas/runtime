// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	public class BasicWarning
	{
		[ExpectedWarning ("IL2026", "RUC")]
		public static void Main ()
		{
			RUC ();
		}

		[RequiresUnreferencedCode ("RUC")]
		static void RUC () { }
	}
}
