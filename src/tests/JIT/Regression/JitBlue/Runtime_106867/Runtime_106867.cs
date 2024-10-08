// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// Generated by Fuzzlyn v2.3 on 2024-08-23 10:10:06
// Run on Arm64 Windows
// Seed: 13584223539078280353-vectort,vector64,vector128,armsve
// Reduced from 87.4 KiB to 0.8 KiB in 00:00:52
// Hits JIT assert in Release:
// Assertion failed 'secondId->idReg1() != secondId->idReg4()' in 'S0:M6(ubyte,double):this' during 'Emit code' (IL size 81; hash 0x596acd7c; FullOpts)
//
//     File: C:\dev\dotnet\runtime2\src\coreclr\jit\emitarm64sve.cpp Line: 18601
//
using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

public struct S0
{
    public void M6(byte arg0, double arg1)
    {
        var vr0 = Vector128.CreateScalar(119.12962f).AsVector();
        var vr3 = Runtime_106867.s_2;
        var vr4 = Vector128.CreateScalar(1f).AsVector();
        var vr5 = Runtime_106867.s_2;
        var vr2 = Sve.FusedMultiplySubtractNegated(vr3, vr4, vr5);
        if ((Sve.ConditionalExtractLastActiveElement(vr0, 0, vr2) < 0))
        {
            this = this;
        }
    }
}

public class Runtime_106867
{
    public static Vector<float> s_2;
    public static double[] s_5 = new double[]
    {
        0
    };
    public static byte s_16;

    [Fact]
    public static void TestEntryPoint()
    {
        if (Sve.IsSupported)
        {
            var vr6 = s_5[0];
            new S0().M6(s_16, vr6);
        }
    }
}
