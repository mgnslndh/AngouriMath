﻿
/* Copyright (c) 2019-2020 Angourisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
 * modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using AngouriMath;
using AngouriMath.Extensions;
using Xunit;

namespace UnitTests.PatternsTest
{
    public class BooleanSimplify
    {
        [Theory]
        [InlineData("true and true", "true")]
        [InlineData("false implies anything", "true")]
        [InlineData("false or anything", "false or anything")]
        [InlineData("true or anything", "true")]
        [InlineData("anything or false", "anything or false")]
        [InlineData("anything or true", "true")]
        [InlineData("anything or anything", "anything")]
        [InlineData("anything and anything", "anything")]
        [InlineData("anything implies anything", "true")]
        [InlineData("not a or not b", "not (a and b)")]
        [InlineData("not a and not b", "not (a or b)")]
        public void Test(string expr, string expected) 
            => Assert.Equal(expected.ToEntity(), expr.Simplify());
    }
}
