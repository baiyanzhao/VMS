using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using LibGit2Sharp;

namespace VMS
{
	internal class FileExportFilter : Filter, IDisposable
	{
		private Process process;
		private FilterMode mode;

		public FileExportFilter(string name, IEnumerable<FilterAttributeEntry> attributes)
			: base(name, attributes)
		{
		}

		public void Dispose() => process.Dispose();

		protected override void Create(string path, string root, FilterMode mode)
		{
			this.mode = mode;
			try
			{
				// launch git-lfs
				process = new Process();
				process.StartInfo.FileName = "git";
				process.StartInfo.Arguments = string.Format("lfs {0} {1}", mode == FilterMode.Clean ? "clean" : "smudge", path);
				process.StartInfo.WorkingDirectory = root + "/.git/";
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.Start();
			}
			catch(Exception e)
			{
				MessageBox.Show("LFS Create Error: " + e.Message);
			}
		}

		protected override void Clean(string path, string root, Stream input, Stream output)
		{
			try
			{
				input.CopyTo(process.StandardInput.BaseStream); // write file data to stdin
				input.Flush();
			}
			catch(Exception e)
			{
				MessageBox.Show("LFS Clean Error: " + e.Message);
			}
		}

		protected override void Complete(string path, string root, Stream output)
		{
			try
			{
				// finalize stdin and wait for git-lfs to finish
				process.StandardInput.Flush();
				process.StandardInput.Close();
				if(mode == FilterMode.Clean)
				{
					process.WaitForExit();

					// write git-lfs pointer for 'clean' to git or file data for 'smudge' to working copy
					process.StandardOutput.BaseStream.CopyTo(output);
					process.StandardOutput.BaseStream.Flush();
					process.StandardOutput.Close();
					output.Flush();
					output.Close();
				}
				else
				{
					// write git-lfs pointer for 'clean' to git or file data for 'smudge' to working copy
					process.StandardOutput.BaseStream.CopyTo(output);
					process.StandardOutput.BaseStream.Flush();
					process.StandardOutput.Close();
					output.Flush();
					output.Close();

					process.WaitForExit();
				}

				process.Dispose();
			}
			catch(Exception e)
			{
				MessageBox.Show("LFS Complete Error: " + e.Message);
			}
		}

		protected override void Smudge(string path, string root, Stream input, Stream output)
		{
			try
			{
				input.CopyTo(process.StandardInput.BaseStream); // write git-lfs pointer to stdin
				input.Flush();
			}
			catch(Exception e)
			{
				MessageBox.Show("LFS Smudge Error: " + e.Message);
			}
		}
	}
}
