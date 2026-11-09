// The game's interop assembly exports a stripped NullableAttribute with no usable
// constructors. Supply our own internal compiler metadata rather than binding to it.
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.All)]
internal sealed class NullableAttribute : Attribute
{
    public NullableAttribute(byte value) { }
    public NullableAttribute(byte[] values) { }
}
