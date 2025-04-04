﻿using StbImageSharp;
using System.Diagnostics;
using System.Reflection;

namespace OvenTK.Lib;

/// <summary>
/// Collection of helper methods
/// </summary>
public static partial class Extensions
{
    private static readonly FieldInfo _functions = typeof(WGL).GetField("EntryPointNames", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly FieldInfo _pointers = typeof(WGL).GetField("EntryPoints", BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Safe lookup for OpenGL and DirectX Interop
    /// </summary>
    /// <remarks>Usable only after context creation...</remarks>
    public static bool IsNVDXInterop()
    {
        var functions = (string[])_functions.GetValue(null)!;
        var i = 0;
        for (; i < functions.Length; i++)
        {
            if (functions[i] == "wglDXOpenDeviceNV")
                break;
        }
        var pointers = (IntPtr[])_pointers.GetValue(null)!;

        return pointers[i] != IntPtr.Zero;
    }

    /// <summary>
    /// Throws if text is not ASCII
    /// </summary>
    /// <param name="text"></param>
    /// <exception cref="ArgumentOutOfRangeException">When a char is outside of ASCII range</exception>
    public static void EnsureASCII(this string text)
    {
        foreach (var c in text)
            if (c > 128)
                throw new ArgumentOutOfRangeException(nameof(text), text, $"Contains non ASCII character: {c}");
    }

#if !NET8_0_OR_GREATER
    private static class ListHelper<T>
    {
        internal static void Grow(List<T> list, int capacity)
        {
            Debug.Assert(list.Capacity < capacity);

            int newCapacity = list.Capacity == 0 ? 4 : 2 * list.Capacity;

            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newCapacity > 0X7FFFFFC7) newCapacity = 0X7FFFFFC7;

            // If the computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newCapacity < capacity) newCapacity = capacity;

            list.Capacity = newCapacity;
        }

#if !NET6_0_OR_GREATER && !NETSTANDARD2_0_OR_GREATER
        internal static readonly bool IsReferenceOrContainsReferences = ReferenceOrContainsRefs(typeof(T));

        private static bool ReferenceOrContainsRefs(Type t)
        {
            if (!t.IsValueType)
                return true;

            if (t.IsPrimitive || t.IsEnum)
                return false;

            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var f in t.GetFields(Flags))
            {
                if (ReferenceOrContainsRefs(f.FieldType))
                {
                    return true;
                }
            }

            return false;
        }
#endif
    }
#endif

    /// <summary>
    /// A slighlty unsafe and as performant version of <see cref="System.Runtime.InteropServices.CollectionsMarshal.AsSpan{T}(List{T}?)"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    /// <remarks>On .NET 6 and up delegates work to the original implementation</remarks>
#if NET6_0_OR_GREATER
    [Obsolete("Use CollectionsMarshal.AsSpan")]
    [MethodImpl(MethodImplOptions.AggressiveInlining|MethodImplOptions.AggressiveOptimization)]
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static unsafe Span<T> ToSpanUnsafe<T>(this List<T> list)
    {
#if NET6_0_OR_GREATER
        return CollectionsMarshal.AsSpan(list);
#else
        if (list is null)
            return default;
        var p = (nint)(*(void**)&list) + sizeof(nint);
        return new Span<T>(*(T[]*)p,0,*(int*)(p+sizeof(nint)*2));
#endif
    }

    /// <summary>
    /// A slighlty unsafe and as performant version of <see cref="System.Runtime.InteropServices.CollectionsMarshal.SetCount{T}(List{T}, int)"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="count">new list count</param>
    /// <returns></returns>
    /// <remarks>On .NET 8 and up delegates work to the original implementation</remarks>
#if NET8_0_OR_GREATER
    [Obsolete("Use CollectionsMarshal.SetCount")]
#endif
#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static unsafe void SetCountUnsafe<T>(this List<T> list, int count)
    {
#if NET8_0_OR_GREATER
        CollectionsMarshal.SetCount(list, count);
#elif NET6_0_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Must be not negative");

        var p = (nint)(*(void**)&list) + sizeof(nint);
        var size = (int*)(p + sizeof(nint));
        var version = (int*)(p + sizeof(nint) + sizeof(int));

        (*version)++;

        if (count > list.Capacity)
        {
            ListHelper<T>.Grow(list, count);
        }
        else if (count < list.Count && RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(*(T[]*)p, count, list.Count - count);
        }

        *size = count;
#else
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Must be not negative");

        var p = (nint)(*(void**)&list) + sizeof(nint);
        var size = (int*)(p + sizeof(nint)*2);
        var version = (int*)(p + sizeof(nint)*2 + sizeof(int));

        (*version)++;

        if (count > list.Capacity)
        {
            ListHelper<T>.Grow(list, count);
        }
        else if (count < list.Count && ListHelper<T>.IsReferenceOrContainsReferences)
        {
            Array.Clear(*(T[]*)p, count, list.Count - count);
        }

        *size = count;
#endif
    }

