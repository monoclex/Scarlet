using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
		private readonly ILogger<FileCache> _logger;

		public FileCache(string cacheDirectory)
			: this(cacheDirectory, TimeSpan.FromDays(1))
		{
		}

		public FileCache(string cacheDirectory, TimeSpan timeToExpire)
			: this(NullLogger<FileCache>.Instance, cacheDirectory, timeToExpire)
		{
		}

		public FileCache(ILogger<FileCache> logger, string cacheDirectory, TimeSpan timeToExpire)
		{
			_logger = logger;
			_cacheDirectory = Path.GetFullPath(cacheDirectory);
			_timeToExpire = timeToExpire;

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
			int attempt = 0;

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
				attempt++;

				_logger.LogWarning("Unable to delete lockfile {0}, on attempt {1}", lockFile, attempt);

				// pretty bad
				// TODO: catch multiple failures <-- how?
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

		public async ValueTask<OwnedMemory> TryRead(string cacheKey, Func<ValueTask<OwnedMemory>> compute)
		{
			var failures = -1;
			var cacheFile = GetCacheFile(cacheKey);
			var lockFile = GetLockFile(cacheFile);

		RETRY: /*********** RETRY ENTRY ************/
			failures++;

			if (failures >= 10)
			{
				_logger.LogWarning("Failed more than 10 times while attempting to read '{0}' located at {1}", cacheKey, cacheFile);

				// the computation *could* fail, and we might have to rethrow.
				// BUT, if the computation *fails*, and the file *exists*,
				// that means we'll be returning expired data to the user
				//
				// but returning something is better than nothing.

				/*********** FUNCTION EXIT ************/
				// ^ guarenteed to exit after this point

				try
				{
					return await compute().ConfigureAwait(false);
				}
				catch
				{
					if (File.Exists(cacheFile))
					{
						// cacheFile is BOUND to be expired.
						try
						{
							return File.ReadAllBytes(cacheFile);
						}
						catch (IOException)
						{
							// if we can't, then that's odd, but oh well.
						}
					}

					throw;
				}
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

					return result; /*********** FUNCTION EXIT ************/
				}
				catch (Exception)
				{
					// an exception occurred while computing
					// we will release the lock, and then rethrow the exception to the caller
					ReleaseLock(lockFile);

					// *though*, we won't throw if we have some kind of data to give back to the caller
					if (File.Exists(cacheFile))
					{
						// cacheFile is BOUND to be expired.
						try
						{
							return File.ReadAllBytes(cacheFile);
						}
						catch (IOException)
						{
							// if we can't, then that's odd, but oh well.
						}
					}

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