namespace GameModule.QuestModule
{
    using System;
    using System.Collections.Generic;

    public class SignalBatchQueue<T>
    {
        private readonly Queue<T>  queue = new();
        private readonly Action<T> handler;
        private readonly int       maxPerFrame;

        public SignalBatchQueue(Action<T> handler, int maxPerFrame = int.MaxValue)
        {
            this.handler     = handler ?? throw new ArgumentNullException(nameof(handler));
            this.maxPerFrame = maxPerFrame;
        }

        public void Enqueue(T signal) { this.queue.Enqueue(signal); }

        public void Flush()
        {
            var count = 0;

            while (this.queue.Count > 0 && count++ < this.maxPerFrame)
            {
                var signal = this.queue.Dequeue();
                this.handler(signal);
            }
        }

        public void Clear() { this.queue.Clear(); }

        public int Count => this.queue.Count;
    }
}