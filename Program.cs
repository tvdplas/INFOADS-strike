namespace ADS_randomized
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var numOfPeople = int.Parse(Console.ReadLine());
            var numOfDays = int.Parse(Console.ReadLine());
            var days = Enumerable.Range(0, numOfDays).Select(_ =>
            {
                return new Day(Console.ReadLine());
            }).ToArray();

            // Run algorithms
            Console.WriteLine("########### Offline ###########");
            OfflineAlg(numOfPeople, days);
            Console.WriteLine("########### Online ############");
            OnlineAlg(numOfPeople, days);
        }

        static void OfflineAlg(int numOfPeople, Day[] days)
        {
            // Determine total cost per day
            double totalHotelCost = 0;
            double[] C = new double[days.Length];
            for (int i = 0; i < days.Length; i++)
            {
                C[i] = totalHotelCost + days[i].PricePerSeat;
                totalHotelCost += days[i].PricePerHotel;
            }

            // Order days by total cost to send home
            var Q = days
                .Select((day, index) => (day, index))
                .OrderBy(((Day day, int index) x) => C[x.index])
                .ToList();
            var A = new int[days.Length];

            // Determine assignments for each day by greedily using the cheapest days
            int nLeft = numOfPeople;
            while (nLeft > 0 && Q.Count > 0)
            {
                (var day, var index) = Q[0];
                Q.RemoveAt(0);
                A[index] = Math.Min(nLeft, day.Seats);
                nLeft -= day.Seats;
            }

            // Output all assignments ordered by original day ordering
            double totalPrice = 0;
            for (int i = 0; i < A.Length; i++)
            {
                Console.WriteLine($"{numOfPeople - A[i]}, {A[i]}");
                numOfPeople -= A[i];
                totalPrice += days[i].PricePerSeat * A[i];
                totalPrice += days[i].PricePerHotel * numOfPeople;
            }

            Console.WriteLine($"Total price: {totalPrice}");
        }

        static void OnlineAlg(int numOfPeople, Day[] days)
        {
            int numOfPeopleToGo = numOfPeople;
            double totalPrice = 0;

            // Greedily assign people to each following day
            for (int i = 0; i < days.Length; i++)
            {
                Day day = days[i];
                int numberOfPeopleSent = numOfPeopleToGo - day.Seats >= 0 ? day.Seats : numOfPeopleToGo;

                numOfPeopleToGo -= numberOfPeopleSent;
                totalPrice += day.PricePerSeat * numberOfPeopleSent;
                totalPrice += day.PricePerHotel * numOfPeopleToGo;

                Console.WriteLine($"{numberOfPeopleSent}, {numOfPeopleToGo}");
            }

            Console.WriteLine($"Total price: {totalPrice}");
        }
    }



    internal class Day
    {
        public int Seats { get; set; }
        public double PricePerSeat { get; set; }
        public double PricePerHotel { get; set; }

        public Day(string s)
        {
            string[] input = s.Split(", ");
            Seats = int.Parse(input[0]);
            PricePerSeat = double.Parse(input[1]);
            PricePerHotel = double.Parse(input[2]);
        }

        public Day(int seats, double pricePerSeat, double pricePerHotel)
        {
            Seats = seats;
            PricePerSeat = pricePerSeat;
            PricePerHotel = pricePerHotel;
        }

        public override string ToString()
        {
            return $"{Seats}, {PricePerSeat}, {PricePerHotel}";
        }
    }
}