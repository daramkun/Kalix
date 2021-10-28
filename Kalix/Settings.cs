using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace Kalix
{
	public class Settings : INotifyPropertyChanged
	{
		private static Settings _instance;

		public static Settings SharedSettings
		{
			get
			{
				switch (_instance)
				{
					case null when File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}\\Kalix.config.json"):
					{
						using Stream stream = File.Open($"{AppDomain.CurrentDomain.BaseDirectory}\\Kalix.config.json", FileMode.Open);
						using TextReader reader = new StreamReader(stream, Encoding.UTF8, true, -1, true);
						var jsonString = reader.ReadToEnd();
						var jsonBytes = Encoding.UTF8.GetBytes(jsonString);

						_instance = JsonSerializer.Deserialize<Settings>(jsonBytes);
						break;
					}

					case null:
						_instance = new Settings();
						break;
				}

				return _instance;
			}
		}
		
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#region Fields
		private string _targetPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
		private bool _fileOverwrite = false;
		private bool _fileDelete = false;
		private SaveFormat _saveFormat = SaveFormat.JointPhotographicExpertsGroup;
		private SaveOptions _saveOptions = new()
		{
			Quality = 80,
			UseLosslessCompression = false,
		};
		private ProcessOptions _processOptions = new()
		{
			ResizeFilter = ResizeFilter.Bicubic,
			MaximumHeight = 12000,
			UseGrayscale = false,
			UseGrayscaleOnlyGrayscale = true,
			UseIndexedColor = false,
			UseIndexedColorOnlyUnder256Colors = true,
			NoConvertToPngIfHasTransparentColor = true,
			DetermineThreshold = 20,
		};
		private int _parallelProcessorCount = Environment.ProcessorCount / 2;

		private WindowState _windowState = WindowState.Normal;
		private int _windowLeft = 128, _windowTop = 128, _windowWidth = 1024, _windowHeight = 768;
		#endregion

		#region Basic Properties
		public string TargetPath
		{
			get => _targetPath;
			set
			{
				_targetPath = value;
				OnPropertyChanged();
			}
		}

		public bool FileOverwrite
		{
			get => _fileOverwrite;
			set
			{
				_fileOverwrite = value;
				OnPropertyChanged();
			}
		}

		public bool FileDelete
		{
			get => _fileDelete;
			set
			{
				_fileDelete = value;
				OnPropertyChanged();
			}
		}

		public SaveFormat SaveFormat
		{
			get => _saveFormat;
			set
			{
				_saveFormat = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SaveFormatInteger));
			}
		}

		public int SaveFormatInteger
		{
			get => (int)_saveFormat;
			set
			{
				_saveFormat = (SaveFormat)value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SaveFormat));
			}
		}

		public SaveOptions SaveOptions => _saveOptions;
		public ProcessOptions ProcessOptions => _processOptions;

		public int ParallelProcessorCount
		{
			get => _parallelProcessorCount;
			set
			{
				_parallelProcessorCount = value;
				OnPropertyChanged();
			}
		}

		public WindowState WindowState
		{
			get => _windowState;
			set
			{
				_windowState = value;
				OnPropertyChanged();
			}
		}

		public int WindowLeft
		{
			get => _windowLeft;
			set
			{
				_windowLeft = value;
				OnPropertyChanged();
			}
		}

		public int WindowTop
		{
			get => _windowTop;
			set
			{
				_windowTop = value;
				OnPropertyChanged();
			}
		}

		public int WindowWidth
		{
			get => _windowWidth;
			set
			{
				_windowWidth = value;
				OnPropertyChanged();
			}
		}

		public int WindowHeight
		{
			get => _windowHeight;
			set
			{
				_windowHeight = value;
				OnPropertyChanged();
			}
		}
		#endregion

		#region Save Options
		public int Quality
		{
			get => _saveOptions.Quality;
			set
			{
				_saveOptions.Quality = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SaveOptions));
			}
		}

		public bool UseLosslessCompression
		{
			get => _saveOptions.UseLosslessCompression;
			set
			{
				_saveOptions.UseLosslessCompression = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SaveOptions));
			}
		}
		#endregion

		#region Process Options
		public int MaximumHeight
		{
			get => _processOptions.MaximumHeight;
			set
			{
				_processOptions.MaximumHeight = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ProcessOptions));
			}
		}
		public ResizeFilter ResizeFilter
		{
			get => _processOptions.ResizeFilter;
			set
			{
				_processOptions.ResizeFilter = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ResizeFilterInteger));
				OnPropertyChanged(nameof(ProcessOptions));
			}
		}
		public int ResizeFilterInteger
		{
			get => (int)_processOptions.ResizeFilter;
			set
			{
				_processOptions.ResizeFilter = (ResizeFilter)value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ResizeFilter));
				OnPropertyChanged(nameof(ProcessOptions));
			}
		}

		public bool UseGrayscale
		{
			get => _processOptions.UseGrayscale;
			set
			{
				_processOptions.UseGrayscale = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ProcessOptions));
			}
		}
		public bool UseGrayscaleOnlyGrayscale
		{
			get => _processOptions.UseGrayscaleOnlyGrayscale;
			set
			{
				_processOptions.UseGrayscaleOnlyGrayscale = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ProcessOptions));
			}
		}

		public bool UseIndexedColor
		{
			get => _processOptions.UseIndexedColor;
			set
			{
				_processOptions.UseIndexedColor = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ProcessOptions));
			}
		}
		public bool UseIndexedColorOnlyUnder256Colors
		{
			get => _processOptions.UseIndexedColorOnlyUnder256Colors;
			set
			{
				_processOptions.UseIndexedColorOnlyUnder256Colors = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ProcessOptions));
			}
		}

		public bool NoConvertToPngIfHasTransparentColor
		{
			get => _processOptions.NoConvertToPngIfHasTransparentColor;
			set
			{
				_processOptions.NoConvertToPngIfHasTransparentColor = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ProcessOptions));
			}
		}

		public int DetermineThreshold
		{
			get => _processOptions.DetermineThreshold;
			set
			{
				_processOptions.DetermineThreshold = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(ProcessOptions));
			}
		}
		#endregion

		public void Save()
		{
			using Stream stream = File.Open($"{AppDomain.CurrentDomain.BaseDirectory}\\Kalix.config.json", FileMode.Create);
			var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(this, typeof(Settings), new JsonSerializerOptions()
			{
				WriteIndented = false,
				AllowTrailingCommas = false,
			});
			stream.Write(jsonBytes, 0, jsonBytes.Length);
		}
	}
}
