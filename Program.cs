using System.Data;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace ADS_randomized
{
    internal class Program
    {
        static bool DEBUG = false;

        static int NUMBER_OF_PEOPLE_LOWER = 50;
        static int NUMBER_OF_PEOPLE_UPPER = 2000;
        static int NUMBER_OF_DAYS_LOWER = 10;
        static int NUMBER_OF_DAYS_UPPER = 100;
        static int NUMBER_OF_SEATS_LOWER = 0;
        static int NUMBER_OF_SEATS_UPPER = 200;
        static int HOTEL_PRICE_LOWER = 0;
        static int HOTEL_PRICE_UPPER = 750;
        static int SEAT_PRICE_LOWER = 0;
        static int SEAT_PRICE_UPPER = 1000;
        static int NUMBER_OF_TRIALS = 50000;
        static string RANDOM_DISTRIBUTION = "uniform"; // Possible values: "uniform", "normal"
        static Random random = new Random();

        static void Main(string[] args)
        {
            foreach (var variablePrice in new int[] { 500, 375, 250, 100, 75, 50, 25, 10, 0 }.Reverse())
            {
                HOTEL_PRICE_UPPER = variablePrice;
                // Make folder for future reference
                string folderPath = "./runs/" + DateTime.Now.ToString("MM-dd-HH-mm-ss-f");
                Directory.CreateDirectory(folderPath);
                string[] config = new string[]
                {
                    $"NUMBER_OF_PEOPLE_LOWER: {NUMBER_OF_PEOPLE_LOWER}",
                    $"NUMBER_OF_PEOPLE_UPPER: {NUMBER_OF_PEOPLE_UPPER}",
                    $"NUMBER_OF_DAYS_LOWER: {NUMBER_OF_DAYS_LOWER}",
                    $"NUMBER_OF_DAYS_UPPER: {NUMBER_OF_DAYS_UPPER}",
                    $"NUMBER_OF_SEATS_LOWER: {NUMBER_OF_SEATS_LOWER}",
                    $"NUMBER_OF_SEATS_UPPER: {NUMBER_OF_SEATS_UPPER}",
                    $"HOTEL_PRICE_LOWER: {HOTEL_PRICE_LOWER}",
                    $"HOTEL_PRICE_UPPER: {HOTEL_PRICE_UPPER}",
                    $"SEAT_PRICE_LOWER: {SEAT_PRICE_LOWER}",
                    $"SEAT_PRICE_UPPER: {SEAT_PRICE_UPPER}",
                    $"NUMBER_OF_TRIALS: {NUMBER_OF_TRIALS}",
                    $"RANDOM_DISTRIBUTION: {RANDOM_DISTRIBUTION}",
                };
                File.WriteAllLines(folderPath + "/config.yaml", config);
            
                // Store calculated prices
                List<string> dataCsv = new List<string>() { "Trial number;Offline price;Online price;Difference;pmax/pmin;competitive ratio;ratio of ratios"};
                // For immediate mean/stddev/confidence interval calculations
                List<double> WCRs = new List<double>();
                List<double> ratios = new List<double>();

                for (int i =0; i < NUMBER_OF_TRIALS; i++)
                {
                    if (i % (NUMBER_OF_TRIALS / 100) == 0)
                        Console.WriteLine($"Starting trial {i}");

                    // Generate testcase
                    var numOfPeople = GetRandomValue(NUMBER_OF_PEOPLE_LOWER, NUMBER_OF_PEOPLE_UPPER);
                    var numOfDays = GetRandomValue(NUMBER_OF_DAYS_LOWER, NUMBER_OF_DAYS_UPPER);
                    var days = Enumerable.Range(0, numOfDays).Select(_ =>
                    {
                        var seats = GetRandomValue(NUMBER_OF_SEATS_LOWER, NUMBER_OF_SEATS_UPPER);
                        var seatprice = GetRandomValue(SEAT_PRICE_LOWER, SEAT_PRICE_UPPER);
                        var hotelprice = GetRandomValue(HOTEL_PRICE_LOWER, HOTEL_PRICE_UPPER);
                        return new Day(seats, seatprice, hotelprice);
                    }).ToArray();

                    if (numOfPeople > days.Sum(day => day.Seats))
                    {
                        if (DEBUG)
                            Console.WriteLine($"Trial {i} rerun due to invalid configuration");
                        i--;
                        continue;
                    }

                    // Export testcase for future inspection
                    if (DEBUG)
                    {
                        List<string> lines = new List<string> { numOfPeople.ToString(), numOfDays.ToString() };
                        lines.AddRange(days.Select(x => x.ToString()));
                        File.WriteAllLines(folderPath + $"/trial_{i}", lines);
                    }


                    // Run algorithms
                    int offlinePrice = OfflineAlg(numOfPeople, days);
                    int onlinePrice = OnlineAlg(numOfPeople, days);

                    // Generate CSV of interesting data
                    int pmin = days.Min(day => day.PricePerSeat);  
                    int pmax = days.Max(day => day.PricePerSeat);
                    double cCompetitiveRatio = (double)onlinePrice / (double)offlinePrice;
                    double theoreticalMaxRatio = (double)pmax / (double)pmin;
                    double WCR = 
                        (pmin == 0) ? 
                        -1 : 
                        (theoreticalMaxRatio == 1) ? 
                        0 :
                        (cCompetitiveRatio - 1) / (theoreticalMaxRatio - 1);
                    dataCsv.Add($"{i};{offlinePrice};{onlinePrice};{onlinePrice - offlinePrice};{(theoreticalMaxRatio == double.PositiveInfinity ? "" : theoreticalMaxRatio)};{cCompetitiveRatio};{(WCR == -1 ? "": WCR)}");

                    if (WCR != -1) WCRs.Add(WCR);
                    if (!double.IsInfinity(cCompetitiveRatio) && !double.IsNaN(cCompetitiveRatio)) ratios.Add(cCompetitiveRatio);
                }

                File.WriteAllLines(folderPath + "/data.csv", dataCsv);

                double wcrMean = WCRs.Average();
                double wcrStdDev = WCRs.StandardDeviation();
                double wcrLowerConf = Math.Max(0, wcrMean - 1.96 * wcrStdDev);
                double wcrUpperConf = Math.Max(0, wcrMean + 1.96 * wcrStdDev);
                double ratioMean = ratios.Average();
                double ratioStdDev = ratios.StandardDeviation();
                double ratioLowerConf = Math.Max(0, ratioMean - 1.96 * ratioStdDev);
                double ratioUpperConf = Math.Max(0, ratioMean + 1.96 * ratioStdDev);

                var resultCsv = new string[]
                {
                    "Variable;Mean;Std dev;95% error size;95% error lower;95% upper",
                    $"WCR;{wcrMean};{wcrStdDev};{wcrStdDev * 1.96};{wcrLowerConf};{wcrUpperConf}",
                    $"Competitive ratio;{ratioMean};{ratioStdDev};{ratioStdDev * 1.96};{ratioLowerConf};{ratioUpperConf}",
                };

                File.WriteAllLines(folderPath + "/results.csv", resultCsv);
            }
        }

        static int GetRandomValue(int lower, int upper)
        {
            if (RANDOM_DISTRIBUTION == "uniform")
                return random.Next(lower, upper);
            else if (RANDOM_DISTRIBUTION == "normal")
            {
                // Allow for 3 sigma of normal distribution, otherwise fall back
                Normal normal = new Normal((lower + upper) / 2, (upper - lower) / 6);
                return Math.Min(Math.Max((int)normal.Sample(), lower), upper);
            }
            else throw new Exception("Not a valid distribution");
        }

        static int OfflineAlg(int numOfPeople, Day[] days)
        {
            int totalHotelCost = 0;
            int[] C = new int[days.Length];
            for (int i = 0; i < days.Length; i++)
            {
                C[i] = totalHotelCost + days[i].PricePerSeat;
                totalHotelCost += days[i].PricePerHotel;
            }
            var Q = days
                .Select((day, index) => (day, index))
                .OrderBy(((Day day, int index) x) => C[x.index])
                .ToList();
            var A = new int[days.Length];

            int nLeft = numOfPeople;
            while (nLeft > 0 && Q.Count > 0) {
                (var day, var index) = Q[0];
                Q.RemoveAt(0);
                A[index] = Math.Min(nLeft, day.Seats);
                nLeft -= day.Seats;
            }

            int totalPrice  = 0;
            for (int i = 0; i < A.Length; i++)
            {
                if (DEBUG)
                    Console.WriteLine($"{numOfPeople - A[i]}, {A[i]}");
                numOfPeople -= A[i];
                totalPrice += days[i].PricePerSeat * A[i];
                totalPrice += days[i].PricePerHotel * numOfPeople;
            }

            if (DEBUG)
                Console.WriteLine($"Total price: {totalPrice}");

            return totalPrice;
        }

        static int OnlineAlg(int numOfPeople, Day[] days) 
        {
            int numOfPeopleToGo = numOfPeople;
            int totalPrice = 0;
            for (int i = 0; i < days.Length; i++) 
            {
                Day day = days[i];
                int numberOfPeopleSent = numOfPeopleToGo - day.Seats >= 0 ? day.Seats : numOfPeopleToGo;

                numOfPeopleToGo -= numberOfPeopleSent;
                totalPrice += day.PricePerSeat * numberOfPeopleSent;
                totalPrice += day.PricePerHotel * numOfPeopleToGo;

                if (DEBUG && numberOfPeopleSent > 0)
                    Console.WriteLine($"{numberOfPeopleSent}, {numOfPeopleToGo}");
            }

            if (DEBUG)
                Console.WriteLine($"Total price: {totalPrice}");

            return totalPrice;
        }
    }



    internal class Day
    {
        public int Seats { get; set; }
        public int PricePerSeat { get; set; }
        public int PricePerHotel { get; set; }

        public Day(string s)
        {
            string[] input = s.Split(", ");
            Seats = int.Parse(input[0]);
            PricePerSeat = int.Parse(input[1]);
            PricePerHotel = int.Parse(input[2]);
        }

        public Day(int seats, int pricePerSeat, int pricePerHotel)
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