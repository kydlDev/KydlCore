using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using KydlCore.Core;

namespace KydlCore.Common;

/// <summary>
/// KydlCore D-084: the attribute DrakesTestLab finds by NAME. The mod does not reference DrakesTestLab; a static method with this attribute becomes a test hook
/// (`{ "do": "hook", "name": "<Name>", "args": {...} }` in a suite). One copy, in KydlCore.dll; every mod uses it.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TestHookAttribute : Attribute
{
    public TestHookAttribute(string name) { Name = name; }
    public string Name { get; }
}
