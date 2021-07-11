using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kalix
{
	public static class ProcessingFormat
	{
		static readonly string[] ContainerFormats = new[] { "zip", "rar", "7z", "tar" };
		static readonly string[] ImageFormats = new[] { "bmp", "png", "jpg", "png", "tif", "tga", "webp" };

		public static bool IsSupportContainerFormat(string extension)
			=> ContainerFormats.Contains(extension);

		public static bool IsSupportImageFormat(string extension)
			=> ImageFormats.Contains(extension);
	}
}
