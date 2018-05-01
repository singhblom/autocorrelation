using System;
using System.IO;
using System.Linq;

namespace autocorrelation
{
    class Formants
    {

        private int hw;
        private double [] d;
        private double [] hann_filter;

        private int Hw { get => hw; }
        private double[] D { get => d; }
        private double[] Hann_filter { get => hann_filter; }

        private int formants;

        public Formants(int halfwidth, int num_formants)
        {
            hw = halfwidth;
            d = new double[4*Hw-1];
            for(int i=0; i<4*Hw-1; i++)
            {
                D[i] = 2*Hw;
            }
            hann_filter = Hann(2*Hw);
            formants = num_formants;
            // hann_filter = new double []
            // {
            //     0f, 0.11697778f, 0.41317591f, 0.75f, 0.96984631f,
            //     0.96984631f, 0.75f, 0.41317591f, 0.11697778f,  0f
            // };
        }

        private double [] Hann (int bins)
        {
           var ret = new double[bins];
           for(int i = 0; i<bins; i++)
           {
               var s = Math.Sin((Math.PI * i)/(bins-1));
               ret[i] = (double) (s*s);
           }
           return ret;
        }
         
        private double Mean (double [] x)
        {
            var sum = 0.0;
            for (int i = 0; i < x.Length; i ++)
                sum += x[i];
            return sum / x.Length;
        }

        private double [] VectorTimesVector(double [] x, double [] y)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i]*y[i];
            }
            return ret;
        }

        private double [] VectorDivideVector(double [] x, double [] y)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i]/y[i];
            }
            return ret;
        }

        private double [] VectorSubtractScalar(double [] x, double a)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i] - a;
            }
            return ret;
        }

        private double [] VectorDivideScalar(double [] x, double a)
        {
            var ret = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                ret[i] = x[i] / a;
            }
            return ret;
        }

        private double [] SliceTo(double []x, int index)
        {
            int len;
            if(index >= 0)
            {
                len = Math.Min(x.Length, index);
            } else
            {
                len = Math.Max(0, x.Length+index);
            }
            var ret = new double[len];
            for(int i=0; i<len; i++)
            {
                ret[i] = x[i];
            }
            return ret;
        }

        private double [] SliceFrom(double []x, int index)
        {
            int start;
            int len;
            if(index >= 0)
            {
                start = index;
                len = x.Length-Math.Min(x.Length, index);
            } else
            {
                start = x.Length+index;
                len = -index;
            }
            var ret = new double[len];
            for(int i=start; i<x.Length; i++)
            {
                ret[i-start] = x[i];
            }
            return ret;
        }

        //c_{av}[k] = sum_n a[n+k] * conj(v[n])
        private double [] Autocorrelate (double []x)
        {
            var c_av = new double[2*x.Length-1];
            for(int k=0; k<x.Length-1; k++)
            {
                c_av[k] = 0;
                for(int n=0;n<=k; n++)
                {
                    c_av[k] += x[n]*x[x.Length-k+n-1];
                }
            }
            for(int k=1; k<x.Length; k++)
            {
                c_av[k+x.Length-1] = 0;
                for(int n=0; n<x.Length-k; n++)
                {
                    c_av[k+x.Length-1] += x[k+n]*x[n];
                }
            }
            c_av[x.Length-1] = 0;
            for(int n=0; n<x.Length; n++)
            {
                c_av[x.Length-1] += x[n]*x[n];
            }
            return c_av;
        }

        public string VectorToString(double [] x)
        {
            return "["
                + String.Join(", ", x.Select(
                    xx => string.Format("{0}", xx))) 
                + "]";
        }

        public double [] StandardForm(double [] sample)
        {
            var hanned = VectorTimesVector(Hann_filter, sample);
            var noDC = VectorSubtractScalar(hanned, Mean(hanned));
            var autocor = Autocorrelate(noDC);
            var acov = VectorDivideVector(autocor, D);
            acov = SliceFrom(acov, 2*Hw-1);
            acov = SliceTo(acov, formants+1);
            var acf = VectorDivideScalar(acov, acov[0]+1e-20);
            return acf;
        }

    // For a nice wav reader, see also https://gist.github.com/yomakkkk/2290864.


    // convert two bytes to one double in the range -1 to 1
    static double bytesToDouble(byte firstByte, byte secondByte) {
        // convert two bytes to one short (little endian)
        short s = (short) ((secondByte << 8) | firstByte);
        // convert to range from -1 to (just below) 1
        return s / 32768.0;
    }

    // Returns left and right double arrays. 'right' will be null if sound is mono.
    public void openWav(string filename, out double[] left, out double[] right)
    {
        byte[] wav = File.ReadAllBytes(filename);

        // Determine if mono or stereo
        int channels = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

        // Get past all the other sub chunks to get to the data subchunk:
        int pos = 12;   // First Subchunk ID from 12 to 16

        // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
        while(!(wav[pos]==100 && wav[pos+1]==97 && wav[pos+2]==116 && wav[pos+3]==97)) {
            pos += 4;
            int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
            pos += 4 + chunkSize;
        }
        pos += 8;

        // Pos is now positioned to start of actual sound data.
        int samples = (wav.Length - pos)/2;     // 2 bytes per sample (16 bit sound mono)
        if (channels == 2) samples /= 2;        // 4 bytes per sample (16 bit stereo)

        // Allocate memory (right will be null if only mono sound)
        left = new double[samples];
        if (channels == 2) right = new double[samples];
        else right = null;

        // Write to double array/s:
        int i=0;
        while (pos < wav.Length) {
            left[i] = bytesToDouble(wav[pos], wav[pos + 1]);
            pos += 2;
            if (channels == 2) {
                right[i] = bytesToDouble(wav[pos], wav[pos + 1]);
                pos += 2;
            }
            i++;
        }
    }

    }
    class Program
    {
        static void Main(string[] args)
        {
            var formant = new Formants(4, 6);
            var sample = new double [] {
                3.1, 14.1, -5.1, 46.1, -21.1, 31.5, -28.5, 4.6
            };
            var result = formant.StandardForm(sample);
            Console.WriteLine(formant.VectorToString(result));
        }
    }
}
