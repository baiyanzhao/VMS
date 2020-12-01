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
			var process = ProcLFS("clean", path, root, input);
			process.WaitForExit(); //wait for git-lfs to finish

			// write git-lfs pointer for 'clean' to git or file data for 'smudge' to working copy
			process.StandardOutput.BaseStream.CopyTo(output);
			process.StandardOutput.BaseStream.Flush();
			process.StandardOutput.Close();
			output.Flush();
			output.Close();
			process.Dispose();
		}

		protected override void Smudge(string path, string root, Stream input, Stream output)
		{
			var process = ProcLFS("smudge", path, root, input);
			output.Close(); //No used, create a new Prarallel Stream
			View.ProgressWindow.CreatePrarallel(delegate
			{
				try
				{
					View.ProgressWindow.Update(path);
					using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
					process.StandardOutput.BaseStream.CopyTo(stream);
					process.StandardOutput.BaseStream.Flush();
					process.StandardOutput.Close();
				}
				catch(Exception e)
				{
					MessageBox.Show("LFS Smudge Error: " + e.Message);
				}
				finally
				{
					process.WaitForExit();
					process.Dispose();
				}
			});
		}

		private static Process ProcLFS(string mode, string path, string root, Stream input)
		{
			var process = new Process();
			process.StartInfo.FileName = "git";
			process.StartInfo.Arguments = string.Format("lfs {0} {1}", mode, path);
			process.StartInfo.WorkingDirectory = root + "/.git/";
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.Start();

			input.CopyTo(process.StandardInput.BaseStream); //Smudge: write git-lfs pointer to stdin; Clean: write file data to stdin
			input.Flush();
			process.StandardInput.Flush();
			process.StandardInput.Close();
			return process;
		}
	}
}
