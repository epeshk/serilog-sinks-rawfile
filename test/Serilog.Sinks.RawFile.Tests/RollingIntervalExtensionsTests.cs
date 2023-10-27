﻿using System;
using Xunit;

namespace Serilog.Sinks.RawFile.Tests
{
    public class RollingIntervalExtensionsTests
    {
        public static object?[][] IntervalInstantCurrentNextCheckpoint => new[]
        {
            new object?[]{ RawFileRollingInterval.Infinite, new DateTime(2018, 01, 01),           null, null },
            new object?[]{ RawFileRollingInterval.Year,     new DateTime(2018, 01, 01),           new DateTime(2018, 01, 01), new DateTime(2019, 01, 01) },
            new object?[]{ RawFileRollingInterval.Year,     new DateTime(2018, 06, 01),           new DateTime(2018, 01, 01), new DateTime(2019, 01, 01) },
            new object?[]{ RawFileRollingInterval.Month,    new DateTime(2018, 01, 01),           new DateTime(2018, 01, 01), new DateTime(2018, 02, 01) },
            new object?[]{ RawFileRollingInterval.Month,    new DateTime(2018, 01, 14),           new DateTime(2018, 01, 01), new DateTime(2018, 02, 01) },
            new object?[]{ RawFileRollingInterval.Day,      new DateTime(2018, 01, 01),           new DateTime(2018, 01, 01), new DateTime(2018, 01, 02) },
            new object?[]{ RawFileRollingInterval.Day,      new DateTime(2018, 01, 01, 12, 0, 0), new DateTime(2018, 01, 01), new DateTime(2018, 01, 02) },
            new object?[]{ RawFileRollingInterval.Hour,     new DateTime(2018, 01, 01, 0, 0, 0),  new DateTime(2018, 01, 01), new DateTime(2018, 01, 01, 1, 0, 0) },
            new object?[]{ RawFileRollingInterval.Hour,     new DateTime(2018, 01, 01, 0, 30, 0), new DateTime(2018, 01, 01), new DateTime(2018, 01, 01, 1, 0, 0) },
            new object?[]{ RawFileRollingInterval.Minute,   new DateTime(2018, 01, 01, 0, 0, 0),  new DateTime(2018, 01, 01), new DateTime(2018, 01, 01, 0, 1, 0) },
            new object?[]{ RawFileRollingInterval.Minute,   new DateTime(2018, 01, 01, 0, 0, 30), new DateTime(2018, 01, 01), new DateTime(2018, 01, 01, 0, 1, 0) }
        };

        [Theory]
        [MemberData(nameof(IntervalInstantCurrentNextCheckpoint))]
        public void NextIntervalTests(RawFileRollingInterval interval, DateTime instant, DateTime? currentCheckpoint, DateTime? nextCheckpoint)
        {
            var current = interval.GetCurrentCheckpoint(instant);
            Assert.Equal(currentCheckpoint, current);

            var next = interval.GetNextCheckpoint(instant);
            Assert.Equal(nextCheckpoint, next);
        }
    }
}
