using System;
using System.Collections.Generic;

namespace VoidMode
{
    public class AppConfig
    {
        public List<string> AppPaths { get; set; } = new List<string>();
        public bool EnableBlackScreen { get; set; } = true;
        public bool EnableDisplayOff { get; set; } = true;
        public bool EnableMute { get; set; } = true;
        public bool EnableAutoKill { get; set; } = false;
    }
}
