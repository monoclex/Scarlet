using Scarlet.Api.Misc;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Api
{
	/// <summary>
	/// Scarlet's <see cref="FileCache"/> is a simplistic take on caching.
	/// </summary>
	public class FileCache
	{
		private readonly string _cacheDirectory;

		public FileCache(string cacheDirectory)
		{
			_cacheDirectory = Path.GetFullPath(cacheDirectory);
			Directory.CreateDirectory(_cacheDirectory);

			// delete all `.lock` files from the last execution
			foreach (var file in Directory.GetFiles(_cacheDirectory))
			{
				if (file.EndsWith(".lock"))
				{
					File.Delete(file);
				}
			}
		}

		public FileInfo CacheDirectory => new FileInfo(_cacheDirectory);

		private string GetCacheFile(string cacheKey)
		{
			var fileName = Convert.ToBase64String(Encoding.ASCII.GetBytes(cacheKey));
			var fileFullPath = Path.Combine(_cacheDirectory, fileName);

			return fileFullPath;
		}

		private string GetLockFile(string cacheFile)
			=> cacheFile + ".lock";

		private bool TryObtainLock(string lockFile)
		{
			if (IsLock(lockFile)) return false;

			try
			{
				using var fs = new FileStream(lockFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
				return true;
			}
			catch (IOException)
			{
				// couldn't make the file, don't have the lock
				return false;
			}
		}

		private void ReleaseLock(string lockFile)
		{
		// using a goto instead of a while because it's easier i guess
		RETRY_DELETION:

			try
			{
				if (IsLock(lockFile))
				{
					File.Delete(lockFile);
				}
			}
			catch (IOException)
			{
				// pretty bad
				// TODO: catch multiple failures
				goto RETRY_DELETION;
			}
		}

		private bool IsLock(string lockFile)
			=> File.Exists(lockFile);

		public async ValueTask<Memory<byte>> TryRead(string cacheKey, Func<ValueTask<MustFreeBlock>> compute)
		{
			var failures = -1;
			var cacheFile = GetCacheFile(cacheKey);
			var lockFile = GetLockFile(cacheFile);

		RETRY: /*********** RETRY ENTRY ************/
			failures++;

			if (failures >= 10)
			{
				// bad
				// TODO: log warning
				Memory<byte> copied;

				using (var computed = await compute().ConfigureAwait(false))
				{
					copied = new byte[computed.Memory.Length];
					computed.Memory.CopyTo(copied);
				}

				return copied;
			}

			if (IsLock(lockFile))
			{
				await WaitForFileDeletion(lockFile).ConfigureAwait(false);
			}

			if (File.Exists(cacheFile))
			{
				try
				{
					// TODO: delete cache file if it expired
					var data = File.ReadAllBytes(cacheFile);
					return data; /*********** FUNCTION EXIT ************/
				}
				catch (IOException)
				{
					// we failed while reading it, this may mean a couple things:
					// - someone aquired a lock on the file and delete it
					// - (TODO: more reasons)
					//
					// so we're going to completely retry from ground zero.
					// we don't want to try lock the file and create it, because just because
					// we couldn't read it doesn't mean it doesn't exist, so we're not going to try make it.
					goto RETRY; /*********** RETRY LOOP ************/
				}
			}

			// so at this point, the cache item doesn't exist and the lock file probably doesn't exist
			// let's try to aquire the lock
			if (TryObtainLock(lockFile))
			{
				// we have the lock
				try
				{
					using var result = await compute().ConfigureAwait(false);

					try
					{
						using (var fs = File.OpenWrite(cacheFile))
						{
							await fs.WriteAsync(result.Memory, default).ConfigureAwait(false);
						}
					}
					catch (IOException)
					{
						try
						{
							if (File.Exists(cacheFile))
							{
								File.Delete(cacheFile);
							}
						}
						catch (IOException)
						{
							// if we couldn't delete the existing old cache file,
							// that will most likely mean that it has already been
							// deleted. if it hasn't, oh well i guess
						}
					}
					finally
					{
						ReleaseLock(lockFile);
					}

					// TODO: don't new up arrays
					var copy = new byte[result.Memory.Length];
					result.Memory.CopyTo(copy);
					return copy; /*********** FUNCTION EXIT ************/
				}
				catch (Exception)
				{
					// an exception occurred while computing
					// we will release the lock, and then rethrow the exception to the caller
					ReleaseLock(lockFile);
					throw;
				}
			}

			goto RETRY; /*********** RETRY LOOP ************/
		}

		private ValueTask WaitForFileDeletion(string file)
		{
			var tcs = new TaskCompletionSource<byte>();
			var watcher = new FileSystemWatcher(Path.GetDirectoryName(file), Path.GetFileName(file));

			watcher.Deleted += (sender, e) =>
			{
				if (!tcs.Task.IsCompleted)
				{
					tcs.SetResult(default);
				}
			};

			watcher.EnableRaisingEvents = true;

			// the lock might've dissapeared by the time we get down here and we would've never picked up on it
			if (!IsLock(file))
			{
				tcs.SetCanceled();
				watcher.Dispose();
				return new ValueTask();
			}

			// dispose the watcher once the task is done
			tcs.Task.ContinueWith(_ => watcher.Dispose());

			return new ValueTask(tcs.Task);
		}
	}
}