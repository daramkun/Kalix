using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Daramee.FileTypeDetector;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers.Zip;

namespace Kalix
{
	public class FileInfo : IEquatable<FileInfo>, INotifyPropertyChanged
	{
		private bool _queued = true;
		private KalixStatus _status = KalixStatus.Waiting;
		private long _convedFileSize = 0;
		private double _progress = 0;

		public bool Queued
		{
			get => _queued;
			set
			{
				_queued = value;
				DoPropertyChanged(nameof(Queued));
			}
		}
		public string OriginalFilename { get; }
		public string Filename => Path.GetFileName(OriginalFilename);

		public long FileSize { get; }

		public long ConvedFileSize
		{
			get => _convedFileSize;
			set
			{
				_convedFileSize = value;
				DoPropertyChanged(nameof(ConvedFileSize));
			}
		}

		public KalixStatus Status
		{
			get => _status;
			set
			{
				_status = value;
				DoPropertyChanged(nameof(Status));
			}
		}

		public double Progress
		{
			get => _progress;
			set
			{
				_progress = value;
				DoPropertyChanged(nameof(Progress));
			}
		}

		public string Extension { get; private set; }

		public FileInfo(string filename)
		{
			OriginalFilename = filename;

			using var stream = new FileStream(OriginalFilename, FileMode.Open, FileAccess.Read);
			FileSize = stream.Length;
		}

		public void CheckExtension()
		{
			if (!File.Exists(OriginalFilename))
			{
				Extension = null;
				return;
			}

			try
			{
				using Stream stream = new FileStream(OriginalFilename, FileMode.Open, FileAccess.Read, FileShare.Read);

				if (stream.Length == 0)
				{
					Extension = null;
					return;
				}

				var detector = DetectorService.DetectDetector(stream);
				if (detector == null || !(ProcessingFormat.IsSupportContainerFormat(detector.Extension)
										  || ProcessingFormat.IsSupportImageFormat(detector.Extension)))
					Extension = null;
				else
					Extension = detector.Extension;
			}
			catch
			{
				Extension = null;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void DoPropertyChanged(string name) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }

		public bool Equals(FileInfo other) => Path.GetFullPath(OriginalFilename) == Path.GetFullPath(other?.OriginalFilename ?? string.Empty);

		public void DoProcess(Settings settings, CancellationToken token)
		{
			if (!Queued || token.IsCancellationRequested)
			{
				Status = KalixStatus.Cancelled;
				Progress = 0;
				return;
			}

			Status = KalixStatus.Processing;

			var newFilename = string.Empty;
			var tempFilename = string.Empty;

			MemoryStream writeMemoryStream = null;
			if (ProcessingFormat.IsSupportContainerFormat(Extension))
			{
				using var originalStream = new FileStream(OriginalFilename, FileMode.Open);
				using var originalArchive = ArchiveFactory.Open(originalStream);
				
				using var targetArchive = ArchiveFactory.Create(ArchiveType.Zip);

				using var readMemoryStream = new MemoryStream();

				var proceedEntries = 0;
				var totalEntries = originalArchive.Entries.Count();
				foreach (var entry in originalArchive.Entries)
				{
					if (entry.IsDirectory)
						continue;

					if (token.IsCancellationRequested)
					{
						Status = KalixStatus.Cancelled;
						return;
					}

					using var originalEntryStream = entry.OpenEntryStream();
					StreamCopy(readMemoryStream, originalEntryStream);

					writeMemoryStream = new MemoryStream();

					var saveFormat = settings.SaveFormat;
					var result = KalixBridge.DoConversion(readMemoryStream, writeMemoryStream, ref saveFormat,
						settings.SaveOptions, settings.ProcessOptions);

					if (result != true)
						StreamCopy(writeMemoryStream, readMemoryStream);

					targetArchive.AddEntry(entry.Key, writeMemoryStream, true);

					Progress = ++proceedEntries / (double) totalEntries;
				}

				var filename = Path.GetFileNameWithoutExtension(OriginalFilename) + ".zip";
				newFilename = Path.Combine(settings.TargetPath ?? string.Empty, filename);

				var tempname = GenerateTempFileName();
				tempFilename = Path.Combine(settings.TargetPath ?? string.Empty, tempname);
				using var targetStream = new FileStream(tempFilename, FileMode.Create, FileAccess.Write);
				targetArchive.SaveTo(targetStream, new ZipWriterOptions(CompressionType.Deflate)
				{
					ArchiveEncoding = new ArchiveEncoding(Encoding.UTF8, Encoding.Default)
				});

				targetStream.Flush();
				ConvedFileSize = targetStream.Length;

				Progress = 1;
				Status = KalixStatus.Done;
			}
			else
			{
				using var originalStream = new FileStream(OriginalFilename, FileMode.Open);
				writeMemoryStream = new MemoryStream();

				var saveFormat = settings.SaveFormat;
				var result = KalixBridge.DoConversion(originalStream, writeMemoryStream, ref saveFormat,
					settings.SaveOptions, settings.ProcessOptions);

				if (result != true)
				{
					Progress = 0;
					Status = KalixStatus.Failed;
				}

				var filename = Path.GetFileNameWithoutExtension(OriginalFilename) + GetExtension(OriginalFilename, saveFormat);
				newFilename = Path.Combine(settings.TargetPath ?? string.Empty, filename);

				var tempname = GenerateTempFileName();
				tempFilename = Path.Combine(settings.TargetPath ?? string.Empty, tempname);
				using var targetStream = new FileStream(tempFilename, FileMode.Create, FileAccess.Write);
				StreamCopy(targetStream, writeMemoryStream);

				targetStream.Flush();
				ConvedFileSize = targetStream.Length;

				Progress = 1;
				Status = KalixStatus.Done;
			}
			
			if (Status == KalixStatus.Done)
				File.Move(tempFilename, newFilename, settings.FileOverwrite);
			else
				File.Delete(tempFilename);

			if (settings.FileDelete)
				File.Delete(OriginalFilename);

			writeMemoryStream?.Dispose();
		}

		private static string GenerateTempFileName()
		{
			var guid = Guid.NewGuid();
			var byteArray = guid.ToByteArray();
			return BitConverter.ToString(byteArray).Replace("-", "").ToLower();
		}

		private static string GetExtension(string originalFilename, SaveFormat saveFormat)
		{
			return saveFormat switch
			{
				SaveFormat.PublicNetworkGraphics => ".png",
				SaveFormat.GraphicsInterchangeFormat => ".gif",
				SaveFormat.JointPhotographicExpertsGroup => ".jpg",
				SaveFormat.WebP => ".webp",
				SaveFormat.SameFormat => throw new ArgumentException("Same Format is not valid", nameof(saveFormat)),
				_ => Path.GetExtension(originalFilename)
			};
		}
		
		private static void StreamCopy(Stream dest, Stream src)
		{
			var copyBuffer = new byte[4096];

			try
			{
				dest.SetLength(0);
			}
			catch
			{
				// ignored
			}

			if (src.CanSeek)
				src.Position = 0;
			
			while (true)
			{
				var read = src.Read(copyBuffer, 0, copyBuffer.Length);
				if (read == 0)
					break;
				dest.Write(copyBuffer, 0, copyBuffer.Length);
			}

			dest.Position = 0;
		}
	}
}
