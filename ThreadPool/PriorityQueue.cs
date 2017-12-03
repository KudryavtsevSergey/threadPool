using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileLogger;

namespace ThreadPool
{
    class PriorityQueue<P, V>
    {
        private SortedDictionary<P, Queue<V>> list = new SortedDictionary<P, Queue<V>>();

        Logger logger = Logger.getInstance();

        public void Enqueue(P priority, V value)
        {
            Queue<V> q;
            if (!list.TryGetValue(priority, out q))
            {
                q = new Queue<V>();
                list.Add(priority, q);
            }
            q.Enqueue(value);
        }

        public V Dequeue()
        {
            var pair = list.First();
            var v = pair.Value.Dequeue();
            if (pair.Value.Count == 0)
                list.Remove(pair.Key);
            logger.LogInfo("Взята задача с приоритетом: " + pair.Key);
            return v;
        }

        public int Count { get { return list.Count();  } private set { } }
    }
}
