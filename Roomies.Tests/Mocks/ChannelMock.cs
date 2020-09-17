using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Roomies.Tests.Mocks
{
    public class ChannelMock<T> : Channel<T>
    {
        public List<T> Items => ((ChannelWriterMock<T>)Writer).Items;

        public ChannelMock(ChannelWriterMock<T> channelWriter) => Writer = channelWriter;
    }

    public class ChannelWriterMock<T> : ChannelWriter<T>
    {
        public List<T> Items = new List<T>();

        public override ValueTask WriteAsync(T item, CancellationToken cancellationToken = default)
        {
            Items.Add(item);
            return new ValueTask();
        }

        public override bool TryWrite(T item)
        {
            Items.Add(item);
            return true;
        }

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default) => new ValueTask<bool>(true);
    }
}
