using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MmOneSimulation
{
    class Program
    {

        static void Main()
        {
            var simulator = new Simulator();
            simulator.Run();
        }
    }

    class Simulator
    {
        private bool _isServerBusy;
        private readonly EventCalendar _eventCalendar = new EventCalendar();
        private readonly Queue<Customer> _queue = new Queue<Customer>();

        private readonly Queue<double> _interarrivalTimes;
        private readonly Queue<double> _serviceTimes;
        private readonly List<Customer> _customersComplete = new List<Customer>();
        private Customer _customerBeingServed;
        private readonly List<Tuple<double, int>> _queueHistory = new List<Tuple<double, int>>();

        public Simulator()
        {
            List<double> listArrivals = File.ReadAllLines(@"Data1.txt").Select(double.Parse).ToList();
            // ignore the last row of data1
            listArrivals.RemoveAt(listArrivals.Count - 1);
            _interarrivalTimes = new Queue<double>(listArrivals);
            var listServices = File.ReadAllLines(@"Data2.txt").Select(double.Parse);
            _serviceTimes = new Queue<double>(listServices);
        }

        public void Run()
        {
            // per 1c - first arrival at time zero
            double time = 0;
            ProcessArrival(time);
            KeyValuePair<double, ScheduleKind> nextEvent = _eventCalendar.GetNext(time);
            while (!nextEvent.Equals(default(KeyValuePair<double, ScheduleKind>)))
            {
                switch (nextEvent.Value)
                {
                    case ScheduleKind.Arrival:
                        ProcessArrival(nextEvent.Key);
                        break;
                    case ScheduleKind.Departure:
                        ProcessDeparture(nextEvent.Key);
                        break;
                    case ScheduleKind.Start:
                        ProcessStart(time);
                        break;
                }
                time = nextEvent.Key;
                nextEvent = _eventCalendar.GetNext(time);
            }

            var queueTimes = new Dictionary<int, double>();
            for (int i = 0; i < _queueHistory.Count - 1; i++)
            {
                var timeSpan = _queueHistory[i + 1].Item1 - _queueHistory[i].Item1;
                var queueCount = _queueHistory[i].Item2;
                if (queueTimes.ContainsKey(queueCount))
                    queueTimes[queueCount] = queueTimes[queueCount] + timeSpan;
                else
                    queueTimes.Add(queueCount, timeSpan);
            }
            double totalTime = 0;
            double greaterThanTwoTime = 0;
            foreach (var qt in queueTimes)
            {
                Console.WriteLine($"Queue Count: {qt.Key}; Total Time: {qt.Value};");
                totalTime += qt.Value;
                if (qt.Key > 2)
                    greaterThanTwoTime += qt.Value;
            }
            double percentThreeOrMoreTime = greaterThanTwoTime / totalTime;
            double average = 0;
            foreach (var qt in queueTimes)
            {
                var a = qt.Value / totalTime;
                var b = a * qt.Key;
                average += b;
            }
            Console.WriteLine($"3a Average number of entities in the queue: {average}");
            Console.WriteLine($"3b % time that 3 or more customers in queue: {percentThreeOrMoreTime}");
            var serverIdle = queueTimes[0];
            Console.WriteLine($"3c server utilization: { (totalTime - serverIdle) / totalTime}");
            Console.ReadLine();
        }

        public void ProcessArrival(double arrivalTime)
        {
            if (_isServerBusy)
            {
                _queue.Enqueue(new Customer(arrivalTime));
                var numInQueue = _queue.Count;
                _queueHistory.Add(new Tuple<double, int>(arrivalTime, numInQueue));
            }
            else
            {
                _queue.Enqueue(new Customer(arrivalTime));
                // dont need to add to queue history here bc we do it in the start
                ProcessStart(arrivalTime);
            }
            if (_interarrivalTimes.Count == 0)
                return;
            var nextArrival = arrivalTime + _interarrivalTimes.Dequeue();
            _eventCalendar.Add(nextArrival, ScheduleKind.Arrival);
        }

        public void ProcessStart(double time)
        {
            if (_queue.Count == 0)
                return;
            _isServerBusy = true;
            _customerBeingServed = _queue.Dequeue();
            _customerBeingServed.StartTime = time;
            var numInQueue = _queue.Count;
            _queueHistory.Add(new Tuple<double, int>(time, numInQueue));
            var departureTime = time + _serviceTimes.Dequeue();
            _eventCalendar.Add(departureTime, ScheduleKind.Departure);
        }

        public void ProcessDeparture(double time)
        {
            _isServerBusy = false;
            _customerBeingServed.DepartureTime = time;
            _customersComplete.Add(_customerBeingServed);
            _customerBeingServed = null;
            ProcessStart(time);
        }
    }

    public enum ScheduleKind { Start, Departure, Arrival }

    class EventCalendar
    {
        private readonly SortedList<double, ScheduleKind> _eventCalendar = new SortedList<double, ScheduleKind>();

        public KeyValuePair<double, ScheduleKind> GetNext(double time)
        {
            var next = _eventCalendar.FirstOrDefault(ec => ec.Key > time);
            return next;
        }

        public KeyValuePair<double, ScheduleKind> GetNext(double time, ScheduleKind scheduleKind)
        {
            var next = _eventCalendar.FirstOrDefault(ec => ec.Key > time && ec.Value == scheduleKind);
            return next;
        }

        public void Add(double time, ScheduleKind scheduleKind)
        {
            _eventCalendar.Add(time, scheduleKind);
        }
    }

    class Customer
    {
        private readonly double _arrivalTime;
        public Customer(double arrivalTime)
        {
            _arrivalTime = arrivalTime;
        }
        public double StartTime { get; set; }
        public double DepartureTime { get; set; }
        public double ServiceTime => DepartureTime - StartTime;
        public double QueueTime => StartTime - _arrivalTime;
    }
}
