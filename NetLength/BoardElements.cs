using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLength
{
    public class BoardAnalysisException : Exception
    {
        public BoardAnalysisException() : base() { }
        public BoardAnalysisException(string msg) : base(msg) { }
    }

    public class LayerStack
    {
        public List<Layer> Layers;

        public LayerStack()
        {
            Layers = new List<Layer>();
        }

        public void AddLayer(Layer l)
        {
            l.Index = Layers.Count;
            Layers.Add(l);
        }

        public double GetMinHeight(string[] layer1, string[] layer2)
        {
            double min = double.MaxValue;

            foreach (string s in layer1)
            {
                foreach (string ss in layer2)
                {
                    double d = GetHeight(s, ss);
                    if (d < min) min = d;
                }
            }
            return min;
        }

        public double GetHeight(string layer1, string layer2)
        {
            int lmin = int.MaxValue;
            int lmax = int.MinValue;

            if (layer1 == null || layer2 == null) return 0;

            if (layer1 == layer2) return 0;

            foreach (Layer l in Layers)
            {
                if (l.Name == layer1 || l.Name == layer2)
                {
                    if (l.Index > lmax) lmax = l.Index;
                    if (l.Index < lmin) lmin = l.Index;
                }
            }

            if (lmin == int.MaxValue || lmin == int.MinValue)
                throw new BoardAnalysisException("Unable to find one of these layers: " + layer1 + " " + layer2);

            double height = 0;
            for (int i = lmin; i < lmax; lmax++)
                height += Layers[i].Height;

            return height;
        }
    }

    public class Layer
    {
        public enum LayerType
        {
            Signal,
            Dielectric,
            Plane,
        }

        public String Name;
        public double Height;
        public LayerType Type;
        public int Index;

        public Layer() { }

        public Layer(String name, double height, LayerType type)
        {
            Name = name;
            Height = height;
            Type = type;
            Index = -1;
        }

        public static string LayerMatch(string[] L1, string[] L2)
        {
            if (L1 == null || L2 == null) return null;
            foreach (string s in L1)
            {
                foreach (string s2 in L2)
                {
                    if (s == s2) return s;
                }
            }
            return null;
        }

        public static bool LayerMatch(string L1, string[] L2)
        {
            if (L1 == null || L2 == null) return false;
            foreach (string s2 in L2)
            {
                if (L1 == s2) return true;
            }
            return false;
        }
    }

    public class PadStack
    {
        public string Name;
        public double HoleSize;
        public Dictionary<string, PadStackLayerDef> PadInfo;

        public PadStack()
        {
            PadInfo = new Dictionary<string, PadStackLayerDef>();
        }


        public void AddPadLayerDef(string layer, PadStackLayerDef def)
        {
            PadInfo.Add(layer, def);
        }

        public PadStackLayerDef this[string layer]
        {
            get
            {
                return PadInfo[layer];
            }
        }

        public class PadStackLayerDef
        {
            public double sx;
            public double sy;
            public double rotation;
            public PadType Type;
            public enum PadType : int
            {
                Normal = 0,
                Rectangular = 1,
                Oblong = 2,
            }
        }
    }

    public class RouteElement
    {
        public string Net;
        public string[] Layer;
        public RouteNode[] Nodes;
        public string Type;

        public virtual double GetLength(string layer1, string layer2)
        {
            return 0;
        }

        public virtual List<IntersectResult> Intersect(RouteElement obj)
        {
            List<IntersectResult> res = new List<IntersectResult>();

            foreach (RouteNode n in Nodes)
            {
                foreach (RouteNode nn in obj.Nodes)
                {
                    // Can intersect only if they share a common layer
                    string l = NetLength.Layer.LayerMatch(n.Parent.Layer, nn.Parent.Layer);
                    if (l != null)
                    {
                        IntersectResult r = n.Intersect(nn);
                        r.layer = l;
                        if (r.Intersects) res.Add(r);
                    }
                }
            }

            res.Sort(delegate(IntersectResult p1, IntersectResult p2) { return p1.dist.CompareTo(p2.dist); });

            return res;
        }
    }

    public class RouteNode
    {
        public Vec2 Pos;
        public double Diameter;
        public RouteElement Parent;

        public RouteNode(Vec2 pos, double diameter, RouteElement parent)
        {
            Pos = pos;
            Diameter = diameter;
            Parent = parent;
        }

        public IntersectResult Intersect(RouteNode node)
        {
            IntersectResult res = new IntersectResult();

            double dist = Maths.Dist(this.Pos, node.Pos);

            if (dist < (this.Diameter / 2.0) + (node.Diameter / 2.0))
            {
                res.dist = dist;
                res.location = (this.Pos + node.Pos) / 2.0;
                res.node1 = this;
                res.node2 = node;
                res.Intersects = true;
                return res;
            }

            res.dist = dist;
            res.Intersects = false;
            return res;
        }
    }

    public class Segment : RouteElement
    {
        private double Length;

        public Segment(Vec2 p1, Vec2 p2, double width, string layer, string net)
        {
            Net = net;
            Layer = new string[] { layer };
            Nodes = new RouteNode[2];
            Nodes[0] = new RouteNode(p1, width, this);
            Nodes[1] = new RouteNode(p2, width, this);
            Length = Maths.Dist(Nodes[0].Pos, Nodes[1].Pos);
            Type = "TRACK";
        }

        public override double GetLength(string layer1, string layer2)
        {
            return Length;
        }
    }

    public class Via : RouteElement
    {
        public Via(Dictionary<string, PadStack> PadStacks, Vec2 pos, string padstack, string net)
        {
            Net = net;
            PadStack ps = PadStacks[padstack];
            Layer = new string[ps.PadInfo.Count];
            Nodes = new RouteNode[ps.PadInfo.Count];
            Type = "VIA";

            int i = 0;
            foreach (KeyValuePair<string, PadStack.PadStackLayerDef> def in ps.PadInfo)
            {
                Layer[i] = def.Key;
                Nodes[i] = new RouteNode(pos, Math.Min(def.Value.sx, def.Value.sy), this);
                i++;
            }
        }

        public override double GetLength(string layer1, string layer2)
        {
            if (Board.ViaLength)
                return Board.Stackup.GetHeight(layer1, layer2);

            return 0;
        }
    }

    public class Pad : RouteElement
    {
        public string Name;

        public Pad(Dictionary<string, PadStack> PadStacks, Vec2 pos, string padstack, string name, string net)
        {
            Net = net;
            PadStack ps = PadStacks[padstack];
            Layer = new string[ps.PadInfo.Count];
            Nodes = new RouteNode[ps.PadInfo.Count];
            Type = "PAD";

            int i = 0;
            foreach (KeyValuePair<string, PadStack.PadStackLayerDef> def in ps.PadInfo)
            {
                Layer[i] = def.Key;
                Nodes[i] = new RouteNode(pos, Math.Min(def.Value.sx, def.Value.sy), this);
                i++;
            }
            Name = name;
        }

        public override double GetLength(string layer1, string layer2)
        {
            if (Board.ViaLength)
                return Board.Stackup.GetHeight(layer1, layer2);

            return 0;
        }
    }

    public class Arc : RouteElement
    {
        private double Length;

        public Arc(Vec2 center, Vec2 start, Vec2 end, double diameter, double width, string layer, string net)
        {
            Net = net;
            Layer = new string[] { layer };

            Nodes = new RouteNode[2];
            Nodes[0] = new RouteNode(start, width, this);
            Nodes[1] = new RouteNode(end, width, this);
            Type = "ARC";

            double angle;
            double aStart = (end - center).Angle();
            double aEnd = (start - center).Angle();
            if (aEnd < aStart) angle = (Math.PI * 2.0 - aStart) + aEnd;
            else angle = aEnd - aStart;

            Length = angle * diameter / 2;
        }

        public override double GetLength(string layer1, string layer2)
        {
            return Length;
        }
    }

    public class IntersectResult
    {
        public bool Intersects;
        public Vec2 location;
        public double dist;
        public RouteNode node1, node2;
        public string layer;
    }
}
