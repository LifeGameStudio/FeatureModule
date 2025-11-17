namespace GameModule.QuestModule
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Hàng đợi xử lý signal theo batch, giới hạn số phần tử / lần Flush.
    /// Chỉ gọi onBatchCompleted một lần khi queue chuyển từ non-empty -> empty.
    /// </summary>
    public sealed class SignalBatchQueue<T>
    {
        private readonly Queue<T> queue;
        private readonly Action<T> handler;
        private readonly Action    onBatchCompleted;
        private readonly int       maxPerFlush;

        private bool wasProcessing;

        public int Count => this.queue.Count;

        public SignalBatchQueue(
            Action<T> handler,
            Action onBatchCompleted,
            int maxPerFlush = 32,
            int initialCapacity = 64)
        {
            this.handler          = handler ?? throw new ArgumentNullException(nameof(handler));
            this.onBatchCompleted = onBatchCompleted;
            this.maxPerFlush      = Math.Max(1, maxPerFlush);
            this.queue            = new Queue<T>(Math.Max(1, initialCapacity));
        }

        public void Enqueue(T signal)
        {
            this.queue.Enqueue(signal);
        }

        /// <summary>
        /// Xử lý tối đa maxPerFlush phần tử trong queue.
        /// Nếu trong quá trình đó queue trở nên rỗng và trước đó có processing,
        /// sẽ gọi onBatchCompleted đúng 1 lần.
        /// </summary>
        public void Flush()
        {
            if (this.queue.Count > 0)
            {
                this.wasProcessing = true;
            }

            var processed = 0;

            while (this.queue.Count > 0 && processed < this.maxPerFlush)
            {
                var signal = this.queue.Dequeue();
                this.handler(signal);
                processed++;
            }

            if (this.wasProcessing && this.queue.Count == 0)
            {
                this.wasProcessing = false;
                this.onBatchCompleted?.Invoke();
            }
        }

        public void Clear()
        {
            this.queue.Clear();
            this.wasProcessing = false;
        }
    }
}
