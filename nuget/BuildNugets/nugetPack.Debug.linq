<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\Microsoft.VisualBasic.dll</Reference>
  <Namespace>Microsoft.VisualBasic</Namespace>
</Query>

void Main()
{
	/// Kuriimu2 Nuget Packer (Debug)

	Directory.SetCurrentDirectory(Path.GetDirectoryName(Util.CurrentQueryPath));

	var version = File.ReadAllText("version.txt", Encoding.ASCII);
	
	// Ask for the new version
	var newVersion = Interaction.InputBox("Enter the new version:", "New Version", version);

	if (Regex.IsMatch(newVersion, @"\d\.\d\.\d"))
	{
		File.WriteAllText("version.txt", newVersion, Encoding.ASCII);

		// The project files to update with the new version.
		var libraries = new List<string> {
			@"..\..\src\Kontract\Kontract.csproj",
			@"..\..\src\Komponent\Komponent.csproj",
			@"..\..\src\Kanvas\Kanvas.csproj",
			@"..\..\src\Kryptography\Kryptography.csproj",
			@"..\..\src\Kompression\Kompression.csproj",
			@"..\..\src\Kore\Kore.csproj"
		};

		// Set the new version in the project files.
		foreach (var library in libraries)
		{
			var content = File.ReadAllText(library, Encoding.UTF8);
			content = Regex.Replace(content, @"<PackageVersion>\d.\d.\d</PackageVersion>", "<PackageVersion>"+newVersion+"</PackageVersion>");
			File.WriteAllText(library, content, Encoding.UTF8);
		}

		// Generate the NuGet packages.
		var batch = Process.Start(@"nugetPack.Debug.bat");
		batch.WaitForExit();

		// Restore the project files to v2.0.0.
		foreach (var library in libraries)
		{
			var content = File.ReadAllText(library, Encoding.UTF8);
			content = Regex.Replace(content, @"<PackageVersion>\d.\d.\d</PackageVersion>", "<PackageVersion>2.0.0</PackageVersion>");
			File.WriteAllText(library, content, Encoding.UTF8);
		}
	}
}