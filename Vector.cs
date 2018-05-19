using System;
using System.Linq;

namespace autocorrelation
{
    class Vector
    {
        private double[] x;

        public Vector(double[] data)
        {
            if (data.Length == 0) { throw new Exception("Dimension must be at least one"); }
            x = data;
        }

        // Define the indexer to allow client code to use [] notation.
        public double this[int i]
        {
            get { return x[i]; }
            set { x[i] = value; }
        }

        public int Length { get => x.Length; }

        public double Max => this.x.Aggregate(Double.MinValue, (x, y) => Math.Max(x, y));

        public double Min => this.x.Aggregate(Double.MaxValue, (x, y) => Math.Min(x, y));

        public double Sum => this.x.Aggregate(0.0, (x, y) => x + y);

        public double Mean => this.Sum / this.Length;

        public double Norm => Math.Sqrt(this.Dot(this));

        public double Dot(Vector y)
        {
            if (this.Length != y.Length) { throw new Exception("Dimensions in dot product have to be equal"); }
            var ret = 0.0;
            for (int i = 0; i < x.Length; i++)
            {
                ret += x[i] * y[i];
            }
            return ret;
        }

        public static Vector operator *(Vector x, Vector y)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i] * y[i];
            }
            return new Vector(ret);
        }

        public static Vector operator /(Vector x, Vector y)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i] / y[i];
            }
            return new Vector(ret);
        }


        public static Vector operator +(Vector x, Vector y)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i] + y[i];
            }
            return new Vector(ret);
        }

        public static Vector operator -(Vector x, Vector y)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i] - y[i];
            }
            return new Vector(ret);
        }

        public static Vector operator *(Vector x, double a)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i] * a;
            }
            return new Vector(ret);
        }

        public static Vector operator /(Vector x, double a)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i] / a;
            }
            return new Vector(ret);
        }
        public static Vector operator +(Vector x, double a)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i] + a;
            }
            return new Vector(ret);
        }

        public static Vector operator -(Vector x, double a)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i] - a;
            }
            return new Vector(ret);
        }

        public Vector SliceTo(int index)
        {
            int len;
            if (index >= 0)
            {
                len = Math.Min(x.Length, index);
            }
            else
            {
                len = Math.Max(0, x.Length + index);
            }
            var ret = new double[len];
            for (int i = 0; i < len; i++)
            {
                ret[i] = x[i];
            }
            return new Vector(ret);
        }

        public Vector SliceFrom(int index)
        {
            int start;
            int len;
            if (index >= 0)
            {
                start = index;
                len = x.Length - Math.Min(x.Length, index);
            }
            else
            {
                start = x.Length + index;
                len = -index;
            }
            var ret = new double[len];
            for (int i = start; i < x.Length; i++)
            {
                ret[i - start] = x[i];
            }
            return new Vector(ret);
        }

        //c_{av}[k] = sum_n a[n+k] * conj(v[n])
        public Vector Autocorrelate()
        {
            var c_av = new double[2 * x.Length - 1];
            for (int k = 0; k < x.Length - 1; k++)
            {
                c_av[k] = 0;
                for (int n = 0; n <= k; n++)
                {
                    c_av[k] += x[n] * x[x.Length - k + n - 1];
                }
            }
            for (int k = 1; k < x.Length; k++)
            {
                c_av[k + x.Length - 1] = 0;
                for (int n = 0; n < x.Length - k; n++)
                {
                    c_av[k + x.Length - 1] += x[k + n] * x[n];
                }
            }
            c_av[x.Length - 1] = 0;
            for (int n = 0; n < x.Length; n++)
            {
                c_av[x.Length - 1] += x[n] * x[n];
            }
            return new Vector(c_av);
        }

        override public string ToString()
        {
            return "["
                + String.Join(", ", x.Select(
                    xx => string.Format("{0}", xx)))
                + "]";
        }
    }
}
