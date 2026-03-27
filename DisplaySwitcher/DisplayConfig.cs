using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DisplaySwitcher
{
    /// <summary>
    /// Wraps the Windows SetDisplayConfig / QueryDisplayConfig API.
    /// Used to enumerate monitors and apply display profiles.
    /// </summary>
    public static class DisplayConfig
    {
        #region Win32 Structs & Enums

        [Flags]
        public enum QueryDisplayFlags : uint
        {
            AllPaths = 0x00000001,
            OnlyActivePaths = 0x00000002,
            DatabaseCurrent = 0x00000004
        }

        [Flags]
        public enum DisplayConfigFlags : uint
        {
            None = 0,
            UseSuppliedDisplayConfig = 0x00000020,
            Apply = 0x00000080,
            NoOptimization = 0x00000100,
            SaveToDatabase = 0x00000200,
            AllowChanges = 0x00000400,
            PathPersistIfRequired = 0x00000800,
            ForceModeEnumeration = 0x00001000,
            AllowPathOrderChanges = 0x00002000,
            VirtualModeAware = 0x00008000
        }

        public enum DisplayConfigTopology : uint
        {
            Internal = 0x00000001,
            Clone = 0x00000002,
            Extend = 0x00000004,
            External = 0x00000008,
            ForceUint32 = 0xFFFFFFFF
        }

        public enum DisplayConfigVideoOutputTechnology : uint
        {
            Other = 0xFFFFFFFF,
            Hd15 = 0,
            Svideo = 1,
            CompositeVideo = 2,
            ComponentVideo = 3,
            Dvi = 4,
            Hdmi = 5,
            Lvds = 6,
            DJpn = 8,
            Sdi = 9,
            DisplayportExternal = 10,
            DisplayportEmbedded = 11,
            UdiExternal = 12,
            UdiEmbedded = 13,
            Sdtvdongle = 14,
            Miracast = 15,
            Internal = 0x80000000,
            ForceUint32 = 0xFFFFFFFF
        }

        public enum DisplayConfigScanlineOrdering : uint
        {
            Unspecified = 0,
            Progressive = 1,
            Interlaced = 2,
            InterlacedUpperfieldfirst = 2,
            InterlacedLowerfieldfirst = 3,
            ForceUint32 = 0xFFFFFFFF
        }

        public enum DisplayConfigRotation : uint
        {
            Identity = 1,
            Rotate90 = 2,
            Rotate180 = 3,
            Rotate270 = 4,
            ForceUint32 = 0xFFFFFFFF
        }

        public enum DisplayConfigScaling : uint
        {
            Identity = 1,
            Centered = 2,
            Stretched = 3,
            Aspectratiocenteredmax = 4,
            Custom = 5,
            Preferred = 128,
            ForceUint32 = 0xFFFFFFFF
        }

        public enum DisplayConfigPixelFormat : uint
        {
            Pixelformat8bpp = 1,
            Pixelformat16bpp = 2,
            Pixelformat24bpp = 3,
            Pixelformat32bpp = 4,
            PixelformatNongdi = 5,
            PixelformatForceUint32 = 0xFFFFFFFF
        }

        public enum DisplayConfigModeInfoType : uint
        {
            Zero = 0,
            Source = 1,
            Target = 2,
            DesktopImage = 3,
            ForceUint32 = 0xFFFFFFFF
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigPathSourceInfo
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigPathTargetInfo
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public DisplayConfigVideoOutputTechnology outputTechnology;
            public DisplayConfigRotation rotation;
            public DisplayConfigScaling scaling;
            public DisplayConfigRational refreshRate;
            public DisplayConfigScanlineOrdering scanLineOrdering;
            public bool targetAvailable;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigRational
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigPathInfo
        {
            public DisplayConfigPathSourceInfo sourceInfo;
            public DisplayConfigPathTargetInfo targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfig2DRegion
        {
            public uint cx;
            public uint cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigVideoSignalInfo
        {
            public ulong pixelRate;
            public DisplayConfigRational hSyncFreq;
            public DisplayConfigRational vSyncFreq;
            public DisplayConfig2DRegion activeSize;
            public DisplayConfig2DRegion totalSize;
            public uint videoStandard;
            public DisplayConfigScanlineOrdering scanLineOrdering;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigTargetMode
        {
            public DisplayConfigVideoSignalInfo targetVideoSignalInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PointL
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigSourceMode
        {
            public uint width;
            public uint height;
            public DisplayConfigPixelFormat pixelFormat;
            public PointL position;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigModeInfo
        {
            public DisplayConfigModeInfoType infoType;
            public uint id;
            public LUID adapterId;
            public DisplayConfigUnion modeInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DisplayConfigUnion
        {
            [FieldOffset(0)]
            public DisplayConfigTargetMode targetMode;
            [FieldOffset(0)]
            public DisplayConfigSourceMode sourceMode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DisplayConfigDeviceInfoHeader
        {
            public DisplayConfigDeviceInfoType type;
            public uint size;
            public LUID adapterId;
            public uint id;
        }

        public enum DisplayConfigDeviceInfoType : uint
        {
            GetSourceName = 1,
            GetTargetName = 2,
            GetTargetPreferredMode = 3,
            GetAdapterName = 4,
            SetTargetPersistence = 5,
            GetTargetBaseType = 6,
            GetSupportVirtualResolution = 7,
            ForceUint32 = 0xFFFFFFFF
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DisplayConfigTargetDeviceNameFlags
        {
            public uint value;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DisplayConfigTargetDeviceName
        {
            public DisplayConfigDeviceInfoHeader header;
            public DisplayConfigTargetDeviceNameFlags flags;
            public DisplayConfigVideoOutputTechnology outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string monitorFriendlyDeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string monitorDevicePath;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DisplayConfigSourceDeviceName
        {
            public DisplayConfigDeviceInfoHeader header;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string viewGdiDeviceName;
        }

        #endregion

        #region P/Invoke

        [DllImport("user32.dll")]
        public static extern int QueryDisplayConfig(
            QueryDisplayFlags flags,
            ref uint numPathArrayElements,
            [Out] DisplayConfigPathInfo[] pathInfoArray,
            ref uint numModeInfoArrayElements,
            [Out] DisplayConfigModeInfo[] modeInfoArray,
            IntPtr currentTopologyId
        );

        [DllImport("user32.dll")]
        public static extern int SetDisplayConfig(
            uint numPathArrayElements,
            [In] DisplayConfigPathInfo[] pathInfoArray,
            uint numModeInfoArrayElements,
            [In] DisplayConfigModeInfo[] modeInfoArray,
            DisplayConfigFlags flags
        );

        [DllImport("user32.dll")]
        public static extern int GetDisplayConfigBufferSizes(
            QueryDisplayFlags flags,
            out uint numPathArrayElements,
            out uint numModeInfoArrayElements
        );

        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(
            ref DisplayConfigTargetDeviceName deviceName
        );

        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(
            ref DisplayConfigSourceDeviceName sourceName
        );

        #endregion

        #region Public API

        /// <summary>
        /// Returns all currently connected monitors (active + inactive).
        /// </summary>
        public static List<MonitorInfo> GetAllMonitors()
        {
            var result = new List<MonitorInfo>();

            // Query ALL paths (not just active) to find inactive monitors too
            int err = GetDisplayConfigBufferSizes(QueryDisplayFlags.AllPaths,
                out uint numPaths, out uint numModes);
            if (err != 0) return result;

            var paths = new DisplayConfigPathInfo[numPaths];
            var modes = new DisplayConfigModeInfo[numModes];

            err = QueryDisplayConfig(QueryDisplayFlags.AllPaths,
                ref numPaths, paths, ref numModes, modes, IntPtr.Zero);
            if (err != 0) return result;

            var seen = new HashSet<string>();

            for (int i = 0; i < numPaths; i++)
            {
                var path = paths[i];

                // Get target (monitor) name
                var targetName = new DisplayConfigTargetDeviceName();
                targetName.header.size = (uint)Marshal.SizeOf<DisplayConfigTargetDeviceName>();
                targetName.header.adapterId = path.targetInfo.adapterId;
                targetName.header.id = path.targetInfo.id;
                targetName.header.type = DisplayConfigDeviceInfoType.GetTargetName;
                DisplayConfigGetDeviceInfo(ref targetName);

                // Get source (GDI device name like \\.\DISPLAY1)
                var sourceName = new DisplayConfigSourceDeviceName();
                sourceName.header.size = (uint)Marshal.SizeOf<DisplayConfigSourceDeviceName>();
                sourceName.header.adapterId = path.sourceInfo.adapterId;
                sourceName.header.id = path.sourceInfo.id;
                sourceName.header.type = DisplayConfigDeviceInfoType.GetSourceName;
                DisplayConfigGetDeviceInfo(ref sourceName);

                string devicePath = targetName.monitorDevicePath;
                if (string.IsNullOrEmpty(devicePath)) continue;
                if (seen.Contains(devicePath)) continue;
                seen.Add(devicePath);

                bool isActive = (path.flags & 0x1) != 0; // DISPLAYCONFIG_PATH_ACTIVE

                string friendlyName = targetName.monitorFriendlyDeviceName;
                if (string.IsNullOrWhiteSpace(friendlyName))
                    friendlyName = $"Monitor ({path.targetInfo.outputTechnology})";

                var mon = new MonitorInfo
                {
                    FriendlyName = friendlyName,
                    DevicePath = devicePath,
                    GdiDeviceName = sourceName.viewGdiDeviceName,
                    AdapterId = path.targetInfo.adapterId,
                    TargetId = path.targetInfo.id,
                    SourceId = path.sourceInfo.id,
                    IsActive = isActive,
                    PathIndex = i
                };

                // Populate resolution / position / refresh rate for active monitors
                if (isActive)
                {
                    uint srcIdx = path.sourceInfo.modeInfoIdx;
                    if (srcIdx != 0xFFFFFFFF && srcIdx < numModes &&
                        modes[srcIdx].infoType == DisplayConfigModeInfoType.Source)
                    {
                        var sm = modes[srcIdx].modeInfo.sourceMode;
                        mon.Width = sm.width;
                        mon.Height = sm.height;
                        mon.PositionX = sm.position.x;
                        mon.PositionY = sm.position.y;
                        mon.IsPrimary = sm.position.x == 0 && sm.position.y == 0;
                        mon.PixelFormat = (uint)sm.pixelFormat;
                    }

                    uint tgtIdx = path.targetInfo.modeInfoIdx;
                    if (tgtIdx != 0xFFFFFFFF && tgtIdx < numModes &&
                        modes[tgtIdx].infoType == DisplayConfigModeInfoType.Target)
                    {
                        var sig = modes[tgtIdx].modeInfo.targetMode.targetVideoSignalInfo;
                        mon.PixelRate = sig.pixelRate;
                        mon.HSyncFreqN = sig.hSyncFreq.Numerator;
                        mon.HSyncFreqD = sig.hSyncFreq.Denominator;
                        mon.VSyncFreqN = sig.vSyncFreq.Numerator;
                        mon.VSyncFreqD = sig.vSyncFreq.Denominator;
                        mon.ActiveWidth = sig.activeSize.cx;
                        mon.ActiveHeight = sig.activeSize.cy;
                        mon.TotalWidth = sig.totalSize.cx;
                        mon.TotalHeight = sig.totalSize.cy;
                        mon.VideoStandard = sig.videoStandard;
                        mon.ScanLineOrdering = (uint)sig.scanLineOrdering;
                    }
                }

                result.Add(mon);
            }

            return result;
        }

        /// <summary>
        /// Applies a saved DisplayProfile by re-enabling/disabling monitors.
        /// Uses SetDisplayConfig to rebuild the path array.
        /// </summary>
        public static bool ApplyProfile(DisplayProfile profile)
        {
            var logPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DisplaySwitcher", "display_log.txt");
            var log = new System.Text.StringBuilder();
            log.AppendLine($"=== ApplyProfile '{profile.Name}' @ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

            try
            {
                log.AppendLine($"Profil enthält {profile.Monitors.Count} Monitor(e):");
                foreach (var m in profile.Monitors)
                    log.AppendLine($"  - {m.FriendlyName} | {m.Width}x{m.Height} | {m.DevicePath}");

                var profileMonByPath = profile.Monitors
                    .ToDictionary(m => m.DevicePath, m => m, StringComparer.OrdinalIgnoreCase);

                // ── STEP 1: Activate/deactivate monitors (no explicit modes) ──────────
                // AllPaths inactive entries have undefined/colliding sourceInfo.id values.
                // Setting modeInfoIdx=INVALID lets Windows assign valid source IDs itself.

                int err = GetDisplayConfigBufferSizes(QueryDisplayFlags.AllPaths,
                    out uint numPaths, out uint numModes);
                if (err != 0) { log.AppendLine($"Step1 GetBufferSizes err={err}"); return false; }

                var paths = new DisplayConfigPathInfo[numPaths];
                var modes = new DisplayConfigModeInfo[numModes];
                err = QueryDisplayConfig(QueryDisplayFlags.AllPaths,
                    ref numPaths, paths, ref numModes, modes, IntPtr.Zero);
                if (err != 0) { log.AppendLine($"Step1 QueryDisplayConfig err={err}"); return false; }

                var alreadyMatched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                // Track (adapterLow, adapterHigh, sourceId) tuples to avoid activating
                // two paths that share the same source – that would produce clone/duplicate mode.
                var usedSources = new HashSet<(uint, int, uint)>();

                for (int i = 0; i < numPaths; i++)
                {
                    var tn = new DisplayConfigTargetDeviceName();
                    tn.header.size = (uint)Marshal.SizeOf<DisplayConfigTargetDeviceName>();
                    tn.header.adapterId = paths[i].targetInfo.adapterId;
                    tn.header.id = paths[i].targetInfo.id;
                    tn.header.type = DisplayConfigDeviceInfoType.GetTargetName;
                    DisplayConfigGetDeviceInfo(ref tn);

                    string dp = tn.monitorDevicePath ?? string.Empty;
                    bool wantedTarget = profileMonByPath.ContainsKey(dp) && !alreadyMatched.Contains(dp);

                    var srcKey = (paths[i].sourceInfo.adapterId.LowPart,
                                  paths[i].sourceInfo.adapterId.HighPart,
                                  paths[i].sourceInfo.id);
                    bool sourceAvailable = !usedSources.Contains(srcKey);

                    if (wantedTarget && sourceAvailable)
                    {
                        paths[i].flags |= 1u;
                        paths[i].sourceInfo.modeInfoIdx = 0xFFFFFFFF; // let Windows pick
                        paths[i].targetInfo.modeInfoIdx = 0xFFFFFFFF;
                        alreadyMatched.Add(dp);
                        usedSources.Add(srcKey);
                        log.AppendLine($"  Step1 AKTIVIERT [{i:D2}] {tn.monitorFriendlyDeviceName}");
                    }
                    else
                    {
                        paths[i].flags &= ~1u;
                        paths[i].sourceInfo.modeInfoIdx = 0xFFFFFFFF;
                        paths[i].targetInfo.modeInfoIdx = 0xFFFFFFFF;
                        if (wantedTarget)
                            log.AppendLine($"  Step1 SKIP (source conflict) [{i:D2}] {tn.monitorFriendlyDeviceName}");
                    }
                }

                var flags1 = DisplayConfigFlags.UseSuppliedDisplayConfig |
                             DisplayConfigFlags.Apply |
                             DisplayConfigFlags.SaveToDatabase |
                             DisplayConfigFlags.AllowChanges;

                err = SetDisplayConfig(numPaths, paths, numModes, modes, flags1);
                log.AppendLine($"Step1 (aktiviere Monitore): err={err}");
                if (err != 0)
                {
                    log.AppendLine($"ERGEBNIS: Fehler bei Step1! Win32-Code={err}");
                    return false;
                }

                // ── STEP 2: Apply resolution / position / refresh rate ─────────────
                // Now the monitors are active. Query ACTIVE paths to get real source IDs,
                // then replace mode content with saved values.

                bool hasModeData = profile.Monitors.Any(m => m.Width > 0);
                if (!hasModeData)
                {
                    log.AppendLine("Keine Modusdaten im Profil – Step2 übersprungen.");
                    log.AppendLine("ERGEBNIS: Erfolg.");
                    return true;
                }

                err = GetDisplayConfigBufferSizes(QueryDisplayFlags.OnlyActivePaths,
                    out uint numActive, out uint numActiveModes);
                if (err != 0) { log.AppendLine($"Step2 GetBufferSizes err={err} (Monitore sind trotzdem aktiv)."); log.AppendLine("ERGEBNIS: Teilweise (nur Step1)."); return true; }

                var activePaths = new DisplayConfigPathInfo[numActive];
                var activeModes = new DisplayConfigModeInfo[numActiveModes];
                err = QueryDisplayConfig(QueryDisplayFlags.OnlyActivePaths,
                    ref numActive, activePaths, ref numActiveModes, activeModes, IntPtr.Zero);
                if (err != 0) { log.AppendLine($"Step2 QueryDisplayConfig err={err}"); log.AppendLine("ERGEBNIS: Teilweise (nur Step1)."); return true; }

                // Clone the active modes array; we'll replace entries for our monitors
                var newModes = activeModes.Take((int)numActiveModes).ToArray();

                for (int i = 0; i < numActive; i++)
                {
                    var tn = new DisplayConfigTargetDeviceName();
                    tn.header.size = (uint)Marshal.SizeOf<DisplayConfigTargetDeviceName>();
                    tn.header.adapterId = activePaths[i].targetInfo.adapterId;
                    tn.header.id = activePaths[i].targetInfo.id;
                    tn.header.type = DisplayConfigDeviceInfoType.GetTargetName;
                    DisplayConfigGetDeviceInfo(ref tn);

                    string dp = tn.monitorDevicePath ?? string.Empty;
                    if (!profileMonByPath.TryGetValue(dp, out var profMon) || profMon.Width == 0) continue;

                    int hz = profMon.VSyncFreqD > 0 ? (int)Math.Round((double)profMon.VSyncFreqN / profMon.VSyncFreqD) : 0;
                    log.AppendLine($"  Step2 [{i:D2}] {tn.monitorFriendlyDeviceName,-28} | {profMon.Width}x{profMon.Height} @{hz}Hz pos=({profMon.PositionX},{profMon.PositionY})");

                    // Replace source mode (resolution + position)
                    uint srcIdx = activePaths[i].sourceInfo.modeInfoIdx;
                    if (srcIdx != 0xFFFFFFFF && srcIdx < numActiveModes &&
                        newModes[srcIdx].infoType == DisplayConfigModeInfoType.Source)
                    {
                        newModes[srcIdx] = new DisplayConfigModeInfo
                        {
                            infoType = DisplayConfigModeInfoType.Source,
                            id = newModes[srcIdx].id,
                            adapterId = newModes[srcIdx].adapterId,
                            modeInfo = new DisplayConfigUnion
                            {
                                sourceMode = new DisplayConfigSourceMode
                                {
                                    width = profMon.Width,
                                    height = profMon.Height,
                                    pixelFormat = (DisplayConfigPixelFormat)(profMon.PixelFormat > 0 ? profMon.PixelFormat : 4u),
                                    position = new PointL { x = profMon.PositionX, y = profMon.PositionY }
                                }
                            }
                        };
                    }

                    // Replace target mode (refresh rate / signal info)
                    uint tgtIdx = activePaths[i].targetInfo.modeInfoIdx;
                    if (tgtIdx != 0xFFFFFFFF && tgtIdx < numActiveModes &&
                        newModes[tgtIdx].infoType == DisplayConfigModeInfoType.Target)
                    {
                        newModes[tgtIdx] = new DisplayConfigModeInfo
                        {
                            infoType = DisplayConfigModeInfoType.Target,
                            id = newModes[tgtIdx].id,
                            adapterId = newModes[tgtIdx].adapterId,
                            modeInfo = new DisplayConfigUnion
                            {
                                targetMode = new DisplayConfigTargetMode
                                {
                                    targetVideoSignalInfo = new DisplayConfigVideoSignalInfo
                                    {
                                        pixelRate = profMon.PixelRate,
                                        hSyncFreq = new DisplayConfigRational { Numerator = profMon.HSyncFreqN, Denominator = profMon.HSyncFreqD },
                                        vSyncFreq = new DisplayConfigRational { Numerator = profMon.VSyncFreqN, Denominator = profMon.VSyncFreqD },
                                        activeSize = new DisplayConfig2DRegion { cx = profMon.ActiveWidth, cy = profMon.ActiveHeight },
                                        totalSize = new DisplayConfig2DRegion { cx = profMon.TotalWidth, cy = profMon.TotalHeight },
                                        videoStandard = profMon.VideoStandard,
                                        scanLineOrdering = (DisplayConfigScanlineOrdering)profMon.ScanLineOrdering
                                    }
                                }
                            }
                        };
                    }
                }

                var flags2 = DisplayConfigFlags.UseSuppliedDisplayConfig |
                             DisplayConfigFlags.Apply |
                             DisplayConfigFlags.SaveToDatabase |
                             DisplayConfigFlags.AllowChanges;

                err = SetDisplayConfig(numActive, activePaths, (uint)newModes.Length, newModes, flags2);
                log.AppendLine($"Step2 (Modus anwenden): err={err}");
                log.AppendLine(err == 0 ? "ERGEBNIS: Erfolg." : $"ERGEBNIS: Fehler Step2! Win32-Code={err} (Monitore sind trotzdem aktiv)");

                return true; // monitors were activated in step 1 regardless
            }
            catch (Exception ex)
            {
                log.AppendLine($"AUSNAHME: {ex}");
                return false;
            }
            finally
            {
                try { System.IO.File.AppendAllText(logPath, log.ToString() + Environment.NewLine); } catch { }
            }
        }

        #endregion
    }

    public class MonitorInfo
    {
        public string FriendlyName { get; set; } = string.Empty;
        public string DevicePath { get; set; } = string.Empty;
        public string GdiDeviceName { get; set; } = string.Empty;
        public DisplayConfig.LUID AdapterId { get; set; }
        public uint TargetId { get; set; }
        public uint SourceId { get; set; }
        public bool IsActive { get; set; }
        public int PathIndex { get; set; }

        // Source mode – resolution & desktop arrangement
        public uint Width { get; set; }
        public uint Height { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public bool IsPrimary { get; set; }
        public uint PixelFormat { get; set; }

        // Target mode – refresh rate & signal info
        public ulong PixelRate { get; set; }
        public uint HSyncFreqN { get; set; }
        public uint HSyncFreqD { get; set; }
        public uint VSyncFreqN { get; set; }
        public uint VSyncFreqD { get; set; }
        public uint ActiveWidth { get; set; }
        public uint ActiveHeight { get; set; }
        public uint TotalWidth { get; set; }
        public uint TotalHeight { get; set; }
        public uint VideoStandard { get; set; }
        public uint ScanLineOrdering { get; set; }

        public override string ToString() => FriendlyName;
    }
}
