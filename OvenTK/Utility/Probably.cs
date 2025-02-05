#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
#endif
using TValue = float;//or float

namespace OvenTK.Lib.Utility;

/// <summary>
/// A probability boolean...
/// </summary>
public readonly struct P :
#if NET8_0_OR_GREATER
    IBinaryFloatingPointIeee754<P>,
    IMinMaxValue<P>,
    //IUtf8SpanFormattable,
#elif NET6_0_OR_GREATER
    ISpanFormattable,
    IComparable,
    IEquatable<P>,
    IComparable<P>,
#else
    IComparable,
    IEquatable<P>,
    IComparable<P>,
#endif
    IComparable<TValue>,
    IComparable<bool>,
    IEquatable<TValue>,
    IEquatable<bool>,
    IConvertible
{
    /// <summary>
    /// probability value from 0 inclusive to 1 inclusive
    /// </summary>
    public readonly TValue Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    }

    [ThreadStatic]
    private static Random? _local;
    private static readonly Random _global = new();

    /// <summary>
    /// Gets the per thread random instance
    /// </summary>
    public static Random Random
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_local is null)
            {
                int seed;
                lock (_global)
                {
                    seed = _global.Next();
                }

                _local = new Random(seed);
            }

            return _local;
        }
    }

    /// <summary>
    /// The constant for 'maybe' equivalent
    /// </summary>
    public static readonly P Maybe = new(0.5f);
    /// <summary>
    /// The constant for 'false' equivalent
    /// </summary>
    public static readonly P False = new(0);
    /// <summary>
    /// The constant for 'true' equivalent
    /// </summary>
    public static readonly P True = new(1);
    /// <summary>
    /// Minimal non 0 probability
    /// </summary>
    public static readonly P Epsilon = new(TValue.Epsilon);

    /// <summary>
    /// <see cref="True"/>
    /// </summary>
    public static P MaxValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => True;
    }

    /// <summary>
    /// <see cref="False"/>
    /// </summary>
    public static P MinValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => False;
    }

#if NET8_0_OR_GREATER

    /// <summary>
    /// False with -0
    /// </summary>
    /// <remarks>Do not use</remarks>
    public static readonly P NegativeZero = new(TValue.NegativeZero);

    /// <summary>
    /// Not supported
    /// </summary>
    static P IFloatingPointIeee754<P>.Epsilon
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Epsilon;
    }

    /// <summary>
    /// Not supported
    /// </summary>
    static P IFloatingPointIeee754<P>.NaN
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => throw new NotSupportedException();
    }

    /// <summary>
    /// Not supported
    /// </summary>
    static P IFloatingPointIeee754<P>.NegativeInfinity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => throw new NotSupportedException();
    }

    /// <summary>
    /// <see cref="False"/> (nearly)
    /// </summary>
    /// <remarks>Do not use</remarks>
    static P IFloatingPointIeee754<P>.NegativeZero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => NegativeZero;
    }

    /// <summary>
    /// Not supported
    /// </summary>
    static P IFloatingPointIeee754<P>.PositiveInfinity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => throw new NotSupportedException();
    }

    /// <summary>
    /// Not supported
    /// </summary>
    static P ISignedNumber<P>.NegativeOne
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => throw new NotSupportedException();
    }

    /// <summary>
    /// Not supported
    /// </summary>
    static P IFloatingPointConstants<P>.E
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => throw new NotSupportedException();
    }

    /// <summary>
    /// Not supported
    /// </summary>
    static P IFloatingPointConstants<P>.Pi
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => throw new NotSupportedException();
    }

    /// <summary>
    /// Not supported
    /// </summary>
    static P IFloatingPointConstants<P>.Tau
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => throw new NotSupportedException();
    }
    