    /// <summary>
    /// Get the byte size of <paramref name="arr"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static int SizeOf<T>(this BufferBase.Mapping<T> arr) where T : struct => arr.Buffer.Size;

    /// <summary>
    /// Get the byte size of <paramref name="arr"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static int SizeOf<T>(this BufferBase.RangeMapping<T> arr) where T : struct => arr.Buffer.Size;

    /// <summary>
    /// Get the byte size of <paramref name="arr"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static int SizeOf<T>(this ReadOnlySpan<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    /// <summary>
    /// Get the byte size of <paramref name="arr"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static int SizeOf<T>(this Span<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    /// <summary>
    /// Get the byte size of <paramref name="arr"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static int SizeOf<T>(this ReadOnlyMemory<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    /// <summary>
    /// Get the byte size of <paramref name="arr"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static int SizeOf<T>(this Memory<T> arr) => arr.Length * Unsafe.SizeOf<T>();

    /// <summary>
    /// Get the byte size of <paramref name="arr"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static int SizeOf<T>(this T[,,,] arr) => arr.Length * Unsafe.SizeOf<T>();

    /// <summary>
    /// Get the byte size of <paramref name="arr"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static int SizeOf<T>(this T[,,] arr) => arr.Length * Unsafe.SizeOf<T>();

    /// <summary>
    /// Get the byte size of <paramref name="arr"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static int SizeOf<T>(this T[,] arr) => arr.Length * Unsafe.SizeOf<T>();

    /// <summary>
    /// Get the byte size of <paramref name="arr"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static int SizeOf<T>(this T[] arr) => arr.Length * Unsafe.SizeOf<T>();

    /// <summary>
    /// Get the byte size of <typeparamref name="T"/> (<paramref name="_"/>)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_"></param>
    /// <returns></returns>
    public static int SizeOf<T>(this T _) where T : struct => Unsafe.SizeOf<T>();

    /// <summary>
    /// Unsafe helper to wrap <see langword="nint"/> pointer to data of size <paramref name="bytes"/> into span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="p"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static unsafe Span<T> AsSpanUnsafe<T>(this nint p, int bytes) where T : struct =>
        new((void*)p, bytes / sizeof(T));

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into read only span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe Span<T> AsSpanUnsafe<T>(this Span<byte> data) where T : struct
    {
        fixed (byte* ptr = data)
            return new(ptr, data.Length / sizeof(T));
    }

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into read only span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe Span<T> AsSpanUnsafe<T>(this byte[] data) where T : struct
    {
        fixed (byte* ptr = data)
            return new(ptr, data.Length / sizeof(T));
    }

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe Span<T> AsSpanUnsafe<T>(this T[] data) where T : struct
    {
        fixed (T* ptr = data)
            return new(ptr, data.Length);
    }

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe Span<T> AsSpanUnsafe<T>(this T[,] data) where T : struct
    {
        fixed (T* ptr = data)
            return new(ptr, data.Length);
    }

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe Span<T> AsSpanUnsafe<T>(this T[,,] data) where T : struct
    {
        fixed (T* ptr = data)
            return new(ptr, data.Length);
    }

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe Span<T> AsSpanUnsafe<T>(this T[,,,] data) where T : struct
    {
        fixed (T* ptr = data)
            return new(ptr, data.Length);
    }

    /// <summary>
    /// Unsafe helper to wrap <see langword="nint"/> pointer to data of size <paramref name="bytes"/> into read only span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="p"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static unsafe ReadOnlySpan<T> AsReadOnlySpanUnsafe<T>(this nint p, int bytes) where T : struct =>
        new((void*)p, bytes / sizeof(T));

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into read only span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe ReadOnlySpan<T> AsReadOnlySpanUnsafe<T>(this Span<byte> data) where T : struct
    {
        fixed (byte* ptr = data)
            return new(ptr, data.Length / sizeof(T));
    }

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into read only span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe ReadOnlySpan<T> AsReadOnlySpanUnsafe<T>(this ReadOnlySpan<byte> data) where T : struct
    {
        fixed (byte* ptr = data)
            return new(ptr, data.Length / sizeof(T));
    }

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into read only span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe ReadOnlySpan<T> AsReadOnlySpanUnsafe<T>(this byte[] data) where T : struct
    {
        fixed (byte* ptr = data)
            return new(ptr, data.Length / sizeof(T));
    }

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into read only span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe ReadOnlySpan<T> AsReadOnlySpanUnsafe<T>(this T[] data) where T : struct
    {
        fixed (T* ptr = data)
            return new(ptr, data.Length);
    }

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into read only span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe ReadOnlySpan<T> AsReadOnlySpanUnsafe<T>(this T[,] data) where T : struct
    {
        fixed (T* ptr = data)
            return new(ptr, data.Length);
    }

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into read only span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe ReadOnlySpan<T> AsReadOnlySpanUnsafe<T>(this T[,,] data) where T : struct
    {
        fixed (T* ptr = data)
            return new(ptr, data.Length);
    }

