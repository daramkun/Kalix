using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Kalix.Converters
{
	class ListViewItemColorConverter : IValueConverter
	{
		private static readonly Dictionary<KalixStatus, SolidColorBrush> Brushes = new()
		{
			[KalixStatus.Waiting] = new SolidColorBrush(Colors.Transparent),
			[KalixStatus.Processing] = new SolidColorBrush(Colors.AliceBlue),
			[KalixStatus.Done] = new SolidColorBrush(Colors.LightGreen),
			[KalixStatus.Failed] = new SolidColorBrush(Colors.Pink),
			[KalixStatus.Cancelled] = new SolidColorBrush(Colors.LightYellow),
		};

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is KalixStatus status && Brushes.ContainsKey(status))
				return Brushes[status];
			return new SolidColorBrush(Colors.DarkRed);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
