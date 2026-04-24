using System;

namespace System.Runtime.InteropServices;

/// <summary>
/// A dummy attribute used by ClangSharp generated code to preserve original C/C++ type names.
/// The compiler just needs this to exist so it doesn't throw CS0246 errors.
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = true)]
internal sealed class NativeTypeNameAttribute : Attribute
{
    public NativeTypeNameAttribute(string name) { }
}