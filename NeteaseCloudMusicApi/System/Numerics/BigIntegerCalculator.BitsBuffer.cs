// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Numerics;

// ATTENTION: always pass BitsBuffer by reference,
// it's a structure for performance reasons. Furthermore
// it's a mutable one, so use it only with care!

internal static partial class BigIntegerCalculator
{
    internal struct BitsBuffer
    {
        private uint[] _bits;
        private int _length;

        public BitsBuffer(int size, uint value)
        {
            Debug.Assert(size >= 1);

            _bits = new uint[size];
            _length = value != 0 ? 1 : 0;

            _bits[0] = value;
        }

        public BitsBuffer(int size, uint[] value)
        {
            Debug.Assert(value != null);
            Debug.Assert(size >= ActualLength(value));

            _bits = new uint[size];
            _length = ActualLength(value);

            Array.Copy(value, 0, _bits, 0, _length);
        }

        public unsafe void MultiplySelf(ref BitsBuffer value,
                                        ref BitsBuffer temp)
        {
            Debug.Assert(temp._length == 0);
            Debug.Assert(_length + value._length <= temp._bits.Length);

            // Executes a multiplication for this and value, writes the
            // result to temp. Switches this and temp arrays afterwards.

            fixed (uint* b = _bits, v = value._bits, t = temp._bits)
            {
                if (_length < value._length)
                {
                    Multiply(v, value._length,
                             b, _length,
                             t, _length + value._length);
                }
                else
                {
                    Multiply(b, _length,
                             v, value._length,
                             t, _length + value._length);
                }
            }

            Apply(ref temp, _length + value._length);
        }

        public unsafe void SquareSelf(ref BitsBuffer temp)
        {
            Debug.Assert(temp._length == 0);
            Debug.Assert(_length + _length <= temp._bits.Length);

            // Executes a square for this, writes the result to temp.
            // Switches this and temp arrays afterwards.

            fixed (uint* b = _bits, t = temp._bits)
            {
                Square(b, _length,
                       t, _length + _length);
            }

            Apply(ref temp, _length + _length);
        }

        public void Reduce(ref FastReducer reducer) =>
            // Executes a modulo operation using an optimized reducer.
            // Thus, no need of any switching here, happens in-line.

            _length = reducer.Reduce(_bits, _length);

        public unsafe void Reduce(uint[] modulus)
        {
            Debug.Assert(modulus != null);

            // Executes a modulo operation using the divide operation.
            // Thus, no need of any switching here, happens in-line.

            if (_length >= modulus.Length)
            {
                fixed (uint* b = _bits, m = modulus)
                {
                    Divide(b, _length,
                           m, modulus.Length,
                           null, 0);
                }

                _length = ActualLength(_bits, modulus.Length);
            }
        }

        public readonly uint[] GetBits() => _bits;

        public readonly int GetSize() => _bits.Length;

        private void Apply(ref BitsBuffer temp, int maxLength)
        {
            Debug.Assert(temp._length == 0);
            Debug.Assert(maxLength <= temp._bits.Length);

            // Resets this and switches this and temp afterwards.
            // The caller assumed an empty temp, the next will too.

            Array.Clear(_bits, 0, _length);

            (_bits, temp._bits) = (temp._bits, _bits);
            _length = ActualLength(_bits, maxLength);
        }
    }
}
