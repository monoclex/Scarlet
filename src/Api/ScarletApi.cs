using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Scarlet.Api.Game;

namespace Scarlet.Api
{
	/// <summary>
	/// Represents the main way of accessing the Scarlet API, internally.
	/// </summary>
	public class ScarletApi
	{
		// TODO: fill with stuff
		private readonly FileCache _cache;
		private readonly IScarletGameApi _everybodyEdits;
		private readonly IScarletGameApi _everybodyEditsUniverse;

		public ScarletApi
		(
			FileCache cache,
			IScarletGameApi everybodyEdits,
			IScarletGameApi everybodyEditsUniverse
		)
		{
			_cache = cache;
			_everybodyEdits = everybodyEdits;
			_everybodyEditsUniverse = everybodyEditsUniverse;
		}

		// TODO: figure out a clean way to take the results of an
		// IScarletGameApi and store them effectively and retrieve them.
		public ValueTask<Memory<byte>> EEMinimap(string worldId, int scale = 1)
		{
			return _cache.TryRead($"ee.[{worldId}].minimap.[{scale}]", async () =>
			{
				var world = await GetWorld($"ee.[{worldId}]", _everybodyEdits, worldId).ConfigureAwait(false);

				world.SerializeRequest.Scale = scale;
				return PngSerializer.Serialize(world.SerializeRequest);
			});
		}

		public ValueTask<Memory<byte>> EEMetadata(string worldId)
		{
			return _cache.TryRead($"ee.[{worldId}].metadata", async () =>
			{
				var world = await GetWorld($"ee.[{worldId}]", _everybodyEdits, worldId).ConfigureAwait(false);
				return world.Metadata;
			});
		}

		public void EEUpdate(string worldId)
		{
			_cache.Age($"ee.[{worldId}]");
			_cache.Age($"ee.[{worldId}].metadata");

			foreach (var i in Config.EnumerateScale)
			{
				_cache.Age($"ee.[{worldId}].minimap.[{i}]");
			}
		}

		public ValueTask<Memory<byte>> EEUMinimap(string worldId, int scale = 1)
		{
			return _cache.TryRead($"eeu.[{worldId}].minimap.[{scale}]", async () =>
			{
				var world = await GetWorld($"eeu.[{worldId}]", _everybodyEditsUniverse, worldId).ConfigureAwait(false);

				world.SerializeRequest.Scale = scale;
				return PngSerializer.Serialize(world.SerializeRequest);
			});
		}

		public ValueTask<Memory<byte>> EEUMetadata(string worldId)
		{
			return _cache.TryRead($"eeu.[{worldId}].metadata", async () =>
			{
				var world = await GetWorld($"eeu.[{worldId}]", _everybodyEditsUniverse, worldId).ConfigureAwait(false);
				return world.Metadata;
			});
		}

		public void EEUUpdate(string worldId)
		{
			_cache.Age($"eeu.[{worldId}]");
			_cache.Age($"eeu.[{worldId}].metadata");

			foreach (var i in Config.EnumerateScale)
			{
				_cache.Age($"eeu.[{worldId}].minimap.[{i}]");
			}
		}

		private async ValueTask<ScarletGameWorld> GetWorld(string cacheKey, IScarletGameApi gameApi, string worldId)
		{
			var result = await _cache.TryRead(cacheKey, async () =>
			{
				// TODO: seperate out serialization logic from ScarletApi
				var world = await gameApi.World(worldId).ConfigureAwait(false);

				var blocksSize = sizeof(ushort) * world.SerializeRequest.Blocks.Length;
				var rgba32Size = Rgba32.Size * world.SerializeRequest.Palette.Length;

				var targetSize =
					sizeof(int) // width
					+ sizeof(int) // height
					+ sizeof(int) // blocks.Length
					+ sizeof(int) // palette.Length
					+ sizeof(int) // scale
					+ sizeof(int) // metadata.Length
					+ blocksSize // blocks
					+ rgba32Size // palette colors
					+ world.Metadata.Length // metadata
					;

				Memory<byte> targetMemory = new byte[targetSize];
				WriteTo(targetMemory);
				return targetMemory;

				void WriteTo(Memory<byte> targetMemory)
				{
					var target = targetMemory.Span;

					BinaryPrimitives.WriteInt32LittleEndian(target, world.SerializeRequest.Width);
					BinaryPrimitives.WriteInt32LittleEndian(target.Slice(4, 4), world.SerializeRequest.Height);
					BinaryPrimitives.WriteInt32LittleEndian(target.Slice(8, 4), world.SerializeRequest.Blocks.Length);
					BinaryPrimitives.WriteInt32LittleEndian(target.Slice(12, 4), world.SerializeRequest.Palette.Length);
					BinaryPrimitives.WriteInt32LittleEndian(target.Slice(16, 4), world.SerializeRequest.Scale);
					BinaryPrimitives.WriteInt32LittleEndian(target.Slice(20, 4), world.Metadata.Length);

					// https://adamstorr.azurewebsites.net/blog/span-t-byte-int-conversions
					// -> https://github.com/dotnet/runtime/issues/24689
					MemoryMarshal.Cast<ushort, byte>(world.SerializeRequest.Blocks.Span) // Span<ushort> directly to Span<byte>
						.CopyTo(target.Slice(24, blocksSize));

					MemoryMarshal.Cast<Rgba32, byte>(world.SerializeRequest.Palette.Span)
						.CopyTo(target.Slice(24 + blocksSize, rgba32Size));

					world.Metadata.Span.CopyTo(target.Slice(24 + blocksSize + rgba32Size, world.Metadata.Length));
				}
			}).ConfigureAwait(false);

			return Deserialize(result.Span);

			static ScarletGameWorld Deserialize(Span<byte> result)
			{
				Memory<ushort> blocks = new ushort[BinaryPrimitives.ReadInt32LittleEndian(result.Slice(8, 4))];
				Memory<Rgba32> rgba32 = new Rgba32[BinaryPrimitives.ReadInt32LittleEndian(result.Slice(12, 4))];
				Memory<byte> metadata = new byte[BinaryPrimitives.ReadInt32LittleEndian(result.Slice(20, 4))];

				var scarletGameWorld = new ScarletGameWorld
				{
					SerializeRequest = new PngSerializer.SerializeWorldRequest
					{
						Width = BinaryPrimitives.ReadInt32LittleEndian(result.Slice(0, 4)),
						Height = BinaryPrimitives.ReadInt32LittleEndian(result.Slice(4, 4)),
						Blocks = blocks,
						Palette = rgba32,
						Scale = BinaryPrimitives.ReadInt32LittleEndian(result.Slice(16, 4)),
					},
					Metadata = metadata
				};

				MemoryMarshal.Cast<byte, ushort>(result.Slice(24, blocks.Length * 2))
					.CopyTo(blocks.Span);

				MemoryMarshal.Cast<byte, Rgba32>(result.Slice(24 + blocks.Length * 2, rgba32.Length * 4))
					.CopyTo(rgba32.Span);

				result.Slice(24 + blocks.Length * 2 + rgba32.Length * 4, metadata.Length)
					.CopyTo(metadata.Span);

				return scarletGameWorld;
			}
		}
	}
}
