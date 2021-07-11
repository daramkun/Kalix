using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kalix
{
	public enum SaveFormat : byte
	{
		SameFormat,
		PublicNetworkGraphics,
		GraphicsInterchangeFormat,
		JointPhotographicExpertsGroup,
		WebP,
		//Av1BasedImageFormat,

		Max = WebP,
	};

	public enum ResizeFilter : sbyte
	{
		Nearest,
		Bilinear,
		Bicubic,
		Lanczos,
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SaveOptions
	{
		[MarshalAs(UnmanagedType.I4)]
		public int Quality;
		[MarshalAs(UnmanagedType.I1)]
		public bool UseLosslessCompression;
	};

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct ProcessOptions
	{
		[MarshalAs(UnmanagedType.I4)]
		public int MaximumHeight;
		[MarshalAs(UnmanagedType.I1)]
		public ResizeFilter ResizeFilter;

		[MarshalAs(UnmanagedType.I1)]
		public bool UseGrayscale;
		[MarshalAs(UnmanagedType.I1)]
		public bool UseGrayscaleOnlyGrayscale;

		[MarshalAs(UnmanagedType.I1)]
		public bool UseIndexedColor;
		[MarshalAs(UnmanagedType.I1)]
		public bool UseIndexedColorOnlyUnder256Colors;

		[MarshalAs(UnmanagedType.I1)]
		public bool NoConvertToPngIfHasTransparentColor;

		[MarshalAs(UnmanagedType.I4)]
		public int DetermineThreshold;
	};

	public static class KalixBridge
	{
		private enum LoadFormat : byte
		{
			Unknown,
			PublicNetworkGraphics,
			GraphicsInterchangeFormat,
			JointPhotographicExpertsGroup,
			WebP,
			//Av1BasedImageFormat,
		};

		private enum KalixResult : byte
		{
			Ok,

			CannotLoad,
			CannotSave,
			CannotProcess,
			ProcessPass,
			Fail,
		}

		private enum KalixStreamOffset : byte
		{
			Begin,
			Current,
			End,
		};

		private delegate long KalixStreamRead(IntPtr data, IntPtr buffer, long length);
		private delegate long KalixStreamWrite(IntPtr data, IntPtr buffer, long length);
		private delegate void KalixStreamFlush(IntPtr data);
		private delegate long KalixStreamPosition(IntPtr data);
		private delegate long KalixStreamLength(IntPtr data);
		private delegate bool KalixStreamSeek(IntPtr data, KalixStreamOffset offset, long pos);

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct KalixStream
		{
			[MarshalAs(UnmanagedType.SysInt)]
			IntPtr data;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			KalixStreamRead read;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			KalixStreamWrite write;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			KalixStreamFlush flush;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			KalixStreamPosition position;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			KalixStreamLength length;
			[MarshalAs(UnmanagedType.FunctionPtr)]
			KalixStreamSeek seek;

			public static KalixStream CreateKalixStream(Stream stream)
			{
				var kalixStream = new KalixStream();
				var gcHandle = GCHandle.Alloc(stream, GCHandleType.Normal);
				kalixStream.data = (IntPtr)gcHandle;
				kalixStream.read = (data, buffer, length) =>
				{
					var targetStream = GCHandle.FromIntPtr(data).Target as Stream;
					long total = 0;
					while (total < length)
					{
						var readLength = (length - total) > int.MaxValue ? int.MaxValue : (int)(length - total);
						var tempBuffer = new byte[readLength];
						var read = targetStream?.Read(tempBuffer, 0, readLength) ?? 0;
						if (read == 0)
							break;

						Marshal.Copy(tempBuffer, (int) total, buffer, read);
						total += read;
					}
					return total;
				};
				kalixStream.write = (data, buffer, length) =>
				{
					var targetStream = GCHandle.FromIntPtr(data).Target as Stream;
					long total = 0;
					while (total < length)
					{
						var writeLength = (length - total) > int.MaxValue ? int.MaxValue : (int)(length - total);

						var tempBuffer = new byte[writeLength];
						Marshal.Copy(buffer, tempBuffer, (int) total, writeLength);

						targetStream?.Write(tempBuffer, 0, writeLength);
						total += writeLength;
					}
					return total;
				};
				kalixStream.seek = (data, offset, pos) =>
				{
					try
					{
						var targetStream = GCHandle.FromIntPtr(data).Target as Stream;
						var managedOffset = offset switch
						{
							KalixStreamOffset.Begin => SeekOrigin.Begin,
							KalixStreamOffset.Current => SeekOrigin.Current,
							KalixStreamOffset.End => SeekOrigin.End,
							_ => throw new ArgumentOutOfRangeException(nameof(offset), offset, null)
						};
						targetStream?.Seek(pos, managedOffset);
					}
					catch
					{
						return false;
					}

					return true;
				};
				kalixStream.flush = (data) =>
				{
					var targetStream = GCHandle.FromIntPtr(data).Target as Stream;
					targetStream?.Flush();
				};
				kalixStream.position = (data) =>
				{
					var targetStream = GCHandle.FromIntPtr(data).Target as Stream;
					return targetStream?.Position ?? -1;
				};
				kalixStream.length = (data) =>
				{
					var targetStream = GCHandle.FromIntPtr(data).Target as Stream;
					return targetStream?.Length ?? -1;
				};

				return kalixStream;
			}

			public static void DestroyKalixStream(KalixStream stream)
			{
				var gcHandle = GCHandle.FromIntPtr(stream.data);
				if (gcHandle.IsAllocated)
					gcHandle.Free();
			}
		}

		[DllImport("Kalix.Core.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "Kalix_DoConversion")]
		private static extern KalixResult Kalix_DoConversion(in KalixStream inputStream, in KalixStream outputStream, ref SaveFormat saveFormat, in SaveOptions saveOptions, in ProcessOptions processOptions);

		[DllImport("Kalix.Core.dll", CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "Kalix_Is_Valid_Options")]
		private static extern bool Kalix_Is_Valid_Options(in SaveOptions saveOptions, in ProcessOptions processOptions);
		[DllImport("Kalix.Core.dll", CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "Kalix_Is_Valid_Options")]
		private static extern bool Kalix_Is_Valid_Options_For_Save_Options(in SaveOptions saveOptions, IntPtr processOptions);
		[DllImport("Kalix.Core.dll", CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "Kalix_Is_Valid_Options")]
		private static extern bool Kalix_Is_Valid_Options_For_Process_Options(IntPtr saveOptions, ProcessOptions processOptions);

		public static bool IsValidOptions(in SaveOptions saveOptions, in ProcessOptions processOptions) =>
			Kalix_Is_Valid_Options(saveOptions, processOptions);

		public static bool IsValidOptions(in SaveOptions saveOptions) =>
			Kalix_Is_Valid_Options_For_Save_Options(saveOptions, IntPtr.Zero);

		public static bool IsValidOptions(in ProcessOptions processOptions) =>
			Kalix_Is_Valid_Options_For_Process_Options(IntPtr.Zero, processOptions);

		public static bool? DoConversion(Stream inputStream, Stream outputStream, ref SaveFormat saveFormat, in SaveOptions saveOptions, in ProcessOptions processOptions)
		{
			var kalixInputStream = KalixStream.CreateKalixStream(inputStream);
			var kalixOutputStream = KalixStream.CreateKalixStream(outputStream);

			try
			{
				var result = Kalix_DoConversion(kalixInputStream, kalixOutputStream, ref saveFormat, saveOptions,
					processOptions);
				return result switch
				{
					KalixResult.Ok => true,
					KalixResult.ProcessPass => null,
					_ => false
				};
			}
			finally
			{
				KalixStream.DestroyKalixStream(kalixOutputStream);
				KalixStream.DestroyKalixStream(kalixInputStream);
			}
		}
	}
}
