using System;
using System.IO;
using System.Text;

/// <summary> 
/// FileEncoding 的摘要说明 
/// </summary> 
namespace FileEncoding
{
	/// <summary> 
	/// 获取文件的编码格式 
	/// </summary> 
	public static class EncodingType
	{
		/// <summary> 
		/// 给定文件的路径，读取文件的二进制数据，判断文件的编码类型 
		/// </summary> 
		/// <param name=“file“>文件路径</param> 
		/// <returns>文件的编码类型</returns> 
		public static System.Text.Encoding GetType(string file)
		{
			using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
			return GetType(fs);
		}

		/// <summary> 
		/// 通过给定的文件流，判断文件的编码类型 
		/// </summary> 
		/// <param name=“fs“>文件流</param> 
		/// <returns>文件的编码类型</returns> 
		public static System.Text.Encoding GetType(FileStream fs)
		{
			//var Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
			//var UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
			//var UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM 
			var reVal = Encoding.Default;
			using var reader = new BinaryReader(fs, Encoding.Default);
			if(fs == null || fs.Length < 3)
				return reVal;

			var bytes = reader.ReadBytes((int)fs.Length);
			if(IsUTF8Bytes(bytes) || (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF))
			{
				reVal = Encoding.UTF8;
			}
			else if(bytes[0] == 0xFE && bytes[1] == 0xFF && bytes[2] == 0x00)
			{
				reVal = Encoding.BigEndianUnicode;
			}
			else if(bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x41)
			{
				reVal = Encoding.Unicode;
			}
			return reVal;
		}

		/// <summary> 
		/// 判断是否是不带 BOM 的 UTF8 格式 
		/// </summary> 
		/// <param name=“data“></param> 
		/// <returns></returns> 
		private static bool IsUTF8Bytes(byte[] data)
		{
			var charByteCounter = 1; //计算当前正分析的字符应还有的字节数 
			byte curByte; //当前分析的字节. 
			for(var i = 0; i < data.Length; i++)
			{
				curByte = data[i];
				if(charByteCounter == 1)
				{
					if(curByte >= 0x80)
					{
						//判断当前 
						while(((curByte <<= 1) & 0x80) != 0)
						{
							charByteCounter++;
						}
						//标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X 
						if(charByteCounter == 1 || charByteCounter > 6)
						{
							return false;
						}
					}
				}
				else
				{
					//若是UTF-8 此时第一位必须为1 
					if((curByte & 0xC0) != 0x80)
					{
						return false;
					}
					charByteCounter--;
				}
			}
			if(charByteCounter > 1)
			{
				throw new Exception("非预期的byte格式");
			}
			return true;
		}
	}
}
