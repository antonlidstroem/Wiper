using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiper.Core.Services
{
    public static class ByteSizeFormatter
    {
        public static string FormatSize(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            double dblSByte = bytes;
            int i = 0;
            while (dblSByte >= 1024 && i < suffix.Length - 1)
            {
                i++;
                dblSByte /= 1024;
            }

            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:0.##} {1}", dblSByte, suffix[i]);
        }
    }
}
