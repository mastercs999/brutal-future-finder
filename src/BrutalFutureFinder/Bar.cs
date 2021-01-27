using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrutalFutureFinder
{
    public class Bar
    {
        public DateTime DateTimeFrom;
        public DateTime DateTimeTo;

        //public double Open { get; set; }
        //public double High { get; set; }
        //public double Low { get; set; }
        //public double Close { get; set; }
        //public double Volume { get; set; }

        public double AdjustedOpen { get; set; }
        public double AdjustedHigh { get; set; }
        public double AdjustedLow { get; set; }
        public double AdjustedClose { get; set; }
        public double AdjustedVolume { get; set; }

        public double AdjustedOpenGain { get; set; }
        public double AdjustedHighGain { get; set; }
        public double AdjustedLowGain { get; set; }
        public double AdjustedCloseGain { get; set; }

        public static List<Bar> LoadBarsFromFile(string filePath)
        {
            List<Bar> bars = new List<Bar>();

            // Load all lines
            string[] lines = File.ReadAllLines(filePath).Skip(1).ToArray();

            // Parse data
            int back = 1;
            for (int i = 0; i < lines.Length; ++i)
            {
                // Get fields
                string[] fields = lines[i].Split(new char[] { ',' });

                // Parse fields
                DateTime date = DateTime.ParseExact(fields[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                //double open = double.Parse(fields[1], CultureInfo.InvariantCulture);
                //double high = double.Parse(fields[2], CultureInfo.InvariantCulture);
                //double low = double.Parse(fields[3], CultureInfo.InvariantCulture);
                //double close = double.Parse(fields[4], CultureInfo.InvariantCulture);
                //double volume = double.Parse(fields[5], CultureInfo.InvariantCulture);

                // Adjust rest of the data
                double adjustedOpen = double.Parse(fields[6], CultureInfo.InvariantCulture);
                double adjustedHigh = double.Parse(fields[7], CultureInfo.InvariantCulture);
                double adjustedLow = double.Parse(fields[8], CultureInfo.InvariantCulture);
                double adjustedClose = double.Parse(fields[9], CultureInfo.InvariantCulture);
                double adjustedVolume = double.Parse(fields[10], CultureInfo.InvariantCulture);

                // Parse
                Bar b = new Bar()
                {
                    DateTimeFrom = date,
                    DateTimeTo = date.AddDays(1),

                    //Open = open,
                    //High = high,
                    //Low = low,
                    //Close = close,
                    //Volume = volume,

                    AdjustedOpen = adjustedOpen,
                    AdjustedHigh = adjustedHigh,
                    AdjustedLow = adjustedLow,
                    AdjustedClose = adjustedClose,
                    AdjustedVolume = adjustedVolume,

                    AdjustedOpenGain = bars.Count < back ? 0 : (adjustedOpen - bars[i - back].AdjustedOpen) / bars[i - back].AdjustedOpen,
                    AdjustedHighGain = bars.Count < back ? 0 : (adjustedHigh - bars[i - back].AdjustedHigh) / bars[i - back].AdjustedHigh,
                    AdjustedLowGain = bars.Count < back ? 0 : (adjustedLow - bars[i - back].AdjustedLow) / bars[i - back].AdjustedLow,
                    AdjustedCloseGain = bars.Count < back ? 0 : (adjustedClose - bars[i - back].AdjustedClose) / bars[i - back].AdjustedClose,
                };

                bars.Add(b);
            }

            return bars;
        }
        public static List<Bar> LoadAllFromFromDirectory(string path)
        {
            List<Bar> bars = new List<Bar>();

            // Nacteme vsechno ze souboru
            string[] files = Directory.GetFiles(path);
            foreach (string p in files)
                bars.AddRange(LoadBarsFromFile(p));

            // Nacteme dalsi podslozky
            string[] directories = Directory.GetDirectories(path);
            foreach (string d in directories)
                bars.AddRange(LoadAllFromFromDirectory(d));

            return bars;
        }
        private static DateTime GetDateTime(string line)
        {
            string[] parts = line.Split(new char[] { '.', ',', ':' });

            int year = int.Parse(parts[0]);
            int month = int.Parse(parts[1]);
            int day = int.Parse(parts[2]);
            int hour = int.Parse(parts[3]);
            int minute = int.Parse(parts[4]);

            return new DateTime(year, month, day, hour, minute, 0);
        }
    }
}
