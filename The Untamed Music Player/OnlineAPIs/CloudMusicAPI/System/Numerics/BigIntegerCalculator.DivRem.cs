using System.Diagnostics;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.System.Numerics;
internal static partial class BigIntegerCalculator
{
    public static uint Remainder(uint[] left, uint right)
    {
        Debug.Assert(left != null);
        Debug.Assert(left.Length >= 1);

        // Same as above, but only computing the remainder.

        var carry = 0UL;
        for (var i = left.Length - 1; i >= 0; i--)
        {
            var value = (carry << 32) | left[i];
            carry = value % right;
        }

        return (uint)carry;
    }

    public static unsafe uint[] Divide(uint[] left, uint[] right)
    {
        Debug.Assert(left != null);
        Debug.Assert(right != null);
        Debug.Assert(left.Length >= 1);
        Debug.Assert(right.Length >= 1);
        Debug.Assert(left.Length >= right.Length);

        // Same as above, but only returning the quotient.

        // NOTE: left will get overwritten, we need a local copy

        var localLeft = CreateCopy(left);
        var bits = new uint[left.Length - right.Length + 1];

        fixed (uint* l = &localLeft[0], r = &right[0], b = &bits[0])
        {
            Divide(l, localLeft.Length,
                   r, right.Length,
                   b, bits.Length);
        }

        return bits;
    }

    public static unsafe uint[] Remainder(uint[] left, uint[] right)
    {
        Debug.Assert(left != null);
        Debug.Assert(right != null);
        Debug.Assert(left.Length >= 1);
        Debug.Assert(right.Length >= 1);
        Debug.Assert(left.Length >= right.Length);

        // Same as above, but only returning the remainder.

        // NOTE: left will get overwritten, we need a local copy

        var localLeft = CreateCopy(left);

        fixed (uint* l = &localLeft[0], r = &right[0])
        {
            Divide(l, localLeft.Length,
                   r, right.Length,
                   null, 0);
        }

        return localLeft;
    }

    private static unsafe void Divide(uint* left, int leftLength,
                                      uint* right, int rightLength,
                                      uint* bits, int bitsLength)
    {
        Debug.Assert(leftLength >= 1);
        Debug.Assert(rightLength >= 1);
        Debug.Assert(leftLength >= rightLength);
        Debug.Assert(bitsLength == leftLength - rightLength + 1
            || bitsLength == 0);

        // Executes the "grammar-school" algorithm for computing q = a / b.
        // Before calculating q_i, we get more bits into the highest bit
        // block of the divisor. Thus, guessing digits of the quotient
        // will be more precise. Additionally we'll get r = a % b.

        var divHi = right[rightLength - 1];
        var divLo = rightLength > 1 ? right[rightLength - 2] : 0;

        // We measure the leading zeros of the divisor
        var shift = LeadingZeros(divHi);
        var backShift = 32 - shift;

        // And, we make sure the most significant bit is set
        if (shift > 0)
        {
            var divNx = rightLength > 2 ? right[rightLength - 3] : 0;

            divHi = (divHi << shift) | (divLo >> backShift);
            divLo = (divLo << shift) | (divNx >> backShift);
        }

        // Then, we divide all of the bits as we would do it using
        // pen and paper: guessing the next digit, subtracting, ...
        for (var i = leftLength; i >= rightLength; i--)
        {
            var n = i - rightLength;
            var t = i < leftLength ? left[i] : 0;

            var valHi = ((ulong)t << 32) | left[i - 1];
            var valLo = i > 1 ? left[i - 2] : 0;

            // We shifted the divisor, we shift the dividend too
            if (shift > 0)
            {
                var valNx = i > 2 ? left[i - 3] : 0;

                valHi = (valHi << shift) | (valLo >> backShift);
                valLo = (valLo << shift) | (valNx >> backShift);
            }

            // First guess for the current digit of the quotient,
            // which naturally must have only 32 bits...
            var digit = valHi / divHi;
            if (digit > 0xFFFFFFFF)
            {
                digit = 0xFFFFFFFF;
            }

            // Our first guess may be a little bit to big
            while (DivideGuessTooBig(digit, valHi, valLo, divHi, divLo))
            {
                --digit;
            }

            if (digit > 0)
            {
                // Now it's time to subtract our current quotient
                var carry = SubtractDivisor(left + n, leftLength - n,
                                             right, rightLength, digit);
                if (carry != t)
                {
                    Debug.Assert(carry == t + 1);

                    // Our guess was still exactly one too high
                    carry = AddDivisor(left + n, leftLength - n,
                                       right, rightLength);
                    --digit;

                    Debug.Assert(carry == 1);
                }
            }

            // We have the digit!
            if (bitsLength != 0)
            {
                bits[n] = (uint)digit;
            }

            if (i < leftLength)
            {
                left[i] = 0;
            }
        }
    }

