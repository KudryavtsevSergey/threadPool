using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lab6
{

    public delegate void TaskDelegate();

    public class TaskQueue
    {
        private List<Task> tasksList;
        private Queue<TaskDelegate> tasks;
        private int activeTaskCount;
        private object closeObject = new object();
        private bool isClosed;
        private Task additionalTask;
        private Logger logger = Logger.getInstance();
        private int taskCount;
        private int maxTaskCount;
        private int MaxTaskCount
        {
            get
            {
                return maxTaskCount;
            }
            set
            {
                if (taskCount >= value)
                {
                    throw new ArgumentException("To many tasks.");
                }
                this.maxTaskCount = value;
            }
        }

        public TaskQueue(int maxTaskCount)
        {
            this.isClosed = false;
            this.taskCount = 3;
            this.MaxTaskCount = maxTaskCount;
            tasks = new Queue<TaskDelegate>();
            tasksList = new List<Task>();

            additionalTask = new Task(() => DoCreateThreads(), TaskCreationOptions.LongRunning);
            additionalTask.Start();
            Logger.getInstance().WriteLog("Дополнительный поток создан.");
        }

        private void DoCreateThreads()
        {
            while (!isClosed)
            {
                if (activeTaskCount < taskCount)
                {
                    Interlocked.Increment(ref activeTaskCount);
                    var task = new Task(() => DoThreadWork(), TaskCreationOptions.LongRunning);
                    tasksList.Add(task);
                    task.Start();
                    Logger.getInstance().WriteLog("Создан поток номер" + activeTaskCount);
                }
            }
            CloseTasks();
        }

        private void CloseTasks()
        {
            for (int i = 0; i < tasksList.Count; i++)
                DoEnqueueTask(null);

            lock (closeObject)
            {
                while (activeTaskCount > 0)
                    Monitor.Wait(closeObject);
            }

            foreach (Task task in tasksList)
                task.Wait();
        }

        public void Close()
        {
            isClosed = true;
            additionalTask.Wait();
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
                tasks.Enqueue(task);
                Monitor.Pulse(tasks);
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
                        task();
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
                Interlocked.Decrement(ref activeTaskCount);
                if (activeTaskCount == 0)
                    Monitor.Pulse(closeObject);
            }
        }
    }

    class Logger
    {
        private static Logger instance;
        private string test = "logger.log";

        private static object syncRoot = new Object();

        public static Logger getInstance()
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                        instance = new Logger();
                }
            }
            return instance;
        }

        public void WriteLog(string text)
        {
            using (StreamWriter writer = new StreamWriter(test, false))
            {
                writer.WriteLine(text);
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
            var taskQueue = new TaskQueue(4);
            for (int i = 0; i < 10; i++)
                taskQueue.EnqueueTask(TestTask);
            Console.ReadLine();
            taskQueue.Close();
        }
    }
}
