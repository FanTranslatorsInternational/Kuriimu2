<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\Microsoft.VisualBasic.dll</Reference>
  <Namespace>Microsoft.VisualBasic</Namespace>
</Query>

void Main()
{
	/// Kuriimu2 Nuget Packer (Debug)

	Directory.SetCurrentDirectory(Path.GetDirectoryName(Util.CurrentQueryPath));

	var version = File.ReadAllText("version.txt", Encoding.ASCII);
	var newVersion = Interaction.InputBox("Enter the new version:", "New Version", version);

	if (Regex.IsMatch(newVersion, @"\d\.\d\.\d"))
	{
		File.WriteAllText("version.txt", newVersion, Encoding.ASCII);

		var assemblies = new List<string> {
			@"..\..\src\Kontract\Properties\AssemblyInfo.cs",
			@"..\..\src\Komponent\Properties\AssemblyInfo.cs",
			@"..\..\src\Kanvas\Properties\AssemblyInfo.cs",
			@"..\..\src\Kryptography\Properties\AssemblyInfo.cs",
			@"..\..\src\Kore\Properties\AssemblyInfo.cs"
		};

		foreach (var assembly in assemblies)
		{
			var content = File.ReadAllText(assembly, Encoding.UTF8);
			content = Regex.Replace(content, "\\(\"\\d\\.\\d\\.\\d\\.\\d\"\\)", "(\"" + newVersion + ".0\")");
			File.WriteAllText(assembly, content, Encoding.UTF8);
		}

		var batch = Process.Start("cmd.exe", "/c nugetPack.Debug.bat");
		batch.WaitForExit();
	}
}

// Define other methods and classes here
