using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using LibGit2Sharp;

namespace VMS
{
	internal class FileExportFilter : Filter
	{
		public FileExportFilter(string name, IEnumerable<FilterAttributeEntry> attributes)
			: base(name, attributes)
		{
		}

		protected override void Clean(string path, string root, Stream input, Stream output)
		{
			using var process = new Process();
			StartLFS(process, "clean", path, root, input);
			process.WaitForExit(); //wait for git-lfs to finish

			//write git-lfs pointer for 'clean' to git
			process.StandardOutput.BaseStream.CopyTo(output);
			process.StandardOutput.BaseStream.Flush();
			process.StandardOutput.Close();
			output.Flush();
			output.Close();
		}

		protected override void Smudge(string path, string root, Stream input, Stream output)
		{
			var buffer = new MemoryStream((int)input.Length);
	
			output.Close(); //No used, create a new Prarallel Stream Instead
			input.CopyTo(buffer);
			buffer.Position = 0;
			View.ProgressWindow.CreatePrarallel(delegate
			{
				try
				{
					View.ProgressWindow.Update(path);
					using var process = new Process();
					StartLFS(process, "smudge", path, root, buffer);

					using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
					process.StandardOutput.BaseStream.CopyTo(stream); //write file data for 'smudge' to working copy
					process.StandardOutput.BaseStream.Flush();
					process.StandardOutput.Close();
					process.WaitForExit();
				}
				catch(Exception e)
				{
					MessageBox.Show("LFS Smudge Error: " + e.Message);
				}
			});
		}

		private static void StartLFS(Process process, string mode, string path, string root, Stream input)
		{
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = string.Format("lfs {0} {1}", mode, path);
			process.StartInfo.WorkingDirectory = root + "/.git/";
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.Start();

			input.CopyTo(process.StandardInput.BaseStream); //write file data to stdin -or- write git-lfs pointer to stdin
			input.Flush();
			process.StandardInput.Flush();
			process.StandardInput.Close();
		}
	}
}