#endif

    /// <summary>
    /// <see cref="True"/>
    /// </summary>
    public static P One
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => True;
    }

    /// <summary>
    /// 2
    /// </summary>
    public static int Radix
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 2;
    }

    /// <summary>
    /// <see cref="False"/>
    /// </summary>
    public static P Zero
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => False;
    }

    /// <summary>
    /// <see cref="False"/>
    /// </summary>
    public static P AdditiveIdentity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => False;
    }

    /// <summary>
    /// <see cref="True"/>
    /// </summary>
    public static P MultiplicativeIdentity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => True;
    }

    /// <summary>
    /// safe constructor clamping to valid <see cref="Value"/>
    /// </summary>
    /// <param name="value"><see cref="Value"/></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal P(TValue value) => Value = value;

    /// <summary>
    /// safe constructor clamping to valid <see cref="Value"/>
    /// </summary>
    /// <param name="value"><see cref="Value"/></param>
    /// <remarks>
    /// NaN becomes Maybe
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Of(TValue value) => TValue.IsNaN(value) ? throw new NotFiniteNumberException() : value switch
    {
        >= 1 => True,
        <= 0 => False,
        _ => new(value),
    };

    /// <summary>
    /// unsafe constructor clamping to valid <see cref="Value"/>
    /// </summary>
    /// <param name="value"><see cref="Value"/></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static P ClampSigned(TValue value) => value switch
    {
        >= 1 => True,
        <= 0 => False,
        _ => new(value),
    };

    /// <summary>
    /// unsafe constructor clamping to valid <see cref="Value"/>
    /// </summary>
    /// <param name="value"><see cref="Value"/></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static P ClampUnsigned(TValue value) => value is >= 1 ? True : new(value);

    /// <summary>
    /// the equality operator allows also to compare with values of type matching <see cref="Value"/>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj switch
    {
        P p => Value.Equals(p.Value),
        TValue d => Value.Equals(d),
        bool b => Value.Equals(b ? 1 : 0),
        _ => false
    };

    /// <summary>
    /// if probability values equal
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(P other) => Value.Equals(other.Value);

    /// <summary>
    /// if probability values equal
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(TValue other) => Value.Equals(other);

    /// <summary>
    /// if probability values equal
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(bool other) => Value.Equals(other ? 1 : 0);

    /// <summary>
    /// if probability values equal
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(bool? other) => Value.Equals(other switch
    {
        true => 1,
        false => 0,
        _ => 0.5f,
    });

    /// <summary>
    /// compares probability values
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(object? obj) => obj switch
    {
        P p => Value.CompareTo(p.Value),
        TValue d => Value.CompareTo(d),
        bool b => Value.CompareTo(b ? 1 : 0),
        _ => 1,
    };

    /// <summary>
    /// compares probability values
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(P other) => Value.CompareTo(other.Value);

    /// <summary>
    /// compares probability values
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(TValue other) => Value.CompareTo(other);

    /// <summary>
    /// compares probability values
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(bool other) => Value.CompareTo(other ? 1 : 0);

    /// <summary>
    /// compares probability values
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(bool? other) => Value.CompareTo(other switch
    {
        true => 1,
        false => 0,
        _ => 0.5f,
    });

    /// <summary>
    /// delegates to probability value
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    /// delegates to probability value
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString() => Value.ToString();

    /// <summary>
    /// Formats the <see cref="Value"/>
    /// </summary>
    /// <param name="format"></param>
    /// <param name="formatProvider"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>
    /// The underlying <see cref="TValue.GetTypeCode"/>
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TypeCode GetTypeCode() => Value.GetTypeCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IConvertible.ToBoolean(IFormatProvider? provider) => Convert.ToBoolean(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    byte IConvertible.ToByte(IFormatProvider? provider) => Convert.ToByte(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    char IConvertible.ToChar(IFormatProvider? provider) => Convert.ToChar(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    DateTime IConvertible.ToDateTime(IFormatProvider? provider) => Convert.ToDateTime(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    decimal IConvertible.ToDecimal(IFormatProvider? provider) => Convert.ToDecimal(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    double IConvertible.ToDouble(IFormatProvider? provider) => Convert.ToDouble(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    short IConvertible.ToInt16(IFormatProvider? provider) => Convert.ToInt16(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int IConvertible.ToInt32(IFormatProvider? provider) => Convert.ToInt32(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    long IConvertible.ToInt64(IFormatProvider? provider) => Convert.ToInt64(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    sbyte IConvertible.ToSByte(IFormatProvider? provider) => Convert.ToSByte(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    float IConvertible.ToSingle(IFormatProvider? provider) => Convert.ToSingle(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ushort IConvertible.ToUInt16(IFormatProvider? provider) => Convert.ToUInt16(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    uint IConvertible.ToUInt32(IFormatProvider? provider) => Convert.ToUInt32(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ulong IConvertible.ToUInt64(IFormatProvider? provider) => Convert.ToUInt64(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    object IConvertible.ToType(Type conversionType, IFormatProvider? provider)
    {
        return ToType(Value, conversionType, provider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static object ToType<TValue>(TValue value, Type type, IFormatProvider? provider) where TValue : IConvertible =>
            value.ToType(type, provider);
    }

    /// <summary>
    /// <see cref="IConvertible"/> to string delegate
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(IFormatProvider? provider) => Value.ToString(provider);

#if NET6_0_OR_GREATER

    /// <summary>
    /// Formats the <see cref="Value"/>
    /// </summary>
    /// <param name="destination"></param>
    /// <param name="charsWritten"></param>
    /// <param name="format"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        Value.TryFormat(destination, out charsWritten, format, provider);

#endif

#if NET8_0_OR_GREATER

    /// <summary>
    /// <see cref="TValue.IsPow2(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPow2(P value) => TValue.IsPow2(value.Value);

    /// <summary>
    /// <see cref="TValue.Log2(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Log2(P value) => Of(TValue.Log2(value.Value));

    /// <summary>
    /// <see cref="TValue.Atan2(TValue, TValue)"/>
    /// </summary>
    /// <param name="y"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Atan2(P y, P x) => Of(TValue.Atan2(y.Value, x.Value));

    /// <summary>
    /// <see cref="TValue.Atan2Pi(TValue, TValue)"/>
    /// </summary>
    /// <param name="y"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Atan2Pi(P y, P x) => Of(TValue.Atan2Pi(y.Value, x.Value));

    /// <summary>
    /// <see cref="TValue.BitDecrement(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P BitDecrement(P x) => Of(TValue.BitDecrement(x.Value));

    /// <summary>
    /// <see cref="TValue.BitIncrement(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P BitIncrement(P x) => Of(TValue.BitIncrement(x.Value));

    /// <summary>
    /// <see cref="TValue.FusedMultiplyAdd(TValue, TValue, TValue)"/>
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="addend"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P FusedMultiplyAdd(P left, P right, P addend) => Of(TValue.FusedMultiplyAdd(left.Value, right.Value, addend.Value));

    /// <summary>
    /// <see cref="TValue.Ieee754Remainder(TValue, TValue)"/>
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Ieee754Remainder(P left, P right) => Of(TValue.Ieee754Remainder(left.Value, right.Value));

    /// <summary>
    /// <see cref="TValue.ILogB(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ILogB(P x) => TValue.ILogB(x.Value);

    /// <summary>
    /// <see cref="TValue.ScaleB(TValue, int)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P ScaleB(P x, int n) => Of(TValue.ScaleB(x.Value, n));

    /// <summary>
    /// <see cref="TValue.Exp(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Exp(P x) => Of(TValue.Exp(x.Value));

    /// <summary>
    /// <see cref="TValue.Exp10(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Exp10(P x) => Of(TValue.Exp10(x.Value));

    /// <summary>
    /// <see cref="TValue.Exp2(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Exp2(P x) => Of(TValue.Exp2(x.Value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int IFloatingPoint<P>.GetExponentByteCount() => ((IFloatingPoint<TValue>)Value).GetExponentByteCount();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int IFloatingPoint<P>.GetExponentShortestBitLength() => ((IFloatingPoint<TValue>)Value).GetExponentShortestBitLength();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int IFloatingPoint<P>.GetSignificandBitLength() => ((IFloatingPoint<TValue>)Value).GetSignificandBitLength();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int IFloatingPoint<P>.GetSignificandByteCount() => ((IFloatingPoint<TValue>)Value).GetSignificandByteCount();

    /// <summary>
    /// <see cref="TValue.Round(TValue, int, MidpointRounding)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="digits"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Round(P x, int digits, MidpointRounding mode) => Of(TValue.Round(x.Value, digits, mode));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IFloatingPoint<P>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten) =>
        ((IFloatingPoint<TValue>)Value).TryWriteExponentBigEndian(destination, out bytesWritten);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IFloatingPoint<P>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten) => 
        ((IFloatingPoint<TValue>)Value).TryWriteExponentLittleEndian(destination, out bytesWritten);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IFloatingPoint<P>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten) => 
        ((IFloatingPoint<TValue>)Value).TryWriteSignificandBigEndian(destination, out bytesWritten);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IFloatingPoint<P>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten) => 
        ((IFloatingPoint<TValue>)Value).TryWriteSignificandLittleEndian(destination, out bytesWritten);

    /// <summary>
    /// <see cref="TValue.Acosh(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Acosh(P x) => Of(TValue.Acosh(x.Value));

    /// <summary>
    /// <see cref="TValue.Asinh(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Asinh(P x) => Of(TValue.Asinh(x.Value));

    /// <summary>
    /// <see cref="TValue.Atanh(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Atanh(P x) => Of(TValue.Atanh(x.Value));

    /// <summary>
    /// <see cref="TValue.Cosh(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Cosh(P x) => Of(TValue.Cosh(x.Value));

    /// <summary>
    /// <see cref="TValue.Sinh(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Sinh(P x) => Of(TValue.Sinh(x.Value));

    /// <summary>
    /// <see cref="TValue.Tanh(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Tanh(P x) => Of(TValue.Tanh(x.Value));

    /// <summary>
    /// <see cref="TValue.Log(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Log(P x) => Of(TValue.Log(x.Value));

    /// <summary>
    /// <see cref="TValue.Log(TValue, TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="newBase"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Log(P x, P newBase) => Of(TValue.Log(x.Value, newBase.Value));

    /// <summary>
    /// <see cref="TValue.Log10(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Log10(P x) => Of(TValue.Log10(x.Value));

    /// <summary>
    /// <see cref="TValue.Pow(TValue, TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Pow(P x, P y) => Of(TValue.Pow(x.Value, y.Value));

    /// <summary>
    /// <see cref="TValue.Cbrt(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Cbrt(P x) => Of(TValue.Cbrt(x.Value));

    /// <summary>
    /// <see cref="TValue.Hypot(TValue, TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Hypot(P x, P y) => Of(TValue.Hypot(x.Value, y.Value));

    /// <summary>
    /// <see cref="TValue.RootN(TValue, int)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P RootN(P x, int n) => Of(TValue.RootN(x.Value, n));

    /// <summary>
    /// <see cref="TValue.Sqrt(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Sqrt(P x) => Of(TValue.Sqrt(x.Value));

    /// <summary>
    /// <see cref="TValue.Acos(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Acos(P x) => Of(TValue.Acos(x.Value));

    /// <summary>
    /// <see cref="TValue.AcosPi(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P AcosPi(P x) => Of(TValue.AcosPi(x.Value));

    /// <summary>
    /// <see cref="TValue.Asin(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Asin(P x) => Of(TValue.Asin(x.Value));

    /// <summary>
    /// <see cref="TValue.AsinPi(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P AsinPi(P x) => Of(TValue.AsinPi(x.Value));

    /// <summary>
    /// <see cref="TValue.Atan(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Atan(P x) => Of(TValue.Atan(x.Value));

    /// <summary>
    /// <see cref="TValue.AtanPi(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P AtanPi(P x) => Of(TValue.AtanPi(x.Value));

    /// <summary>
    /// <see cref="TValue.Cos(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Cos(P x) => Of(TValue.Cos(x.Value));

    /// <summary>
    /// <see cref="TValue.CosPi(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P CosPi(P x) => Of(TValue.CosPi(x.Value));

    /// <summary>
    /// <see cref="TValue.Sin(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Sin(P x) => Of(TValue.Sin(x.Value));

    /// <summary>
    /// <see cref="TValue.SinPi(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P SinPi(P x) => Of(TValue.SinPi(x.Value));

    /// <summary>
    /// <see cref="TValue.SinCos(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (P Sin, P Cos) SinCos(P x)
    {
        var (s, c) = TValue.SinCos(x.Value);
        return (Of(s), Of(c));
    }

    /// <summary>
    /// <see cref="TValue.SinCosPi(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (P SinPi, P CosPi) SinCosPi(P x)
    {
        var (s, c) = TValue.SinCosPi(x.Value);
        return (Of(s), Of(c));
    }

    /// <summary>
    /// <see cref="TValue.Tan(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Tan(P x) => Of(TValue.Tan(x.Value));

    /// <summary>
    /// <see cref="TValue.TanPi(TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P TanPi(P x) => Of(TValue.TanPi(x.Value));

    /// <summary>
    /// <see cref="TValue.Abs(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Abs(P value) => Of(TValue.Abs(value.Value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<P>.IsCanonical(P value) => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<P>.IsComplexNumber(P value) => false;

    /// <summary>
    /// <see cref="TValue.IsEvenInteger(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEvenInteger(P value) => TValue.IsEvenInteger(value.Value);

    /// <summary>
    /// <see cref="TValue.IsFinite(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFinite(P value) => TValue.IsFinite(value.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<P>.IsImaginaryNumber(P value) => false;

    /// <summary>
    /// <see cref="TValue.IsInfinity(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInfinity(P value) => TValue.IsInfinity(value.Value);

    /// <summary>
    /// <see cref="TValue.IsInteger(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInteger(P value) => TValue.IsInteger(value.Value);

    /// <summary>
    /// <see cref="TValue.IsNaN(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNaN(P value) => TValue.IsNaN(value.Value);

    /// <summary>
    /// <see cref="TValue.IsNegative(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNegative(P value) => TValue.IsNegative(value.Value);

    /// <summary>
    /// <see cref="TValue.IsNegativeInfinity(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNegativeInfinity(P value) => TValue.IsNegativeInfinity(value.Value);

    /// <summary>
    /// <see cref="TValue.IsNormal(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNormal(P value) => TValue.IsNormal(value.Value);

    /// <summary>
    /// <see cref="TValue.IsOddInteger(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOddInteger(P value) => TValue.IsOddInteger(value.Value);

    /// <summary>
    /// <see cref="TValue.IsPositive(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPositive(P value) => TValue.IsPositive(value.Value);

    /// <summary>
    /// <see cref="TValue.IsPositiveInfinity(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPositiveInfinity(P value) => TValue.IsPositiveInfinity(value.Value);

    /// <summary>
    /// <see cref="TValue.IsRealNumber(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRealNumber(P value) => TValue.IsRealNumber(value.Value);

    /// <summary>
    /// <see cref="TValue.IsSubnormal(TValue)"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSubnormal(P value) => TValue.IsSubnormal(value.Value);

    /// <summary>
    /// If is zero (equal to False)
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsZero(P value) => value.Value is 0;

    /// <summary>
    /// <see cref="TValue.MaxMagnitude(TValue, TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P MaxMagnitude(P x, P y) => TValue.MaxMagnitude(x.Value, y.Value);

    /// <summary>
    /// <see cref="TValue.MaxMagnitudeNumber(TValue, TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P MaxMagnitudeNumber(P x, P y) => TValue.MaxMagnitudeNumber(x.Value, y.Value);

    /// <summary>
    /// <see cref="TValue.MinMagnitude(TValue, TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P MinMagnitude(P x, P y) => TValue.MinMagnitude(x.Value, y.Value);

    /// <summary>
    /// <see cref="TValue.MinMagnitudeNumber(TValue, TValue)"/>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P MinMagnitudeNumber(P x, P y) => TValue.MinMagnitudeNumber(x.Value, y.Value);

    /// <summary>
    /// <see cref="TValue.Parse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?)"/>
    /// </summary>
    /// <param name="s"></param>
    /// <param name="style"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => Of(TValue.Parse(s, style, provider));

    /// <summary>
    /// <see cref="TValue.Parse(string, NumberStyles, IFormatProvider?)"/>
    /// </summary>
    /// <param name="s"></param>
    /// <param name="style"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Parse(string s, NumberStyles style, IFormatProvider? provider) => Of(TValue.Parse(s, style, provider));

    /// <summary>
    /// <see cref="TValue.TryParse(ReadOnlySpan{char}, NumberStyles , IFormatProvider?, out TValue)"/>
    /// </summary>
    /// <param name="s"></param>
    /// <param name="style"></param>
    /// <param name="provider"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out P result)
    {
        var b = TValue.TryParse(s, style, provider, out var result2);
        result = Of(result2);
        return b;
    }

    /// <summary>
    /// <see cref="TValue.TryParse(string?, NumberStyles, IFormatProvider?, out TValue)"/>
    /// </summary>
    /// <param name="s"></param>
    /// <param name="style"></param>
    /// <param name="provider"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out P result)
    {
        var b = TValue.TryParse(s, style, provider, out var result2);
        result = Of(result2);
        return b;
    }

    /// <summary>
    /// <see cref="TValue.Parse(ReadOnlySpan{char}, IFormatProvider?)"/>
    /// </summary>
    /// <param name="s"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Of(TValue.Parse(s, provider));

    /// <summary>
    /// <see cref="TValue.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TValue)"/>
    /// </summary>
    /// <param name="s"></param>
    /// <param name="provider"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out P result)
    {
        var b = TValue.TryParse(s, provider, out var result2);
        result = Of(result2);
        return b;
    }

    /// <summary>
    /// <see cref="TValue.Parse(string, IFormatProvider?)"/>
    /// </summary>
    /// <param name="s"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P Parse(string s, IFormatProvider? provider) => Of(TValue.Parse(s, provider));

    /// <summary>
    /// <see cref="TValue.TryParse(string?, IFormatProvider?, out TValue)"/>
    /// </summary>
    /// <param name="s"></param>
    /// <param name="provider"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out P result)
    {
        var b = TValue.TryParse(s, provider, out var result2);
        result = Of(result2);
        return b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<P>.TryConvertFromChecked<TOther>(TOther value, out P result)
    {
        var b = TryConvertFromChecked(value, out TValue result2);
        result = Of(result2);
        return b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryConvertFromChecked<TNumber>(TOther value, out TNumber result) where TNumber : struct, INumberBase<TNumber> =>
            TNumber.TryConvertFromChecked(value, out result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<P>.TryConvertFromSaturating<TOther>(TOther value, out P result)
    {
        var b = TryConvertFromSaturating(value, out TValue result2);
        result = Of(result2);
        return b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryConvertFromSaturating<TNumber>(TOther value, out TNumber result) where TNumber : struct, INumberBase<TNumber> =>
            TNumber.TryConvertFromSaturating(value, out result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<P>.TryConvertFromTruncating<TOther>(TOther value, out P result)
    {
        var b = TryConvertFromTruncating(value, out TValue result2);
        result = Of(result2);
        return b;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryConvertFromTruncating<TNumber>(TOther value, out TNumber result) where TNumber : struct, INumberBase<TNumber> =>
            TNumber.TryConvertFromTruncating(value, out result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<P>.TryConvertToChecked<TOther>(P value, out TOther result)
    {
        return TryConvertToChecked(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryConvertToChecked<TNumber>(TNumber value, out TOther result) where TNumber : struct, INumberBase<TNumber> =>
            TNumber.TryConvertToChecked(value, out result!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<P>.TryConvertToSaturating<TOther>(P value, out TOther result)
    {
        return TryConvertToSaturating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryConvertToSaturating<TNumber>(TNumber value, out TOther result) where TNumber : struct, INumberBase<TNumber> =>
            TNumber.TryConvertToSaturating(value, out result!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool INumberBase<P>.TryConvertToTruncating<TOther>(P value, out TOther result)
    {
        return TryConvertToTruncating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryConvertToTruncating<TNumber>(TNumber value, out TOther result) where TNumber : struct, INumberBase<TNumber> =>
            TNumber.TryConvertToTruncating(value, out result!);
    }

    /// <summary>
    /// Bitwise Not
    /// </summary>
    /// <param name="p"></param>
    /// <returns>No longer probability</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static P IBitwiseOperators<P, P, P>.operator ~(P p)
    {
        return Of(BitwiseNot(p.Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TNumber BitwiseNot<TNumber>(TNumber value) where TNumber : struct, IBitwiseOperators<TNumber, TNumber, TNumber> => ~value;
    }

    /// <summary>
    /// Bitwise And
    /// </summary>
    /// <param name="l"></param>
    /// <param name="r"></param>
    /// <returns>No longer probability</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static P IBitwiseOperators<P, P, P>.operator &(P l, P r)
    {
        return Of(BitwiseAnd(l.Value, r.Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TNumber BitwiseAnd<TNumber>(TNumber l, TNumber r) where TNumber : struct, IBitwiseOperators<TNumber, TNumber, TNumber> => l & r;
    }

    /// <summary>
    /// Bitwise Or
    /// </summary>
    /// <param name="l"></param>
    /// <param name="r"></param>
    /// <returns>No longer probability</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static P IBitwiseOperators<P, P, P>.operator |(P l, P r)
    {
        return Of(BitwiseOr(l.Value, r.Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TNumber BitwiseOr<TNumber>(TNumber l, TNumber r) where TNumber : struct, IBitwiseOperators<TNumber, TNumber, TNumber> => l | r;
    }

    /// <summary>
    /// Bitwise Xor
    /// </summary>
    /// <param name="l"></param>
    /// <param name="r"></param>
    /// <returns>No longer probability</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static P IBitwiseOperators<P, P, P>.operator ^(P l, P r)
    {
        return Of(BitwiseXor(l.Value, r.Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static TNumber BitwiseXor<TNumber>(TNumber l, TNumber r) where TNumber : struct, IBitwiseOperators<TNumber, TNumber, TNumber> => l ^ r;
    }
#endif

    /// <summary>
    /// Calculate odds
    /// </summary>
    /// <param name="p"></param>
    /// <returns>No longer probability</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue operator ~(P p) => p.Value is 1 ? TValue.PositiveInfinity : p.Value / (1 - p.Value);

    /// <summary>
    /// Inverts the probability
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator !(P p) => new(1 - p.Value);

    /// <summary>
    /// Apply advantage logic 'p or p'
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator +(P p) => new(p.Value + p.Value - p.Value * p.Value);

    /// <summary>
    /// Apply disadvantage logic 'p and p'
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator -(P p) => new(p.Value * p.Value);

    /// <summary>
    /// Apply advantage logic 'p or p' and modify the source
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator ++(P p) => new(p.Value + p.Value - p.Value * p.Value);

    /// <summary>
    /// Apply disadvantage logic 'p and p' and modify the source
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator --(P p) => new(p.Value * p.Value);

    /// <summary>
    /// Add numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator +(TValue a, P b) => Of(a + b.Value);

    /// <summary>
    /// Add numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator +(P a, TValue b) => Of(a.Value + b);

    /// <summary>
    /// Add numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator +(P a, P b) => ClampUnsigned(a.Value + b.Value);

    /// <summary>
    /// Subtract numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator -(TValue a, P b) => Of(a - b.Value);

    /// <summary>
    /// Subtract numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator -(P a, TValue b) => Of(a.Value - b);

    /// <summary>
    /// Subtract numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator -(P a, P b) => ClampSigned(a.Value - b.Value);

    /// <summary>
    /// Multiply numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator *(TValue a, P b) => Of(a * b.Value);

    /// <summary>
    /// Multiply numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator *(P a, TValue b) => Of(a.Value * b);

    /// <summary>
    /// Multiply numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator *(P a, P b) => new(a.Value * b.Value);

    /// <summary>
    /// Divide numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator /(TValue a, P b) => Of(a / b.Value);

    /// <summary>
    /// Divide numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator /(P a, TValue b) => Of(a.Value / b);

    /// <summary>
    /// Divide numeric values of probability
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator /(P a, P b) => b.Value switch
    {
        0 => a.Value is 0 ? False : True,
        _ => ClampUnsigned(a.Value / b.Value),
    };

    /// <summary>
    /// Compute chance of any event happening
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator |(TValue a, P b) => Of(a + b.Value - a * b.Value);

    /// <summary>
    /// Compute chance of any event happening
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator |(P a, TValue b) => Of(a.Value + b - a.Value * b);

    /// <summary>
    /// Compute chance of any event happening
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator |(P a, P b) => new(a.Value + b.Value - a.Value * b.Value);

    /// <summary>
    /// Compute chance of only one event happening
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator ^(TValue a, P b) => Of(a + b.Value - 2 * (a * b.Value));

    /// <summary>
    /// Compute chance of only one event happening
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator ^(P a, TValue b) => Of(a.Value + b - 2 * (a.Value * b));

    /// <summary>
    /// Compute chance of only one event happening
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator ^(P a, P b) => ClampSigned(a.Value + b.Value - 2 * (a.Value * b.Value));

    /// <summary>
    /// Compute chance of both events happening
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator &(TValue a, P b) => Of(a * b.Value);

    /// <summary>
    /// Compute chance of both events happening
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator &(P a, TValue b) => Of(a.Value * b);

    /// <summary>
    /// Compute chance of both events happening
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator &(P a, P b) => new(a.Value * b.Value);

    /// <summary>
    /// Compute chance of event happening given other event
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator %(TValue a, P b) => Of(a * b.Value / b.Value);

    /// <summary>
    /// Compute chance of event happening given other event
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator %(P a, TValue b) => Of(a.Value * b / b);

    /// <summary>
    /// Compute chance of event happening given other event
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator %(P a, P b) => b.Value switch
    {
        0 => False,
        _ => ClampSigned(a.Value * b.Value / b.Value),
    };

    /// <summary>
    /// Compute the chance of 'a and !b', inverse of that is '!a or b'
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator >>(P a, TValue b) => Of(1 - (a.Value - a.Value * b));

    /// <summary>
    /// Compute the chance of 'a and !b', inverse of that is '!a or b'
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator >>(P a, P b) => ClampSigned(1 - (a.Value - a.Value * b.Value));//Maybe new

    /// <summary>
    /// Compute the chance of '!a and b', inverse of that is 'a or !b'
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator <<(P a, TValue b) => Of(1 - (b - a.Value * b));

    /// <summary>
    /// Compute the chance of '!a and b', inverse of that is 'a or !b'
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator <<(P a, P b) => ClampSigned(1 - (b.Value - a.Value * b.Value));//Maybe new

    /// <summary>
    /// Compute the chance of neither event happening if probabilities don't overlap and relate to same event
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator >>>(P a, TValue b) => Of(1 - a.Value - b);

    /// <summary>
    /// Compute the chance of neither event happening if probabilities don't overlap and relate to same event
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static P operator >>>(P a, P b) => ClampSigned(1 - a.Value - b.Value);

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(TValue a, P b) => a == b.Value;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(P a, TValue b) => a.Value == b;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool operator ==(P a, P b) => a.Value == b.Value;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(TValue a, P b) => a != b.Value;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(P a, TValue b) => a.Value != b;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(P a, P b) => a.Value != b.Value;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(TValue a, P b) => a >= b.Value;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(P a, TValue b) => a.Value >= b;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(P a, P b) => a.Value >= b.Value;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(TValue a, P b) => a <= b.Value;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(P a, TValue b) => a.Value <= b;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(P a, P b) => a.Value <= b.Value;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(TValue a, P b) => a > b.Value;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(P a, TValue b) => a.Value > b;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(P a, P b) => a.Value > b.Value;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(TValue a, P b) => a < b.Value;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(P a, TValue b) => a.Value < b;

    /// <summary>
    /// Compare probability values
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(P a, P b) => a.Value < b.Value;

    /// <summary>
    /// Resolve to boolean using <see cref="Random"/>
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator true(P p) => p.Value > Random.NextDouble();

    /// <summary>
    /// Resolve to boolean using <see cref="Random"/>
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator false(P p) => p.Value <= Random.NextDouble();

    /// <summary>
    /// Get probability value
    /// </summary>
    /// <param name="p"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator float(P p) => (float)p.Value;

    /// <summary>
    /// Create probability value
    /// </summary>
    /// <param name="v"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator P(float v) => Of(v);

    /// <summary>
    /// Get probability value
    /// </summary>
    /// <param name="p"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator double(P p) => p.Value;

    /// <summary>
    /// Create probability value
    /// </summary>
    /// <param name="v"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator P(double v) => Of((TValue)v);

    /// <summary>
    /// Resolve to boolean using <see cref="Random"/>, mid point is null
    /// </summary>
    /// <param name="p"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator bool?(P p) => p.Value switch
    {
        > 0.5f => (p.Value - 0.5f) * 2 > Random.NextDouble() ? true : null,
        _ => p.Value * 2 > Random.NextDouble() ? null : false,
    };

    /// <summary>
    /// Resolve to boolean using <see cref="Random"/>
    /// </summary>
    /// <param name="p"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator bool(P p) => p.Value > Random.NextDouble();

    /// <summary>
    /// Resolve to probability
    /// </summary>
    /// <param name="v"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator P(bool? v) => v switch
    {
        true => True,
        false => False,
        _ => Maybe,
    };

    /// <summary>
    /// Resolve to probability
    /// </summary>
    /// <param name="v"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator P(bool v) => v ? True : False;
}