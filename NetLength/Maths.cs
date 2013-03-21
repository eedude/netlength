using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetLength
{
    /// <summary>
    /// 2D Vector (double based)
    /// </summary>
    public class Vec2
    {
        public double x, y;

        public Vec2(double X, double Y)
        {
            x = X;
            y = Y;
        }
        /// <summary>
        /// Returns the absolute angle of this vector in the plane
        /// </summary>
        /// <returns></returns>
        public double Angle()
        {
            return Math.Atan2(y, x);
        }
        /// <summary>
        /// Returns the length of this vector
        /// </summary>
        /// <returns></returns>
        public double Abs()
        {
            return Math.Sqrt(x * x + y * y);
        }
        public Vec2 GetUnity()
        {
            return this / this.Abs();
        }
        public Vec2 GetNormal()
        {
            return new Vec2(this.y, -this.x);
        }

        /// <summary>
        /// Returns the scalar product of p1 and p2
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double ScalarProduct(Vec2 p1, Vec2 p2)
        {
            return (p1.x * p2.x + p1.y * p2.y);
        }

        public static Vec2 operator +(Vec2 p1, Vec2 p2)
        {
            return new Vec2(p1.x + p2.x, p1.y + p2.y);
        }
        public static Vec2 operator -(Vec2 p1, Vec2 p2)
        {
            return new Vec2(p1.x - p2.x, p1.y - p2.y);
        }

        public static Vec2 operator *(double m, Vec2 p1)
        {
            return new Vec2(p1.x * m, p1.y * m);
        }
        public static Vec2 operator *(Vec2 p1, double m)
        {
            return m * p1;
        }

        public static Vec2 operator /(Vec2 p1, double m)
        {
            return p1 * (1 / m);
        }
        public static Vec2 operator /(double m, Vec2 p1)
        {
            return p1 * (1 / m);
        }

        /// <summary>
        /// Creates a unity length vector based on the provided angle
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Vec2 FromAngle(double angle)
        {
            return new Vec2(Math.Cos(angle), Math.Sin(angle));
        }

        public override string ToString()
        {
            return x.ToString() + "/" + y.ToString();
        }

        public static double AngleBetween(Vec2 a, Vec2 b)
        {
            double dotProd = ScalarProduct(a, b);
            double lenProd = a.Abs() * b.Abs();
            double divOperation = dotProd / lenProd;
            return Math.Acos(divOperation);
        }

        public static Vec2 Zero = new Vec2(0, 0);
    }

    public static class Maths
    {
        public static double Dist(Vec2 p1, Vec2 p2)
        {
            return Math.Sqrt((p1.x - p2.x) * (p1.x - p2.x) + (p1.y - p2.y) * (p1.y - p2.y));
        }

        public static double AngleBetweenTwoPoints(Vec2 p1, Vec2 p2)
        {
            Vec2 p = p2 - p1;
            return Math.Atan2(p.y, p.x);
        }

        public static Vec2 ClosestPointOnSegment(Vec2 p1, Vec2 p2, Vec2 pt)
        {
            Vec2 length = p2 - p1;
            double l2 = length.Abs();

            if (l2 == 0) return p1;

            double t = Vec2.ScalarProduct(pt - p1, length) / l2;

            if (t < 0) return p1;
            if (t > 1) return p2;
            return p1 + t * length;
        }

        public static double DistToSegment(Vec2 p1, Vec2 p2, Vec2 pt)
        {
            return Maths.Dist(ClosestPointOnSegment(p1, p2, pt), pt);
        }

    }
}
