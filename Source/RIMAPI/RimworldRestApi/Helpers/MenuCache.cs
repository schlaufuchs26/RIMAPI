using System;

namespace RIMAPI.Helpers
{
    /// <summary>
    /// One expensive value, built on first use and kept until it is dropped
    /// (ticket #3946): the dev-action menu holds 60,456 entries and costs about
    /// 8 s to walk, and a filter that matched little paid that again on the next
    /// call. The owner calls Invalidate when what the value was built from
    /// changed, so the next read builds it again.
    ///
    /// This file stays free of game types so the test project can compile it.
    /// </summary>
    public sealed class MenuCache<T>
        where T : class
    {
        private readonly object sync = new object();
        private T value;

        /// <summary>How often the builder ran; the tests read it, the mod does not.</summary>
        public int Builds { get; private set; }

        /// <summary>True while a value is held, so the next Get returns it.</summary>
        public bool Holds
        {
            get
            {
                lock (sync)
                {
                    return value != null;
                }
            }
        }

        /// <summary>
        /// The held value, or the builder's result on the first call after a
        /// drop. A caller that arrives while the build runs waits for it
        /// instead of starting a second walk.
        /// </summary>
        public T Get(Func<T> build)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            lock (sync)
            {
                if (value == null)
                {
                    value = build();
                    Builds++;
                }
                return value;
            }
        }

        /// <summary>Drops the value, so the next Get builds it again.</summary>
        public void Invalidate()
        {
            lock (sync)
            {
                value = null;
            }
        }
    }
}
