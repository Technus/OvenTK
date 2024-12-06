using System.ComponentModel;
using System.Reflection;

namespace OvenTK.Lib;
public static class EnumStorage<TEnum> where TEnum : struct, Enum
{
    public static Type Type { get; }
    public static IReadOnlyDictionary<TEnum, string?> Descriptions { get; }
    public static IReadOnlyList<TEnum> EnumValues { get; }
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
