using FingerprintDecryptor.Bitmaps;

namespace FingerprintDecryptor.Preprocessing;

public static class GaborEnhancer
{
    private static byte ApplyFilter(GrayBitmap image, int x, int y, GaborFilter filter)
    {
        int half = filter.Size / 2;
        double sum = 0;

        for (int fy = 0; fy < filter.Size; fy++)
        {
            for (int fx = 0; fx < filter.Size; fx++)
            {
                int ix = x + fx - half;
                int iy = y + fy - half;

                if (ix < 0) ix = -ix;
                if (ix >= image.Width) ix = 2 * image.Width - ix - 2;
                if (iy < 0) iy = -iy;
                if (iy >= image.Height) iy = 2 * image.Height - iy - 2;

                sum += image.Pixels[ix, iy] * filter.Kernel[fx, fy];
            }
        }

        double localMean = EstimateLocalMean(image, x, y, filter.Size);
        double result = localMean + sum;
        return (byte)Math.Clamp(result, 0, 255);
    }

    private static double EstimateLocalMean(GrayBitmap image, int x, int y, int filterSize)
    {
        int half = filterSize / 2;
        double sum = 0;
        int count = 0;

        for (int dy = -half; dy <= half; dy++)
        {
            for (int dx = -half; dx <= half; dx++)
            {
                int ix = Math.Clamp(x + dx, 0, image.Width - 1);
                int iy = Math.Clamp(y + dy, 0, image.Height - 1);
                sum += image.Pixels[ix, iy];
                count++;
            }
        }

        return sum / count;
    }


    private static GaborFilter FindBestFilter(List<GaborFilter> bank, double theta, double freq)
    {
        GaborFilter bestFilter = null;
        double minDist = double.MaxValue;

        theta = theta % Math.PI;
        if (theta < 0) theta += Math.PI;

        foreach (var filter in bank)
        {
            double orientDiff = Math.Abs(theta - filter.Orientation);
            orientDiff = Math.Min(orientDiff, Math.PI - orientDiff);

            double freqDiff = Math.Abs(freq - filter.Frequency);

            double dist = orientDiff + freqDiff * 10.0;

            if (dist < minDist)
            {
                minDist = dist;
                bestFilter = filter;
            }
        }

        return bestFilter;
    }

    public static GrayBitmap Enhance(GrayBitmap image,
        double[,] orientation,
        double[,] frequency,
        List<GaborFilter> filterBank,
        bool[,] fingerMask = null)
    {
        int width = image.Width;
        int height = image.Height;
        var result = new GrayBitmap(width, height);

        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                if (fingerMask == null || fingerMask[x, y])
                {
                    double theta = orientation[x, y];
                    double freq = frequency[x, y];

                    if (double.IsNaN(theta) || double.IsNaN(freq) || freq <= 0)
                    {
                        result.Pixels[x, y] = image.Pixels[x, y];
                        continue;
                    }

                    var filter = FindBestFilter(filterBank, theta, freq);

                    if (filter != null)
                    {
                        result.Pixels[x, y] = ApplyFilter(image, x, y, filter);
                    }
                    else
                    {
                        result.Pixels[x, y] = image.Pixels[x, y];
                    }
                }
                else
                {
                    result.Pixels[x, y] = 255;
                }
            }
        });
        return result;
    }
}