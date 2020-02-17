using System;
using System.IO;
using System.IO.Compression;

namespace Updater
{
	class Program
	{
		static void Main(string[] args)
		{
			String runAfter = args[args.Length - 1];
			System.Console.WriteLine("Décompression de la mise à jour...");
			if (Directory.Exists("update"))
				Directory.Delete("update", true);
			ZipFile.ExtractToDirectory("Three-D-Velocity-Binaries-master.zip", "update");
			System.Console.WriteLine("Copie des nouveaux fichiers...");
			DirectoryCopy("update\\Three-D-Velocity-Binaries-master", ".", true);
			System.Console.WriteLine("Nettoyage des fichiers temporaires...");
			Directory.Delete("update", true);
			File.Delete("Three-D-Velocity-Binaries-master.zip");
			System.Console.WriteLine("Terminé! Démarrage de " + runAfter + "...");
			System.Diagnostics.Process.Start(runAfter);
			System.Console.WriteLine("Sortie");
		}

		private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists) {
				throw new DirectoryNotFoundException(
					"Le dossier source n'existe pas: "
					+ sourceDirName);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName)) {
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files) {
				// Don't copy the updater.exe program, since it's in use and it won't change anyway
				if (file.Name.Contains("Updater"))
					continue;
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, true);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs) {
				foreach (DirectoryInfo subdir in dirs) {
					string temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}
	}
}
