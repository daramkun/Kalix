using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kalix
{
	public static class ParallelQueueExecutor
	{
		public static void Execute<T>(ConcurrentQueue<T> queue, Action<T> executor, CancellationToken cancellationToken, int threadCount = -1)
		{
			if (cancellationToken.IsCancellationRequested)
				return;

			if (threadCount <= 0)
				threadCount = Environment.ProcessorCount;

			CancellationTokenSource doneCondition = new CancellationTokenSource();

			void Executor()
			{
				while (!cancellationToken.IsCancellationRequested && !doneCondition.IsCancellationRequested)
				{
					if (queue.TryDequeue(out var obj))
						executor(obj);
					Thread.Sleep(0);
				}
			}

			var threads = new Thread[threadCount];
			for (var i = 0; i < threadCount; ++i)
			{
				threads[i] = new Thread(Executor);
				threads[i].Start();
			}

			while (!queue.IsEmpty)
			{

			}

			doneCondition.Cancel();

			for (var i = 0; i < threadCount; ++i)
				threads[i].Join();
		}

		public static void Execute<T>(ConcurrentQueue<T> queue, Action<T> executor, int threadCount = -1)
		{
			var _ = new CancellationTokenSource();
			Execute<T>(queue, executor, _.Token, threadCount);
		}
	}
}
