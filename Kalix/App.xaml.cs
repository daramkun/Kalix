using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Daramee.FileTypeDetector;

namespace Kalix
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			DetectorService.AddDetectors(Assembly.GetEntryAssembly(), FormatCategories.Archive);
			DetectorService.AddDetectors(Assembly.GetEntryAssembly(), FormatCategories.Image);
		}
	}
}
