
using System;
using System.Threading;

namespace ConcurrentLab1
{
    internal static class Program
    {
        private const string ALBUM = "121297";
        private const int RUN_MS = 30_000;

        private static volatile bool Stop = false;

        // Zasób współdzielony: 1-elementowy bufor
        private static readonly object Gate = new object();
        private static int? Value = null;

        // ----- BUFOR: PUT -----
        private static void Put(int v)
        {
            lock (Gate)
            {
                while (Value is not null && !Stop) Monitor.Wait(Gate);
                if (Stop) return;
                Value = v;
                Monitor.PulseAll(Gate);
            }
        }

        // ----- BUFOR: TAKE -----
        private static int Take()
        {
            lock (Gate)
            {
                while (Value is null && !Stop) Monitor.Wait(Gate);
                if (Stop) throw new ThreadInterruptedException();
                int v = Value!.Value;
                Value = null;
                Monitor.PulseAll(Gate);
                return v;
            }
        }

        // ----- PRODUCENT -----
        private static void Producer(int min, int max)
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            try
            {
                while (!Stop)
                {
                    Put(rnd.Next(min, max + 1));
                    Thread.Sleep(rnd.Next(120, 300));
                }
            }
            catch (ThreadInterruptedException) { }
        }

        // ----- KONSUMENT -----
        private static void Consumer()
        {
            long sum = 0;
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            try
            {
                while (!Stop)
                {
                    sum += Take();
                    Thread.Sleep(rnd.Next(150, 350));
                }
            }
            catch (ThreadInterruptedException) { }
            finally
            {
                Console.WriteLine($"[{Thread.CurrentThread.Name}] SUMA = {sum}");
            }
        }

        // ----- MONITOR -----
        private static void Stats(Thread t1, Thread t2, Thread t3)
        {
            try
            {
                while (!Stop)
                {
                    int? snap;
                    lock (Gate) snap = Value;
                    Console.WriteLine("=== MONITOR ===");
                    Console.WriteLine($"Shared value: {(snap is null ? "null" : snap.ToString())}");
                    Console.WriteLine($"T1 [{t1.Name}] -> {t1.ThreadState}");
                    Console.WriteLine($"T2 [{t2.Name}] -> {t2.ThreadState}");
                    Console.WriteLine($"T3 [{t3.Name}] -> {t3.ThreadState}");
                    Console.WriteLine("===============");
                    Thread.Sleep(1000);
                }
            }
            catch (ThreadInterruptedException) { }
        }

        // ----- UTYLITY -----
        private static void WakeAll() { lock (Gate) Monitor.PulseAll(Gate); }
        private static void InterruptSafe(Thread t) { try { t.Interrupt(); } catch { } }

        // ----- MAIN -----
        private static void Main()
        {
            var t1 = new Thread(() => Producer(21, 37))     { Name = $"{ALBUM}#Writer#1" };
            var t2 = new Thread(() => Producer(1337, 4200)) { Name = $"{ALBUM}#Writer#2" };
            var t3 = new Thread(Consumer)                   { Name = $"{ALBUM}#Reader#1" };
            var t4 = new Thread(() => Stats(t1, t2, t3))    { Name = $"{ALBUM}#Stats", IsBackground = true };

            t1.Start(); t2.Start(); t3.Start(); t4.Start();

            Thread.Sleep(RUN_MS);
            Stop = true;
            WakeAll();
            InterruptSafe(t1); InterruptSafe(t2); InterruptSafe(t3); InterruptSafe(t4);
            t1.Join(); t2.Join(); t3.Join();
        }
    }
}
