using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace VMS.Data
{
	static class Global
	{
		const string FILE_BRANCH_INFO = "Info.json";    //定制信息
		const string FILE_PRESET = "Sys\\Preset.json";  //预置
		const string FILE_SETTING_LOCAL = "Sys\\Setting.json";  //设置

		public static Preset _preset;
		public static Setting Setting;
		public static readonly string FILE_SETTING = ApplicationDeployment.IsNetworkDeployed ? Path.Combine(ApplicationDeployment.CurrentDeployment.DataDirectory, FILE_SETTING_LOCAL) : FILE_SETTING_LOCAL;

		static Global()
		{
			//配置文件
			try
			{
				_preset = new JavaScriptSerializer().Deserialize<Preset>(File.ReadAllText(FILE_PRESET));
				Setting = new JavaScriptSerializer().Deserialize<Setting>(File.ReadAllText(FILE_SETTING));
			}
			catch(Exception)
			{ }

			//配置默认值
			_preset = _preset ?? new Preset();
			_preset.RepoUrl = _preset.RepoUrl ?? @"http://admin:admin@192.168.120.129:2507/r/Test.git";
			_preset.Users = _preset.Users ?? new List<Preset.User> { new Preset.User { Name = "Root" }, new Preset.User { Name = "User" } };
			File.WriteAllText(FILE_PRESET, new JavaScriptSerializer().Serialize(_preset));

			Setting = Setting ?? new Setting();
			Setting.PackageFolder = Setting.PackageFolder ?? @"D:\Package\";
			Setting.CompareToolPath = Setting.CompareToolPath ?? @"D:\Program Files\Beyond Compare 4\BCompare.exe";
			Setting.LoaclRepoPath = Setting.LoaclRepoPath ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VMS\";
		}
	}
}
