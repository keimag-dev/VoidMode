using System;

namespace VoidMode
{
    public sealed class StartupOptions
    {
        public bool StartDirectly { get; private set; }
        public bool SelfTest { get; private set; }
        public bool DebugSafeMode { get; private set; }
        public int? DebugAutoExitSeconds { get; private set; }

        public static StartupOptions Parse(string[] args)
        {
            var options = new StartupOptions();

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.Equals("--start", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("/start", StringComparison.OrdinalIgnoreCase))
                {
                    options.StartDirectly = true;
                    continue;
                }

                if (arg.Equals("--self-test", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("/self-test", StringComparison.OrdinalIgnoreCase))
                {
                    options.SelfTest = true;
                    continue;
                }

                if (arg.Equals("--debug-safe", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("/debug-safe", StringComparison.OrdinalIgnoreCase))
                {
                    options.DebugSafeMode = true;
                    continue;
                }

                const string autoExitPrefix = "--debug-auto-exit=";
                if (arg.StartsWith(autoExitPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(arg.Substring(autoExitPrefix.Length), out var seconds) && seconds > 0)
                    {
                        options.DebugAutoExitSeconds = seconds;
                    }
                    continue;
                }

                if (arg.Equals("--debug-auto-exit", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out var seconds) && seconds > 0)
                    {
                        options.DebugAutoExitSeconds = seconds;
                        i++;
                    }
                }
            }

            if (options.DebugAutoExitSeconds.HasValue)
            {
                options.StartDirectly = true;
            }

            return options;
        }
    }
}
