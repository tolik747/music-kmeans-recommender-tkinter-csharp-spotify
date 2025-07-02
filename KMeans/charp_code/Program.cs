using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//  Trieda KMeans realizuje algoritmus k-means pre zoskupovanie (klastrovanie) dátových bodov
class KMeans
{
    private int k; // Počet klastrov (skupín)
    private int maxIterations; // Maximálny počet iterácií (cyklov)
    private List<double[]> centroids; // Zoznam centier (centroidov) každého klastru

    //  Konštruktor: nastavuje počet klastrov a maximálny počet iterácií
    public KMeans(int k, int maxIterations = 100)
    {
        this.k = k;
        this.maxIterations = maxIterations;
        this.centroids = new List<double[]>();
    }

    //  Metóda Fit vykonáva samotný k-means algoritmus a vracia priradenie klastrov
    public int[] Fit(double[][] data)
    {
        int numSamples = data.Length; // Počet vzoriek (napr. počet piesní)
        int numFeatures = data[0].Length; // Počet číselných vlastností (napr. tempo, energia, ...)
        int[] labels = new int[numSamples]; // Pole pre výsledné priradenie každej vzorky ku klastru

        Random rand = new Random();

        // 🔹 1. Inicializácia centier náhodným výberom k rôznych vzoriek z datasetu
        for (int i = 0; i < k; i++)
        {
            centroids.Add(data[rand.Next(numSamples)]);
        }

        // 🔹 2. Hlavný cyklus algoritmu - opakuje sa maxIterations-krát alebo kým sa centrá nezmenia
        for (int iter = 0; iter < maxIterations; iter++)
        {
            bool hasChanged = false; // Sleduje, či sa v tejto iterácii zmenili priradenia klastrov

            // Každý bod priraďujeme ku najbližšiemu centru (centroidu)
            for (int i = 0; i < numSamples; i++)
            {
                int bestCluster = 0;
                double bestDistance = EuclideanDistance(data[i], centroids[0]);

                // Porovnajme vzdialenosť s ostatnými centroidmi
                for (int j = 1; j < k; j++)
                {
                    double distance = EuclideanDistance(data[i], centroids[j]);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestCluster = j;
                    }
                }

                // Ak sa klaster zmenil, zaznač túto zmenu
                if (labels[i] != bestCluster)
                {
                    labels[i] = bestCluster;
                    hasChanged = true;
                }
            }

            //  Aktualizácia centroidov: vypočíta sa priemer všetkých bodov patriacich do daného klastru
            for (int j = 0; j < k; j++)
            {
                double[] newCentroid = new double[numFeatures];
                int count = 0;

                for (int i = 0; i < numSamples; i++)
                {
                    if (labels[i] == j)
                    {
                        for (int f = 0; f < numFeatures; f++)
                        {
                            newCentroid[f] += data[i][f]; // Sčítavame hodnoty atribútov
                        }
                        count++;
                    }
                }

                if (count > 0)
                {
                    for (int f = 0; f < numFeatures; f++)
                    {
                        newCentroid[f] /= count; // Vypočítame priemer (nový centroid)
                    }
                    centroids[j] = newCentroid;
                }
            }

            //  Ak sa žiadna vzorka nepresunula do iného klastru — končíme iterácie
            if (!hasChanged)
                break;
        }

        //  Vrátime výsledné klastrové priradenia pre každý bod
        return labels;
    }

    //  Pomocná metóda na výpočet Euklidovskej vzdialenosti medzi dvoma bodmi
    private double EuclideanDistance(double[] point1, double[] point2)
    {
        double sum = 0;
        for (int i = 0; i < point1.Length; i++)
        {
            sum += Math.Pow(point1[i] - point2[i], 2); // Rozdiely na druhú pre každú vlastnosť
        }
        return Math.Sqrt(sum); // Druhá odmocnina — výsledná vzdialenosť
    }
}

// Hlavný program
class Program
{
    static void Main()
    {
        // Cesta k súboru so vstupnými dátami
        string filePath = "/Users/mac/c#/k/charp_code/spotify1.csv";
        var data = LoadDataFromCSV(filePath);

        int k = 3;  // Počet klastrov
        KMeans kMeans = new KMeans(k);

        // Spustenie učenia modelu
        int[] labels = kMeans.Fit(data.features);

        // Výstup do nového CSV súboru s klastrami
        using (var writer = new StreamWriter("/Users/mac/c#/k/clustered_songs.csv"))
        {
            writer.WriteLine("Title,Artist,Cluster");
            for (int i = 0; i < labels.Length; i++)
            {
                writer.WriteLine($"\"{data.names[i]}\",\"{data.artists[i]}\",{labels[i]}");
            }
        }
        Console.WriteLine("file clustered_songs.csv");
    }