    /// <summary>
    /// Unsafe helper to wrap <paramref name="data"/> into read only span
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static unsafe ReadOnlySpan<T> AsReadOnlySpanUnsafe<T>(this T[,,,] data) where T : struct
    {
        fixed (T* ptr = data)
            return new(ptr, data.Length);
    }

    /// <summary>
    /// Makes vertexes for <see cref="MakeRectIndices"/> for rectangle based on 2 triangles<br/>
    /// in such a way that:
    /// <code>
    ///    uint xFlag = uint(gl_VertexID % 2);
    ///    uint yFlag = uint(gl_VertexID / 2);
    /// </code>
    /// can be used to make decisions for further processing
    /// </summary>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <returns></returns>
    public static float[] MakeRectVertices(float w, float h)
    {
        w /= 2;
        h /= 2;
        return [
            -w,-h,
            +w,-h,
            -w,+h,
            +w,+h,
        ];
    }

    /// <summary>
    /// Makes indices for <see cref="MakeRectVertices(float, float)"/> for rectangle based on 2 triangles<br/>
    /// in such a way that:
    /// <code>
    ///    uint xFlag = uint(gl_VertexID % 2);
    ///    uint yFlag = uint(gl_VertexID / 2);
    /// </code>
    /// can be used to make decisions for further processing
    /// </summary>
    /// <returns></returns>
    public static byte[] MakeRectIndices() =>
    [
        1,3,2,
        2,1,0,
    ];

    /// <summary>
    /// Creates an array filled with consecutive numbers starting from 0 up to <paramref name="countLineSegments"/> inclusive
    /// </summary>
    /// <param name="countLineSegments"></param>
    /// <returns></returns>
    public static byte[] MakeLineStripIndices(byte countLineSegments)
    {
        var result = new byte[countLineSegments + 1];
        for (byte i = 0; i < result.Length; i++)
        {
            result[i] = i;
        }
        return result;
    }

    /// <summary>
    /// Creates an array filled with consecutive numbers starting from 0 up to <paramref name="countTrangles"/>+1 inclusive
    /// </summary>
    /// <param name="countTrangles"></param>
    /// <returns></returns>
    public static byte[] MakeTriangleStripIndices(byte countTrangles)
    {
        var result = new byte[countTrangles + 2];
        for (byte i = 0; i < result.Length; i++)
        {
            result[i] = i;
        }
        return result;
    }

    /// <summary>
    /// Helper for Stopwatch Elapsed timestamp difference calculation
    /// </summary>
    /// <param name="startingTimestamp"></param>
    /// <param name="endingTimestamp"></param>
    /// <returns></returns>
    public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) =>
        new((long)((endingTimestamp - startingTimestamp) * (10000000D / Stopwatch.Frequency)));

    /// <summary>
    /// Loads image from stream (not the raw pixels, a image file)
    /// </summary>
    /// <param name="image">stream to read and dispose</param>
    /// <param name="flipY">should flip vertically</param>
    /// <returns></returns>
    public static ImageResult LoadImageAndDispose(this Stream image, bool flipY = true)
    {
        using var stream = image;
        // OpenGL has it's texture origin in the lower left corner instead of the top left corner,
        // so we tell StbImageSharp to flip the image when loading.
        StbImage.stbi_set_flip_vertically_on_load_thread(flipY ? 1 : 0);

        // Here we open a stream to the file and pass it to StbImageSharp to load.
        return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
    }

    /// <summary>
    /// Get enum description or to string value
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="enumValue"></param>
    /// <returns></returns>
    public static string GetDescription<TEnum>(this TEnum enumValue) where TEnum : struct, Enum
    {
        if (!EnumStorage<TEnum>.Descriptions.TryGetValue(enumValue, out var description))
            throw new InvalidOperationException($"Enum value: {enumValue} is not defined");
        if (description is null)
            throw new InvalidOperationException($"Enum value: {enumValue} has no description");
        return description;
    }

    /// <summary>
    /// Gets random <typeparamref name="TEnum"/> with option to set probability for <see langword="default"/>
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="random">generator to use</param>
    /// <param name="zeroChance"><see langword="default"/> chance</param>
    /// <returns></returns>
    public static TEnum GetRandom<TEnum>(this Random random, float zeroChance = -1) where TEnum : struct, Enum
    {
        var count = EnumStorage<TEnum>.EnumValuesWithoutDefault.Count;
        if (zeroChance is -1)
            zeroChance = 1f / (count + 1);

        var randomRoll = random.NextDouble();
        if (randomRoll < zeroChance)
            return default;

        randomRoll -= zeroChance;
        var maxRoll = 1 - zeroChance;
        var proportion = randomRoll / maxRoll;

        if (proportion >= 1)//just in case of rounding errors
            return EnumStorage<TEnum>.EnumValuesWithoutDefault[count - 1];

        var id = (int)(proportion * count);

        return EnumStorage<TEnum>.EnumValuesWithoutDefault[id];
    }
}