    private static unsafe uint AddDivisor(uint* left, int leftLength,
                                          uint* right, int rightLength)
    {
        Debug.Assert(leftLength >= 0);
        Debug.Assert(rightLength >= 0);
        Debug.Assert(leftLength >= rightLength);

        // Repairs the dividend, if the last subtract was too much

        var carry = 0UL;

        for (var i = 0; i < rightLength; i++)
        {
            var digit = left[i] + carry + right[i];
            left[i] = unchecked((uint)digit);
            carry = digit >> 32;
        }

        return (uint)carry;
    }

    private static unsafe uint SubtractDivisor(uint* left, int leftLength,
                                               uint* right, int rightLength,
                                               ulong q)
    {
        Debug.Assert(leftLength >= 0);
        Debug.Assert(rightLength >= 0);
        Debug.Assert(leftLength >= rightLength);
        Debug.Assert(q <= 0xFFFFFFFF);

        // Combines a subtract and a multiply operation, which is naturally
        // more efficient than multiplying and then subtracting...

        var carry = 0UL;

        for (var i = 0; i < rightLength; i++)
        {
            carry += right[i] * q;
            var digit = unchecked((uint)carry);
            carry >>= 32;
            if (left[i] < digit)
            {
                ++carry;
            }

            left[i] = unchecked(left[i] - digit);
        }

        return (uint)carry;
    }

    private static bool DivideGuessTooBig(ulong q, ulong valHi, uint valLo,
                                          uint divHi, uint divLo)
    {
        Debug.Assert(q <= 0xFFFFFFFF);

        // We multiply the two most significant limbs of the divisor
        // with the current guess for the quotient. If those are bigger
        // than the three most significant limbs of the current dividend
        // we return true, which means the current guess is still too big.

        var chkHi = divHi * q;
        var chkLo = divLo * q;

        chkHi += (chkLo >> 32);
        chkLo &= 0xFFFFFFFF;

        if (chkHi < valHi)
        {
            return false;
        }

        if (chkHi > valHi)
        {
            return true;
        }

        if (chkLo < valLo)
        {
            return false;
        }

        if (chkLo > valLo)
        {
            return true;
        }

        return false;
    }

    private static uint[] CreateCopy(uint[] value)
    {
        Debug.Assert(value != null);
        Debug.Assert(value.Length != 0);

        var bits = new uint[value.Length];
        Array.Copy(value, 0, bits, 0, bits.Length);
        return bits;
    }

    private static int LeadingZeros(uint value)
    {
        if (value == 0)
        {
            return 32;
        }

        var count = 0;
        if ((value & 0xFFFF0000) == 0)
        {
            count += 16;
            value <<= 16;
        }
        if ((value & 0xFF000000) == 0)
        {
            count += 8;
            value <<= 8;
        }
        if ((value & 0xF0000000) == 0)
        {
            count += 4;
            value <<= 4;
        }
        if ((value & 0xC0000000) == 0)
        {
            count += 2;
            value <<= 2;
        }
        if ((value & 0x80000000) == 0)
        {
            count += 1;
        }

        return count;
    }
}
