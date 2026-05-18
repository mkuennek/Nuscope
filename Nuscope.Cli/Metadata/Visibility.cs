using System.Reflection;
using System.Reflection.Metadata;

namespace Nuscope.Cli;

internal static class Visibility
{
    /// <summary>
    /// Converts type visibility metadata into the C# accessibility text shown in reports.
    /// </summary>
    public static string FromTypeAttributes(TypeAttributes attributes) =>
        (attributes & TypeAttributes.VisibilityMask) switch
        {
            TypeAttributes.Public or TypeAttributes.NestedPublic => "public",
            TypeAttributes.NestedFamily => "protected",
            TypeAttributes.NestedFamORAssem => "protected internal",
            TypeAttributes.NestedFamANDAssem => "private protected",
            TypeAttributes.NestedAssembly => "internal",
            _ => "private"
        };

    /// <summary>
    /// Converts field visibility metadata into the C# accessibility text shown in reports.
    /// </summary>
    public static string FromFieldAttributes(FieldAttributes attributes) =>
        (attributes & FieldAttributes.FieldAccessMask) switch
        {
            FieldAttributes.Public => "public",
            FieldAttributes.Family => "protected",
            FieldAttributes.FamORAssem => "protected internal",
            FieldAttributes.FamANDAssem => "private protected",
            FieldAttributes.Assembly => "internal",
            _ => "private"
        };

    /// <summary>
    /// Converts method visibility metadata into the C# accessibility text shown in reports.
    /// </summary>
    public static string FromMethodAttributes(MethodAttributes attributes) =>
        (attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public => "public",
            MethodAttributes.Family => "protected",
            MethodAttributes.FamORAssem => "protected internal",
            MethodAttributes.FamANDAssem => "private protected",
            MethodAttributes.Assembly => "internal",
            _ => "private"
        };

    /// <summary>
    /// Determines property visibility from its getter and setter methods, preferring public when any accessor is public.
    /// </summary>
    public static string FromAccessor(MetadataReader reader, PropertyAccessors accessors)
    {
        var visibilities = new List<string>(3);
        Add(reader, accessors.Getter);
        Add(reader, accessors.Setter);
        return visibilities.Contains("public", StringComparer.Ordinal) ? "public" : visibilities.FirstOrDefault() ?? "private";

        void Add(MetadataReader metadataReader, MethodDefinitionHandle handle)
        {
            if (!handle.IsNil)
            {
                visibilities.Add(FromMethodAttributes(metadataReader.GetMethodDefinition(handle).Attributes));
            }
        }
    }

    /// <summary>
    /// Determines event visibility from add, remove, and raise methods, preferring public when any accessor is public.
    /// </summary>
    public static string FromAccessor(MetadataReader reader, EventAccessors accessors)
    {
        var visibilities = new List<string>(3);
        Add(reader, accessors.Adder);
        Add(reader, accessors.Remover);
        Add(reader, accessors.Raiser);
        return visibilities.Contains("public", StringComparer.Ordinal) ? "public" : visibilities.FirstOrDefault() ?? "private";

        void Add(MetadataReader metadataReader, MethodDefinitionHandle handle)
        {
            if (!handle.IsNil)
            {
                visibilities.Add(FromMethodAttributes(metadataReader.GetMethodDefinition(handle).Attributes));
            }
        }
    }
}
