using System.Threading.Channels;

namespace Morningstar.Streaming.Client.Services.Channels
{
    public class CountingChannel<T>
    {
        public Channel<T> Chnl { get; }
        public int Count => 0;

        public CountingChannel()
        {
            Chnl = Channel.CreateUnbounded<T>();
        }

        public bool TryWrite(T item)
        {
            var written = Chnl.Writer.TryWrite(item);
            return written;
        }

        public async ValueTask<T> ReadAsync(CancellationToken cancellationToken)
        {
            var item = await Chnl.Reader.ReadAsync(cancellationToken);
            return item;
        }
    }
}
