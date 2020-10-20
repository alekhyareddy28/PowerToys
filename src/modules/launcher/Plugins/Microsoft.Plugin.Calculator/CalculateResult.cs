﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Plugin.Calculator
{
    public struct CalculateResult : IEquatable<CalculateResult>
    {
        public bool ValidResult { get; set; }

        public decimal Result { get; set; }

        public decimal RoundedResult { get; set; }

        public bool Equals(CalculateResult other)
        {
            return Result == other.Result && RoundedResult == other.RoundedResult && ValidResult == other.ValidResult;
        }

        public override bool Equals(object obj)
        {
            return obj is CalculateResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Result, RoundedResult, ValidResult);
        }

        public static bool operator ==(CalculateResult left, CalculateResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CalculateResult left, CalculateResult right)
        {
            return !(left == right);
        }
    }
}
