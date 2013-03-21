using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace NetLength
{
    public class HypFile
    {
        public LayerStack Stackup;
        public Dictionary<string, PadStack> PadStacks;
        public Dictionary<string, List<RouteElement>> Objects;
        public Dictionary<string, List<Pad>> Pads;
        public double LoadTime;
        List<string> NetsToLoad;

        private double StrToDouble(string s)
        {
            double d;
            if (!double.TryParse(s, out d)) throw new ParseException("Unable to convert '" + s + "' to double");
            return d;
        }

        private double HypToMils(double d)
        {
            return 1000 * d;
        }

        private double HypToMils(string d)
        {
            return 1000 * StrToDouble(d);
        }

        /// <summary>
        /// Reads a parameter line, trims spaces and tabs and split the
        /// comma separated values
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public string[] ParseLine(string line, char delimiter)
        {
            string l = line.Trim().TrimStart('(').TrimEnd(')');
            var insideQuotes = false;

            var parts = new List<string>();

            var j = 0;

            for (var i = 0; i < l.Length; i++)
            {
                if (l[i] == '"')
                {
                        insideQuotes = !insideQuotes;
                }
                else if (l[i] == delimiter)
                {
                        if (!insideQuotes)
                        {
                            parts.Add(l.Substring(j, i - j));
                            j = i + 1;
                        }
                }
            }
            parts.Add(l.Substring(j, l.Length-j));
            return parts.ToArray();
        }


        public Dictionary<string, string> ParseSubsection(string line, char delimiter)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();

            string[] args = ParseLine(line, delimiter);
            foreach (string s in args)
            {
                string[] div = s.Split('=');
                dic.Add(div[0], (div.Length> 1? div[1] : ""));
            }

            return dic;
        }

        /// <summary>
        /// Parses the layer stack information
        /// </summary>
        /// <param name="rd"></param>
        private void ParseLayerStack(StringReader rd)
        {
            string line;

            line = rd.ReadLine();

            while ((line = rd.ReadLine()) != null)
            {
                Layer l = new Layer();

                Dictionary<string, string> d = ParseSubsection(line, ' ');
                l.Height = HypToMils(d["T"]);
                l.Name = d["L"];
                if (d.ContainsKey("SIGNAL"))
                    l.Type = Layer.LayerType.Signal;
                else if (d.ContainsKey("PLANE"))
                    l.Type = Layer.LayerType.Plane;
                else if (d.ContainsKey("DIELECTRIC"))
                    l.Type = Layer.LayerType.Dielectric;
                else
                    throw new ParseException("Unknown layer type: " + line);

                Stackup.AddLayer(l);
            }
        }

        /// <summary>
        /// Parses a Padstack block
        /// </summary>
        /// <param name="rd"></param>
        private void ParsePadstack(StringReader rd)
        {
            string line;
            string[] args;

            line = rd.ReadLine();
            string index;
            double holesize;
            args = ParseLine(line, ',');
            index = args[0].Split('=')[1];
            holesize = HypToMils(args[1]);

            PadStack pad = new PadStack();
            pad.Name = index;
            pad.HoleSize = holesize;

            while ((line = rd.ReadLine()) != null)
            {
                PadStack.PadStackLayerDef d = new PadStack.PadStackLayerDef();
                args = ParseLine(line, ',');
                d.Type = ((PadStack.PadStackLayerDef.PadType)(int)StrToDouble(args[1]));
                d.sx = HypToMils(args[2]);
                d.sy = HypToMils(args[3]);
                d.rotation = StrToDouble(args[4]);

                if (args[0] == "MDEF")
                {
                    foreach (Layer l in Stackup.Layers)
                    {
                        // Add definition only for signal layers
                        // And only if it wasnt already defined
                        if (l.Type == Layer.LayerType.Signal && !pad.PadInfo.ContainsKey(l.Name))
                            pad.AddPadLayerDef(l.Name, d);
                    }
                }
                else if (args[0] == "ADEF")
                {
                    // Do not care about antipads on plane layers
                    continue;
                }
                else
                {
                    // Override any previous definition on this layer
                    pad.PadInfo.Remove(args[0]);
                    pad.AddPadLayerDef(args[0], d);
                }
            }
            PadStacks.Add(pad.Name, pad);
        }

        /// <summary>
        /// Parses a Net block
        /// </summary>
        /// <param name="rd"></param>
        private void ParseNet(StringReader rd)
        {
            string Net;
            string line;
            string[] args;

            line = rd.ReadLine();
            args = ParseLine(line, ',');
            Net = args[0].Split('=')[1];

            bool found = false;
            foreach (string s in NetsToLoad)
            {
                if (Regex.Match(Net, s).Success)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return;

            Objects.Add(Net, new List<RouteElement>());
            Pads.Add(Net, new List<Pad>());

            while ((line = rd.ReadLine()) != null)
            {
                Dictionary<string,string> d = ParseSubsection(line, ' ');
                RouteElement r;

                if (d.ContainsKey("SEG"))
                {
                    r = new Segment(new Vec2(HypToMils(d["X1"]), HypToMils(d["Y1"])),
                        new Vec2(HypToMils(d["X2"]), HypToMils(d["Y2"])), HypToMils(d["W"]), d["L"], Net);
                }
                else if (d.ContainsKey("ARC"))
                {
                    r = new Arc(new Vec2(HypToMils(d["XC"]), HypToMils(d["YC"])),
                        new Vec2(HypToMils(d["X1"]), HypToMils(d["Y1"])),
                        new Vec2(HypToMils(d["X2"]), HypToMils(d["Y2"])),
                        HypToMils(d["R"]) * 2.0, HypToMils(d["W"]), d["L"], Net);
                }
                else if (d.ContainsKey("VIA"))
                {
                    r = new Via(PadStacks, new Vec2(HypToMils(d["X"]), HypToMils(d["Y"])), d["P"], Net);
                }
                else if (d.ContainsKey("PIN"))
                {
                    r = new Pad(PadStacks, new Vec2(HypToMils(d["X"]), HypToMils(d["Y"])), d["P"], d["R"], Net);
                    Pads[Net].Add((Pad)r);
                }
                else
                {
                    throw new ParseException("Unknown object type: " + line);
                }
                Objects[Net].Add(r);
            }
        }

        /// <summary>
        /// Parse a record from the HYP file
        /// </summary>
        /// <param name="block">Content of the Record</param>
        private void ParseRecord(string block)
        {
            StringReader rd = new StringReader(block);

            if (block.StartsWith("STACKUP", true, null))
                ParseLayerStack(rd);
            else if (block.StartsWith("PADSTACK", true, null))
                ParsePadstack(rd);
            else if (block.StartsWith("NET", true, null))
                ParseNet(rd);
        }

        /// <summary>
        /// Loads a Hyperlynx file
        /// </summary>
        /// <param name="filename"></param>
        public void LoadFile(string filename, List<string> netsToLoad)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            NetsToLoad = netsToLoad;
            StreamReader sr = new StreamReader(filename);
            String block, line;

            try
            {
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine().Trim();
                    if (line.StartsWith("{"))
                    {
                        block = line + '\n';
                        while (!line.EndsWith("}"))
                        {
                            line = sr.ReadLine().Trim();
                            block += line + '\n';
                        }
                        block = block.TrimStart('{').TrimEnd('\n').TrimEnd('}');

                        ParseRecord(block);
                    }
                }
            }
            catch (Exception e)
            {
                throw new ParseException("Hyperlynx parse error: " + e.ToString());
            }

            sw.Stop();
            LoadTime = sw.ElapsedMilliseconds;
        }

        public HypFile()
        {
            Stackup = new LayerStack();
            PadStacks = new Dictionary<string, PadStack>();
            Objects = new Dictionary<string, List<RouteElement>>();
            Pads = new Dictionary<string, List<Pad>>();
        }

        public class ParseException : Exception
        {
            public ParseException() { }
            public ParseException(string msg) : base(msg) { }
        }
    }
}
