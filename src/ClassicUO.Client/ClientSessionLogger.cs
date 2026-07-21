#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ClassicUO
{
    /// <summary>
    /// Always-on login / character-load breadcrumb log under launcher install-root Logs\.
    /// Verbose until the first in-world GameScene is ready; then rare breadcrumbs only.
    /// Never logs passwords or account secrets.
    /// </summary>
    internal static class ClientSessionLogger
    {
        private static readonly object Sync = new object();
        private static readonly StringBuilder Buffer = new StringBuilder(4096);

        private static StreamWriter _writer;
        private static string _sessionFilePath;
        private static bool _started;
        private static bool _loginPhase = true;
        private static bool _firstChanceHooked;

        private static bool _loggedStatusPacket;
        private static bool _loggedSkillsPacket;
        private static bool _loggedEquipPacket;
        private static int _firstChanceLogged;
        private const int MaxFirstChanceLogs = 25;

        public static string SessionFilePath
        {
            get
            {
                lock (Sync)
                {
                    return _sessionFilePath;
                }
            }
        }

        public static bool IsLoginPhase
        {
            get
            {
                lock (Sync)
                {
                    return _loginPhase;
                }
            }
        }

        public static void Start()
        {
            lock (Sync)
            {
                if (_started)
                {
                    return;
                }

                try
                {
                    string logsDir = TryGetLauncherLogsDirectory();

                    if (string.IsNullOrEmpty(logsDir))
                    {
                        return;
                    }

                    Directory.CreateDirectory(logsDir);

                    string fileName = $"client-session-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                    _sessionFilePath = Path.Combine(logsDir, fileName);

                    _writer = new StreamWriter(
                        new FileStream(
                            _sessionFilePath,
                            FileMode.Append,
                            FileAccess.Write,
                            FileShare.ReadWrite,
                            4096,
                            FileOptions.SequentialScan
                        ),
                        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
                    )
                    {
                        AutoFlush = false
                    };

                    _started = true;
                    _loginPhase = true;
                    _loggedStatusPacket = false;
                    _loggedSkillsPacket = false;
                    _loggedEquipPacket = false;
                    _firstChanceLogged = 0;

                    AppendLineUnlocked("SESSION", "Client session log started (always-on login/character-load breadcrumbs)");
                    FlushUnlocked();

                    HookFirstChanceExceptionsUnlocked();
                }
                catch
                {
                    DisposeWriterUnlocked();
                    _started = false;
                }
            }
        }

        public static void LogClientStart()
        {
            string cuoVersion = CUOEnviroment.Version ?? "unknown";
            string marker = TryReadClientVersionMarker();
            string processPath = string.Empty;

            try
            {
                processPath = Environment.ProcessPath ?? CUOEnviroment.ExecutablePath ?? string.Empty;
            }
            catch
            {
                processPath = CUOEnviroment.ExecutablePath ?? string.Empty;
            }

            string cuoDllInfo = TryDescribeCuoDll();

            Stage(
                "ClientStart",
                $"cuo={cuoVersion}; marker={marker}; exe={processPath}; {cuoDllInfo}; os={Environment.OSVersion}; x64={Environment.Is64BitProcess}"
            );
        }

        public static void Stage(string stage, string details = null)
        {
            if (string.IsNullOrWhiteSpace(stage))
            {
                return;
            }

            lock (Sync)
            {
                if (!_started && !TryEnsureStartedUnlocked())
                {
                    return;
                }

                AppendLineUnlocked(stage, details);
                FlushUnlocked();
            }
        }

        /// <summary>
        /// Buffered line (no flush). Use for non-stage notes during login; flushed by next Stage/Flush.
        /// </summary>
        public static void Write(string stage, string details = null)
        {
            lock (Sync)
            {
                if (!_started && !TryEnsureStartedUnlocked())
                {
                    return;
                }

                if (!_loginPhase)
                {
                    return;
                }

                AppendLineUnlocked(stage, details);
            }
        }

        public static void Exception(string context, Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            lock (Sync)
            {
                if (!_started && !TryEnsureStartedUnlocked())
                {
                    return;
                }

                string detail = $"{context}: {ex.GetType().FullName}: {ex.Message}";
                AppendLineUnlocked("Exception", detail);

                try
                {
                    AppendLineUnlocked("ExceptionStack", ex.StackTrace);
                }
                catch
                {
                    // ignore
                }

                FlushUnlocked();
            }
        }

        public static void ExceptionObject(string context, object exceptionObject)
        {
            if (exceptionObject is Exception ex)
            {
                Exception(context, ex);

                return;
            }

            Stage("Exception", $"{context}: {exceptionObject}");
        }

        public static void Flush()
        {
            lock (Sync)
            {
                FlushUnlocked();
            }
        }

        public static void MarkWorldReady(string details = null)
        {
            lock (Sync)
            {
                if (!_started && !TryEnsureStartedUnlocked())
                {
                    return;
                }

                AppendLineUnlocked("InWorldReady", details ?? "Game scene loaded; throttling session log to rare breadcrumbs");
                _loginPhase = false;
                UnhookFirstChanceExceptionsUnlocked();
                FlushUnlocked();
            }
        }

        /// <summary>
        /// Post-login rare breadcrumb (always flushed). Ignored spam should not use this.
        /// </summary>
        public static void Breadcrumb(string stage, string details = null)
        {
            Stage(stage, details);
        }

        public static void LogStatusPacketOnce(uint serial, string name)
        {
            lock (Sync)
            {
                if (!_loginPhase || _loggedStatusPacket)
                {
                    return;
                }

                _loggedStatusPacket = true;
            }

            Stage("StatusPacket", $"serial=0x{serial:X8}; name={SanitizeName(name)}");
        }

        public static void LogSkillsPacketOnce(byte type)
        {
            lock (Sync)
            {
                if (!_loginPhase || _loggedSkillsPacket)
                {
                    return;
                }

                _loggedSkillsPacket = true;
            }

            Stage("SkillsPacket", $"type=0x{type:X2}");
        }

        public static void LogEquipPacketOnce(uint serial, ushort graphic, byte layer)
        {
            lock (Sync)
            {
                if (!_loginPhase || _loggedEquipPacket)
                {
                    return;
                }

                _loggedEquipPacket = true;
            }

            Stage("EquipmentPacket", $"serial=0x{serial:X8}; graphic=0x{graphic:X4}; layer={layer}");
        }

        public static void Shutdown()
        {
            lock (Sync)
            {
                if (!_started)
                {
                    return;
                }

                try
                {
                    AppendLineUnlocked("SessionEnd", "Client session logger shutting down");
                    FlushUnlocked();
                }
                catch
                {
                    // ignore
                }

                UnhookFirstChanceExceptionsUnlocked();
                DisposeWriterUnlocked();
                _started = false;
            }
        }

        public static string TryResolveLauncherInstallRoot()
        {
            try
            {
                string clientDir = CUOEnviroment.ExecutablePath;

                if (string.IsNullOrWhiteSpace(clientDir))
                {
                    clientDir = AppContext.BaseDirectory;
                }

                if (string.IsNullOrWhiteSpace(clientDir))
                {
                    clientDir = Environment.CurrentDirectory;
                }

                DirectoryInfo dir = new DirectoryInfo(
                    clientDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                );

                if (dir.Name.Equals("Client", StringComparison.OrdinalIgnoreCase) && dir.Parent != null)
                {
                    return dir.Parent.FullName;
                }

                for (DirectoryInfo walk = dir; walk != null; walk = walk.Parent)
                {
                    if (
                        Directory.Exists(Path.Combine(walk.FullName, "Client"))
                        && (
                            File.Exists(Path.Combine(walk.FullName, "0nE UO Launcher.exe"))
                            || File.Exists(Path.Combine(walk.FullName, "UODreams Launcher.exe"))
                            || File.Exists(Path.Combine(walk.FullName, "launcher.settings.json"))
                        )
                    )
                    {
                        return walk.FullName;
                    }

                    if (walk.Name.Equals("Client", StringComparison.OrdinalIgnoreCase) && walk.Parent != null)
                    {
                        return walk.Parent.FullName;
                    }
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        public static string TryGetLauncherLogsDirectory()
        {
            string root = TryResolveLauncherInstallRoot();

            if (string.IsNullOrEmpty(root))
            {
                return null;
            }

            return Path.Combine(root, "Logs");
        }

        private static bool TryEnsureStartedUnlocked()
        {
            if (_started)
            {
                return true;
            }

            // Nested start without re-entering lock: Start() takes the lock, so call inline open.
            try
            {
                string logsDir = TryGetLauncherLogsDirectory();

                if (string.IsNullOrEmpty(logsDir))
                {
                    return false;
                }

                Directory.CreateDirectory(logsDir);

                string fileName = $"client-session-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                _sessionFilePath = Path.Combine(logsDir, fileName);

                _writer = new StreamWriter(
                    new FileStream(
                        _sessionFilePath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.ReadWrite,
                        4096,
                        FileOptions.SequentialScan
                    ),
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
                )
                {
                    AutoFlush = false
                };

                _started = true;
                AppendLineUnlocked("SESSION", "Client session log started (lazy)");
                FlushUnlocked();
                HookFirstChanceExceptionsUnlocked();

                return true;
            }
            catch
            {
                DisposeWriterUnlocked();
                _started = false;

                return false;
            }
        }

        private static void AppendLineUnlocked(string stage, string details)
        {
            try
            {
                string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string line = string.IsNullOrEmpty(details)
                    ? $"[{ts}] [{stage}]"
                    : $"[{ts}] [{stage}] {details}";

                Buffer.AppendLine(line);
            }
            catch
            {
                // ignore
            }
        }

        private static void FlushUnlocked()
        {
            if (_writer == null)
            {
                return;
            }

            try
            {
                if (Buffer.Length > 0)
                {
                    _writer.Write(Buffer.ToString());
                    Buffer.Clear();
                }

                _writer.Flush();
            }
            catch
            {
                // ignore
            }
        }

        private static void DisposeWriterUnlocked()
        {
            try
            {
                _writer?.Dispose();
            }
            catch
            {
                // ignore
            }

            _writer = null;
        }

        private static void HookFirstChanceExceptionsUnlocked()
        {
            if (_firstChanceHooked)
            {
                return;
            }

            try
            {
                AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
                _firstChanceHooked = true;
            }
            catch
            {
                _firstChanceHooked = false;
            }
        }

        private static void UnhookFirstChanceExceptionsUnlocked()
        {
            if (!_firstChanceHooked)
            {
                return;
            }

            try
            {
                AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
            }
            catch
            {
                // ignore
            }

            _firstChanceHooked = false;
        }

        private static void OnFirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e?.Exception == null)
            {
                return;
            }

            // Avoid recursion / noise from logging IO itself.
            if (e.Exception is IOException || e.Exception is ObjectDisposedException)
            {
                return;
            }

            bool shouldLog = false;

            lock (Sync)
            {
                if (!_started || !_loginPhase || _firstChanceLogged >= MaxFirstChanceLogs)
                {
                    return;
                }

                _firstChanceLogged++;
                shouldLog = true;

                AppendLineUnlocked(
                    "FirstChanceException",
                    $"#{_firstChanceLogged} {e.Exception.GetType().FullName}: {e.Exception.Message}"
                );
                FlushUnlocked();
            }

            if (!shouldLog)
            {
                // keep analyzer happy; logging done under lock
            }
        }

        private static string TryReadClientVersionMarker()
        {
            try
            {
                string path = Path.Combine(CUOEnviroment.ExecutablePath ?? string.Empty, "uodreams-client.version");

                if (File.Exists(path))
                {
                    return File.ReadAllText(path).Trim();
                }
            }
            catch
            {
                // ignore
            }

            return "n/a";
        }

        private static string TryDescribeCuoDll()
        {
            try
            {
                string dir = CUOEnviroment.ExecutablePath ?? AppContext.BaseDirectory;
                string dllPath = Path.Combine(dir, "cuo.dll");

                if (!File.Exists(dllPath))
                {
                    return "cuo.dll=missing";
                }

                FileInfo fi = new FileInfo(dllPath);
                string ver = "n/a";

                try
                {
                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(dllPath);
                    ver = fvi.FileVersion ?? fvi.ProductVersion ?? "n/a";
                }
                catch
                {
                    // native AOT may not expose managed version info
                }

                return $"cuo.dll={fi.Length}bytes; fileVer={ver}; mtime={fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
            }
            catch (Exception ex)
            {
                return $"cuo.dll=error:{ex.GetType().Name}";
            }
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            // Strip control chars; never treat as secret but keep log readable.
            char[] chars = name.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsControl(chars[i]))
                {
                    chars[i] = '?';
                }
            }

            return new string(chars);
        }
    }
}
