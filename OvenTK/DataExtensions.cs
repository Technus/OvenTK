namespace OvenTK.OvenTK;

public static class DataExtensions
{
    public static int SizeOf<T>(this ReadOnlySpan<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this Span<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this ReadOnlyMemory<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this Memory<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T[,,] arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T[,] arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T[] arr) => arr.Length * Unsafe.SizeOf<T>();

    public static int SizeOf<T>(this T _) where T : struct => Unsafe.SizeOf<T>();
}
