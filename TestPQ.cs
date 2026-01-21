using System;
using System.Collections.Generic;
class Program {
  static void Main() {
    var pq = new PriorityQueue<int, int>();
    pq.Enqueue(1, 1);
    pq.Clear();
    Console.WriteLine("Cleared, Count: " + pq.Count);
  }
}
