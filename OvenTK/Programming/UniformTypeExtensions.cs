namespace OvenTK.Lib;

/// <summary>
/// Helper for <see cref="ActiveUniformType"/>
/// </summary>
public static class UniformTypeExtensions
{
    /// <summary>
    /// Overall type groups for uniforms
    /// </summary>
    public enum UniformType
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        None,
        /// <summary>
        /// A simple value like float or matrix
        /// </summary>
        Value,
        /// <summary>
        /// Some form of *Sampler*
        /// </summary>
        Sampler,
        /// <summary>
        /// Some form of *Image*
        /// </summary>
        Image,
        /// <summary>
        /// Atomic uint counter
        /// </summary>
        Counter,
    }

    /// <summary>
    /// Check if that is an typical uniform value
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsValue(this UniformType type) => type is UniformType.Value;

    /// <summary>
    /// Check if that is some sort of texture/sampler/image uniform
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsTexture(this UniformType type) => type is UniformType.Sampler or UniformType.Image;

    /// <summary>
    /// Returns <see cref="UniformType"/> group matching <paramref name="uniformType"/>
    /// </summary>
    /// <param name="uniformType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static UniformType GetOverallType(this ActiveUniformType uniformType) => uniformType switch
    {
        ActiveUniformType.Bool or
        ActiveUniformType.BoolVec2 or
        ActiveUniformType.BoolVec3 or
        ActiveUniformType.BoolVec4 or
        ActiveUniformType.Double or
        ActiveUniformType.DoubleVec2 or
        ActiveUniformType.DoubleVec3 or
        ActiveUniformType.DoubleVec4 or
        ActiveUniformType.Float or
        ActiveUniformType.FloatMat2 or
        ActiveUniformType.FloatMat2x3 or
        ActiveUniformType.FloatMat2x4 or
        ActiveUniformType.FloatMat3 or
        ActiveUniformType.FloatMat3x2 or
        ActiveUniformType.FloatMat3x4 or
        ActiveUniformType.FloatMat4 or
        ActiveUniformType.FloatMat4x2 or
        ActiveUniformType.FloatMat4x3 or
        ActiveUniformType.FloatVec2 or
        ActiveUniformType.FloatVec3 or
        ActiveUniformType.FloatVec4 or
        ActiveUniformType.Int or
        ActiveUniformType.IntVec2 or
        ActiveUniformType.IntVec3 or
        ActiveUniformType.IntVec4 or
        ActiveUniformType.UnsignedInt or
        ActiveUniformType.UnsignedIntVec2 or
        ActiveUniformType.UnsignedIntVec3 or
        ActiveUniformType.UnsignedIntVec4 => UniformType.Value,

        ActiveUniformType.IntSampler1D or
        ActiveUniformType.IntSampler1DArray or
        ActiveUniformType.IntSampler2D or
        ActiveUniformType.IntSampler2DArray or
        ActiveUniformType.IntSampler2DMultisample or
        ActiveUniformType.IntSampler2DMultisampleArray or
        ActiveUniformType.IntSampler2DRect or
        ActiveUniformType.IntSampler3D or
        ActiveUniformType.IntSamplerBuffer or
        ActiveUniformType.IntSamplerCube or
        ActiveUniformType.IntSamplerCubeMapArray or
        ActiveUniformType.Sampler1D or
        ActiveUniformType.Sampler1DArray or
        ActiveUniformType.Sampler1DArrayShadow or
        ActiveUniformType.Sampler1DShadow or
        ActiveUniformType.Sampler2D or
        ActiveUniformType.Sampler2DArray or
        ActiveUniformType.Sampler2DArrayShadow or
        ActiveUniformType.Sampler2DMultisample or
        ActiveUniformType.Sampler2DMultisampleArray or
        ActiveUniformType.Sampler2DRect or
        ActiveUniformType.Sampler2DRectShadow or
        ActiveUniformType.Sampler2DShadow or
        ActiveUniformType.Sampler3D or
        ActiveUniformType.SamplerBuffer or
        ActiveUniformType.SamplerCube or
        ActiveUniformType.SamplerCubeMapArray or
        ActiveUniformType.SamplerCubeMapArrayShadow or
        ActiveUniformType.SamplerCubeShadow or
        ActiveUniformType.UnsignedIntSampler1D or
        ActiveUniformType.UnsignedIntSampler1DArray or
        ActiveUniformType.UnsignedIntSampler2D or
        ActiveUniformType.UnsignedIntSampler2DArray or
        ActiveUniformType.UnsignedIntSampler2DMultisample or
        ActiveUniformType.UnsignedIntSampler2DMultisampleArray or
        ActiveUniformType.UnsignedIntSampler2DRect or
        ActiveUniformType.UnsignedIntSampler3D or
        ActiveUniformType.UnsignedIntSamplerBuffer or
        ActiveUniformType.UnsignedIntSamplerCube or
        ActiveUniformType.UnsignedIntSamplerCubeMapArray => UniformType.Sampler,

        ActiveUniformType.Image1D or
        ActiveUniformType.Image1DArray or
        ActiveUniformType.Image2D or
        ActiveUniformType.Image2DArray or
        ActiveUniformType.Image2DMultisample or
        ActiveUniformType.Image2DMultisampleArray or
        ActiveUniformType.Image2DRect or
        ActiveUniformType.Image3D or
        ActiveUniformType.ImageBuffer or
        ActiveUniformType.ImageCube or
        ActiveUniformType.ImageCubeMapArray or
        ActiveUniformType.IntImage1D or
        ActiveUniformType.IntImage1DArray or
        ActiveUniformType.IntImage2D or
        ActiveUniformType.IntImage2DArray or
        ActiveUniformType.IntImage2DMultisample or
        ActiveUniformType.IntImage2DMultisampleArray or
        ActiveUniformType.IntImage2DRect or
        ActiveUniformType.IntImage3D or
        ActiveUniformType.IntImageBuffer or
        ActiveUniformType.IntImageCube or
        ActiveUniformType.IntImageCubeMapArray or
        ActiveUniformType.UnsignedIntImage1D or
        ActiveUniformType.UnsignedIntImage1DArray or
        ActiveUniformType.UnsignedIntImage2D or
        ActiveUniformType.UnsignedIntImage2DArray or
        ActiveUniformType.UnsignedIntImage2DMultisample or
        ActiveUniformType.UnsignedIntImage2DMultisampleArray or
        ActiveUniformType.UnsignedIntImage2DRect or
        ActiveUniformType.UnsignedIntImage3D or
        ActiveUniformType.UnsignedIntImageBuffer or
        ActiveUniformType.UnsignedIntImageCube or
        ActiveUniformType.UnsignedIntImageCubeMapArray => UniformType.Image,

        ActiveUniformType.UnsignedIntAtomicCounter => UniformType.Counter,

        _ => throw new ArgumentOutOfRangeException(nameof(uniformType),uniformType,"Undefined value"),
    };
}
