using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raptor.Attributes
{
/// <summary>
/// Excludes a public method from automatic Raptor FFI registration.
/// Use this on helper methods inside a <see cref="RaptorModuleAttribute"/> class
/// that should not be exposed to scripts.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RaptorIgnoreAttribute : Attribute { }
}
