using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace KHSave.SaveEditor.Services
{
    public class DesktopAppIdentity : IAppIdentity
    {
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private static readonly string _assemblyLocation = Path
            .Combine(AppContext.BaseDirectory, _assembly.ManifestModule.ToString())
                .Replace(".dll", ".exe");
        private static readonly FileVersionInfo _fvi = FileVersionInfo.GetVersionInfo(_assemblyLocation);

        public string Name => _fvi.ProductName;

        public string Version => _fvi.ProductVersion;
    }
}
