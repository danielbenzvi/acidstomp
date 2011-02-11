using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AcidStomp
{
    public static class AppCompatibility
    {
        static bool _isMono = Type.GetType("Mono.Runtime") != null;

        public static bool IsMono
        {
            get
            {
                return _isMono;
            }
        }
    }
}
