using FingerprintDecryptor.Bitmaps;

namespace FingerprintDecryptor.Preprocessing;

public static class Estimators
{
    public static double[,] EstimateOrientation(GrayBitmap image, int blockSize = 16)
    {
        int width = image.Width;
        int height = image.Height;
        int blocksX = (width + blockSize - 1) / blockSize;
        int blocksY = (height + blockSize - 1) / blockSize;

        var orientation = new double[blocksX, blocksY];

        Parallel.For(0, blocksY, by =>
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                int startX = bx * blockSize;
                int startY = by * blockSize;
                orientation[bx, by] = EstimateBlockOrientation(image, startX, startY, blockSize);
            }
        });

        orientation = SmoothOrientationField(orientation, blocksX, blocksY);

        return orientation;
    }

    private static double EstimateBlockOrientation(GrayBitmap image, int startX, int startY, int blockSize)
    {
        double vx = 0, vy = 0;

        int endX = Math.Min(startX + blockSize, image.Width - 1);
        int endY = Math.Min(startY + blockSize, image.Height - 1);
        startX = Math.Max(startX, 1);
        startY = Math.Max(startY, 1);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int gx = (image.Pixels[x + 1, y - 1] - image.Pixels[x - 1, y - 1]) +
                         (2 * image.Pixels[x + 1, y] - 2 * image.Pixels[x - 1, y]) +
                         (image.Pixels[x + 1, y + 1] - image.Pixels[x - 1, y + 1]);

                int gy = (image.Pixels[x - 1, y + 1] - image.Pixels[x - 1, y - 1]) +
                         (2 * image.Pixels[x, y + 1] - 2 * image.Pixels[x, y - 1]) +
                         (image.Pixels[x + 1, y + 1] - image.Pixels[x + 1, y - 1]);

                vx += 2 * gx * gy;
                vy += gx * gx - gy * gy;
            }
        }

        double theta = 0.5 * Math.Atan2(vx, vy);

        theta = theta % Math.PI;
        if (theta < 0) theta += Math.PI;

        return theta;
    }

    private static double[,] SmoothOrientationField(double[,] orientation, int blocksX, int blocksY)
    {
        var smoothed = new double[blocksX, blocksY];

        var sin2Theta = new double[blocksX, blocksY];
        var cos2Theta = new double[blocksX, blocksY];

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                sin2Theta[bx, by] = Math.Sin(2 * orientation[bx, by]);
                cos2Theta[bx, by] = Math.Cos(2 * orientation[bx, by]);
            }
        }

        Parallel.For(1, blocksY - 1, by =>
        {
            for (int bx = 1; bx < blocksX - 1; bx++)
            {
                double sumSin = 0, sumCos = 0;
                double totalWeight = 0;

                double[,] weights = new double[,]
                {
                    { 1, 2, 1 },
                    { 2, 4, 2 },
                    { 1, 2, 1 }
                };

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        double weight = weights[dy + 1, dx + 1];
                        sumSin += weight * sin2Theta[bx + dx, by + dy];
                        sumCos += weight * cos2Theta[bx + dx, by + dy];
                        totalWeight += weight;
                    }
                }

                double avgSin = sumSin / totalWeight;
                double avgCos = sumCos / totalWeight;
                smoothed[bx, by] = 0.5 * Math.Atan2(avgSin, avgCos);

                if (smoothed[bx, by] < 0) smoothed[bx, by] += Math.PI;
            }
        });

        for (int bx = 0; bx < blocksX; bx++)
        {
            smoothed[bx, 0] = orientation[bx, 0];
            smoothed[bx, blocksY - 1] = orientation[bx, blocksY - 1];
        }

        for (int by = 0; by < blocksY; by++)
        {
            smoothed[0, by] = orientation[0, by];
            smoothed[blocksX - 1, by] = orientation[blocksX - 1, by];
        }

        return smoothed;
    }

    public static double[,] EstimateFrequency(GrayBitmap image, double[,] orientation, int blockSize = 16,
        int windowSize = 32)
    {
        int width = image.Width;
        int height = image.Height;
        int blocksX = orientation.GetLength(0);
        int blocksY = orientation.GetLength(1);

        var frequency = new double[blocksX, blocksY];

        Parallel.For(0, blocksY, by =>
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                double theta = orientation[bx, by];
                int centerX = bx * blockSize + blockSize / 2;
                int centerY = by * blockSize + blockSize / 2;

                frequency[bx, by] = EstimateBlockFrequency(image, centerX, centerY, theta, windowSize);
            }
        });

        frequency = SmoothFrequencyField(frequency, blocksX, blocksY);

        return frequency;
    }

    private static double EstimateBlockFrequency(GrayBitmap image, int centerX, int centerY, double theta,
        int windowSize)
    {
        int halfWindow = windowSize / 2;
        var signal = new List<double>();

        double perpendicularTheta = theta + Math.PI / 2;

        for (double d = -halfWindow; d < halfWindow; d += 0.5)
        {
            double x = centerX + d * Math.Cos(perpendicularTheta);
            double y = centerY + d * Math.Sin(perpendicularTheta);

            int x0 = (int)Math.Floor(x), x1 = x0 + 1;
            int y0 = (int)Math.Floor(y), y1 = y0 + 1;

            if (x0 >= 0 && x1 < image.Width && y0 >= 0 && y1 < image.Height)
            {
                double fx = x - x0, fy = y - y0;
                double val = image.Pixels[x0, y0] * (1 - fx) * (1 - fy) +
                             image.Pixels[x1, y0] * fx * (1 - fy) +
                             image.Pixels[x0, y1] * (1 - fx) * fy +
                             image.Pixels[x1, y1] * fx * fy;
                signal.Add(val);
            }
        }

        if (signal.Count < windowSize / 2)
            return 0.1;

        var peaks = new List<int>();
        for (int i = 2; i < signal.Count - 2; i++)
        {
            double localMax = signal.Max();
            double localMin = signal.Min();
            double threshold = localMin + (localMax - localMin) * 0.5;

            if (signal[i] > signal[i - 1] && signal[i] > signal[i - 2] &&
                signal[i] > signal[i + 1] && signal[i] > signal[i + 2] &&
                signal[i] > threshold)
            {
                peaks.Add(i);
            }
        }

        if (peaks.Count < 2)
            return 0.1;

        double totalDistance = 0;
        int count = 0;
        for (int i = 1; i < peaks.Count; i++)
        {
            double distance = peaks[i] - peaks[i - 1];
            if (distance > 3)
            {
                totalDistance += distance;
                count++;
            }
        }

        if (count < 1)
            return 0.1;

        double avgDistance = totalDistance / count;

        double frequency = 1.0 / avgDistance;
        frequency = Math.Clamp(frequency, 0.05, 0.25);

        return frequency;
    }

    private static double[,] SmoothFrequencyField(double[,] frequency, int blocksX, int blocksY)
    {
        var smoothed = new double[blocksX, blocksY];

        Parallel.For(1, blocksY - 1, by =>
        {
            for (int bx = 1; bx < blocksX - 1; bx++)
            {
                double sum = 0;
                double totalWeight = 0;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        double weight = dx == 0 && dy == 0 ? 4 : 1;
                        sum += weight * frequency[bx + dx, by + dy];
                        totalWeight += weight;
                    }
                }

                smoothed[bx, by] = sum / totalWeight;
            }
        });

        for (int bx = 0; bx < blocksX; bx++)
        {
            smoothed[bx, 0] = frequency[bx, 0];
            smoothed[bx, blocksY - 1] = frequency[bx, blocksY - 1];
        }

        for (int by = 0; by < blocksY; by++)
        {
            smoothed[0, by] = frequency[0, by];
            smoothed[blocksX - 1, by] = frequency[blocksX - 1, by];
        }

        return smoothed;
    }

    public static double[,] Upsample(double[,] blockParam, int blockSize, int width, int height)
    {
        int blocksX = blockParam.GetLength(0);
        int blocksY = blockParam.GetLength(1);
        var pixelParam = new double[width, height];

        Parallel.For(0, height, y =>
        {
            int by = Math.Min(y / blockSize, blocksY - 1);
            for (int x = 0; x < width; x++)
            {
                int bx = Math.Min(x / blockSize, blocksX - 1);
                pixelParam[x, y] = blockParam[bx, by];
            }
        });

        return pixelParam;
    }
}