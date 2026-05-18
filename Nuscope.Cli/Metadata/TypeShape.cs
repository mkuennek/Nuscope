using System.Reflection;

namespace Nuscope.Cli;

internal sealed record TypeShape(TypeKind Kind, IReadOnlyList<string> Modifiers, string Classification)
{
    /// <summary>
    /// Builds the structured and display classification for a type definition.
    /// </summary>
    public static TypeShape FromAttributes(TypeAttributes attributes, string? baseType)
    {
        if (attributes.HasFlag(TypeAttributes.Interface))
        {
            return new TypeShape(TypeKind.Interface, [], "interface");
        }

        if (baseType is "System.Enum")
        {
            return new TypeShape(TypeKind.Enum, [], "enum");
        }

        if (baseType is "System.ValueType")
        {
            return new TypeShape(TypeKind.Struct, [], "struct");
        }

        if (baseType is "System.MulticastDelegate" or "System.Delegate")
        {
            return new TypeShape(TypeKind.Delegate, [], "delegate");
        }

        if (attributes.HasFlag(TypeAttributes.Sealed) && attributes.HasFlag(TypeAttributes.Abstract))
        {
            return new TypeShape(TypeKind.Class, ["static"], "static class");
        }

        if (attributes.HasFlag(TypeAttributes.Sealed))
        {
            return new TypeShape(TypeKind.Class, ["sealed"], "sealed class");
        }

        if (attributes.HasFlag(TypeAttributes.Abstract))
        {
            return new TypeShape(TypeKind.Class, ["abstract"], "abstract class");
        }

        return new TypeShape(TypeKind.Class, [], "class");
    }
}