    // Funkcia na načítanie dát zo súboru CSV
    static (double[][] features, string[] names, string[] artists) LoadDataFromCSV(string filename)
    {
        List<double[]> data = new List<double[]>();
        List<string> names = new List<string>();
        List<string> artists = new List<string>();

        using (var reader = new StreamReader(filename))
        {
            string header = reader.ReadLine();
            string[] columnNames;

            // Detekcia oddeľovača (čiarka alebo bodkočiarka)
            if (header.Contains(";"))
                columnNames = header.Split(';');
            else
                columnNames = header.Split(',');

            // Čistenie názvov stĺpcov
            for (int i = 0; i < columnNames.Length; i++)
                columnNames[i] = columnNames[i].Trim().ToLower();

            // Potrebné stĺpce
            Dictionary<string, int> requiredColumns = new Dictionary<string, int>
            {
                { "danceability", -1 },
                { "energy", -1 },
                { "db", -1 },
                { "valence", -1 },
                { "bpm", -1 }
            };

            // Nájdeme indexy potrebných stĺpcov
            for (int i = 0; i < columnNames.Length; i++)
            {
                if (requiredColumns.ContainsKey(columnNames[i]))
                    requiredColumns[columnNames[i]] = i;
            }

            if (requiredColumns.Values.Contains(-1))
            {
                Console.WriteLine("error: niektoré stĺpce chýbajú v CSV!");
                Console.WriteLine($" Stĺpce v hlavičke: {string.Join(", ", columnNames)}");
                return (new double[0][], new string[0], new string[0]);
            }

            // Načítanie riadkov
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var values = line.Contains(";") ? line.Split(';') : line.Split(',');

                try
                {
                    string trackName = values[0].Trim('"');
                    string artist = values[1].Trim('"');

                    // Načítanie číselných vlastností
                    double danceability = double.Parse(values[requiredColumns["danceability"]]);
                    double energy = double.Parse(values[requiredColumns["energy"]]);
                    double loudness = double.Parse(values[requiredColumns["db"]]);
                    double valence = double.Parse(values[requiredColumns["valence"]]);
                    double tempo = double.Parse(values[requiredColumns["bpm"]]);

                    data.Add(new double[] { danceability, energy, loudness, valence, tempo });
                    names.Add(trackName);
                    artists.Add(artist);
                }
                catch (Exception e)
                {
                    Console.WriteLine($" chyba v riadku: {line}");
                    Console.WriteLine($"   → chyba: {e.Message}");
                }
            }
        }

        return (data.ToArray(), names.ToArray(), artists.ToArray());
    }

    // Voliteľná funkcia: analyzuje priemerné hodnoty pre každý klaster
    static void AnalyzeClusters(double[][] features, int[] labels, int k)
    {
        int numFeatures = features[0].Length;
        double[][] clusterAverages = new double[k][];
        for (int i = 0; i < k; i++)
            clusterAverages[i] = new double[numFeatures];

        int[] clusterCounts = new int[k];

        for (int i = 0; i < features.Length; i++)
        {
            int cluster = labels[i];
            clusterCounts[cluster]++;
            for (int j = 0; j < numFeatures; j++)
                clusterAverages[cluster][j] += features[i][j];
        }

        Console.WriteLine("\n📊 Priemerné hodnoty pre každý klaster:");
        for (int i = 0; i < k; i++)
        {
            if (clusterCounts[i] > 0)
            {
                for (int j = 0; j < numFeatures; j++)
                    clusterAverages[i][j] /= clusterCounts[i];
            }

            Console.WriteLine($"🔹 Klaster {i}:");
            Console.WriteLine($"   - Danceability: {clusterAverages[i][0]:F2}");
            Console.WriteLine($"   - Energy: {clusterAverages[i][1]:F2}");
            Console.WriteLine($"   - Loudness: {clusterAverages[i][2]:F2}");
            Console.WriteLine($"   - Valence: {clusterAverages[i][3]:F2}");
            Console.WriteLine($"   - Tempo: {clusterAverages[i][4]:F2}\n");
        }
    }
}
