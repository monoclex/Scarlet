using FluentAssertions;
using Scarlet.Api.Caching;
using System;
using System.IO;
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
		#endregion

		#region WhenEntryRequested_ThenAComputationOccurs_OnlyOnce
		private int _counter;

		private async ValueTask<Memory<byte>> Compute()
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
		#endregion
	}
}
