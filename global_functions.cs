using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libfbclient.net
{
    class global_functions
    {
        public static System.DateTime GetDateFromUnixTimestamp(double unixTimestamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimestamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
