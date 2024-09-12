using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Numerics.BigInteger;
using static System.Globalization.CultureInfo;
using static System.Globalization.NumberStyles;
using static System.Numerics.BigInteger;

namespace RadialReview.Core.Utilities.Encrypt {
  using System.Diagnostics.CodeAnalysis;
  using System.Numerics;
#pragma warning disable S101 // Types should be named in PascalCase
  public sealed class Fnv1a128 : Fnv1aBigBase
#pragma warning restore S101 // Types should be named in PascalCase
    {
    /// <inheritdoc />
    /// <summary>
    /// Initializes a new instance of the <see cref="Fnv1a128"/> class.
    /// </summary>
    /// <exception cref="System.ArgumentException">style is not a
    /// <see cref="System.Globalization.NumberStyles"></see> value.   -or-  style includes the
    /// <see cref="AllowHexSpecifier"></see> or <see cref="HexNumber"></see> flag along with another
    /// value.</exception>
    /// <exception cref="System.ArgumentNullException">value is null.</exception>
    /// <exception cref="System.FormatException">value does not comply with the input pattern specified by
    /// style.</exception>
    public Fnv1a128() : base(
        Parse("100000000000000000000000000000000", AllowHexSpecifier, InvariantCulture),
        Parse("0000000001000000000000000000013B", AllowHexSpecifier, InvariantCulture),
        Parse("6C62272E07BB014262B821756295C58D", AllowHexSpecifier, InvariantCulture),
        128) {
    }
  }
  /// <inheritdoc />
  /// <summary>
  /// Implements the FNV-1a variant hashing algorithm for subtypes using the BigInteger class.
  /// </summary>
  // ReSharper disable once InconsistentNaming
#pragma warning disable S101 // Types should be named in PascalCase
  public abstract class Fnv1aBigBase : System.Security.Cryptography.HashAlgorithm
#pragma warning restore S101 // Types should be named in PascalCase
    {
    /// <summary>
    /// The "wrap-around" modulo value for keeping multiplication within the number of bits.
    /// </summary>
    private readonly BigInteger _modValue;

    /// <summary>
    /// The prime.
    /// </summary>
    private readonly BigInteger _prime;

    /// <summary>
    /// The non-zero offset basis.
    /// </summary>
    private readonly BigInteger _offsetBasis;

    /// <summary>
    /// The hash.
    /// </summary>
    private BigInteger _hash;

    /// <inheritdoc />
    /// <summary>
    /// Initializes a new instance of the <see cref="Fnv1aBigBase"/> class.
    /// </summary>
    /// <param name="modValue">The "wrap-around" modulo value for keeping multiplication within the number of
    /// bits.</param>
    /// <param name="prime">The FNV-1a prime.</param>
    /// <param name="offsetBasis">The FNV-1a offset basis.</param>
    /// <param name="hashSizeValue">The size, in bits, of the computed hash code.</param>
    // ReSharper disable once TooManyDependencies
    protected Fnv1aBigBase(
        in BigInteger modValue,
        in BigInteger prime,
        in BigInteger offsetBasis,
        in int hashSizeValue) {
      this._modValue = modValue;
      this._prime = prime;
      this._offsetBasis = offsetBasis;
      this.Init();
      this.HashSizeValue = hashSizeValue;
    }

    /// <inheritdoc />
    /// <summary>
    /// Initializes an implementation of the <see cref="T:System.Security.Cryptography.HashAlgorithm" /> class.
    /// </summary>
    public override sealed void Initialize() => this.Init();

    /// <inheritdoc />
    /// <summary>
    /// When overridden in a derived class, routes data written to the object into the hash algorithm for computing
    /// the hash.
    /// </summary>
    /// <param name="array">The input to compute the hash code for.</param>
    /// <param name="ibStart">The offset into the byte array from which to begin using data.</param>
    /// <param name="cbSize">The number of bytes in the byte array to use as data.</param>
#pragma warning disable IDE0079 // Remove unnecessary suppression
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed. Suppression is OK here.")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
    protected override void HashCore(byte[] array, int ibStart, int cbSize) {
      for (int i = ibStart; i < cbSize; i++) {
        unchecked {
          this._hash ^= array[i];
          this._hash = (this._hash * this._prime) % this._modValue;
        }
      }
    }

    /// <inheritdoc />
    /// <summary>
    /// When overridden in a derived class, finalizes the hash computation after the last data is processed by the
    /// cryptographic stream object.
    /// </summary>
    /// <returns>
    /// The computed hash code.
    /// </returns>
    protected override byte[] HashFinal() => this._hash.ToByteArray();

    /// <summary>
    /// Initializes the hash for this instance.
    /// </summary>
    private void Init() => this._hash = this._offsetBasis;
  }
}

