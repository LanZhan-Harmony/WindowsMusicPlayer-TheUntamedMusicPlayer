using System.Diagnostics;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.System.Numerics;
internal static partial class BigIntegerCalculator
{
    private static readonly int SquareThreshold = 32;
    private static readonly int AllocationThreshold = 256;

    private static unsafe void Square(uint* value, int valueLength,
                                      uint* bits, int bitsLength)
    {
        Debug.Assert(valueLength >= 0);
        Debug.Assert(bitsLength == valueLength + valueLength);

        // Executes different algorithms for computing z = a * a
        // based on the actual length of a. If a is "small" enough
        // we stick to the classic "grammar-school" method; for the
        // rest we switch to implementations with less complexity
        // albeit more overhead (which needs to pay off!).

        // NOTE: useful thresholds needs some "empirical" testing,
        // which are smaller in DEBUG mode for testing purpose.

        if (valueLength < SquareThreshold)
        {
            // Squares the bits using the "grammar-school" method.
            // Envisioning the "rhombus" of a pen-and-paper calculation
            // we see that computing z_i+j += a_j * a_i can be optimized
            // since a_j * a_i = a_i * a_j (we're squaring after all!).
            // Thus, we directly get z_i+j += 2 * a_j * a_i + c.

            // ATTENTION: an ordinary multiplication is safe, because
            // z_i+j + a_j * a_i + c <= 2(2^32 - 1) + (2^32 - 1)^2 =
            // = 2^64 - 1 (which perfectly matches with ulong!). But
            // here we would need an UInt65... Hence, we split these
            // operation and do some extra shifts.

            for (var i = 0; i < valueLength; i++)
            {
                var carry = 0UL;
                for (var j = 0; j < i; j++)
                {
                    var digit1 = bits[i + j] + carry;
                    var digit2 = (ulong)value[j] * value[i];
                    bits[i + j] = unchecked((uint)(digit1 + (digit2 << 1)));
                    carry = (digit2 + (digit1 >> 1)) >> 31;
                }
                var digits = (ulong)value[i] * value[i] + carry;
                bits[i + i] = unchecked((uint)digits);
                bits[i + i + 1] = (uint)(digits >> 32);
            }
        }
        else
        {
            // Based on the Toom-Cook multiplication we split value
            // into two smaller values, doing recursive squaring.
            // The special form of this multiplication, where we
            // split both operands into two operands, is also known
            // as the Karatsuba algorithm...

            // https://en.wikipedia.org/wiki/Toom-Cook_multiplication
            // https://en.wikipedia.org/wiki/Karatsuba_algorithm

            // Say we want to compute z = a * a ...

            // ... we need to determine our new length (just the half)
            var n = valueLength >> 1;
            var n2 = n << 1;

            // ... split value like a = (a_1 << n) + a_0
            var valueLow = value;
            var valueLowLength = n;
            var valueHigh = value + n;
            var valueHighLength = valueLength - n;

            // ... prepare our result array (to reuse its memory)
            var bitsLow = bits;
            var bitsLowLength = n2;
            var bitsHigh = bits + n2;
            var bitsHighLength = bitsLength - n2;

            // ... compute z_0 = a_0 * a_0 (squaring again!)
            Square(valueLow, valueLowLength,
                   bitsLow, bitsLowLength);

            // ... compute z_2 = a_1 * a_1 (squaring again!)
            Square(valueHigh, valueHighLength,
                   bitsHigh, bitsHighLength);

            var foldLength = valueHighLength + 1;
            var coreLength = foldLength + foldLength;

            if (coreLength < AllocationThreshold)
            {
                var fold = stackalloc uint[foldLength];
                new Span<uint>(fold, foldLength).Clear();
                var core = stackalloc uint[coreLength];
                new Span<uint>(core, coreLength).Clear();

                // ... compute z_a = a_1 + a_0 (call it fold...)
                Add(valueHigh, valueHighLength,
                    valueLow, valueLowLength,
                    fold, foldLength);

                // ... compute z_1 = z_a * z_a - z_0 - z_2
                Square(fold, foldLength,
                       core, coreLength);
                SubtractCore(bitsHigh, bitsHighLength,
                             bitsLow, bitsLowLength,
                             core, coreLength);

                // ... and finally merge the result! :-)
                AddSelf(bits + n, bitsLength - n, core, coreLength);
            }
            else
            {
                fixed (uint* fold = new uint[foldLength],
                             core = new uint[coreLength])
                {
                    // ... compute z_a = a_1 + a_0 (call it fold...)
                    Add(valueHigh, valueHighLength,
                        valueLow, valueLowLength,
                        fold, foldLength);

                    // ... compute z_1 = z_a * z_a - z_0 - z_2
                    Square(fold, foldLength,
                           core, coreLength);
                    SubtractCore(bitsHigh, bitsHighLength,
                                 bitsLow, bitsLowLength,
                                 core, coreLength);

                    // ... and finally merge the result! :-)
                    AddSelf(bits + n, bitsLength - n, core, coreLength);
                }
            }
        }
    }

