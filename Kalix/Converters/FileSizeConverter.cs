using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Kalix.Converters
{
    class FileSizeConverter : IValueConverter
	{
		private static readonly string[] Units = {
			"B", "KB", "MB", "GB", "TB", "PB"
		};

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return string.Empty;

			var fileSize = (double)(long)value;
			var unitIndex = 0;

			while (fileSize > 1024)
			{
				fileSize /= 1024;
				++unitIndex;
			}

			return $"{fileSize:F1}{Units[unitIndex]}";

		}

	    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	    {
		    throw new NotImplementedException();
	    }
    }
}
