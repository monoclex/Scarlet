using PlayerIOClient;

using System;
using System.Collections.Generic;

/*
 * This file is a file that exists to convert PlayerIOClient.DatabaseObject and
 * PlayerIOClient.DatabaseArray classes to a representation that will actually
 * be serialized by System.Text.Json properly.
 *
 * Performance is not a primary concern, just the transformation of PIO -> custom.
 */

namespace Scarlet
{
	public class DatabaseObjectEntry
	{
		public DatabaseObjectEntry(string table, string key, Dictionary<string, object> data)
		{
			Table = table;
			Key = key;
			Data = data;
		}

		public string Table { get; }
		public string Key { get; }
		public Dictionary<string, object> Data { get; }
	}

	public static class DatabaseConverter
	{
		public static DatabaseObjectEntry ToObjectEntry(this DatabaseObject databaseObject)
			=> new DatabaseObjectEntry(databaseObject.Table, databaseObject.Key, ToDictionary(databaseObject));

		public static Dictionary<string, object> ToDictionary(this DatabaseObject databaseObject)
		{
			var dictionary = new Dictionary<string, object>(databaseObject.Count);

			foreach (var (key, value) in databaseObject)
			{
				dictionary[key] = Convert(value);
			}

			return dictionary;
		}

		public static object[] ToArray(this DatabaseArray databaseArray)
		{
			var array = new object[databaseArray.Count];

			for (var i = 0; i < databaseArray.Count; i++)
			{
				array[i] = Convert(databaseArray.GetValue(i));
			}

			return array;
		}

		public static object Convert(object playerIOData)
			=> playerIOData switch
			{
				DatabaseArray databaseArray => databaseArray.ToArray(),
				DatabaseObject databaseObject => databaseObject.ToDictionary(),
				// explicitly make sure that the right types are supported
				DateTime dateTime => dateTime,
				byte[] bytes => bytes,
				double @double => @double,
				bool @bool => @bool,
				long @long => @long,
				uint @uint => @uint,
				int @int => @int,
				float @float => @float,
				string @string => @string,
				_ => throw new InvalidOperationException(),
			};
	}
}