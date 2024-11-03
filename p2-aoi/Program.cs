using System.Diagnostics;

public class TodoFile
{
	public TodoFile(string p, string n, int s)
	{
		Parent = p;
		Name = n;
		Size = s;
	}

	public string Parent;
	public string Name;
	private int size;

	public int Size { get => size; set => size = value; }
}

public class TodoFolder
{
	public TodoFolder(List<TodoFile> files, string name)
	{
		Files = files;
		Name = name;
	}

	public List<TodoFile> Files;
	public string Name;
}

internal class Program
{
	static void FormatFolder(string name)
	{
		string[] files = Directory.GetFiles(name, "*.*", SearchOption.AllDirectories);

		foreach (string file in files)
		{
			if (!file.EndsWith(".cpp") && !file.EndsWith(".h") && !file.EndsWith(".c"))
			{
				continue;
			}

			Process.Start("clang-format.exe", file + " -style=file -i");
			Console.WriteLine(file);
		}
	}
	static void FormatFiles()
	{
		if (!File.Exists("clang-format.exe"))
		{
			Console.WriteLine("Unable to format files, clang-format.exe not found!");
			return;
		}

		FormatFolder("src/");
		FormatFolder("include/");
	}

	static FileInfo? CheckFileExists(string path)
	{
		if (File.Exists(path))
		{
			FileInfo newInfo = new(path);
			return newInfo;
		}

		return null;
	}

