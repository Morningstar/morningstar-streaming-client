using System.Threading.Channels;

namespace Morningstar.Streaming.Client.Services.Channels
{
    public class CountingChannel<T>
    {
        public Channel<T> Chnl { get; }
        //private int _count;
        public int Count => 0;

        public CountingChannel()
        {
            Chnl = Channel.CreateUnbounded<T>();
        }

        public bool TryWrite(T item)
        {
            var written = Chnl.Writer.TryWrite(item);
            //if (written) Interlocked.Increment(ref _count);
            return written;
        }

        public async ValueTask<T> ReadAsync(CancellationToken cancellationToken)
        {
            var item = await Chnl.Reader.ReadAsync(cancellationToken);
            //Interlocked.Decrement(ref _count);
            return item;
        }
    }
}
