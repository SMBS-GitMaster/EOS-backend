using RadialReview.Core.Utilities.Encrypt;
using RadialReview.Utilities.Encrypt;
using System;
using System.Security.Cryptography;
using System.Text;

namespace RadialReview {
  public class HashUtil {

    public static int Merge(params int[] hashCodes) {
      unchecked {
        int hash1 = (5381 << 16) + 5381;
        int hash2 = hash1;

        int i = 0;

        foreach (var hashCode in hashCodes) {
          if (i % 2 == 0)
            hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ hashCode;
          else
            hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ hashCode;

          ++i;
        }

        return hash1 + (hash2 * 1566083941);
      }
    }


    /*[Obsolete("Very high collision rate")]
    public static int GetDeterministicHashCode_Old(string str) {
      unchecked {
        //str = EncryptionUtility.Encrypt(str, "ecaf8002-49a4-78b7-846a-3854da2f0e1b");
        int hash1 = (5381 << 16) + 5381;
        int hash2 = hash1;

        for (int i = 0; i < str.Length; i += 2) {
          hash1 = ((hash1 << 5) + hash1) ^ str[i];
          if (i == str.Length - 1)
            break;
          hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
        }
        return hash1 + (hash2 * 1566083941);
      }
    }*/

    public static ulong GetDeterministicHashCodeULong(string input) {
      var inputBytes = Encoding.UTF8.GetBytes(input);
      return GetDeterministicHashCodeULong(inputBytes);
    }
    public static ulong GetDeterministicHashCodeULong(long input) {
      var inputBytes = BitConverter.GetBytes(input);
      return GetDeterministicHashCodeULong(inputBytes);
    }


    public static ulong GetDeterministicHashCodeULong(byte[] inputByte) {
      var m = new Murmur3();
      var hashedBytes = m.ComputeHash(inputByte);

      return BitConverter.ToUInt64(hashedBytes, 0) ^ BitConverter.ToUInt64(hashedBytes, 8);

      //using (var sha = SHA512.Create()) {
      //  var hashedBytes = sha.ComputeHash(inputByte);

      //  return BitConverter.ToUInt64(hashedBytes, 0) ^
      //          BitConverter.ToUInt64(hashedBytes, 8) ^
      //          BitConverter.ToUInt64(hashedBytes, 16) ^
      //          BitConverter.ToUInt64(hashedBytes, 24)^
      //          BitConverter.ToUInt64(hashedBytes, 32) ^
      //          BitConverter.ToUInt64(hashedBytes, 40)^
      //          BitConverter.ToUInt64(hashedBytes, 48) ^
      //          BitConverter.ToUInt64(hashedBytes, 56);
      //}
    }

    [Obsolete("old. Use GetDeterministicHashCodeULong instead.")]
    public static int GetDeterministicHashCode(string input) {
      return (int)(GetDeterministicHashCodeULong(input) % int.MaxValue);
    }



    public class Murmur3 {
      // 128 bit output, 64 bit platform version

      public static ulong READ_SIZE = 16;
      private static ulong C1 = 0x87c37b91114253d5L;
      private static ulong C2 = 0x4cf5ad432745937fL;

      private ulong length;
      private uint seed; // if want to start with a seed, create a constructor
      ulong h1;
      ulong h2;

      private void MixBody(ulong k1, ulong k2) {
        h1 ^= MixKey1(k1);

        h1 = IntHelpers.RotateLeft(h1,27);
        h1 += h2;
        h1 = h1 * 5 + 0x52dce729;

        h2 ^= MixKey2(k2);

        h2 = IntHelpers.RotateLeft(h2,31);
        h2 += h1;
        h2 = h2 * 5 + 0x38495ab5;
      }

      private static ulong MixKey1(ulong k1) {
        k1 *= C1;
        k1 = IntHelpers.RotateLeft(k1,31);
        k1 *= C2;
        return k1;
      }

      private static ulong MixKey2(ulong k2) {
        k2 *= C2;
        k2 = IntHelpers.RotateLeft(k2,33);
        k2 *= C1;
        return k2;
      }

