using System;
using System.Threading;
using FileLogger;
using ThreadPool;

namespace lab6
{

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
            var taskQueue = new TaskQueue(6);
            for (int i = 0; i < 20; i++)
            {
                taskQueue.EnqueueTask(TestTask, i%5);
                logger.LogInfo("Поставлена задача номер: " + i);
            }
            Console.ReadLine();
            taskQueue.Close();
        }
    }
}
