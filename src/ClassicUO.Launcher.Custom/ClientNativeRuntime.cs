using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.Launcher.Custom
{
    internal static class ClientNativeRuntime
    {
        private const string VcRedistUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe";

        private static readonly string[] RequiredNativeLibraries =
        {
            "zlib.dll",
            "FAudio.dll",
            "FNA3D.dll",
            "libtheorafile.dll"
        };

        public static string? Validate(string clientDir)
        {
            if (!Directory.Exists(clientDir))
            {
                return Loc.S(
                    "Cartella Client non trovata.",
                    "Client folder not found.");
            }

            string? sdlError = ValidateSdlLibrary(clientDir);
            if (sdlError != null)
            {
                return sdlError;
            }

            foreach (string lib in RequiredNativeLibraries)
            {
                if (!File.Exists(Path.Combine(clientDir, lib)))
                {
                    return Loc.S(
                        $"Manca {lib} nella cartella Client.\n" +
                        "Reinstalla il client dal launcher (Aggiorna) o ripristina la cartella Client.",
                        $"Missing {lib} in the Client folder.\n" +
                        "Reinstall the client from the launcher (Update) or restore the Client folder.");
                }
            }

            if (!IsVcRuntimeInstalled())
            {
                return Loc.S(
                    "Manca Microsoft Visual C++ Redistributable (x64).\n\n" +
                    $"Scaricalo da:\n{VcRedistUrl}\n\n" +
                    "Su una VM Windows nuova (es. Parallels) è spesso necessario prima di avviare ClassicUO.",
                    "Microsoft Visual C++ Redistributable (x64) is not installed.\n\n" +
                    $"Download it from:\n{VcRedistUrl}\n\n" +
                    "Fresh Windows VMs (e.g. Parallels) often need this before ClassicUO can start.");
            }

            string? loadError = ProbeNativeLibraries(clientDir);
            if (loadError != null)
            {
                return loadError;
            }

            return null;
        }

        private static string? ValidateSdlLibrary(string clientDir)
        {
            bool hasSdl2 = File.Exists(Path.Combine(clientDir, "SDL2.dll"));
            bool hasSdl3 = File.Exists(Path.Combine(clientDir, "SDL3.dll"));

            if (hasSdl2 || hasSdl3)
            {
                return null;
            }

            return Loc.S(
                "Manca SDL2.dll o SDL3.dll nella cartella Client.\n" +
                "Reinstalla il client dal launcher (Aggiorna) o ripristina la cartella Client.",
                "Missing SDL2.dll or SDL3.dll in the Client folder.\n" +
                "Reinstall the client from the launcher (Update) or restore the Client folder.");
        }

        private static string? ResolveSdlLibrary(string clientDir)
        {
            if (File.Exists(Path.Combine(clientDir, "SDL3.dll")))
            {
                return "SDL3.dll";
            }

            if (File.Exists(Path.Combine(clientDir, "SDL2.dll")))
            {
                return "SDL2.dll";
            }

            return null;
        }

        private static bool IsVcRuntimeInstalled()
        {
            string systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
            return File.Exists(Path.Combine(systemDir, "vcruntime140.dll"));
        }

        private static string? ProbeNativeLibraries(string clientDir)
        {
            string? sdlLib = ResolveSdlLibrary(clientDir);
            if (sdlLib == null)
            {
                return null;
            }

            foreach (string lib in new[] { "zlib.dll", sdlLib, "FAudio.dll", "FNA3D.dll" })
            {
                string path = Path.Combine(clientDir, lib);
                if (!File.Exists(path))
                {
                    continue;
                }

                if (NativeLibrary.TryLoad(path, typeof(ClientNativeRuntime).Assembly, null, out IntPtr handle))
                {
                    NativeLibrary.Free(handle);
                    continue;
                }

                int error = Marshal.GetLastWin32Error();
                return Loc.S(
                    $"Impossibile caricare {lib} (errore Windows {error}).\n\n" +
                    "Di solito manca il runtime Visual C++ x64 oppure un file nativo del client è danneggiato.\n\n" +
                    $"Installa: {VcRedistUrl}",
                    $"Unable to load {lib} (Windows error {error}).\n\n" +
                    "This usually means the x64 Visual C++ runtime is missing or a native client file is corrupt.\n\n" +
                    $"Install: {VcRedistUrl}");
            }

            return null;
        }
    }
}
