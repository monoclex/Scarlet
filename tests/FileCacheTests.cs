using FluentAssertions;

using Scarlet.Api;

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Scarlet.Tests
{
	public sealed class FileCacheTests : IDisposable
	{
		private readonly FileCache _cache;

		public FileCacheTests() => _cache = new FileCache(Guid.NewGuid().ToString());

		public void Dispose()
		{
			var directory = _cache.CacheDirectory;

			Directory.Delete(directory.FullName, true);
		}

		#region WhenLockIsPresent_CacheWaitsUntilLockIsGone_ToReadCachedData

		[Fact]
		public async Task WhenLockIsPresent_CacheWaitsUntilLockIsGone_ToReadCachedData()
		{
			const string key = "test";

			// taken from FileCache implementation
			var fileName = Convert.ToBase64String(Encoding.ASCII.GetBytes(key));
			var fileFullPath = Path.Combine(_cache.CacheDirectory.FullName, fileName);
			var lockFullPath = fileFullPath + ".lock";

			using (var writer = File.CreateText(lockFullPath))
			{
			}

			using (var writer = File.CreateText(fileFullPath))
			{
				writer.WriteLine("Hello, World!");
			}

			var taskStarted = false;
			var taskStopped = false;
			Memory<byte> result = default;

			// we have created a lock file - let's start up a request for the cache entry
			var task = Task.Run(async () =>
			{
				taskStarted = true;
				result = await _cache.TryRead(key, async () => throw new Exception()).ConfigureAwait(false);
				taskStopped = true;
			});

			// wait until the method in the task is ran
			SpinWait.SpinUntil(() => taskStarted);

			// we can assume the cache is waiting for the file to exist now, we
			// wait a second to let it compute whatever
			await Task.Delay(1000).ConfigureAwait(false);

			// it shouldn't've stopped yet because the lock still exists
			taskStopped.Should().BeFalse();

			// delete the lock file
			File.Delete(lockFullPath);

			// now that the lock is gone, the task should stop
			SpinWait.SpinUntil(() => taskStopped);

			// make sure the result has "Hello, World!"
			var bytes = result.ToArray();
			bytes.Should().BeEquivalentTo(Encoding.ASCII.GetBytes("Hello, World!" + Environment.NewLine));
		}

		#endregion WhenLockIsPresent_CacheWaitsUntilLockIsGone_ToReadCachedData

		#region WhenCacheCreated_CacheDirectoryIsCreated_IfNotExist

		private string GetDirectoryThatDoesntExist()
		{
			string name = null;

			do
			{
				name = Guid.NewGuid().ToString();
			}
			while (Directory.Exists(name));

			return name;
		}

		[Fact]
		public void WhenCacheCreated_CacheDirectoryIsCreated_IfNotExist()
		{
			string directory = GetDirectoryThatDoesntExist();

			// at this point, the directory the cache will use must not exist
			Directory.Exists(directory).Should().BeFalse();

			var cache = new FileCache(directory);

			// after the cache has been created, if the directory previously did not exist, it should now
			Directory.Exists(directory).Should().BeTrue();
		}

		#endregion WhenCacheCreated_CacheDirectoryIsCreated_IfNotExist

		#region WhenEntryRequested_ThenAComputationOccurs_OnlyOnce

		private int _counter;

		private async ValueTask<Api.Misc.OwnedMemory> Compute()
		{
			Interlocked.Increment(ref _counter);
			await Task.Delay(10).ConfigureAwait(false);
			return new byte[100];
		}

		[Fact]
		public async Task WhenEntryRequested_ThenAComputationOccurs_OnlyOnce()
		{
			_counter.Should().Be(0);

			var result = await _cache.TryRead("key", Compute).ConfigureAwait(false);

			// counter gets incremented because `Compute` was executed
			// now the result should be cached, and the next TryRead should
			// result in a read from the cache
			_counter.Should().Be(1);

			var result2 = await _cache.TryRead("key", Compute).ConfigureAwait(false);

			// counter gets incremented only if `Compute` gets executed
			// `Compute` should not be executed, because it was already executed
			// and the result should be in the cache.
			_counter.Should().Be(1);

			// asserting that both results are the same
			result.Length.Should().Be(result2.Length);
		}

		#endregion WhenEntryRequested_ThenAComputationOccurs_OnlyOnce
	}
}