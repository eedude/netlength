using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLength
{
    public static class Board
    {
        public static bool ViaLength = false;
        public static LayerStack Stackup;
        public static Dictionary<string, PadStack> PadStacks;
        public static Dictionary<string, List<RouteElement>> Objects;
        public static Dictionary<string, List<Pad>> Pads;
        public static double AnalysisTime;

        public static void LoadBoard(string filename, List<string> NetsToLoad, bool viaslength)
        {
            ViaLength = viaslength;

            HypFile hyp = new HypFile();
            hyp.LoadFile(filename, NetsToLoad);

            Stackup = hyp.Stackup;
            PadStacks = hyp.PadStacks;
            Objects = hyp.Objects;
            Pads = hyp.Pads;
        }


        public static RouteElement FollowStart;
        public static List<RouteResult> FollowResults;

        public static void FollowAll()
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            FollowResults = new List<RouteResult>();

            foreach (KeyValuePair<string, List<Pad>> n in Pads)
            {
                foreach (Pad p in n.Value)
                {
                    FollowObject(p, null, null, 0, null);
                }
            }

            //FollowResults.Sort(delegate(RouteResult p1, RouteResult p2) { return p1.name.CompareTo(p2.name); });

            sw.Stop();
            AnalysisTime = sw.ElapsedMilliseconds;
        }

        private static bool FollowObject(RouteElement obj, List<RouteElement> followed, Vec2 curpos, double lgt, string curlayer)
        {
            if (followed == null)
            {
                FollowStart = obj;
                followed = new List<RouteElement>();
                followed.Add(obj);
                curlayer = null;
                curpos = obj.Nodes[0].Pos;
            }

            // if this is a pad, and we have followed at least one object, we're here!
            if (obj.Type == "PAD" && followed.Count > 1)
            {
                RouteResult r = new RouteResult();
                r.start = ((Pad)FollowStart).Name;
                r.last = ((Pad)obj).Name;
                r.net = ((Pad)obj).Net;
                r.length = lgt + Maths.Dist(curpos, obj.Nodes[0].Pos);
                r.name = r.net + "-" + r.start.Split('.')[0] + "-" + r.last.Split('.')[0];
                if (!FollowResults.Contains(r))
                    FollowResults.Add(r);
                return true;
            }

            List<IntersectResult> inter = new List<IntersectResult>();
            // Find all objects that intersects this one
            foreach (RouteElement intersecter in Objects[obj.Net])
            {
                // Do not consider ourself or objects already visited
                if (intersecter == obj) continue;
                if (followed.Contains(intersecter)) continue;

                List<IntersectResult> r = obj.Intersect(intersecter);
                if (r != null && r.Count > 0)
                {
                    inter.Add(r[0]);
                    followed.Add(r[0].node2.Parent);
                }
            }

            foreach (IntersectResult r in inter)
            {
                double l = lgt;
                l += obj.GetLength(curlayer, r.layer) + r.dist;
                FollowObject(r.node2.Parent, followed, r.node2.Pos, l, r.layer);
            }

            // Cul-de-sac
            return false;
        }
    }

    public class RouteResult : IEquatable<RouteResult>
    {
        public double length;
        public string start;
        public string last;
        public string net;
        public string name;

        public bool Equals(RouteResult other)
        {
            if ((this.start == other.start && this.last == other.last) || (this.last == other.start && this.start == other.last))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
