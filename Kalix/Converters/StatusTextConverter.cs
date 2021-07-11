using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Kalix.Converters
{
	class StatusTextConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is KalixStatus status)
				return status switch
				{
					KalixStatus.Waiting => "대기 중",
					KalixStatus.Processing => "변환 중",
					KalixStatus.Done => "완료",
					KalixStatus.Failed => "실패",
					KalixStatus.Cancelled => "취소",
					_ => throw new ArgumentOutOfRangeException()
				};
			throw new ArgumentOutOfRangeException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
