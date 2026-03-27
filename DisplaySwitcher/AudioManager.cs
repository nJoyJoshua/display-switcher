using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DisplaySwitcher
{
    /// <summary>
    /// Controls the default Windows audio playback device via PolicyConfig COM interface.
    /// No external libraries required — uses undocumented but stable Vista+ COM interfaces.
    /// </summary>
    public static class AudioManager
    {
        #region COM Interfaces

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator { }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(EDataFlow dataFlow, EDeviceState stateMask,
                out IMMDeviceCollection devices);
            int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role,
                out IMMDevice endpoint);
            int GetDevice(string id, out IMMDevice device);
            int RegisterEndpointNotificationCallback(IntPtr client);
            int UnregisterEndpointNotificationCallback(IntPtr client);
        }

        [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceCollection
        {
            int GetCount(out uint count);
            int Item(uint index, out IMMDevice device);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
            int OpenPropertyStore(int stgmAccess, out IPropertyStore properties);
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
            int GetState(out EDeviceState state);
        }

        [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            int GetCount(out int count);
            int GetAt(int prop, out PropertyKey key);
            int GetValue(ref PropertyKey key, out PropVariant value);
            int SetValue(ref PropertyKey key, ref PropVariant value);
            int Commit();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public int pid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropVariant
        {
            public short vt;
            public short wReserved1, wReserved2, wReserved3;
            public IntPtr p;
            public int p2;
        }

        private enum EDataFlow { Render = 0, Capture = 1, All = 2 }
        private enum EDeviceState { Active = 1, Disabled = 2, NotPresent = 4, Unplugged = 8, All = 15 }
        private enum ERole { Console = 0, Multimedia = 1, Communications = 2 }

        // Undocumented IPolicyConfig interface — stable since Vista
        [Guid("f8679f50-850a-41cf-9c72-430f290290c8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPolicyConfig
        {
            [PreserveSig] int GetMixFormat(string pszDeviceName, IntPtr ppFormat);
            [PreserveSig] int GetDeviceFormat(string pszDeviceName, bool bDefault, IntPtr ppFormat);
            [PreserveSig] int ResetDeviceFormat(string pszDeviceName);
            [PreserveSig] int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr MixFormat);
            [PreserveSig] int GetProcessingPeriod(string pszDeviceName, bool bDefault, IntPtr pmftDefaultPeriod, IntPtr pmftMinimumPeriod);
            [PreserveSig] int SetProcessingPeriod(string pszDeviceName, IntPtr pmftPeriod);
            [PreserveSig] int GetShareMode(string pszDeviceName, IntPtr pMode);
            [PreserveSig] int SetShareMode(string pszDeviceName, IntPtr mode);
            [PreserveSig] int GetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, IntPtr pv);
            [PreserveSig] int SetPropertyValue(string pszDeviceName, bool bFxStore, IntPtr key, IntPtr pv);
            [PreserveSig] int SetDefaultEndpoint(string pszDeviceName, ERole role);
            [PreserveSig] int SetEndpointVisibility(string pszDeviceName, bool bVisible);
        }

        [ComImport, Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
        private class PolicyConfig { }

        #endregion

        #region Public API

        public class AudioDevice
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public bool IsDefault { get; set; }
            public override string ToString() => Name;
        }

        public static List<AudioDevice> GetPlaybackDevices()
        {
            var list = new List<AudioDevice>();
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Console, out var defaultDev);
                defaultDev.GetId(out string defaultId);

                enumerator.EnumAudioEndpoints(EDataFlow.Render, EDeviceState.Active, out var col);
                col.GetCount(out uint count);

                // Property key for FriendlyName
                var pkey = new PropertyKey
                {
                    fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
                    pid = 14
                };

                for (uint i = 0; i < count; i++)
                {
                    col.Item(i, out var dev);
                    dev.GetId(out string id);
                    dev.OpenPropertyStore(0, out var store);
                    store.GetValue(ref pkey, out var pv);
                    string name = Marshal.PtrToStringUni(pv.p) ?? $"Device {i}";
                    list.Add(new AudioDevice
                    {
                        Id = id,
                        Name = name,
                        IsDefault = id == defaultId
                    });
                }
            }
            catch { /* COM not available or no devices */ }
            return list;
        }

        public static bool SetDefaultPlaybackDevice(string deviceId)
        {
            try
            {
                var policy = (IPolicyConfig)new PolicyConfig();
                policy.SetDefaultEndpoint(deviceId, ERole.Console);
                policy.SetDefaultEndpoint(deviceId, ERole.Multimedia);
                policy.SetDefaultEndpoint(deviceId, ERole.Communications);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string? GetDefaultPlaybackDeviceId()
        {
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Console, out var dev);
                dev.GetId(out string id);
                return id;
            }
            catch { return null; }
        }

        public static List<AudioDevice> GetRecordingDevices()
        {
            var list = new List<AudioDevice>();
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                enumerator.GetDefaultAudioEndpoint(EDataFlow.Capture, ERole.Console, out var defaultDev);
                defaultDev.GetId(out string defaultId);

                enumerator.EnumAudioEndpoints(EDataFlow.Capture, EDeviceState.Active, out var col);
                col.GetCount(out uint count);

                var pkey = new PropertyKey
                {
                    fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
                    pid = 14
                };

                for (uint i = 0; i < count; i++)
                {
                    col.Item(i, out var dev);
                    dev.GetId(out string id);
                    dev.OpenPropertyStore(0, out var store);
                    store.GetValue(ref pkey, out var pv);
                    string name = Marshal.PtrToStringUni(pv.p) ?? $"Device {i}";
                    list.Add(new AudioDevice
                    {
                        Id = id,
                        Name = name,
                        IsDefault = id == defaultId
                    });
                }
            }
            catch { /* COM not available or no devices */ }
            return list;
        }

        public static bool SetDefaultRecordingDevice(string deviceId)
        {
            try
            {
                var policy = (IPolicyConfig)new PolicyConfig();
                policy.SetDefaultEndpoint(deviceId, ERole.Console);
                policy.SetDefaultEndpoint(deviceId, ERole.Multimedia);
                policy.SetDefaultEndpoint(deviceId, ERole.Communications);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string? GetDefaultRecordingDeviceId()
        {
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                enumerator.GetDefaultAudioEndpoint(EDataFlow.Capture, ERole.Console, out var dev);
                dev.GetId(out string id);
                return id;
            }
            catch { return null; }
        }

        #endregion
    }
}
