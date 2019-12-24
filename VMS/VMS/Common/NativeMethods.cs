using System;
using System.Runtime.InteropServices;

namespace VMS
{
	internal static class NativeMethods
	{
		/// <summary>
		/// 控制系统休眠状态
		/// </summary>
		/// <param name="flags">标志</param>
		/// <returns>执行结果</returns>
		[DllImport("kernel32.dll")]
		internal static extern uint SetThreadExecutionState(ExecutionFlag flags);

		[Flags]
		internal enum ExecutionFlag : uint
		{
			System = 0x00000001,    //阻止一次休眠
			Display = 0x00000002,   //阻止一次关闭屏幕
			Continus = 0x80000000,  //持续状态, 单独调用为恢复休眠策略
		}

	}
}
