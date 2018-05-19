using System;


namespace autocorrelation
{
    class Formants
    {
        private int hw;
        private Vector d;
        private Vector hann_filter;

        private int Hw { get => hw; }
        private Vector D { get => d; }
        private Vector Hann_filter { get => hann_filter; }

        private int formants;

        public Formants(int halfwidth, int num_formants)
        {
            hw = halfwidth;
            d = new Vector(new double[4 * Hw - 1]);
            for (int i = 0; i < 4 * Hw - 1; i++)
            {
                D[i] = 2 * Hw;
            }
            hann_filter = Hann(2 * Hw);
            formants = num_formants;
        }

        private Vector Hann(int bins)
        {
            var ret = new double[bins];
            for (int i = 0; i < bins; i++)
            {
                var s = Math.Sin((Math.PI * i) / (bins - 1));
                ret[i] = (double)(s * s);
            }
            return new Vector(ret);
        }

        public Vector StandardForm(Vector sample)
        {
            var hanned = Hann_filter * sample;
            var noDC = hanned - hanned.Mean;
            var autocor = noDC.Autocorrelate();
            var acov = autocor / D;
            acov = acov.SliceFrom(2 * Hw - 1);
            acov = acov.SliceTo(formants + 1);
            var acf = acov / (acov[0] + 1e-20);
            return acf;
        }
    }
}
