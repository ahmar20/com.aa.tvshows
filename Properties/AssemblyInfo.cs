using Android.App;

// SDK-style projects auto-generate AssemblyTitle/Version/etc. from the csproj
// properties (AssemblyName, ApplicationDisplayVersion, ...), so those
// attributes that used to live here have been removed to avoid a duplicate-
// attribute build error. Only assembly-level attributes with no MSBuild
// property equivalent stay in this file.

// Android TV support: neither feature is required, so the app still installs
// normally on phones/tablets, but it's no longer filtered out of the Play Store
// listing on TV devices that have no touchscreen.
[assembly: UsesFeature("android.software.leanback", Required = false)]
[assembly: UsesFeature("android.hardware.touchscreen", Required = false)]
