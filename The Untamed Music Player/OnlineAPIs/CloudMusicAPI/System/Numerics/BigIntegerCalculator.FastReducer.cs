using System.Diagnostics;

namespace The_Untamed_Music_Player.OnlineAPIs.CloudMusicAPI.System.Numerics;
internal static partial class BigIntegerCalculator
{
    internal readonly struct FastReducer
    {
        private readonly uint[] _modulus;
        private readonly uint[] _mu;
        private readonly uint[] _q1;
        private readonly uint[] _q2;

        private readonly int _muLength;

        public FastReducer(uint[] modulus)
        {
            Debug.Assert(modulus is not null);

            // Let r = 4^k, with 2^k > m
            var r = new uint[modulus.Length * 2 + 1];
            r[^1] = 1;

            // Let mu = 4^k / m
            _mu = Divide(r, modulus);
            _modulus = modulus;

            // Allocate memory for quotients once
            _q1 = new uint[modulus.Length * 2 + 2];
            _q2 = new uint[modulus.Length * 2 + 1];

            _muLength = ActualLength(_mu);
        }

        public int Reduce(uint[] value, int length)
        {
            Debug.Assert(value is not null);
            Debug.Assert(length <= value.Length);
            Debug.Assert(value.Length <= _modulus.Length * 2);

            // Trivial: value is shorter
            if (length < _modulus.Length)
            {
                return length;
            }

            // Let q1 = v/2^(k-1) * mu
            var l1 = DivMul(value, length, _mu, _muLength,
                            _q1, _modulus.Length - 1);

            // Let q2 = q1/2^(k+1) * m
            var l2 = DivMul(_q1, l1, _modulus, _modulus.Length,
                            _q2, _modulus.Length + 1);

            // Let v = (v - q2) % 2^(k+1) - i*m
            return SubMod(value, length, _q2, l2,
                          _modulus, _modulus.Length + 1);
        }

        private static unsafe int DivMul(uint[] left, int leftLength,
                                         uint[] right, int rightLength,
                                         uint[] bits, int k)
        {
            Debug.Assert(left is not null);
            Debug.Assert(left.Length >= leftLength);
            Debug.Assert(right is not null);
            Debug.Assert(right.Length >= rightLength);
            Debug.Assert(bits is not null);
            Debug.Assert(bits.Length + k >= leftLength + rightLength);

            // Executes the multiplication algorithm for left and right,
            // but skips the first k limbs of left, which is equivalent to
            // preceding division by 2^(32*k). To spare memory allocations
            // we write the result to an already allocated memory.

            Array.Clear(bits, 0, bits.Length);

            if (leftLength > k)
            {
                leftLength -= k;

                fixed (uint* l = left, r = right, b = bits)
                {
                    if (leftLength < rightLength)
                    {
                        Multiply(r, rightLength,
                                 l + k, leftLength,
                                 b, leftLength + rightLength);
                    }
                    else
                    {
                        Multiply(l + k, leftLength,
                                 r, rightLength,
                                 b, leftLength + rightLength);
                    }
                }

                return ActualLength(bits, leftLength + rightLength);
            }

            return 0;
        }

        private static unsafe int SubMod(uint[] left, int leftLength,
                                         uint[] right, int rightLength,
                                         uint[] modulus, int k)
        {
            Debug.Assert(left is not null);
            Debug.Assert(left.Length >= leftLength);
            Debug.Assert(right is not null);
            Debug.Assert(right.Length >= rightLength);

            // Executes the subtraction algorithm for left and right,
            // but considers only the first k limbs, which is equivalent to
            // preceding reduction by 2^(32*k). Furthermore, if left is
            // still greater than modulus, further subtractions are used.

            if (leftLength > k)
            {
                leftLength = k;
            }

            if (rightLength > k)
            {
                rightLength = k;
            }

            fixed (uint* l = left, r = right, m = modulus)
            {
                SubtractSelf(l, leftLength, r, rightLength);
                leftLength = ActualLength(left, leftLength);

                while (Compare(l, leftLength, m, modulus.Length) >= 0)
                {
                    SubtractSelf(l, leftLength, m, modulus.Length);
                    leftLength = ActualLength(left, leftLength);
                }
            }

            Array.Clear(left, leftLength, left.Length - leftLength);

            return leftLength;
        }
    }
}