      private static ulong MixFinal(ulong k) {
        // avalanche bits

        k ^= k >> 33;
        k *= 0xff51afd7ed558ccdL;
        k ^= k >> 33;
        k *= 0xc4ceb9fe1a85ec53L;
        k ^= k >> 33;
        return k;
      }

      public byte[] ComputeHash(byte[] bb) {
        ProcessBytes(bb);
        return Hash;
      }

      private void ProcessBytes(byte[] bb) {
        h1 = seed;
        this.length = 0L;

        int pos = 0;
        ulong remaining = (ulong)bb.Length;

        // read 128 bits, 16 bytes, 2 longs in eacy cycle
        while (remaining >= READ_SIZE) {
          ulong k1 = IntHelpers.GetUInt64(bb,pos);
          pos += 8;

          ulong k2 = IntHelpers.GetUInt64(bb,pos);
          pos += 8;

          length += READ_SIZE;
          remaining -= READ_SIZE;

          MixBody(k1, k2);
        }

        // if the input MOD 16 != 0
        if (remaining > 0)
          ProcessBytesRemaining(bb, remaining, pos);
      }

      private void ProcessBytesRemaining(byte[] bb, ulong remaining, int pos) {
        ulong k1 = 0;
        ulong k2 = 0;
        length += remaining;

        // little endian (x86) processing
        switch (remaining) {
          case 15:
            k2 ^= (ulong)bb[pos + 14] << 48; // fall through
            goto case 14;
          case 14:
            k2 ^= (ulong)bb[pos + 13] << 40; // fall through
            goto case 13;
          case 13:
            k2 ^= (ulong)bb[pos + 12] << 32; // fall through
            goto case 12;
          case 12:
            k2 ^= (ulong)bb[pos + 11] << 24; // fall through
            goto case 11;
          case 11:
            k2 ^= (ulong)bb[pos + 10] << 16; // fall through
            goto case 10;
          case 10:
            k2 ^= (ulong)bb[pos + 9] << 8; // fall through
            goto case 9;
          case 9:
            k2 ^= (ulong)bb[pos + 8]; // fall through
            goto case 8;
          case 8:
            k1 ^= IntHelpers.GetUInt64(bb,pos);
            break;
          case 7:
            k1 ^= (ulong)bb[pos + 6] << 48; // fall through
            goto case 6;
          case 6:
            k1 ^= (ulong)bb[pos + 5] << 40; // fall through
            goto case 5;
          case 5:
            k1 ^= (ulong)bb[pos + 4] << 32; // fall through
            goto case 4;
          case 4:
            k1 ^= (ulong)bb[pos + 3] << 24; // fall through
            goto case 3;
          case 3:
            k1 ^= (ulong)bb[pos + 2] << 16; // fall through
            goto case 2;
          case 2:
            k1 ^= (ulong)bb[pos + 1] << 8; // fall through
            goto case 1;
          case 1:
            k1 ^= (ulong)bb[pos]; // fall through
            break;
          default:
            throw new Exception("Something went wrong with remaining bytes calculation.");
        }

        h1 ^= MixKey1(k1);
        h2 ^= MixKey2(k2);
      }

      public byte[] Hash {
        get {
          h1 ^= length;
          h2 ^= length;

          h1 += h2;
          h2 += h1;

          h1 = Murmur3.MixFinal(h1);
          h2 = Murmur3.MixFinal(h2);

          h1 += h2;
          h2 += h1;

          var hash = new byte[Murmur3.READ_SIZE];

          Array.Copy(BitConverter.GetBytes(h1), 0, hash, 0, 8);
          Array.Copy(BitConverter.GetBytes(h2), 0, hash, 8, 8);

          return hash;
        }
      }
    }



    public class IntHelpers {
      public static ulong RotateLeft(ulong original, int bits) {
        return (original << bits) | (original >> (64 - bits));
      }

      public static ulong RotateRight(ulong original, int bits) {
        return (original >> bits) | (original << (64 - bits));
      }

      public static ulong GetUInt64(byte[] bb, int pos) {
        return BitConverter.ToUInt64(bb, pos);
        //// we only read aligned longs, so a simple casting is enough
        //fixed (byte* pbyte = &bb[pos]) {
        //  return *((ulong*)pbyte);
        //}
      }
    }

  }
}