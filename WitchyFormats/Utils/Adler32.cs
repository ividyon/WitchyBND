using System;

namespace WitchyFormats;

/* -----------------------------------------------------------------------------
The following code is a C# port of a portion of adler32.c from teh zlib library,
which is licensed under the ZLIB license as follows
Copyright (C) 1995-2022 Jean-loup Gailly and Mark Adler
This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:
1.  The origin of this software must not be misrepresented; you must not
    claim that you wrote the original software. If you use this software
    in a product, an acknowledgment in the product documentation would be
    appreciated but is not required.
2.  Altered source versions must be plainly marked as such, and must not be
    misrepresented as being the original software.
3.  This notice may not be removed or altered from any source distribution.
Jean-loup Gailly        Mark Adler
jloup@gzip.org          madler@alumni.caltech.edu
---------------------------------------------------------------------------- */

/* ----------------------------------------------------------------------------
The ported code to C# is is licensed under the MIT license as follows
Copyright (c) 2022 Christopher Whitley
Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice (including the next
paragraph) shall be included in all copies or substantial portions of the
Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
---------------------------------------------------------------------------- */

/// <summary>
///     Utility class for calculating an Adler-32 checksum
/// </summary>
public class Adler32
{
    private const uint BASE = 65521;    //  Largest prime smaller than 65536
    private const int NMAX = 5552;      //  NMAX is the largest n such that 255n(+1)/2 + (n+1)(BASE-1) <= 2^32-1

    private uint _value;

    /// <summary>
    ///     Gets the current checksum value.
    /// </summary>
    public uint CurrentValue => _value;

    /// <summary>
    ///     Creates a new instance of the <see cref="Adler32"/> class.
    /// </summary>
    /// <remarks>
    ///     This will intiialize the underlying checksum value to 1
    /// </remarks>
    public Adler32() => _value = 1U;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Adler32"/> class.
    /// </summary>
    /// <param name="initial">
    ///     The initiale checksum value to start with.
    /// </param>
    public Adler32(uint initial) => _value = initial;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Adler32"/> class.
    /// </summary>
    /// <param name="initial">
    ///     A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> elements that
    ///     that the initial checksum value will be calcualted from
    /// </param>
    public Adler32(ReadOnlySpan<byte> initial) : this() => _ = Update(initial);

    /// <summary>
    ///     Resets the underlying checksum value of this instance of the
    ///     <see cref="Adler32"/> class to 1.
    /// </summary>
    public void Reset() => _value = 1U;

    /// <summary>
    ///     Updates and returns the underlying checksum value using the
    ///     specified <paramref name="buffer"/>.
    /// </summary>
    /// <param name="buffer">
    ///     A <see cref="ReadOnlySpan{T}"/> of <see cref="byte"/> elements that
    ///     will be added to the underlying checksum value.
    /// </param>
    /// <returns>
    ///     The updated checksum value.
    /// </returns>
    public uint Update(ReadOnlySpan<byte> buffer)
    {
        uint sum2;
        uint n;
        int len = buffer.Length;
        int pos = 0;

        //  Split Adler-32 into component sums
        sum2 = (_value >> 16) & 0xFFFF;
        _value &= 0xFFFF;

        //  In case the user likes doing a byte at a time, keep it fast
        if (len == 1)
        {
            _value += buffer[0];

            if (_value >= BASE)
            {
                _value -= BASE;
            }

            sum2 += _value;

            if (sum2 >= BASE)
            {
                sum2 -= BASE;
            }

            _value |= (sum2 << 16);
            return _value;
        }

        //  In case short lengths are provided, keep it somewhat fast
        if (len < 16)
        {
            for (int i = 0; i < len; i++)
            {
                _value += buffer[pos++];
                sum2 += _value;
            }

            if (_value >= BASE)
            {
                _value -= BASE;
            }

            sum2 %= BASE;

            _value |= (sum2 << 16);
            return _value;
        }

        //  Do length NMAX blocks -- requires just one modulo operation
        while (len >= NMAX)
        {
            len -= NMAX;

            for (n = NMAX / 16; n > 0; --n)
            {
                for (int i = 0; i < 16; i++)
                {
                    _value += buffer[pos++];
                    sum2 += _value;
                }
            }

            _value %= BASE;
            sum2 %= BASE;
        }

        //  Do remaining bytes (less than NMAX, still just one modulo)
        if (len > 0)    //  avoid modulos if none remaining
        {
            while (len >= 16)
            {
                len -= 16;

                for (int i = 0; i < 16; i++)
                {
                    _value += buffer[pos++];
                    sum2 += _value;
                }
            }

            while (len-- > 0)
            {
                _value += buffer[pos++];
                sum2 += _value;
            }

            _value %= BASE;
            sum2 %= BASE;
        }

        //  Recombine sums
        _value |= (sum2 << 16);
        return _value;
    }
}