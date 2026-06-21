using System;
using System.Runtime.InteropServices;

namespace VoidMode
{
    public sealed class PowerRequestManager : IDisposable
    {
        private const uint POWER_REQUEST_CONTEXT_VERSION = 0;
        private const uint POWER_REQUEST_CONTEXT_SIMPLE_STRING = 0x1;
        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);

        private IntPtr _requestHandle = IntPtr.Zero;
        private bool _systemRequiredSet;
        private bool _executionRequiredSet;
        private bool _disposed;

        public void Enable()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PowerRequestManager));
            }

            if (HasHandle)
            {
                return;
            }

            var reason = new REASON_CONTEXT
            {
                Version = POWER_REQUEST_CONTEXT_VERSION,
                Flags = POWER_REQUEST_CONTEXT_SIMPLE_STRING,
                SimpleReasonString = "VoidMode is running in LLM server mode."
            };

            _requestHandle = PowerCreateRequest(ref reason);
            if (!HasHandle)
            {
                AppLogger.Error($"Failed to create power request. Win32Error={Marshal.GetLastWin32Error()}");
                _requestHandle = IntPtr.Zero;
                return;
            }

            _systemRequiredSet = SetRequest(PowerRequestType.PowerRequestSystemRequired, "SystemRequired");
            _executionRequiredSet = SetRequest(PowerRequestType.PowerRequestExecutionRequired, "ExecutionRequired");

            if (_systemRequiredSet || _executionRequiredSet)
            {
                AppLogger.Info($"Sleep prevention enabled. SystemRequired={_systemRequiredSet}, ExecutionRequired={_executionRequiredSet}");
            }
            else
            {
                AppLogger.Error("Power request handle was created, but no sleep prevention request could be set.");
            }
        }

        public void Disable()
        {
            if (!HasHandle)
            {
                return;
            }

            ClearRequestIfSet(PowerRequestType.PowerRequestExecutionRequired, "ExecutionRequired", ref _executionRequiredSet);
            ClearRequestIfSet(PowerRequestType.PowerRequestSystemRequired, "SystemRequired", ref _systemRequiredSet);

            if (!CloseHandle(_requestHandle))
            {
                AppLogger.Error($"Failed to close power request handle. Win32Error={Marshal.GetLastWin32Error()}");
            }

            _requestHandle = IntPtr.Zero;
            AppLogger.Info("Sleep prevention cleared.");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Disable();
            _disposed = true;
        }

        private bool HasHandle => _requestHandle != IntPtr.Zero && _requestHandle != InvalidHandleValue;

        private bool SetRequest(PowerRequestType requestType, string label)
        {
            if (PowerSetRequest(_requestHandle, requestType))
            {
                AppLogger.Info($"Power request set: {label}");
                return true;
            }

            AppLogger.Error($"Failed to set power request: {label}. Win32Error={Marshal.GetLastWin32Error()}");
            return false;
        }

        private void ClearRequestIfSet(PowerRequestType requestType, string label, ref bool isSet)
        {
            if (!isSet)
            {
                return;
            }

            if (PowerClearRequest(_requestHandle, requestType))
            {
                AppLogger.Info($"Power request cleared: {label}");
            }
            else
            {
                AppLogger.Error($"Failed to clear power request: {label}. Win32Error={Marshal.GetLastWin32Error()}");
            }

            isSet = false;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr PowerCreateRequest(ref REASON_CONTEXT context);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PowerSetRequest(IntPtr powerRequest, PowerRequestType requestType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PowerClearRequest(IntPtr powerRequest, PowerRequestType requestType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        private enum PowerRequestType
        {
            PowerRequestDisplayRequired = 0,
            PowerRequestSystemRequired = 1,
            PowerRequestAwayModeRequired = 2,
            PowerRequestExecutionRequired = 3
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct REASON_CONTEXT
        {
            public uint Version;
            public uint Flags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string SimpleReasonString;
        }
    }
}
