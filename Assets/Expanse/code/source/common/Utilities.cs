using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Expanse {

/**
 * @brief: class containing common utilities.
 * */
public class Utilities {

  public static Matrix4x4 quaternionVectorToRotationMatrix(Vector3 v) {
    Quaternion q = Quaternion.Euler(v.x, v.y, v.z);
    return Matrix4x4.Rotate(q);
  }

  public static Vector2Int ToInt2(Vector2 v) {
      return new Vector2Int((int) v.x, (int) v.y);
  }

  public static Vector3Int ToInt3(Vector3 v) {
      return new Vector3Int((int) v.x, (int) v.y, (int) v.z);
  }

  /*
   * Credit to Mohsen Sarkar from stack overflow for this implementation.
   * https://stackoverflow.com/questions/37759848/convert-byte-array-to-16-bits-float
   */
  public static float toTwoByteFloat(byte HO, byte LO)
  {
      var intVal = BitConverter.ToInt32(new byte[] { HO, LO, 0, 0 }, 0);

      int mant = intVal & 0x03ff;
      int exp = intVal & 0x7c00;
      if (exp == 0x7c00) exp = 0x3fc00;
      else if (exp != 0)
      {
          exp += 0x1c000;
          if (mant == 0 && exp > 0x1c400)
              return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | exp << 13 | 0x3ff), 0);
      }
      else if (mant != 0)
      {
          exp = 0x1c400;
          do
          {
              mant <<= 1;
              exp -= 0x400;
          } while ((mant & 0x400) == 0);
          mant &= 0x3ff;
      }
      return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | (exp | mant) << 13), 0);
  }

// Memory optimized version---expects {HO, LO, 0, 0}

  private static byte[] sTwoByteFloatTempBuffer = new byte[4];
  public static float toTwoByteFloatMemoryOptimized(byte[] HO_LO_zero_zero)
  {
      var intVal = BitConverter.ToInt32(HO_LO_zero_zero, 0);

      int mant = intVal & 0x03ff;
      int exp = intVal & 0x7c00;
      if (exp == 0x7c00) exp = 0x3fc00;
      else if (exp != 0)
      {
          exp += 0x1c000;
          if (mant == 0 && exp > 0x1c400) {
              // [HACK]: to avoid using bit converter and dynamically allocating memory,
              // use a static buffer + some manual conversion. This assumes an endian-ness
              // though!
              int modifiedIntValCase1 = (intVal & 0x8000) << 16 | exp << 13 | 0x3ff;
              if (BitConverter.IsLittleEndian) {
                sTwoByteFloatTempBuffer[3] = (byte) (modifiedIntValCase1 >> 24);
                sTwoByteFloatTempBuffer[2] = (byte) (modifiedIntValCase1 >> 16);
                sTwoByteFloatTempBuffer[1] = (byte) (modifiedIntValCase1 >> 8);
                sTwoByteFloatTempBuffer[0] = (byte) (modifiedIntValCase1);
              } else {
                sTwoByteFloatTempBuffer[0] = (byte) (modifiedIntValCase1 >> 24);
                sTwoByteFloatTempBuffer[1] = (byte) (modifiedIntValCase1 >> 16);
                sTwoByteFloatTempBuffer[2] = (byte) (modifiedIntValCase1 >> 8);
                sTwoByteFloatTempBuffer[3] = (byte) (modifiedIntValCase1);
              }
              return BitConverter.ToSingle(sTwoByteFloatTempBuffer, 0);
          }
      }
      else if (mant != 0)
      {
          exp = 0x1c400;
          do
          {
              mant <<= 1;
              exp -= 0x400;
          } while ((mant & 0x400) == 0);
          mant &= 0x3ff;
      }
      // [HACK]: to avoid using bit converter and dynamically allocating memory,
      // use a static buffer + some manual conversion. This assumes an endian-ness
      // though!
      int modifiedIntValCase2 = (intVal & 0x8000) << 16 | (exp | mant) << 13;
      if (BitConverter.IsLittleEndian) {
        sTwoByteFloatTempBuffer[3] = (byte) (modifiedIntValCase2 >> 24);
        sTwoByteFloatTempBuffer[2] = (byte) (modifiedIntValCase2 >> 16);
        sTwoByteFloatTempBuffer[1] = (byte) (modifiedIntValCase2 >> 8);
        sTwoByteFloatTempBuffer[0] = (byte) (modifiedIntValCase2);
      } else {
        sTwoByteFloatTempBuffer[0] = (byte) (modifiedIntValCase2 >> 24);
        sTwoByteFloatTempBuffer[1] = (byte) (modifiedIntValCase2 >> 16);
        sTwoByteFloatTempBuffer[2] = (byte) (modifiedIntValCase2 >> 8);
        sTwoByteFloatTempBuffer[3] = (byte) (modifiedIntValCase2);
      }
      return BitConverter.ToSingle(sTwoByteFloatTempBuffer, 0);
  }

}

} // namespace Expanse
