using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Daramee.Winston.Dialogs;
using Daramee.Winston.File;

namespace Kalix
{
	public partial class MainWindow : Window
	{
		public static MainWindow SharedWindow { get; private set; }

		public ObservableCollection<FileInfo> Files { get; } = new();

		private CancellationTokenSource _cancellationTokenSource;

		private readonly ConcurrentQueue<FileInfo> _processQueue = new();

		public bool IsProcessing => _cancellationTokenSource != null;

		public MainWindow()
		{
			SharedWindow = this;

			InitializeComponent();
		}

		private void MainWindow_OnClosing(object sender, CancelEventArgs e)
		{
			Settings.SharedSettings.Save();
		}

		void AddFile(string filename)
		{
			if (System.IO.File.Exists(filename))
			{
				var fileInfo = new FileInfo(filename);

				if (Files.Contains(fileInfo))
					return;

				fileInfo.CheckExtension();
				if (fileInfo.Extension == null)
					return;

				Dispatcher.Invoke(() => Files.Add(fileInfo));
			}
			else
			{
				foreach (var subDirectoryItem in FilesEnumerator.EnumerateFiles(filename, "*.*", false))
					AddFile(subDirectoryItem);
			}
		}

		private async void ListViewFiles_Drop(object sender, DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(DataFormats.FileDrop))
				return;

			var temp = e.Data.GetData(DataFormats.FileDrop) as string[];

			await Task.Run(() =>
			{
				if (temp == null)
					return;
				foreach (var s in temp)
					AddFile(s);
			});
		}

		private void ListViewFiles_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effects = DragDropEffects.None;
		}

		private void ListViewFiles_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Delete)
				return;
			
			var selected = ListViewFiles.SelectedItems.Cast<FileInfo>().ToList();

			if (!IsProcessing)
			{
				foreach (var fileInfo in selected)
					Files.Remove(fileInfo);
			}
			else
			{
				foreach (var fileInfo in selected.Where(fileInfo => fileInfo.Status is KalixStatus.Waiting))
					Files.Remove(fileInfo);
			}
		}

		private async void ButtonOpen_OnClick(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog { AllowMultiSelection = true };

			if (ofd.ShowDialog(this) != true)
				return;

			await Task.Run(() =>
			{
				foreach (var filename in ofd.FileNames)
					AddFile(filename);
			});
		}

		private void ButtonClear_OnClick(object sender, RoutedEventArgs e)
		{
			if (!IsProcessing)
				Files.Clear();
			else
			{
				foreach (var file in Files.Where(file => file.Status != KalixStatus.Waiting).ToList())
					Files.Remove(file);
			}
		}

		private async void ButtonApply_OnClick(object sender, RoutedEventArgs e)
		{
			ButtonApply.IsEnabled = ButtonClear.IsEnabled = StackPanelSettings.IsEnabled = false;
			ButtonCancel.IsEnabled = true;

			foreach (var fileInfo in Files)
				fileInfo.Status = KalixStatus.Waiting;

			_cancellationTokenSource = new CancellationTokenSource();

			do
			{
				if (!KalixBridge.IsValidOptions(Settings.SharedSettings.SaveOptions))
				{
					TaskDialog.Show("설정 오류.", "저장 설정 오류.", "저장 설정을 다시 한번 확인해주세요.",
						TaskDialogCommonButtonFlags.OK, TaskDialogIcon.Error);
					break;
				}

				if (!KalixBridge.IsValidOptions(Settings.SharedSettings.ProcessOptions))
				{
					TaskDialog.Show("설정 오류.", "처리 설정 오류.", "처리 설정을 다시 한번 확인해주세요.",
						TaskDialogCommonButtonFlags.OK, TaskDialogIcon.Error);
					break;
				}

				_processQueue.Clear();
				foreach (var fileInfo in Files)
					_processQueue.Enqueue(fileInfo);

				await Task.Run(() =>
				{
					ParallelQueueExecutor.Execute(_processQueue, (fileInfo) =>
					{
						fileInfo.DoProcess(Settings.SharedSettings, _cancellationTokenSource.Token);
					}, Settings.SharedSettings.ParallelProcessorCount);
				});
			} while (false);

			_cancellationTokenSource = null;

			GC.Collect();

			ButtonCancel.IsEnabled = false;
			ButtonApply.IsEnabled = ButtonClear.IsEnabled = StackPanelSettings.IsEnabled = true;
		}

		private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
		{
			if (_cancellationTokenSource == null)
				return;

			_cancellationTokenSource.Cancel();
			ButtonCancel.IsEnabled = false;
			ButtonApply.IsEnabled = ButtonClear.IsEnabled = StackPanelSettings.IsEnabled = true;
		}

		private void ButtonLicenses_OnClick(object sender, RoutedEventArgs e)
		{
			new LicenseWindow() { Owner = this }.ShowDialog();
		}
	}
}
