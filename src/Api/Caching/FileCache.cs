using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Api.Caching
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

		private bool IsLock(string lockFile)
			=> File.Exists(lockFile);

		public async ValueTask<Memory<byte>> TryRead(string cacheKey, Func<ValueTask<Memory<byte>>> compute)
		{
			var cacheFile = GetCacheFile(cacheKey);
			var lockFile = GetLockFile(cacheFile);

		RETRY: /*********** RETRY ENTRY ************/
			if (IsLock(lockFile))
			{
				await WaitForFileDeletion(lockFile).ConfigureAwait(false);
			}

			if (File.Exists(cacheFile))
			{
				try
				{
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
				var result = await compute().ConfigureAwait(false);

				try
				{
					using (var fs = File.OpenWrite(cacheFile))
					{
						await fs.WriteAsync(result, default).ConfigureAwait(false);
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
						// oh well i guess
					}
				}
				finally
				{
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

				return result;
			}

			goto RETRY; /*********** RETRY LOOP ************/
		}

		private ValueTask WaitForFileDeletion(string file)
		{
			var tcs = new TaskCompletionSource<byte>();
			var watcher = new FileSystemWatcher(Path.GetDirectoryName(file), Path.GetFileName(file));

			watcher.Deleted += (sender, e) =>
			{
				tcs.SetResult(default);
			};

			watcher.EnableRaisingEvents = true;

			// the lock might've dissapeared by the time we get down here and we would've never picked up on it
			if (!IsLock(file))
			{
				tcs.SetCanceled();
				watcher.Dispose();
				return new ValueTask();
			}

			return new ValueTask(tcs.Task);
		}
	}
}