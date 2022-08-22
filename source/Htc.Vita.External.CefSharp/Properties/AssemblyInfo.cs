using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CefSharp;

[assembly: AssemblyTitle("Htc.Vita.External.CefSharp")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

/* Use the value from SharedAssemblyInfo.cs
[assembly: AssemblyCompany(AssemblyInfo.AssemblyCompany)]
[assembly: AssemblyProduct(AssemblyInfo.AssemblyProduct)]
[assembly: AssemblyCopyright(AssemblyInfo.AssemblyCopyright)]
*/
[assembly: ComVisible(AssemblyInfo.ComVisible)]
/* Use the value from SharedAssemblyInfo.cs
[assembly: AssemblyVersion(AssemblyInfo.AssemblyVersion)]
[assembly: AssemblyFileVersion(AssemblyInfo.AssemblyFileVersion)]
*/
[assembly: CLSCompliant(AssemblyInfo.ClsCompliant)]

[assembly: InternalsVisibleTo(AssemblyInfo.CefSharpCoreProject)]
[assembly: InternalsVisibleTo(AssemblyInfo.CefSharpBrowserSubprocessProject)]
[assembly: InternalsVisibleTo(AssemblyInfo.CefSharpBrowserSubprocessCoreProject)]
[assembly: InternalsVisibleTo(AssemblyInfo.CefSharpWpfProject)]
[assembly: InternalsVisibleTo(AssemblyInfo.CefSharpWinFormsProject)]
[assembly: InternalsVisibleTo(AssemblyInfo.CefSharpOffScreenProject)]
[assembly: InternalsVisibleTo(AssemblyInfo.CefSharpTestProject)]

namespace CefSharp
{
    /// <exclude />
    public static class AssemblyInfo
    {
        public const bool ClsCompliant = false;
        public const bool ComVisible = false;
        public const string AssemblyCompany = "The CefSharp Authors";
        public const string AssemblyProduct = "Htc.Vita.External.CefSharp";
        public const string AssemblyVersion = "83.4.2";
        public const string AssemblyFileVersion = "83.4.2.0";
        public const string AssemblyCopyright = "Copyright © 2020 The CefSharp Authors";
        /*
        public const string CefSharpCoreProject = "CefSharp.Core, PublicKey=" + PublicKey;
        public const string CefSharpBrowserSubprocessProject = "CefSharp.BrowserSubprocess, PublicKey=" + PublicKey;
        public const string CefSharpBrowserSubprocessCoreProject = "CefSharp.BrowserSubprocess.Core, PublicKey=" + PublicKey;
        public const string CefSharpWpfProject = "CefSharp.Wpf, PublicKey=" + PublicKey;
        public const string CefSharpWinFormsProject = "CefSharp.WinForms, PublicKey=" + PublicKey;
        public const string CefSharpOffScreenProject = "CefSharp.OffScreen, PublicKey=" + PublicKey;
        public const string CefSharpTestProject = "CefSharp.Test, PublicKey=" + PublicKey;
        */
        public const string CefSharpCoreProject = "Htc.Vita.External.CefSharp.Core";
        public const string CefSharpBrowserSubprocessProject = "Htc.Vita.External.CefSharp.BrowserSubprocess";
        public const string CefSharpBrowserSubprocessCoreProject = "Htc.Vita.External.CefSharp.BrowserSubprocess.Core";
        public const string CefSharpWpfProject = "Htc.Vita.External.CefSharp.Wpf";
        public const string CefSharpWinFormsProject = "Htc.Vita.External.CefSharp.WinForms";
        public const string CefSharpOffScreenProject = "Htc.Vita.External.CefSharp.OffScreen";
        public const string CefSharpTestProject = "Htc.Vita.External.CefSharp.Test";

        // Use "%ProgramFiles%\Microsoft SDKs\Windows\v7.0A\bin\sn.exe" -Tp <assemblyname> to get PublicKey
        public const string PublicKey = "0024000004800000940000000602000000240000525341310004000001000100c5ddf5d063ca8e695d4b8b5ad76634f148db9a41badaed8850868b75f916e313f15abb62601d658ce2bed877d73314d5ed202019156c21033983fed80ce994a325b5d4d93b0f63a86a1d7db49800aa5638bb3fd98f4a33cceaf8b8ba1800b7d7bff67b72b90837271491b61c91ef6d667be0521ce171f77e114fc2bbcfd185d3";
    }
}
