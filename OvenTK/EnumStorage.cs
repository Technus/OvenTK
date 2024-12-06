using System.ComponentModel;
using System.Reflection;
#pragma warning disable S2743

namespace OvenTK.Lib;
/// <summary>
/// Persistent storage for enum metadata, to speedup access
/// </summary>
/// <typeparam name="TEnum"></typeparam>
public static class EnumStorage<TEnum> where TEnum : struct, Enum
{
    /// <summary>
    /// The <see langword="typeof"/> of <typeparamref name="TEnum"/>
    /// </summary>
    public static Type Type { get; }
    /// <summary>
    /// Helper dictionary to get <see cref="DescriptionAttribute"/> from enum
    /// </summary>
    public static IReadOnlyDictionary<TEnum, string?> Descriptions { get; }
    /// <summary>
    /// All defined enum values
    /// </summary>
    public static IReadOnlyList<TEnum> EnumValues { get; }
    /// <summary>
    /// All defined enum values except <see langword="default"/>
    /// </summary>
    public static IReadOnlyList<TEnum> EnumValuesWithoutDefault { get; }

    static EnumStorage()
    {
        Type = typeof(TEnum);
        EnumValues = Enum.GetValues(Type).Cast<TEnum>().ToList();
        EnumValuesWithoutDefault = EnumValues.Except([default]).ToList();
        Descriptions = EnumValues.ToDictionary(x=>x, GetDescriptionWithReflection);
    }

    private static string? GetDescriptionWithReflection(TEnum enumValue)
    {
        var str = enumValue.ToString();
        var memInfo = Type.GetMember(str);
        if (memInfo is null || memInfo.Length is 0)
            return null;
        var attribute = memInfo[0].GetCustomAttribute<DescriptionAttribute>(false);
        return attribute?.Description ?? null;
    }
}
