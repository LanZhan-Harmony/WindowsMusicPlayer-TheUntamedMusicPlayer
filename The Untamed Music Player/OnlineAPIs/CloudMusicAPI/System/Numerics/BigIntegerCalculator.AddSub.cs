using System.Diagnostics;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.System.Numerics;
internal static partial class BigIntegerCalculator
{
    private static unsafe void Add(uint* left, int leftLength,
                                   uint* right, int rightLength,
                                   uint* bits, int bitsLength)
    {
        Debug.Assert(leftLength >= 0);
        Debug.Assert(rightLength >= 0);
        Debug.Assert(leftLength >= rightLength);
        Debug.Assert(bitsLength == leftLength + 1);

        // Executes the "grammar-school" algorithm for computing z = a + b.
        // While calculating z_i = a_i + b_i we take care of overflow:
        // Since a_i + b_i + c <= 2(2^32 - 1) + 1 = 2^33 - 1, our carry c
        // has always the value 1 or 0; hence, we're safe here.

        var i = 0;
        var carry = 0L;

        for (; i < rightLength; i++)
        {
            var digit = left[i] + carry + right[i];
            bits[i] = unchecked((uint)digit);
            carry = digit >> 32;
        }
        for (; i < leftLength; i++)
        {
            var digit = left[i] + carry;
            bits[i] = unchecked((uint)digit);
            carry = digit >> 32;
        }
        bits[i] = (uint)carry;
    }

    private static unsafe void AddSelf(uint* left, int leftLength,
                                       uint* right, int rightLength)
    {
        Debug.Assert(leftLength >= 0);
        Debug.Assert(rightLength >= 0);
        Debug.Assert(leftLength >= rightLength);

        // Executes the "grammar-school" algorithm for computing z = a + b.
        // Same as above, but we're writing the result directly to a and
        // stop execution, if we're out of b and c is already 0.

        var i = 0;
        var carry = 0L;

        for (; i < rightLength; i++)
        {
            var digit = left[i] + carry + right[i];
            left[i] = unchecked((uint)digit);
            carry = digit >> 32;
        }
        for (; carry != 0 && i < leftLength; i++)
        {
            var digit = left[i] + carry;
            left[i] = (uint)digit;
            carry = digit >> 32;
        }

        Debug.Assert(carry == 0);
    }

    private static unsafe void SubtractSelf(uint* left, int leftLength,
                                            uint* right, int rightLength)
    {
        Debug.Assert(leftLength >= 0);
        Debug.Assert(rightLength >= 0);
        Debug.Assert(leftLength >= rightLength);
        Debug.Assert(Compare(left, leftLength, right, rightLength) >= 0);

        // Executes the "grammar-school" algorithm for computing z = a - b.
        // Same as above, but we're writing the result directly to a and
        // stop execution, if we're out of b and c is already 0.

        var i = 0;
        var carry = 0L;

        for (; i < rightLength; i++)
        {
            var digit = left[i] + carry - right[i];
            left[i] = unchecked((uint)digit);
            carry = digit >> 32;
        }
        for (; carry != 0 && i < leftLength; i++)
        {
            var digit = left[i] + carry;
            left[i] = (uint)digit;
            carry = digit >> 32;
        }

        Debug.Assert(carry == 0);
    }

    private static unsafe int Compare(uint* left, int leftLength,
                                      uint* right, int rightLength)
    {
        Debug.Assert(leftLength >= 0);
        Debug.Assert(rightLength >= 0);

        if (leftLength < rightLength)
        {
            return -1;
        }

        if (leftLength > rightLength)
        {
            return 1;
        }

        for (var i = leftLength - 1; i >= 0; i--)
        {
            if (left[i] < right[i])
            {
                return -1;
            }

            if (left[i] > right[i])
            {
                return 1;
            }
        }

        return 0;
    }
}