	static void RecommendedTodo()
	{
		string ppp = Directory.GetCurrentDirectory();
		List<string> asm_paths = Directory.GetFiles(ppp + "\\asm", "*.s", SearchOption.AllDirectories).ToList();
		List<FileInfo> valid_entries = new();

		List<string> objFiles = File.ReadAllLines(ppp + "/obj_files.mk").ToList();

		for (int i = 0; i < objFiles.Count; i++)
		{
			string line = objFiles[i];
			string trimmedLine = line.TrimEnd('\\');

			if (trimmedLine.EndsWith(".a"))
			{
				objFiles.RemoveAt(i);
				i--;

				string relativePath = trimmedLine.Replace("$(BUILD_DIR)/", "/").Trim();
				relativePath = relativePath.Remove(relativePath.LastIndexOf('/')) + "/makefile";

				FileInfo? f = CheckFileExists(ppp + relativePath);
				if (f != null)
				{
					objFiles.AddRange(File.ReadAllLines(f.FullName));
				}
			}
		}

		foreach (var asm_path in asm_paths)
		{
			string base_file = asm_path.Replace("asm", "src");
			string cpp_file = base_file.Replace(".s", ".cpp");
			string c_file = base_file.Replace(".s", ".c");

			FileInfo? data = CheckFileExists(cpp_file);
			data ??= CheckFileExists(c_file);

			if (data != null)
			{
				// File exists, now we'll check if it's linked or not
				bool found = false;
				string o_filename = asm_path[asm_path.IndexOf("asm")..].Replace(".s", ".o").Replace("asm", "src").Replace("\\", "/");
				for (int i = 0; i < objFiles.Count; i++)
				{
					string? line = objFiles[i];

					if (line.Contains("src") && line.Contains(o_filename)
						|| ((i + 1) < objFiles.Count
							&& objFiles[i + 1].Contains("asm") && objFiles[i + 1].Contains(o_filename)))
					{
						found = true;
					}
				}

				if (found)
				{
					continue;
				}

				valid_entries.Add(data);
			}
		}

		var sorted = from entry in valid_entries orderby entry.Length ascending select entry;

		valid_entries = sorted.ToList();
		string fileContents = "# Unlinked files to decompile (sorted by size)\n\n";

		List<TodoFolder> folderHolders = new()
		{
			new(new(), "Dolphin"),
			new(new(), "JSystem"),
			new(new(), "plugProjectEbisawaU"),
			new(new(), "plugProjectHikinoU"),
			new(new(), "plugProjectKandoU"),
			new(new(), "plugProjectKonoU"),
			new(new(), "plugProjectMorimuraU"),
			new(new(), "plugProjectNishimuraU"),
			new(new(),  "plugProjectOgawaU"),
			new(new(),  "plugProjectYamashitaU"),
			new(new(),  "sysBootupU"),
			new(new(),  "sysCommonU"),
			new(new(),  "sysGCU"),
			new(new(),  "utilityU")
		};

		fileContents += "## Folders\n";
		foreach (var holder in folderHolders)
		{
			fileContents += $"- <a href=\"#{holder.Name}\">{holder.Name}</a>\n";
		}
		fileContents += "\n";

		foreach (var file in valid_entries)
		{
			DirectoryInfo parent = file.Directory.Parent;
			List<string> folders = new();
			while (parent.Name != "src")
			{
				folders.Add(parent.Name);
				parent = parent.Parent;
			}

			string folderPath = string.Empty;
			for (int i = 0; i < folders.Count; i++)
			{
				folderPath += folders[i] + "/";
			}
			folderPath += file.Directory.Name;

			string path = $"{folderPath}/{file.Name}";
			TodoFile newFile = new(folders.Count == 0 ? folderPath : folders[^1], path, (int)file.Length);

			foreach (var folderHolder in folderHolders)
			{
				if (folderHolder.Name == newFile.Parent)
				{
					folderHolder.Files.Add(newFile);
					break;
				}
			}
		}

		foreach (var holder in folderHolders)
		{
			fileContents += $"### <section id=\"{holder.Name}\">{holder.Name}</section>\n";
			fileContents += "| File | Size (bytes) | File | Size (bytes) |\n";
			fileContents += "| ---- | ---- | ---- | ---- |\n";

			for (int i = 0; i < holder.Files.Count; i += 2)
			{
				TodoFile? file1 = holder.Files[i];
				TodoFile? file2 = i + 1 < holder.Files.Count ? holder.Files[i + 1] : null;

				fileContents += $"| <a href=\"https://github.com/projectPiki/pikmin2/tree/main/src/{file1.Name}\">{file1.Name.Replace(holder.Name, "").TrimStart('/')}</a> | {file1.Size} |";

				if (file2 != null)
				{
					fileContents += $" <a href=\"https://github.com/projectPiki/pikmin2/tree/main/src/{file2.Name}\">{file2.Name.Replace(holder.Name, "").TrimStart('/')}</a> | {file2.Size} |\n";
				}
				else
				{
					fileContents += "  |  |\n"; // If there's no second file, use placeholders.
				}
			}

			fileContents += "\n";
		}

		File.WriteAllLines("docs/recommended_todo.md", fileContents.Split("\n"));
	}

	static void RemoveLinkedFiles()
	{
		string[] objFiles = File.ReadAllLines("obj_files.mk");

		for (int i = 0; i < objFiles.Length; i++)
		{
			string l = objFiles[i];

			if (l.Contains("src/"))
			{
				string target = l[l.IndexOf("src/")..].Replace("src", "asm");

				// If previous or next line contains the same
				// target (partial link) then skip
				if (i > 0 && i + 1 < objFiles.Length
					&& (objFiles[i - 1].Contains(target) || objFiles[i + 1].Contains(target)))
				{
					continue;
				}

				// It is solely linked
				string sFile = l.Replace("$(BUILD_DIR)/src", "asm").Replace(".o\\", ".s").Trim();
				if (File.Exists(sFile))
				{
					Console.WriteLine($"Deleting redundant *.s file: {sFile}");
					File.Delete(sFile);
				}
			}
		}
	}

	private static void Main(string[] args)
	{
		Console.WriteLine("Writing docs/recommended_todo.md");
		RecommendedTodo();
		Console.WriteLine();

		Console.WriteLine("Removing linked files");
		RemoveLinkedFiles();
		Console.WriteLine();

		Console.WriteLine("Formatting files");
		FormatFiles();
		Console.WriteLine();
	}
}