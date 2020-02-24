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
		private readonly TimeSpan _timeToExpire;

		public FileCache(string cacheDirectory, TimeSpan timeToExpire)
		{
			_timeToExpire = timeToExpire;

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

		private bool IsExpired(string cacheFile)
		{
			var fileInfo = new FileInfo(cacheFile);

			var creation = fileInfo.CreationTimeUtc;
			var lifetime = DateTime.UtcNow - creation;

			return lifetime > _timeToExpire;
		}

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
					// we don't want to read the cache file if it's expired
					// we'll fall into the case below
					if (!IsExpired(cacheFile))
					{
						var data = File.ReadAllBytes(cacheFile);
						return data; /*********** FUNCTION EXIT ************/
					}
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

					// if it exists, we'll delete it so the creation time changes
					MaybeDelete(cacheFile);

					try
					{
						using (var fs = File.OpenWrite(cacheFile))
						{
							await fs.WriteAsync(result.Memory, default).ConfigureAwait(false);
						}
					}
					catch (IOException)
					{
						MaybeDelete(cacheFile);
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

			static void MaybeDelete(string file)
			{
				try
				{
					if (File.Exists(file))
					{
						File.Delete(file);
					}
				}
				catch (IOException)
				{
				}
			}
		}

		/// <summary>Ages the file so that on the next read, it expires.</summary>
		public void Age(string cacheKey)
		{
			var cacheFile = GetCacheFile(cacheKey);

			if (!File.Exists(cacheFile))
			{
				return;
			}

			var fileInfo = new FileInfo(cacheFile);
			fileInfo.CreationTimeUtc = DateTime.UnixEpoch;
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