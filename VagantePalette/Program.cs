using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VagantePalette
{
    #region JSON container classes
    // Collection of classes for containing data from palettes.json
    class Palette
    {
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("colors")]
        public IList<IList<int>> colors { get; set; }
    }

    class PaletteGroup
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("texture-names")]
        public IList<string> TextureNames { get; set; }
        [JsonProperty("palettes")]
        public IList<Palette> Palettes { get; set; }
    }

    class PaletteCollection
    {
        [JsonProperty("_comment")]
        public string Comment { get; set; }
        [JsonProperty("palette-groups")]
        public IList<PaletteGroup> PaletteGroups { get; set; }
    }

    #endregion

    class PaletteConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<string>) || objectType == typeof(List<IList<int>>);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken token = JToken.FromObject(value);
            if (value is List<string>)
            {
                var list = (List<string>)value;
                string item = "";
                // 4 tabs
                item += "[\n\t\t\t\t\"";
                item += string.Join("\",\"", list);
                item += "\"\n\t\t\t]";

                writer.WriteRawValue(item);
            }
            else if (value is List<IList<int>>)
            {
                var list = (List<IList<int>>)value;
                string item = "";
                item += "[\n\t\t\t\t\t\t[\n";
                foreach (var i in list)
                {
                    item += "\t\t\t\t\t\t\t";
                    item += string.Join(",", i);
                    item += "\n\t\t\t\t\t\t],[\n";
                }
                item = item.Substring(0, item.LastIndexOf("\t\t\t\t\t\t],[\n"));
                item += "\t\t\t\t\t\t]\n\t\t\t\t\t]";

                writer.WriteRawValue(item);
            }
        }
    }

    // Comparable color class
    class Pixel : IComparable
    {
        public int R, G, B, A;

        public Pixel(Color c)
        {
            R = c.R;
            G = c.G;
            B = c.B;
            // Alpha is always 255 in the vagante palette.json
            A = 255;
        }

        // Sort by red -> green -> blue -> alpha
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

        // json array element output
        public override string ToString()
        {
            return "[\n    " + R + "," + G + "," + B + "," + A + "\n]";
        }

        // Hexadecimal output
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
        static PaletteCollection palettes;

        // Insert "pixel" in "colors" if it's not already in (colors.Contains didn't work for some reason)
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
        // Return true if the image is 1 pixel high
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

        static List<Pixel> LoadPaletteColors(Palette palette)
        {
            List<Pixel> pixels = new List<Pixel>();

            foreach (IList<int> color in palette.colors)
            {
                Color newColor = Color.FromArgb(color[3], color[0], color[1], color[2]);
                pixels.Add(new Pixel(newColor));
            }

            return pixels;
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

            // Do the first iteration separately to avoid writing one more ","
            WritePixel(0, colors, ref str_json, ref str_hex, palette);

            for (int i = 1; i < colors.Count; i++)
            {
                str_hex += "\n";
                str_json += ",";
                WritePixel(i, colors, ref str_json, ref str_hex, palette);
            }

            // Update the object that's storing palettes.json data
            UpdatePalettesObject(filename, colors);

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
            if (!file.Contains(".json"))
            {
                bool line = LoadColors(path, colors);
                // Don't sort the image if it's more than 1 line
                if (!line)
                    colors.Sort();
            }
            else
            {
                Palette palette = JsonConvert.DeserializeObject<Palette>(File.ReadAllText(path));
                colors = LoadPaletteColors(palette);
            }
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
                if (!f.Contains(".json"))
                {
                    Console.WriteLine("    - File: " + f);
                    string file = f.Split('\\')[1]; // Remove directory\
                    LoadColors(f, colors);
                }
                else
                {

                }
            }
            WriteData(colors, directory);

            Console.WriteLine("Generated output\\" + directory + "\n-----------");
        }

        // Update the palettes.json object with the given palette at the given name
        static void UpdatePalettesObject(string textureName, ICollection<Pixel> colors)
        {
            if (palettes == null) return;

            List<Pixel> colorList = new List<Pixel>(colors);

            for (int i = 0; i < palettes.PaletteGroups.Count; i++)
            {
                // If the target texture name is found
                if (SearchTextureNames(textureName, palettes.PaletteGroups[i]))
                {
                    palettes.PaletteGroups[i].Palettes[0].colors = new List<IList<int>>();
                    for (int k = 0; k < colorList.Count; k++)
                    {
                        var temp = new List<int>() { colorList[k].R, colorList[k].G, colorList[k].B, colorList[k].A };

                        // Assign new palette
                        palettes.PaletteGroups[i].Palettes[0].colors.Add(temp);
                    }
                    break;
                }
            }
        }

        // Find the target texture name in the given palette group
        static bool SearchTextureNames(string targetTexture, PaletteGroup group)
        {
            string temp = "";

            string pattern = @"\w*\.png$";
            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);

            for (int i = 0; i < group.TextureNames.Count; i++)
            {
                temp = rgx.Match(group.TextureNames[i]).ToString();
                if (temp == targetTexture)
                    return true;
            }

            return false;
        }

        static void SerializePalettes(string path)
        {
            using (StreamWriter streamWriter = new StreamWriter(path))
            using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter)
            {
                Formatting = Formatting.Indented,
                Indentation = 1,
                IndentChar = '\t'
            })
            {

                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new PaletteConverter());
                serializer.Serialize(jsonWriter, palettes);
            }

            Console.WriteLine("Updated palettes.json.");
        }

        static void Main(string[] args)
        {
            if (!Directory.Exists("output"))
                Directory.CreateDirectory("output");

            string jsonPath = "palettes.json";

            if (File.Exists(jsonPath))
                palettes = JsonConvert.DeserializeObject<PaletteCollection>(File.ReadAllText(jsonPath));
            else
                Console.WriteLine("palettes.json not found in directory.");

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

            if (File.Exists(jsonPath))
                SerializePalettes(jsonPath);

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
