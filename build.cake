#addin nuget:https://www.nuget.org/api/v2/?package=Cake.DoInDirectory
// #addin nuget:https://www.nuget.org/api/v2/?package=Cake.FileHelpers
#tool "nuget:?package=xunit.runner.console"


//////////////////////////////////////////////////////////////////////
// CONFIGURATION
//////////////////////////////////////////////////////////////////////

const string TESTER_SERVICE_INTEGRATION_TESTS = "integration-tester";

var PROJECTS_TO_PACK = new List<string>
{
    "Owin.Http.Hosting.csproj",
	"Owin.Websockets.csproj",
	 
};


var BRANCHES_TO_RELEASE = new List<string>() 
{
	"origin/master"
};

var BRANCHES_TO_PREVIEW = new List<string>() 
{
    "origin/dev"	
};

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Full-Build");
var configuration = Argument("configuration", "Release");
var nugetPreReleaseTag = Argument("nugetPreReleaseTag", "dev");
var buildNumber =  Argument<int>("buildNumber",0);
var versionSufix = "1.0.0";
var pre = HasArgument("pre");
var sourcePath = Argument<string>("sourcePath","./src");
var solutionName = "Owin.Http.Hosting.sln";
var nugetToken = Argument("nugetToken", "");
var nugetSource = Argument("nugetSource","");
var branch = Argument("branch", "dev");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var outputDir = Directory("Output/");
var artifactsDir = outputDir + Directory("Artifacts/");
var distDirectory = outputDir + Directory("Dist/");
var nugetPackagesDir = artifactsDir + Directory("NuGets/");
var preReleaseNugetPackagesDir = nugetPackagesDir; //+ Directory("PreRelease/");
var releaseNugetPackagesDir = nugetPackagesDir + Directory("Release/");
var integrationTestResultsOutputDir = outputDir + Directory("IntegrationTestsResults/");


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
    {
		
		
        CleanDirectory(artifactsDir);
        CleanDirectory(integrationTestResultsOutputDir);
    });

Task("Restore-NuGet-Packages")
    .Does(() =>
    {
        DotNetCoreRestore(solutionName);
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
    {
        var dotNetBuildConfig = new DotNetCoreBuildSettings() {
            Configuration = configuration,
            MSBuildSettings = new DotNetCoreMSBuildSettings()
        };
        
        DotNetCoreBuild(solutionName, dotNetBuildConfig);
    });
    
Task("NugetPack")
 .DoesForEach(GetFiles($"./src/**/*.csproj"), (file) => 
    {
             
			var buildNumber = EnvironmentVariable("BUILD_NUMBER");
			 
		      var versionSufix = "";
			
            var publishFolder =  releaseNugetPackagesDir ;
			 
			bool toPack = false;
			
			if(BRANCHES_TO_RELEASE.Any((releasedBranch)=> { return releasedBranch.Equals(branch);}))
			{
				toPack = true;
			}

			if(BRANCHES_TO_PREVIEW.Any((releasedBranch)=> {	return releasedBranch.Equals(branch); }))
			{
				toPack = true;
                versionSufix = $"preview{buildNumber}";
                publishFolder =  preReleaseNugetPackagesDir ;
            }
		    
			var settings = new DotNetCorePackSettings
            {
                Configuration = configuration,
                OutputDirectory =  publishFolder,
                NoDependencies = false,
                NoRestore = true,
				VersionSuffix = versionSufix,
            };
		
			if(toPack && PROJECTS_TO_PACK.Any((projName)=> file.FullPath.Contains(projName)))
			{
				DotNetCorePack(file.FullPath, settings);                 
			}
});
    
Task("Run-Unit-Tests")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
    {
        
        var files = GetFiles("./tests/**/*Tests.csproj");

        int highestExitCode = 0;

        foreach (var file in files){

            DotNetCoreTest(
                file.FullPath,
                new DotNetCoreTestSettings()
                {
                    Configuration = "Release",
                    //NoBuild = true
                });
            
        }
         
    });

Task("Publish").
IsDependentOn("NugetPack")
.Does(() => 
{
	  var accessToken = nugetToken; //EnvironmentVariable("NUGET_ACCESSTOKEN");
	  var filesPath = nugetPackagesDir.Path.FullPath+"/**/*.nupkg";
	   
	  Information("nuget packs:" +filesPath);
	  
	  var packages = GetFiles(filesPath);		
        // Push the package.

		if(BRANCHES_TO_RELEASE.Any((releasedBranch)=> 
		{
			return releasedBranch.Equals(branch);
		}))
		{
			  NuGetPush(packages, new NuGetPushSettings 
            { 
                Source =nugetSource,
                ApiKey = accessToken, 
                Verbosity = NuGetVerbosity.Detailed,
            });
		}
    
});
 

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////
Task("Full-Build")
    .IsDependentOn("Build-AND-Test")
    .IsDependentOn("Publish");
	

Task("Build-AND-Test")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Unit-Tests");

Task("Default")
    .IsDependentOn("Full-Build");
    
//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
