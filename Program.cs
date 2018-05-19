using System;


namespace autocorrelation
{
    class Program
    {
        static void Main(string[] args)
        {
            var formant = new Formants(4, 6);
            var sample = new Vector(new double[] {
                3.1, 14.1, -5.1, 46.1, -21.1, 31.5, -28.5, 4.6
            });
            var result = formant.StandardForm(sample);
            Console.WriteLine(result);
            var numpy_result = new Vector(new double[] {
                1.0,
                -0.6655717170963764,
                0.35135277712109464,
                -0.20410057538955617,
                -0.007250228352834534,
                -0.004610089351772599,
                0.021757155103485887
            });
            Console.WriteLine(
                "Difference between numpy and this is " + (numpy_result - result).Norm);
        }
    }
}
