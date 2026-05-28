namespace FingerprintDecryptor.Preprocessing;

public class GaborFilter
{
    public double[,] Kernel { get; }
    public double Orientation { get; }
    public double Frequency { get; }
    public int Size { get; }

    public GaborFilter(double[,] kernel, double orientation, double frequency)
    {
        Kernel = kernel;
        Orientation = orientation;
        Frequency = frequency;
        Size = kernel.GetLength(0);
    }
}

public static class GaborBank
{
    private const double SigmaX = 3.0;
    private const double SigmaY = 3.0;

    public static List<GaborFilter> GenerateBank(int orientations = 8, int kernelSize = 31,
        double[]? frequencies = null)
    {
        frequencies ??= new[] { 1.0 / 8.0, 1.0 / 10.0, 1.0 / 12.0 };

        var bank = new List<GaborFilter>();

        for (int i = 0; i < orientations; i++)
        {
            double theta = i * Math.PI / orientations;

            foreach (double freq in frequencies)
            {
                var kernel = GenerateKernel(theta, freq, kernelSize);
                bank.Add(new GaborFilter(kernel, theta, freq));
            }
        }

        return bank;
    }

    private static double[,] GenerateKernel(double theta, double frequency, int size)
    {
        int half = size / 2;
        var kernel = new double[size, size];
        double sum = 0;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - half;
                int dy = y - half;

                double xPrime = dx * Math.Cos(theta) + dy * Math.Sin(theta);
                double yPrime = -dx * Math.Sin(theta) + dy * Math.Cos(theta);

                double gaussian = Math.Exp(-0.5 * ((xPrime * xPrime) / (SigmaX * SigmaX) +
                                                   (yPrime * yPrime) / (SigmaY * SigmaY)));

                double sinusoid = Math.Cos(2 * Math.PI * frequency * xPrime);

                kernel[x, y] = gaussian * sinusoid;
                sum += kernel[x, y];
            }
        }

        double mean = sum / (size * size);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                kernel[x, y] -= mean;
            }
        }

        double positiveSum = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (kernel[x, y] > 0)
                    positiveSum += kernel[x, y];
            }
        }

        if (positiveSum > 0)
        {
            double scale = 1.0 / positiveSum;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[x, y] *= scale;
                }
            }
        }

        return kernel;
    }
}