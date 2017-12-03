using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FileLogger;

namespace lab6
{

    public delegate void TaskDelegate();

    public class TaskQueue
    {
        private List<Task> threads;
        private Queue<TaskDelegate> tasks;
        private object closeObject = new object();
        private bool isClosed;
        private Task additionalThread;
        private Logger logger = Logger.getInstance();

        private int minThreadsCount = 3;
        private int maxThreadsCount;
        private int activeThreadsCount;
        private int MaxThreadsCount
        {
            get
            {
                return maxThreadsCount;
            }
            set
            {
                if (minThreadsCount >= value)
                {
                    throw new ArgumentException("To many tasks.");
                }
                this.maxThreadsCount = value;
            }
        }

        public TaskQueue(int maxTaskCount)
        {
            this.isClosed = false;
            this.MaxThreadsCount = maxTaskCount;
            tasks = new Queue<TaskDelegate>();
            threads = new List<Task>();

            additionalThread = new Task(() => DoCreateThreads(), TaskCreationOptions.LongRunning);
            additionalThread.Start();
            logger.LogInfo("Дополнительный поток создан.");
        }

        private void DoCreateThreads()
        {
            while (!isClosed)
            {
                if (activeThreadsCount < minThreadsCount)
                {
                    Interlocked.Increment(ref activeThreadsCount);
                    var task = new Task(() => DoThreadWork(), TaskCreationOptions.LongRunning);
                    threads.Add(task);
                    task.Start();
                    logger.LogInfo("Создан поток номер: " + activeThreadsCount);
                }
                else if(activeThreadsCount == minThreadsCount)
                {
                    logger.LogWarning("Достигнуто максимальное количество потоков.");
                    //Monitor.Wait();
                }
            }
            CloseTasks();
        }

        private void CloseTasks()
        {
            for (int i = 0; i < threads.Count; i++)
                DoEnqueueTask(null);

            lock (closeObject)
            {
                while (activeThreadsCount > 0)
                    Monitor.Wait(closeObject);
            }

            foreach (Task task in threads)
                task.Wait();
        }

        public void Close()
        {
            isClosed = true;
            additionalThread.Wait();
        }

        public void EnqueueTask(TaskDelegate task)
        {
            if (task != null)
                DoEnqueueTask(task);
            else
                throw new ArgumentNullException("task");
        }

        private void DoEnqueueTask(TaskDelegate task)
        {
            lock (tasks)
            {
                if(activeThreadsCount < maxThreadsCount)
                {
                    tasks.Enqueue(task);
                    Monitor.Pulse(tasks);
                }
                else
                {
                    logger.LogWarning("");
                }
            }
        }

        private TaskDelegate DequeueTask()
        {
            lock (tasks)
            {
                while (tasks.Count == 0)
                    Monitor.Wait(tasks);
                TaskDelegate t = tasks.Dequeue();
                return t;
            }
        }

        private void DoThreadWork()
        {
            TaskDelegate task;
            do
            {
                task = DequeueTask();
                try
                {
                    if (task != null)
                    {
                        task();
                        logger.LogInfo("Взята задача номер: 1");
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            } while (task != null);

            lock (closeObject)
            {
                Interlocked.Decrement(ref activeThreadsCount);
                if (activeThreadsCount == 0)
                    Monitor.Pulse(closeObject);
            }
        }
    }

    class Program
    {
        static int TaskCount = 0;

        static void TestTask()
        {
            try
            {

                var taskNumber = Interlocked.Increment(ref TaskCount);
                Console.WriteLine(taskNumber);
                Thread.Sleep(1000);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void Main(string[] args)
        {
            Logger logger = Logger.getInstance();
            var taskQueue = new TaskQueue(4);
            for (int i = 0; i < 10; i++)
            {
                taskQueue.EnqueueTask(TestTask);
                logger.LogInfo("Поставлена задача номер: " + i);
            }
            Console.ReadLine();
            taskQueue.Close();
        }
    }
}
