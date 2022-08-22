#addin "nuget:?package=Cake.Git&version=1.1.0"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var configuration = Argument("configuration", "Debug");
var revision = EnvironmentVariable("BUILD_NUMBER") ?? Argument("revision", "9999");
var target = Argument("target", "Default");


//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define git commit id
var commitId = "SNAPSHOT";

// Define product name and version
var product = "Htc.Vita.External.CefSharp";
var companyName = "HTC";
var version = "83.4.2";
var platformToolset = "v120";
var semanticVersion = $"{version}.{revision}";
var ciVersion = $"{version}.0";
var buildVersion = "Release".Equals(configuration) ? semanticVersion : $"{ciVersion}-CI{revision}";
var assemblyVersion = "Release".Equals(configuration) ? semanticVersion : ciVersion;
var nugetTags = new [] { "htc", "vita", "cef", "cefsharp" };
var projectUrl = "https://github.com/ViveportSoftware/vita_external_cefsharp_csharp";
var description = "HTC Vita External Libraries for .NET platform (CefSharp)";
var visualStudioVersionMap = new Dictionary<string, string>
{
        { "v120", "12.0" },
        { "v140", "14.0" },
        { "v141", "15.0" },
        { "v142", "16.0" },
        { "v143", "17.0" }
};
var msbuildSettings = new MSBuildSettings
{
        Configuration = configuration,
        MaxCpuCount = 0
};

// Define copyright
var copyright = $"Copyright © 2022 - {DateTime.Now.Year}";

// Define timestamp for signing
var lastSignTimestamp = DateTime.Now;
var signIntervalInMilli = 1000 * 5;

// Define path
var solutionFile = File($"./source/{product}.sln");

// Define directories.
var sourceDir = Directory("./source");
var distDir = Directory("./dist");
var tempDir = Directory("./temp");
var generatedDir = Directory("./source/generated");
var packagesDir = Directory("./source/packages");
var nugetDir = distDir + Directory(configuration) + Directory("nuget");

// Define signing key, password and timestamp server
var signKeyEnc = EnvironmentVariable("SIGNKEYENC") ?? "NOTSET";
var signPass = EnvironmentVariable("SIGNPASS") ?? "NOTSET";
var signSha1Uri = new Uri("http://timestamp.digicert.com");
var signSha256Uri = new Uri("http://timestamp.digicert.com");

// Define nuget push source and key
var nugetApiKey = EnvironmentVariable("NUGET_PUSH_TOKEN") ?? EnvironmentVariable("NUGET_APIKEY") ?? "NOTSET";
var nugetSource = EnvironmentVariable("NUGET_PUSH_PATH") ?? EnvironmentVariable("NUGET_SOURCE") ?? "NOTSET";


//////////////////////////////////////////////////////////////////////
// METHODS
//////////////////////////////////////////////////////////////////////

List<NuSpecContent> GetNuSpecContentList(ConvertableDirectoryPath basePath, string[] pathesToPack, string target)
{
    var nuSpecContentList = new List<NuSpecContent>();
    foreach(var pathToPack in pathesToPack)
    {
        var pathTokens = pathToPack.Split('/');
        var pathTokensLength = pathTokens.Length;
        FilePath fileToPack = null;
        for(var i = pathTokensLength - 1; i >= 0; i--)
        {
            if (fileToPack == null)
            {
                fileToPack = File(pathTokens[i]);
            }
            else
            {
                fileToPack = Directory(pathTokens[i]) + fileToPack;
            }
        }
        fileToPack = basePath + fileToPack;
        if (!FileExists(fileToPack))
        {
            Warning($"Binary {fileToPack} does not exist. Skipped.");
        }
        else
        {
            nuSpecContentList.Add(new NuSpecContent
            {
                    Source = pathToPack,
                    Target = target
            });
        }
    }

    return nuSpecContentList;
}

string GetVisualStudioVersion()
{
    if (visualStudioVersionMap.ContainsKey(platformToolset))
    {
        return visualStudioVersionMap[platformToolset];
    }
    return "v120";
}

