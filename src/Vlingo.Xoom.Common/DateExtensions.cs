// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;

namespace Vlingo.Xoom.Common
{
    public static class DateExtensions
    {
        public static double GetCurrentMillis()
        {
            var jan1970 = new DateTime(1970, 1, 1, 0, 0,0, DateTimeKind.Utc);
            var javaSpan = DateTime.UtcNow - jan1970;
            return javaSpan.TotalMilliseconds;
        }

        public static double GetMillis(this DateTime date)
        {
            var jan1970 = new DateTime(1970, 1, 1, 0, 0,0, DateTimeKind.Utc);
            var javaSpan = date - jan1970;
            return javaSpan.TotalMilliseconds;
        }
        
        public static long GetCurrentSeconds(this DateTime dateTime)
        {
            var jan1970 = new DateTime(1970, 1, 1, 0, 0,0, DateTimeKind.Utc);
            var javaSpan = dateTime - jan1970;
            return Convert.ToInt64(javaSpan.TotalSeconds);
        }
    }
}