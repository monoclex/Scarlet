using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/*
 * MessagePipeline is a small utility to make expecting a certain type of
 * message after receiving a request easier and cleaner.
 */

namespace MessagePipeline
{
	public delegate ValueTask PipelineMessageEvent<TMessage>(TMessage message);

	/// <summary>
	/// Represents a callback for subscriptions to a pipeline.
	/// </summary>
	/// <returns>True if the subscription should stay alive, false if it should not.</returns>
	public delegate ValueTask<bool> SubscriptionPipeline<TMessage>(TMessage message);

	public class MessagePipeline<TMessage>
	{
		public event PipelineMessageEvent<TMessage> Pipe;

		public ValueTask FillPipe(TMessage message)
			=> Pipe?.Invoke(message) ?? new ValueTask();

		public void Subscribe(SubscriptionPipeline<TMessage> receiver)
		{
			async ValueTask Subscription(TMessage message)
			{
				var result = await receiver(message).ConfigureAwait(false);

				if (result == false)
				{
					Pipe -= Subscription;
				}
			}

			Pipe += Subscription;
		}
	}

	public static class PipelineExtensions
	{
		public static Task<TMessage> Expect<TMessage>(this MessagePipeline<TMessage> pipeline, Predicate<TMessage> filter, TimeSpan timeSpan)
			=> Expect(pipeline, filter, new CancellationTokenSource(timeSpan).Token);

		public static Task<TMessage> Expect<TMessage>(this MessagePipeline<TMessage> pipeline, Predicate<TMessage> filter, CancellationToken cancellationToken = default)
		{
			var tcs = new TaskCompletionSource<TMessage>();

			pipeline.Subscribe(message =>
			{
				if (filter(message))
				{
					tcs.SetResult(message);
					return new ValueTask<bool>(false);
				}

				// stay subscribed until we receive the message
				return new ValueTask<bool>(true);
			});

			// cancel the TCS if the cancellation token expires
			cancellationToken.Register(tcs.SetCanceled, false);

			return tcs.Task;
		}
	}
}