void GZipFile(FilePath source, DirectoryPath destination)
{
    byte[] contents = System.IO.File.ReadAllBytes(source.FullPath);
    FilePath output = destination.CombineWithFilePath($"{source.GetFilename()}.gz");
    Information($"Compressing {source} to {output}");

    using (var gzipStream = new System.IO.Compression.GZipStream(
            System.IO.File.Create(output.FullPath),
            System.IO.Compression.CompressionLevel.Optimal))
    {
        gzipStream.Write(contents, 0, contents.Length);
    }
}


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Fetch-Git-Commit-ID")
    .ContinueOnError()
    .Does(() =>
{
    var lastCommit = GitLogTip(MakeAbsolute(Directory(".")));
    commitId = lastCommit.Sha;
});

Task("Display-Config")
    .IsDependentOn("Fetch-Git-Commit-ID")
    .Does(() =>
{
    Information($"Build target:        {target}");
    Information($"Build configuration: {configuration}");
    Information($"Build commitId:      {commitId}");
    Information($"Build version:       {buildVersion}");
});

Task("Clean-Workspace")
    .IsDependentOn("Display-Config")
    .Does(() =>
{
    CleanDirectory(distDir);
    CleanDirectory(tempDir);
    CleanDirectory(generatedDir);
    CleanDirectory(packagesDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean-Workspace")
    .Does(() =>
{
    NuGetRestore(new FilePath($"./source/{product}.sln"));
});

Task("Generate-AssemblyInfo")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    CreateDirectory(generatedDir);
    CreateAssemblyInfo(
            new FilePath("./source/generated/SharedAssemblyInfo.cs"),
            new AssemblyInfoSettings
            {
                    Company = $"The CefSharp Authors, {companyName}",
                    Copyright = copyright,
                    FileVersion = assemblyVersion,
                    InformationalVersion = assemblyVersion,
                    Product = $"{product} : {commitId}",
                    Version = version
            }
    );
});

Task("Build-Assemblies-Win32")
    .IsDependentOn("Generate-AssemblyInfo")
    .Does(() =>
{
    MSBuild(
            solutionFile,
            msbuildSettings
                    .SetPlatformTarget(PlatformTarget.Win32)
                    .WithProperty("VisualStudioVersion", GetVisualStudioVersion())
    );
});

Task("Build-Assemblies-x64")
    .IsDependentOn("Build-Assemblies-Win32")
    .Does(() =>
{
    MSBuild(
            solutionFile,
            msbuildSettings
                    .SetPlatformTarget(PlatformTarget.x64)
                    .WithProperty("VisualStudioVersion", GetVisualStudioVersion())
    );
});

Task("Sign-Assemblies")
    .WithCriteria(() => "Release".Equals(configuration) && !"NOTSET".Equals(signPass) && !"NOTSET".Equals(signKeyEnc))
    .IsDependentOn("Build-Assemblies-x64")
    .Does(() =>
{
    var signKey = "./temp/key.pfx";
    System.IO.File.WriteAllBytes(
            signKey,
            Convert.FromBase64String(signKeyEnc)
    );

    var targetPlatforms = new []
    {
            "Win32",
            "x64"
    };
    foreach (var targetPlatform in targetPlatforms)
    {
        System.Threading.Thread.Sleep(signIntervalInMilli);
        Sign(
                $"./temp/{configuration}/CefSharp.BrowserSubprocess.Core/bin/{targetPlatform}/Htc.Vita.External.CefSharp.BrowserSubprocess.Core.dll",
                new SignToolSignSettings
                {
                        AppendSignature = true,
                        CertPath = signKey,
                        DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                        Password = signPass,
                        TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                        TimeStampUri = signSha256Uri
                }
        );

        System.Threading.Thread.Sleep(signIntervalInMilli);
        Sign(
                $"./temp/{configuration}/Htc.Vita.External.CefSharp.Core/bin/{targetPlatform}/Htc.Vita.External.CefSharp.Core.dll",
                new SignToolSignSettings
                {
                        AppendSignature = true,
                        CertPath = signKey,
                        DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                        Password = signPass,
                        TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                        TimeStampUri = signSha256Uri
                }
        );
    }

    targetPlatforms = new []
    {
            "x64",
            "x86"
    };
    foreach (var targetPlatform in targetPlatforms)
    {
        System.Threading.Thread.Sleep(signIntervalInMilli);
        Sign(
                $"./temp/{configuration}/Htc.Vita.External.CefSharp.BrowserSubprocess/bin/{targetPlatform}/Htc.Vita.External.CefSharp.BrowserSubprocess.exe",
                new SignToolSignSettings
                {
                        AppendSignature = true,
                        CertPath = signKey,
                        DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                        Password = signPass,
                        TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                        TimeStampUri = signSha256Uri
                }
        );

        System.Threading.Thread.Sleep(signIntervalInMilli);
        Sign(
                $"./temp/{configuration}/Htc.Vita.External.CefSharp/bin/{targetPlatform}/Htc.Vita.External.CefSharp.dll",
                new SignToolSignSettings
                {
                        AppendSignature = true,
                        CertPath = signKey,
                        DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                        Password = signPass,
                        TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                        TimeStampUri = signSha256Uri
                }
        );
    }

    var features = new []
    {
            "OffScreen",
            "WinForms",
            "Wpf"
    };
    foreach (var targetPlatform in targetPlatforms)
    {
        foreach (var feature in features)
        {
            System.Threading.Thread.Sleep(signIntervalInMilli);
            Sign(
                    $"./temp/{configuration}/Htc.Vita.External.CefSharp.{feature}/bin/{targetPlatform}/Htc.Vita.External.CefSharp.{feature}.dll",
                    new SignToolSignSettings
                    {
                            AppendSignature = true,
                            CertPath = signKey,
                            DigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                            Password = signPass,
                            TimeStampDigestAlgorithm = SignToolDigestAlgorithm.Sha256,
                            TimeStampUri = signSha256Uri
                    }
            );
        }
    }
});

Task("Gzip-Assemblies")
    .IsDependentOn("Sign-Assemblies")
    .Does(() =>
{
    var targetPlatforms = new []
    {
            "Win32",
            "x64"
    };
    foreach (var targetPlatform in targetPlatforms)
    {
        GZipFile(
                File($"./temp/{configuration}/CefSharp.BrowserSubprocess.Core/bin/{targetPlatform}/Htc.Vita.External.CefSharp.BrowserSubprocess.Core.dll"),
                Directory($"./temp/{configuration}/CefSharp.BrowserSubprocess.Core/bin/{targetPlatform}/")
        );
        GZipFile(
                File($"./temp/{configuration}/Htc.Vita.External.CefSharp.Core/bin/{targetPlatform}/Htc.Vita.External.CefSharp.Core.dll"),
                Directory($"./temp/{configuration}/Htc.Vita.External.CefSharp.Core/bin/{targetPlatform}/")
        );
    }

    targetPlatforms = new []
    {
            "x64",
            "x86"
    };
    foreach (var targetPlatform in targetPlatforms)
    {
        GZipFile(
                File($"./temp/{configuration}/Htc.Vita.External.CefSharp.BrowserSubprocess/bin/{targetPlatform}/Htc.Vita.External.CefSharp.BrowserSubprocess.exe"),
                Directory($"./temp/{configuration}/Htc.Vita.External.CefSharp.BrowserSubprocess/bin/{targetPlatform}/")
        );
        GZipFile(
                File($"./temp/{configuration}/Htc.Vita.External.CefSharp.BrowserSubprocess/bin/{targetPlatform}/Htc.Vita.External.CefSharp.BrowserSubprocess.exe.config"),
                Directory($"./temp/{configuration}/Htc.Vita.External.CefSharp.BrowserSubprocess/bin/{targetPlatform}/")
        );
        GZipFile(
                File($"./temp/{configuration}/Htc.Vita.External.CefSharp/bin/{targetPlatform}/Htc.Vita.External.CefSharp.dll"),
                Directory($"./temp/{configuration}/Htc.Vita.External.CefSharp/bin/{targetPlatform}/")
        );
    }

    var features = new []
    {
            "OffScreen",
            "WinForms",
            "Wpf"
    };
    foreach (var targetPlatform in targetPlatforms)
    {
        foreach (var feature in features)
        {
            GZipFile(
                    File($"./temp/{configuration}/Htc.Vita.External.CefSharp.{feature}/bin/{targetPlatform}/Htc.Vita.External.CefSharp.{feature}.dll"),
                    Directory($"./temp/{configuration}/Htc.Vita.External.CefSharp.{feature}/bin/{targetPlatform}/")
            );
        }
    }
});

Task("Build-NuGet-Package")
    .IsDependentOn("Gzip-Assemblies")
    .Does(() =>
{
    CreateDirectory(nugetDir);

    var basePath = tempDir + Directory(configuration);

    var vcRuntimePackageId = "Htc.Vita.External.VC12.Runtime";
    var vcRuntimePackageVersion = "12.0.40664";
    var vcRuntimeFilenameSuffix = "Release".Equals(configuration) ? "" : "d";

    var nuSpecContentList = GetNuSpecContentList(
            basePath,
            new []
            {
                    "CefSharp.BrowserSubprocess.Core/bin/x64/Htc.Vita.External.CefSharp.BrowserSubprocess.Core.dll",
                    "CefSharp.BrowserSubprocess.Core/bin/x64/Htc.Vita.External.CefSharp.BrowserSubprocess.Core.dll.gz",
                    "CefSharp.BrowserSubprocess.Core/bin/x64/Htc.Vita.External.CefSharp.BrowserSubprocess.Core.pdb",
                    "CefSharp.BrowserSubprocess.Core/bin/x64/Htc.Vita.External.CefSharp.BrowserSubprocess.Core.xml",
                    "Htc.Vita.External.CefSharp/bin/x64/Htc.Vita.External.CefSharp.dll",
                    "Htc.Vita.External.CefSharp/bin/x64/Htc.Vita.External.CefSharp.dll.gz",
                    "Htc.Vita.External.CefSharp/bin/x64/Htc.Vita.External.CefSharp.pdb",
                    "Htc.Vita.External.CefSharp.BrowserSubprocess/bin/x64/Htc.Vita.External.CefSharp.BrowserSubprocess.exe",
                    "Htc.Vita.External.CefSharp.BrowserSubprocess/bin/x64/Htc.Vita.External.CefSharp.BrowserSubprocess.exe.gz",
                    "Htc.Vita.External.CefSharp.BrowserSubprocess/bin/x64/Htc.Vita.External.CefSharp.BrowserSubprocess.exe.config",
                    "Htc.Vita.External.CefSharp.BrowserSubprocess/bin/x64/Htc.Vita.External.CefSharp.BrowserSubprocess.exe.config.gz",
                    "Htc.Vita.External.CefSharp.BrowserSubprocess/bin/x64/Htc.Vita.External.CefSharp.BrowserSubprocess.pdb",
                    "Htc.Vita.External.CefSharp.Core/bin/x64/Htc.Vita.External.CefSharp.Core.dll",
                    "Htc.Vita.External.CefSharp.Core/bin/x64/Htc.Vita.External.CefSharp.Core.dll.gz",
                    "Htc.Vita.External.CefSharp.Core/bin/x64/Htc.Vita.External.CefSharp.Core.pdb",
                    "Htc.Vita.External.CefSharp.Core/bin/x64/Htc.Vita.External.CefSharp.Core.xml"
            },
            "CefSharp\\x64"
    );
    nuSpecContentList.AddRange(GetNuSpecContentList(
            basePath,
            new []
            {
                    $"../../source/packages/{vcRuntimePackageId}.{vcRuntimePackageVersion}/content/x64/msvcp120{vcRuntimeFilenameSuffix}.dll",
                    $"../../source/packages/{vcRuntimePackageId}.{vcRuntimePackageVersion}/content/x64/msvcp120{vcRuntimeFilenameSuffix}.dll.gz",
                    $"../../source/packages/{vcRuntimePackageId}.{vcRuntimePackageVersion}/content/x64/msvcr120{vcRuntimeFilenameSuffix}.dll",
                    $"../../source/packages/{vcRuntimePackageId}.{vcRuntimePackageVersion}/content/x64/msvcr120{vcRuntimeFilenameSuffix}.dll.gz",
            },
            "CefSharp\\x64"
    ));
    nuSpecContentList.AddRange(GetNuSpecContentList(
            basePath,
            new []
            {
                    "CefSharp.BrowserSubprocess.Core/bin/Win32/Htc.Vita.External.CefSharp.BrowserSubprocess.Core.dll",
                    "CefSharp.BrowserSubprocess.Core/bin/Win32/Htc.Vita.External.CefSharp.BrowserSubprocess.Core.dll.gz",
                    "CefSharp.BrowserSubprocess.Core/bin/Win32/Htc.Vita.External.CefSharp.BrowserSubprocess.Core.pdb",
                    "CefSharp.BrowserSubprocess.Core/bin/Win32/Htc.Vita.External.CefSharp.BrowserSubprocess.Core.xml",
                    "Htc.Vita.External.CefSharp/bin/x86/Htc.Vita.External.CefSharp.dll",
                    "Htc.Vita.External.CefSharp/bin/x86/Htc.Vita.External.CefSharp.dll.gz",
                    "Htc.Vita.External.CefSharp/bin/x86/Htc.Vita.External.CefSharp.pdb",
                    "Htc.Vita.External.CefSharp.BrowserSubprocess/bin/x86/Htc.Vita.External.CefSharp.BrowserSubprocess.exe",
                    "Htc.Vita.External.CefSharp.BrowserSubprocess/bin/x86/Htc.Vita.External.CefSharp.BrowserSubprocess.exe.gz",
                    "Htc.Vita.External.CefSharp.BrowserSubprocess/bin/x86/Htc.Vita.External.CefSharp.BrowserSubprocess.exe.config",
                    "Htc.Vita.External.CefSharp.BrowserSubprocess/bin/x86/Htc.Vita.External.CefSharp.BrowserSubprocess.exe.config.gz",
                    "Htc.Vita.External.CefSharp.BrowserSubprocess/bin/x86/Htc.Vita.External.CefSharp.BrowserSubprocess.pdb",
                    "Htc.Vita.External.CefSharp.Core/bin/Win32/Htc.Vita.External.CefSharp.Core.dll",
                    "Htc.Vita.External.CefSharp.Core/bin/Win32/Htc.Vita.External.CefSharp.Core.dll.gz",
                    "Htc.Vita.External.CefSharp.Core/bin/Win32/Htc.Vita.External.CefSharp.Core.pdb",
                    "Htc.Vita.External.CefSharp.Core/bin/Win32/Htc.Vita.External.CefSharp.Core.xml"
            },
            "CefSharp\\x86"
    ));
    nuSpecContentList.AddRange(GetNuSpecContentList(
            basePath,
            new []
            {
                    $"../../source/packages/{vcRuntimePackageId}.{vcRuntimePackageVersion}/content/x86/msvcp120{vcRuntimeFilenameSuffix}.dll",
                    $"../../source/packages/{vcRuntimePackageId}.{vcRuntimePackageVersion}/content/x86/msvcp120{vcRuntimeFilenameSuffix}.dll.gz",
                    $"../../source/packages/{vcRuntimePackageId}.{vcRuntimePackageVersion}/content/x86/msvcr120{vcRuntimeFilenameSuffix}.dll",
                    $"../../source/packages/{vcRuntimePackageId}.{vcRuntimePackageVersion}/content/x86/msvcr120{vcRuntimeFilenameSuffix}.dll.gz",
            },
            "CefSharp\\x86"
    ));

    nuSpecContentList.Add(new NuSpecContent
    {
            Source = "../../source/NuGet/Htc.Vita.External.CefSharp.Common.props",
            Target = "build"
    });
    nuSpecContentList.Add(new NuSpecContent
    {
            Source = "../../source/NuGet/Htc.Vita.External.CefSharp.Common.targets",
            Target = "build"
    });

    var cefRedistPackageIdPrefix = "htc_vita_external_cef.redist";
    var cefRedistPackageVersion = "83.5.0.5";

    NuGetPack(new NuGetPackSettings
    {
            Id = $"{product}.Common",
            Version = buildVersion,
            Authors = new [] { "The CefSharp Authors", "HTC" },
            Description = $"{description} [CommitId: {commitId}]",
            Copyright = copyright,
            ProjectUrl = new Uri(projectUrl),
            Tags = nugetTags,
            RequireLicenseAcceptance= false,
            Files = nuSpecContentList.ToArray(),
            Properties = new Dictionary<string, string>
            {
                    { "Configuration", configuration }
            },
            BasePath = basePath,
            OutputDirectory = nugetDir,
            Dependencies = new []
            {
                    new NuSpecDependency
                    {
                            Id = $"{cefRedistPackageIdPrefix}.win-x86",
                            TargetFramework = "net45",
                            Version = cefRedistPackageVersion
                    },
                    new NuSpecDependency
                    {
                            Id = $"{cefRedistPackageIdPrefix}.win-x64",
                            TargetFramework = "net45",
                            Version = cefRedistPackageVersion
                    }
            }
    });

    var features = new []
    {
            "OffScreen",
            "WinForms",
            "Wpf"
    };
    foreach (var feature in features)
    {
        nuSpecContentList = GetNuSpecContentList(
                basePath,
                new []
                {
                        $"{product}.{feature}/bin/x64/{product}.{feature}.dll",
                        $"{product}.{feature}/bin/x64/{product}.{feature}.dll.gz",
                        $"{product}.{feature}/bin/x64/{product}.{feature}.pdb",
                        $"{product}.{feature}/bin/x64/{product}.{feature}.xml"
                },
                "CefSharp\\x64"
        );
        nuSpecContentList.AddRange(GetNuSpecContentList(
                basePath,
                new []
                {
                        $"{product}.{feature}/bin/x86/{product}.{feature}.dll",
                        $"{product}.{feature}/bin/x86/{product}.{feature}.dll.gz",
                        $"{product}.{feature}/bin/x86/{product}.{feature}.pdb",
                        $"{product}.{feature}/bin/x86/{product}.{feature}.xml"
                },
                "CefSharp\\x86"
        ));
        nuSpecContentList.Add(new NuSpecContent
        {
                Source = $"../../source/NuGet/{product}.{feature}.props",
                Target = "build"
        });
        nuSpecContentList.Add(new NuSpecContent
        {
                Source = $"../../source/NuGet/{product}.{feature}.targets",
                Target = "build"
        });

        NuGetPack(new NuGetPackSettings
        {
                Id = product + "." + feature,
                Version = buildVersion,
                Authors = new [] { "The CefSharp Authors", "HTC" },
                Description = $"{description} [CommitId: {commitId}]",
                Copyright = copyright,
                ProjectUrl = new Uri(projectUrl),
                Tags = nugetTags,
                RequireLicenseAcceptance= false,
                Files = nuSpecContentList.ToArray(),
                Properties = new Dictionary<string, string>
                {
                        { "Configuration", configuration }
                },
                BasePath = basePath,
                OutputDirectory = nugetDir,
                Dependencies = new []
                {
                        new NuSpecDependency
                        {
                                Id = $"{product}.Common",
                                TargetFramework = "net45",
                                Version = buildVersion
                        }
                }
        });
    }
});

Task("Publish-NuGet-Package")
    .WithCriteria(() => "Release".Equals(configuration) && !"NOTSET".Equals(nugetApiKey) && !"NOTSET".Equals(nugetSource))
    .IsDependentOn("Build-NuGet-Package")
    .Does(() =>
{
    NuGetPush(
            new FilePath($"./dist/{configuration}/nuget/{product}.{buildVersion}.nupkg"),
            new NuGetPushSettings
            {
                    ApiKey = nugetApiKey,
                    Source = nugetSource
            }
    );
});


//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build-NuGet-Package");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
