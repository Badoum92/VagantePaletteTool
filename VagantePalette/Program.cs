using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;

namespace VagantePalette
{
    // Comparable color class
    class Pixel : IComparable
    {
        public int R, G, B, A;

        public Pixel(Color c)
        {
            R = c.R;
            G = c.G;
            B = c.B;
            A = 255;
        }
        public int CompareTo(Object o)
        {
            Pixel other = o as Pixel;
            int Rcmp = R.CompareTo(other.R);
            if (Rcmp != 0)
                return Rcmp;
            int Gcmp = G.CompareTo(other.G);
            if (Gcmp != 0)
                return Gcmp;
            int Bcmp = B.CompareTo(other.B);
            if (Bcmp != 0)
                return Bcmp;
            int Acmp = A.CompareTo(other.A);
            if (Acmp != 0)
                return Acmp;
            return 0;
        }

        public override string ToString()
        {
            return "[\n    " + R + "," + G + "," + B + "," + A + "\n]";
        }

        public string ToHex()
        {
            string str = "#";
            if (R < 16)
                str += "0";
            str += R.ToString("X");
            if (G < 16)
                str += "0";
            str += G.ToString("X");
            if (B < 16)
                str += "0";
            str += B.ToString("X");
            return str;
        }
    }
    class Program
    {
        static void InsertIfNotIn(ICollection<Pixel> colors, Pixel pixel)
        {
            bool to_add = true;
            foreach (var p in colors)
            {
                if (p.CompareTo(pixel) == 0)
                {
                    to_add = false;
                    break;
                }
            }
            if (to_add)
                colors.Add(pixel);
        }

        // Add the colors found in the images at "path" in "colors"
        static bool LoadColors(string path, ICollection<Pixel> colors)
        {
            Bitmap img = Image.FromFile(path) as Bitmap;
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    var pixel = new Pixel(img.GetPixel(x, y));
                    InsertIfNotIn(colors, pixel);
                }
            }

            bool ret = img.Height == 1;
            img.Dispose();
            return ret;
        }

        // Update output data with the color at index i
        static void WritePixel(int i, ICollection<Pixel> colors, ref string str_json, ref string str_hex, Bitmap palette)
        {
            Pixel p = colors.ElementAt(i);
            str_json += p.ToString();
            str_hex += p.ToHex();
            palette.SetPixel(i, 0, Color.FromArgb(p.A, p.R, p.G, p.B));
        }

        // Generate the output for a colorset
        static void WriteData(ICollection<Pixel> colors, string filename)
        {
            if (colors.Count == 0)
                return;

            string file = filename.Split('.')[0]; // Remove the extension
            if (!Directory.Exists("output/" + file))
                Directory.CreateDirectory("output/" + file);

            Bitmap palette = new Bitmap(colors.Count, 1);
            string str_json = "";
            string str_hex = "";

            WritePixel(0, colors, ref str_json, ref str_hex, palette);

            for (int i = 1; i < colors.Count; i++)
            {
                str_hex += "\n";
                str_json += ",";
                WritePixel(i, colors, ref str_json, ref str_hex, palette);
            }

            string path = "output/" + file + "/" + file;
            File.WriteAllText(path + ".txt", str_json);
            File.WriteAllText(path + "_hex.txt", str_hex);
            palette.Save(path + ".png");
        }

        // Process a single image file
        static void ProcessFile(string path)
        {
            Console.WriteLine("Starting file: " + path);

            string file = path.Split('\\')[1]; // Remove input\
            var colors = new List<Pixel>();
            bool line = LoadColors(path, colors);
            if (!line)
                colors.Sort();
            WriteData(colors, file);

            Console.WriteLine("Generated output\\" + file.Split('.')[0] + "\n-----------"); // Remove the extension
        }

        // Process a directory with several image files
        static void ProcessDirectory(string path)
        {
            Console.WriteLine("Starting directory: " + path);

            string directory = path.Split('\\')[1]; // Remove input\
            var colors = new SortedSet<Pixel>();
            var files = Directory.GetFiles(path);
            foreach (string f in files)
            {
                Console.WriteLine("    - File: " + f);
                string file = f.Split('\\')[1]; // Remove directory\
                LoadColors(f, colors);
            }
            WriteData(colors, directory);

            Console.WriteLine("Generated output\\" + directory + "\n-----------");
        }

        static void Main(string[] args)
        {
            if (!Directory.Exists("output"))
                Directory.CreateDirectory("output");

            string[] files = Directory.GetFiles("input");
            foreach (string f in files)
            {
                ProcessFile(f);
            }

            string[] directories = Directory.GetDirectories("input");
            foreach (string dir in directories)
            {
                ProcessDirectory(dir);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
