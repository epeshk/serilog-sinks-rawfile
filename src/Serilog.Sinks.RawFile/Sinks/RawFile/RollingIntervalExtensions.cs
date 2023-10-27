// Copyright 2023 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Serilog.Sinks.RawFile
{
    static class RollingIntervalExtensions
    {
        public static string GetFormat(this RawFileRollingInterval interval)
        {
            switch (interval)
            {
                case RawFileRollingInterval.Infinite:
                    return "";
                case RawFileRollingInterval.Year:
                    return "yyyy";
                case RawFileRollingInterval.Month:
                    return "yyyyMM";
                case RawFileRollingInterval.Day:
                    return "yyyyMMdd";
                case RawFileRollingInterval.Hour:
                    return "yyyyMMddHH";
                case RawFileRollingInterval.Minute:
                    return "yyyyMMddHHmm";
                default:
                    throw new ArgumentException("Invalid rolling interval");
            }
        }

        public static DateTime? GetCurrentCheckpoint(this RawFileRollingInterval interval, DateTime instant)
        {
            switch (interval)
            {
                case RawFileRollingInterval.Infinite:
                    return null;
                case RawFileRollingInterval.Year:
                    return new DateTime(instant.Year, 1, 1, 0, 0, 0, instant.Kind);
                case RawFileRollingInterval.Month:
                    return new DateTime(instant.Year, instant.Month, 1, 0, 0, 0, instant.Kind);
                case RawFileRollingInterval.Day:
                    return new DateTime(instant.Year, instant.Month, instant.Day, 0, 0, 0, instant.Kind);
                case RawFileRollingInterval.Hour:
                    return new DateTime(instant.Year, instant.Month, instant.Day, instant.Hour, 0, 0, instant.Kind);
                case RawFileRollingInterval.Minute:
                    return new DateTime(instant.Year, instant.Month, instant.Day, instant.Hour, instant.Minute, 0, instant.Kind);
                default:
                    throw new ArgumentException("Invalid rolling interval");
            }
        }

        public static DateTime? GetNextCheckpoint(this RawFileRollingInterval interval, DateTime instant)
        {
            var current = GetCurrentCheckpoint(interval, instant);
            if (current == null)
                return null;

            switch (interval)
            {
                case RawFileRollingInterval.Year:
                    return current.Value.AddYears(1);
                case RawFileRollingInterval.Month:
                    return current.Value.AddMonths(1);
                case RawFileRollingInterval.Day:
                    return current.Value.AddDays(1);
                case RawFileRollingInterval.Hour:
                    return current.Value.AddHours(1);
                case RawFileRollingInterval.Minute:
                    return current.Value.AddMinutes(1);
                default:
                    throw new ArgumentException("Invalid rolling interval");
            }
        }
    }
}
