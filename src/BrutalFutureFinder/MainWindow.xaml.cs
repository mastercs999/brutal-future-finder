using NDtw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WinForms = System.Windows.Forms; 

namespace BrutalFutureFinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int SamplesCount => int.Parse(PocetVzorku.Text);
        public int FutureSize => int.Parse(FutureSize_Text.Text);

        private readonly double[] RatioOHLC = new double[] { 0.25, 0.25, 0.25, 0.25 };

        private string FileFilePath = @"C:\Users\H190317\Desktop\BFF Data\INTC.csv";
        private string DataFilePath = @"C:\Users\H190317\Desktop\BFF Data\";
        private List<Bar> Bars;
        private List<Bar> AllBars;
        private List<Result> Results;
        private Size CanvasSize;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Compute()
        {
            // Zobrazime cekaci mys
            Mouse.OverrideCursor = Cursors.Wait;

            // Spocitame si odkud pokud budeme brat zdrojova data
            int from = int.Parse(Vzorek.Text) - int.Parse(PocetVzorku.Text);
            int count = int.Parse(PocetVzorku.Text);

            // List dat z pocitaneho obdobi
            Bar[] toCount = Bars.Skip(from).Take(count).ToArray();

            // Vybereme data k porovnani
            DateTime firstDate = toCount.FirstOrDefault().DateTimeFrom;
            Bar[] toCompare = AllBars.Where(x => x.DateTimeTo <= firstDate).ToArray();

            // Vypocitame nejlepsi
            List<Result> bestResults = null;
            if (DisplayOHLC.IsChecked == true)
                bestResults = FindBest(toCount, toCompare);
            else
            {
                double[] source = null;
                double[] compare = null;
                if (DisplayOpen.IsChecked == true)
                {
                    source = toCount.Select(x => x.AdjustedOpen).ToArray();
                    compare = toCompare.Select(x => x.AdjustedOpen).ToArray();
                }
                else if (DisplayHigh.IsChecked == true)
                {
                    source = toCount.Select(x => x.AdjustedHigh).ToArray();
                    compare = toCompare.Select(x => x.AdjustedHigh).ToArray();
                }
                else if (DisplayLow.IsChecked == true)
                {
                    source = toCount.Select(x => x.AdjustedLow).ToArray();
                    compare = toCompare.Select(x => x.AdjustedLow).ToArray();
                }
                else
                {
                    source = toCount.Select(x => x.AdjustedClose).ToArray();
                    compare = toCompare.Select(x => x.AdjustedClose).ToArray();
                }

                bestResults = FindBest(source, compare);
            }

            // Seradime vysledky
            Results = bestResults.OrderByDescending(x => x.Corellation).ToList();

            // Uz necekame
            Mouse.OverrideCursor = null;

            // Prekreslime
            Redraw();        
        }
        public List<Result> FindBest(double[] source, double[] compareData)
        {
            List<Result> results = new List<Result>();

            // Vypocitame prvni
            double corellation = PearsonCorrelation(source, compareData, 0, source.Length);
            Result res = new Result(0, corellation);
            results.Add(res);

            Result worst = res;
            for (int i = 1; i < compareData.Length - source.Length; ++i)
            {
                // Vypocitame korelaci
                corellation = PearsonCorrelation(source, compareData, i, source.Length);

                // Pokud je vypocitana korelace lepsi nez nejhorsi v best N
                if (corellation >= worst.Corellation)
                {
                    // Pokud je list plny, tak odstranime nejhorsi
                    if (results.Count == 5)
                        results.Remove(worst);

                    // Pridame lepsiho
                    results.Add(new Result(i, corellation));

                    // Najdeme nejhorsiho
                    worst = results.OrderBy(x => x.Corellation).FirstOrDefault();
                }
            }

            // Vratime vysledek
            return results;
        }
        public List<Result> FindBest(Bar[] source, Bar[] compareData)
        {
            // Roztridime data
            double[] sourceOpen = source.Select(x => x.AdjustedOpenGain).ToArray();
            double[] sourceHigh = source.Select(x => x.AdjustedHighGain).ToArray();
            double[] sourceLow = source.Select(x => x.AdjustedLowGain).ToArray();
            double[] sourceClose = source.Select(x => x.AdjustedCloseGain).ToArray();

            double[] compareDataOpen = compareData.Select(x => x.AdjustedOpenGain).ToArray();
            double[] compareDataHigh = compareData.Select(x => x.AdjustedHighGain).ToArray();
            double[] compareDataLow = compareData.Select(x => x.AdjustedLowGain).ToArray();
            double[] compareDataClose = compareData.Select(x => x.AdjustedCloseGain).ToArray();

            List<Result> results = new List<Result>();

            // Vypocitame prvni
            double corellationOpen = PearsonCorrelation(sourceOpen, compareDataOpen, 0, source.Length);
            double corellationHigh = PearsonCorrelation(sourceHigh, compareDataHigh, 0, source.Length);
            double corellationLow = PearsonCorrelation(sourceLow, compareDataLow, 0, source.Length);
            double corellationClose = PearsonCorrelation(sourceClose, compareDataClose, 0, source.Length);
            double corellation = RatioOHLC[0] * corellationOpen + RatioOHLC[1] * corellationHigh + RatioOHLC[2] * corellationLow + RatioOHLC[3] * corellationClose;
            Result res = new Result(0, corellation);
            results.Add(res);

            Result worst = res;
            for (int i = 1; i < compareData.Length - source.Length; ++i)
            {
                // Vypocitame korelaci
                corellationOpen = PearsonCorrelation(sourceOpen, compareDataOpen, i, source.Length);
                corellationHigh = PearsonCorrelation(sourceHigh, compareDataHigh, i, source.Length);
                corellationLow = PearsonCorrelation(sourceLow, compareDataLow, i, source.Length);
                corellationClose = PearsonCorrelation(sourceClose, compareDataClose, i, source.Length);
                corellation = RatioOHLC[0] * corellationOpen + RatioOHLC[1] * corellationHigh + RatioOHLC[2] * corellationLow + RatioOHLC[3] * corellationClose;

                // Pokud je vypocitana korelace lepsi nez nejhorsi v best N
                if (corellation >= worst.Corellation)
                {
                    // Pokud je list plny, tak odstranime nejhorsi
                    if (results.Count == 5)
                        results.Remove(worst);

                    // Pridame lepsiho
                    results.Add(new Result(i, corellation));

                    // Najdeme nejhorsiho
                    worst = results.OrderBy(x => x.Corellation).FirstOrDefault();
                }
            }

            // Vratime vysledek
            return results;
        }
        public static double PearsonCorrelation(double[] xs, double[] ys, int from, int count)
        {
            var dtw = MyDtw(xs, ys, from, count);
            if (double.IsInfinity(dtw))
                return 0;
            else
                return dtw;

            double sumX = 0;
            double sumX2 = 0;
            double sumY = 0;
            double sumY2 = 0;
            double sumXY = 0;

            int n = xs.Length < count ? xs.Length : count;

            for (int i = 0; i < n; ++i)
            {
                double x = xs[i];
                double y = ys[i + from];

                sumX += x;
                sumX2 += x * x;
                sumY += y;
                sumY2 += y * y;
                sumXY += x * y;
            }

            double stdX = Math.Sqrt(sumX2 / n - sumX * sumX / (double)n / (double)n);
            double stdY = Math.Sqrt(sumY2 / n - sumY * sumY / (double)n / (double)n);
            double covariance = (sumXY / n - sumX * sumY / (double)n / (double)n);

            double result = covariance / stdX / stdY;

            if (double.IsInfinity(result))
                return 0;
            else if (double.IsNaN(result))
                return -1;
            else
                return result;
        }
        public static double MyDtw(double[] xs, double[] ys, int from, int count)
        {
            int n = xs.Length < count ? xs.Length : count;

            return -new Dtw(xs, ys[from..(from + n)]).GetCost();
        }

        private void Redraw()
        {
            // Vycitime platno
            DrawingPlane.Children.Clear();

            // Pokud nejsou dala, nelze nic kreslit
            if (Bars == null || Bars.Count < int.Parse(PocetVzorku.Text))
                return;

            // Nakreslime cervenou caru
            DrawRedLine();

            // Spocitame si odkud pokud budeme zobrazovat data
            int from = int.Parse(Vzorek.Text) - int.Parse(PocetVzorku.Text);
            int count = SamplesCount + FutureSize;

            // Seznam dat k zobrazeni
            List<Bar> toDraw = Bars.Skip(from).Take(count).ToList();

            // Nakreslime graf 
            if (DisplayOHLC.IsChecked == true)
                DrawPrice(toDraw, ShowOnlyPast.IsChecked == true);
            else if (DisplayOpen.IsChecked == true || DisplayHigh.IsChecked == true || DisplayLow.IsChecked == true || DisplayClose.IsChecked == true)
            {
                // Vybereme ciselnou radu k zobrazeni
                List<double> lineToDraw;
                if (DisplayOpen.IsChecked == true)
                    lineToDraw = toDraw.Select(x => x.AdjustedOpen).ToList();
                else if (DisplayHigh.IsChecked == true)
                    lineToDraw = toDraw.Select(x => x.AdjustedHigh).ToList();
                else if (DisplayLow.IsChecked == true)
                    lineToDraw = toDraw.Select(x => x.AdjustedLow).ToList();
                else
                    lineToDraw = toDraw.Select(x => x.AdjustedClose).ToList();

                // Vykreslime graf
                DrawLine(lineToDraw, toDraw, ShowOnlyPast.IsChecked == true);
            }
            else if (Results != null && Results.Count >= 5)
            {
                // Vybereme poradi vysledku
                int resultNumber = 1;
                if (Result1Radio.IsChecked == true)
                    resultNumber = 1;
                else if (Result2Radio.IsChecked == true)
                    resultNumber = 2;
                else if (Result3Radio.IsChecked == true)
                    resultNumber = 3;
                else if (Result4Radio.IsChecked == true)
                    resultNumber = 4;
                else
                    resultNumber = 5;

                // Vybereme seznam dat
                DateTime firstDate = toDraw.FirstOrDefault().DateTimeFrom;
                toDraw = AllBars.Where(x => x.DateTimeTo <= firstDate).Skip(Results[resultNumber - 1].FirstIndex).Take(count).ToList();

                // Zobrazime data
                DrawPrice(toDraw, false);
            }

            // Nakreslime popisky os
            DrawXAxis(toDraw.Select(x => x.DateTimeFrom).ToList());
            DrawYAxis(toDraw.Select(x => x.AdjustedHigh).Max(), toDraw.Select(x => x.AdjustedLow).Min());
        }
        private void DrawPrice(List<Bar> values, bool shortVersion)
        {
            // Najdeme si max a min
            double max = values.Select(x => x.AdjustedHigh).Max();
            double min = values.Select(x => x.AdjustedLow).Min();

            // Spocitame si sirku svicky
            double candleWidth = CanvasSize.Width / (double)values.Count;

            // Calculate height in $$
            double range = CanvasSize.Height / (max - min);

            // Kreslime svicky
            for (int i = 0; i < values.Count; ++i)
            {
                // Pokud kreslime zkracenou verzi, tak kreslime jen do cervene cary
                if (shortVersion && i > int.Parse(PocetVzorku.Text) - 1)
                    break;

                // Telo svicky
                Rectangle teloSvicky = new Rectangle();
                teloSvicky.Width = candleWidth * 2 / 3.0;
                Canvas.SetLeft(teloSvicky, i * candleWidth + candleWidth / 6);
                teloSvicky.Stroke = Brushes.Black;
                teloSvicky.StrokeThickness = 1;
                teloSvicky.Height = Math.Abs(values[i].AdjustedClose - values[i].AdjustedOpen) * range;
                teloSvicky.Cursor = Cursors.Hand;
                if (values[i].AdjustedOpen < values[i].AdjustedClose)
                {
                    teloSvicky.Fill = Brushes.Green;
                    Canvas.SetTop(teloSvicky, (max - values[i].AdjustedClose) * range);
                }
                else
                {
                    teloSvicky.Fill = Brushes.Red;
                    Canvas.SetTop(teloSvicky, (max - values[i].AdjustedOpen) * range);
                }
                if (values[i].AdjustedOpen == values[i].AdjustedClose)
                    teloSvicky.Height = 1;

                // Pridame tooltip telu svicky
                teloSvicky.ToolTip =
                    values[i].DateTimeFrom +
                    "\n\nOpen  : " + values[i].AdjustedOpen +
                    "\nHigh    : " + values[i].AdjustedHigh +
                    "\nLow     : " + values[i].AdjustedLow +
                    "\nClose   : " + values[i].AdjustedClose +
                    "\nOpen G  : " + Math.Round(values[i].AdjustedOpenGain * 100, 2) + "%" +
                    "\nHigh G  : " + Math.Round(values[i].AdjustedHighGain * 100, 2) + "%" +
                    "\nLow G   : " + Math.Round(values[i].AdjustedLowGain * 100, 2) + "%" +
                    "\nClose G : " + Math.Round(values[i].AdjustedCloseGain * 100, 2) + "%";

                // Pridame telo svicky na canvas
                DrawingPlane.Children.Add(teloSvicky);

                // Knoty svicky
                Line knot = new Line();
                knot.Stroke = Brushes.Black;
                knot.Fill = Brushes.Black;
                knot.StrokeThickness = 1;
                Canvas.SetZIndex(knot, -1);
                knot.X1 = knot.X2 = i * candleWidth + candleWidth / 2.0;
                knot.Y1 = (max - values[i].AdjustedLow) * range;
                knot.Y2 = (max - values[i].AdjustedHigh) * range;
                knot.Cursor = Cursors.Hand;

                // Bude mit popisek jak knot
                knot.ToolTip = teloSvicky.ToolTip;

                // Pridame knot svicky na canvas
                DrawingPlane.Children.Add(knot);
            }
        }
        private void DrawLine(List<double> values, List<Bar> bars, bool shortVersion)
        {
            // Najdeme si max a min
            double max = bars.Select(x => x.AdjustedHigh).Max();
            double min = bars.Select(x => x.AdjustedLow).Min();

            // Spocitame si mezeru mezi body
            double space = CanvasSize.Width / (double)values.Count;

            // Spocitame si, jak velky bude 1 pip
            double range = CanvasSize.Height / (max - min);

            // Spocitame si velikost kolecka
            double radiusH = range * 2.5;
            double radiusW = space / 2;
            double radius = radiusW < radiusH ? radiusW : radiusH;

            // Kreslime cary
            for (int i = 0; i < values.Count; ++i)
            {
                // Pokud kreslime zkracenou verzi, tak kreslime jen do cervene cary
                if (shortVersion && i > int.Parse(PocetVzorku.Text) - 1)
                    break;

                // Nakreslime kolecko
                Ellipse circle = new Ellipse();
                circle.StrokeThickness = 0;
                circle.Fill = Brushes.Transparent;
                circle.Width = circle.Height = radius;
                circle.Cursor = Cursors.Hand;
                Canvas.SetLeft(circle, i * space + space / 2.0 - radius / 2);
                Canvas.SetTop(circle, (max - values[i]) * range - radius / 2);
                DrawingPlane.Children.Add(circle);

                // Nastavime kolecku tooltip
                circle.ToolTip = bars[i].DateTimeFrom + "\n\nValue : " + values[i];

                // Prvni nema predchudce
                if (i == 0)
                    continue;

                // Cara mezi predchozim a aktualnim
                Line line = new Line();
                line.Stroke = Brushes.DarkBlue;
                line.Fill = Brushes.DarkBlue;
                line.StrokeThickness = 2;
                line.X1 = (i - 1) * space + space / 2.0;
                line.X2 = i * space + space / 2.0;
                line.Y1 = (max - values[i - 1]) * range;
                line.Y2 = (max - values[i]) * range;

                // Pridame telo svicky na canvas
                DrawingPlane.Children.Add(line);
            }
        }
        private void DrawXAxis(List<DateTime> dates)
        {
            // Spocitame si pocet zobrazenych dat
            int count = (int)(CanvasSize.Width / 80.0);

            // Vypocitame, kolikaty kazdy vzorek zobrazime
            int every = dates.Count / count + 1;

            // Spocitame si sirku svicky
            double candleWidth = CanvasSize.Width / (double)dates.Count;

            // Pokud zadny, tak alespon prvni
            if (every == 0)
                every = dates.Count;

            // Vykreslime data
            for (int i = 0; i < dates.Count; ++i)
            {
                // Text
                int thickness = 1;
                if (i % every == 0)
                {
                    TextBlock text = new TextBlock();
                    text.Text = dates[i].ToShortDateString() + "\n" + dates[i].ToShortTimeString();
                    text.Foreground = Brushes.Black;
                    text.TextAlignment = TextAlignment.Center;
                    Canvas.SetLeft(text, i * candleWidth + candleWidth / 2.0 - 22);
                    Canvas.SetTop(text, CanvasSize.Height + 10);
                    DrawingPlane.Children.Add(text);

                    thickness = 3;
                }

                // Cara
                // Nakreslime vodorovnou osu k casu
                Line line = new Line();
                line.Stroke = Brushes.Silver;
                line.Fill = Brushes.Silver;
                line.StrokeThickness = thickness;
                line.X1 = line.X2 = i * candleWidth + candleWidth / 2.0;
                line.Y1 = CanvasSize.Height;
                line.Y2 = 0;
                Canvas.SetZIndex(line, -10);
                DrawingPlane.Children.Add(line);
            }


        }
        private void DrawYAxis(double maxPrice, double minPrice)
        {
            // Vypocitame, jakou ma 1 pip vysku
            double range = CanvasSize.Height / (maxPrice - minPrice);

            // Vypocitame, kolik cen se nam vleze k zobrazeni
            int numberOfDisplayPrices = (int)(CanvasSize.Height / 40);

            // Vypocitame, jaky bude mezi cenami rozdila
            double deltaPip = Math.Max(((maxPrice - minPrice) / (double)numberOfDisplayPrices), 1);

            // Zobrazime jednotlive popisky
            for (double price = minPrice; price <= maxPrice; price += deltaPip)
            {
                // Text
                TextBlock popisek = new TextBlock();
                popisek.Text = Math.Round(price, 2).ToString();
                popisek.Foreground = Brushes.Black;
                popisek.VerticalAlignment = VerticalAlignment.Center;
                popisek.TextAlignment = TextAlignment.Center;
                Canvas.SetLeft(popisek, CanvasSize.Width + 10);
                Canvas.SetTop(popisek, ((maxPrice - price) * range) - 9);
                DrawingPlane.Children.Add(popisek);

                // Cara
                Line line = new Line();
                line.Stroke = Brushes.Silver;
                line.Fill = Brushes.Silver;
                line.StrokeThickness = 1;
                line.X1 = 0;
                line.X2 = CanvasSize.Width;
                line.Y1 = line.Y2 = ((maxPrice - price) * range);
                Canvas.SetZIndex(line, -10);
                DrawingPlane.Children.Add(line);
            }
        }
        private void DrawRedLine()
        {
            // Nakreslime cervenou caru znacici konec analyzovane oblasti
            Line line = new Line();
            line.Stroke = Brushes.Red;
            line.Fill = Brushes.Red;
            line.StrokeThickness = 3;
            line.X1 = line.X2 = CanvasSize.Width * SamplesCount / (double)(SamplesCount + FutureSize);
            line.Y1 = 0;
            line.Y2 = CanvasSize.Height;

            DrawingPlane.Children.Add(line);
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            // Vytvorime open file dialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Nastavime pocatecni slozku
            dlg.InitialDirectory = Directory.GetParent(FileFilePath).FullName;

            // Nacteme prislusny soubor
            if (dlg.ShowDialog() == true)
            {
                // Ulozime si cestu k souboru
                FileFilePath = dlg.FileName;

                // Nastavime jmeno do labelu
                FilePathLabel.Content = dlg.SafeFileName;
            }
        }
        private void BrowseData_Click(object sender, RoutedEventArgs e)
        {
            // Vytvorime open folder dialog
            WinForms.FolderBrowserDialog dialog = new WinForms.FolderBrowserDialog();

            // Nastavime pocatecni slozku
            dialog.SelectedPath = Directory.GetParent(DataFilePath).FullName;

            // Nacteme prislusny soubor
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                // Ulozime si cestu k souboru
                DataFilePath = dialog.SelectedPath;

                // Nastavime jmeno do labelu
                DataPathLabel.Content = System.IO.Path.GetFileNameWithoutExtension(dialog.SelectedPath);
            }
        }
        private void Load_Click(object sender, RoutedEventArgs e)
        {
            // Cekame
            Mouse.OverrideCursor = Cursors.Wait;

            // Nacteme data pro testovani
            Bars = Bar.LoadBarsFromFile(FileFilePath);
            AllBars = Bar.LoadAllFromFromDirectory(DataFilePath);

            // Zobrazime nejposlednejsi pocet vzorku
            Vzorek.Text = ((int)(Bars.Count - FutureSize)).ToString();

            // Vykreslime graf
            Redraw();

            // Hotovo
            Mouse.OverrideCursor = null;
        }
        private void DrawingPlane_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Ulozime si velikost platna
            CanvasSize = e.NewSize;

            // Prekreslime platno
            Redraw();
        }
        private void Plus_Click(object sender, RoutedEventArgs e)
        {
            // Zvysime cislo vzorku
            Vzorek.Text = (int.Parse(Vzorek.Text) + 1).ToString();

            // Prekreslime
            Redraw();
        }
        private void Minus_Click(object sender, RoutedEventArgs e)
        {
            // Snizime cislo vzorku
            Vzorek.Text = (int.Parse(Vzorek.Text) - 1).ToString();

            // Prekreslime
            Redraw();
        }
        private void RadioChanged(object sender, RoutedEventArgs e)
        {
            // Zobrazime korelaci
            if (Results != null && Results.Count >= 5)
            {
                double korelace = 0;
                if (Result1Radio.IsChecked == true)
                    korelace = Results[0].Corellation;
                else if (Result2Radio.IsChecked == true)
                    korelace = Results[1].Corellation;
                else if (Result3Radio.IsChecked == true)
                    korelace = Results[2].Corellation;
                else if (Result4Radio.IsChecked == true)
                    korelace = Results[3].Corellation;
                else
                    korelace = Results[4].Corellation;

                if (korelace != 0)
                    CorellationText.Text = String.Format("{0:0.00000}", korelace);   
            }

            // Prekreslime
            Redraw();
        }
        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            // Zatim nenacteno
            if (Vzorek == null || PocetVzorku == null || Bars == null)
                return;

            // Zkontrolujeme validitu vstupu
            int vzorek = int.Parse(Vzorek.Text);
            int pocetVzorku = int.Parse(PocetVzorku.Text);

            // Nesmime nastavit pocet vzorku prilis dlouhy
            if (vzorek + FutureSize > Bars.Count)
                Vzorek.Text = ((int)(Bars.Count - FutureSize)).ToString();

            // Prekreslime
            Redraw();
        }
        private void Compute_Click(object sender, RoutedEventArgs e)
        {
            // Pokud nejsou data, tak nic nepocitame
            if (Bars == null || Bars.Count < int.Parse(PocetVzorku.Text))
                return;

            // Zacneme pocitat
            Compute();
        }
    }
}