    private static readonly int MultiplyThreshold = 32;

    private static unsafe void Multiply(uint* left, int leftLength,
                                        uint* right, int rightLength,
                                        uint* bits, int bitsLength)
    {
        Debug.Assert(leftLength >= 0);
        Debug.Assert(rightLength >= 0);
        Debug.Assert(leftLength >= rightLength);
        Debug.Assert(bitsLength == leftLength + rightLength);

        // Executes different algorithms for computing z = a * b
        // based on the actual length of b. If b is "small" enough
        // we stick to the classic "grammar-school" method; for the
        // rest we switch to implementations with less complexity
        // albeit more overhead (which needs to pay off!).

        // NOTE: useful thresholds needs some "empirical" testing,
        // which are smaller in DEBUG mode for testing purpose.

        if (rightLength < MultiplyThreshold)
        {
            // Multiplies the bits using the "grammar-school" method.
            // Envisioning the "rhombus" of a pen-and-paper calculation
            // should help getting the idea of these two loops...
            // The inner multiplication operations are safe, because
            // z_i+j + a_j * b_i + c <= 2(2^32 - 1) + (2^32 - 1)^2 =
            // = 2^64 - 1 (which perfectly matches with ulong!).

            for (var i = 0; i < rightLength; i++)
            {
                var carry = 0UL;
                for (var j = 0; j < leftLength; j++)
                {
                    var digits = bits[i + j] + carry
                        + (ulong)left[j] * right[i];
                    bits[i + j] = unchecked((uint)digits);
                    carry = digits >> 32;
                }
                bits[i + leftLength] = (uint)carry;
            }
        }
        else
        {
            // Based on the Toom-Cook multiplication we split left/right
            // into two smaller values, doing recursive multiplication.
            // The special form of this multiplication, where we
            // split both operands into two operands, is also known
            // as the Karatsuba algorithm...

            // https://en.wikipedia.org/wiki/Toom-Cook_multiplication
            // https://en.wikipedia.org/wiki/Karatsuba_algorithm

            // Say we want to compute z = a * b ...

            // ... we need to determine our new length (just the half)
            var n = rightLength >> 1;
            var n2 = n << 1;

            // ... split left like a = (a_1 << n) + a_0
            var leftLow = left;
            var leftLowLength = n;
            var leftHigh = left + n;
            var leftHighLength = leftLength - n;

            // ... split right like b = (b_1 << n) + b_0
            var rightLow = right;
            var rightLowLength = n;
            var rightHigh = right + n;
            var rightHighLength = rightLength - n;

            // ... prepare our result array (to reuse its memory)
            var bitsLow = bits;
            var bitsLowLength = n2;
            var bitsHigh = bits + n2;
            var bitsHighLength = bitsLength - n2;

            // ... compute z_0 = a_0 * b_0 (multiply again)
            Multiply(leftLow, leftLowLength,
                     rightLow, rightLowLength,
                     bitsLow, bitsLowLength);

            // ... compute z_2 = a_1 * b_1 (multiply again)
            Multiply(leftHigh, leftHighLength,
                     rightHigh, rightHighLength,
                     bitsHigh, bitsHighLength);

            var leftFoldLength = leftHighLength + 1;
            var rightFoldLength = rightHighLength + 1;
            var coreLength = leftFoldLength + rightFoldLength;

            if (coreLength < AllocationThreshold)
            {
                var leftFold = stackalloc uint[leftFoldLength];
                new Span<uint>(leftFold, leftFoldLength).Clear();
                var rightFold = stackalloc uint[rightFoldLength];
                new Span<uint>(rightFold, rightFoldLength).Clear();
                var core = stackalloc uint[coreLength];
                new Span<uint>(core, coreLength).Clear();

                // ... compute z_a = a_1 + a_0 (call it fold...)
                Add(leftHigh, leftHighLength,
                    leftLow, leftLowLength,
                    leftFold, leftFoldLength);

                // ... compute z_b = b_1 + b_0 (call it fold...)
                Add(rightHigh, rightHighLength,
                    rightLow, rightLowLength,
                    rightFold, rightFoldLength);

                // ... compute z_1 = z_a * z_b - z_0 - z_2
                Multiply(leftFold, leftFoldLength,
                         rightFold, rightFoldLength,
                         core, coreLength);
                SubtractCore(bitsHigh, bitsHighLength,
                             bitsLow, bitsLowLength,
                             core, coreLength);

                // ... and finally merge the result! :-)
                AddSelf(bits + n, bitsLength - n, core, coreLength);
            }
            else
            {
                fixed (uint* leftFold = new uint[leftFoldLength],
                             rightFold = new uint[rightFoldLength],
                             core = new uint[coreLength])
                {
                    // ... compute z_a = a_1 + a_0 (call it fold...)
                    Add(leftHigh, leftHighLength,
                        leftLow, leftLowLength,
                        leftFold, leftFoldLength);

                    // ... compute z_b = b_1 + b_0 (call it fold...)
                    Add(rightHigh, rightHighLength,
                        rightLow, rightLowLength,
                        rightFold, rightFoldLength);

                    // ... compute z_1 = z_a * z_b - z_0 - z_2
                    Multiply(leftFold, leftFoldLength,
                             rightFold, rightFoldLength,
                             core, coreLength);
                    SubtractCore(bitsHigh, bitsHighLength,
                                 bitsLow, bitsLowLength,
                                 core, coreLength);

                    // ... and finally merge the result! :-)
                    AddSelf(bits + n, bitsLength - n, core, coreLength);
                }
            }
        }
    }

    private static unsafe void SubtractCore(uint* left, int leftLength,
                                            uint* right, int rightLength,
                                            uint* core, int coreLength)
    {
        Debug.Assert(leftLength >= 0);
        Debug.Assert(rightLength >= 0);
        Debug.Assert(coreLength >= 0);
        Debug.Assert(leftLength >= rightLength);
        Debug.Assert(coreLength >= leftLength);

        // Executes a special subtraction algorithm for the multiplication,
        // which needs to subtract two different values from a core value,
        // while core is always bigger than the sum of these values.

        // NOTE: we could do an ordinary subtraction of course, but we spare
        // one "run", if we do this computation within a single one...

        var i = 0;
        var carry = 0L;

        for (; i < rightLength; i++)
        {
            var digit = core[i] + carry - left[i] - right[i];
            core[i] = unchecked((uint)digit);
            carry = digit >> 32;
        }
        for (; i < leftLength; i++)
        {
            var digit = core[i] + carry - left[i];
            core[i] = unchecked((uint)digit);
            carry = digit >> 32;
        }
        for (; carry != 0 && i < coreLength; i++)
        {
            var digit = core[i] + carry;
            core[i] = (uint)digit;
            carry = digit >> 32;
        }
    }
}
