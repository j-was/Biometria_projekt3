using FingerprintDecryptor.Bitmaps;

namespace FingerprintDecryptor;

public static class ImageProcessing
{
    public static bool[,] CreateMask(GrayBitmap image)
    {
        int width = image.Width;
        int height = image.Height;
        var mask = new bool[width, height];

        int windowSize = 15;
        double varianceThreshold = 1000;
        byte intensityThreshold = 50;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double variance = CalculateLocalVariance(image, x, y, windowSize);
                byte intensity = image.Pixels[x, y];

                mask[x, y] = variance > varianceThreshold || intensity < intensityThreshold;
            }
        }

        mask = SmoothMask(mask, width, height);

        mask = FillSmallHoles(mask, width, height);

        return mask;
    }

    private static double CalculateLocalVariance(GrayBitmap image, int startX, int startY, int blockSize)
    {
        double sum = 0, sumSq = 0;
        int count = 0;

        for (int y = startY; y < startY + blockSize && y < image.Height; y++)
        {
            for (int x = startX; x < startX + blockSize && x < image.Width; x++)
            {
                sum += image.Pixels[x, y];
                sumSq += image.Pixels[x, y] * image.Pixels[x, y];
                count++;
            }
        }

        double mean = sum / count;
        return (sumSq / count) - (mean * mean);
    }

    private static bool[,] SmoothMask(bool[,] mask, int width, int height)
    {
        var smoothed = new bool[width, height];
        double threshold = 0.5;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int trueCount = 0;
                int totalCount = 0;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            if (mask[nx, ny]) trueCount++;
                            totalCount++;
                        }
                    }
                }

                smoothed[x, y] = (double)trueCount / totalCount > threshold;
            }
        }

        return smoothed;
    }

    private static bool[,] FillSmallHoles(bool[,] mask, int width, int height)
    {
        var filled = (bool[,])mask.Clone();
        int maxHoleSize = 20;

        var visited = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!mask[x, y] && !visited[x, y])
                {
                    var hole = new List<(int x, int y)>();
                    bool touchesBorder = false;

                    FloodFill(x, y, mask, visited, hole, ref touchesBorder);

                    if (!touchesBorder && hole.Count <= maxHoleSize)
                    {
                        foreach (var pixel in hole)
                        {
                            filled[pixel.x, pixel.y] = true;
                        }
                    }
                }
            }
        }

        return filled;
    }

    private static void FloodFill(int startX, int startY, bool[,] mask, bool[,] visited,
        List<(int x, int y)> region, ref bool touchesBorder)
    {
        int width = mask.GetLength(0);
        int height = mask.GetLength(1);

        var stack = new Stack<(int x, int y)>();
        stack.Push((startX, startY));
        visited[startX, startY] = true;

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            region.Add((x, y));

            if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                touchesBorder = true;

            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (nx >= 0 && nx < width && ny >= 0 && ny < height
                    && !mask[nx, ny] && !visited[nx, ny])
                {
                    visited[nx, ny] = true;
                    stack.Push((nx, ny));
                }
            }
        }
    }


    public static GrayBitmap Normalize(GrayBitmap input, double desiredMean = 100.0, double desiredVariance = 100.0)
    {
        int width = input.Width;
        int height = input.Height;
        var output = new GrayBitmap(width, height);

        double sum = 0.0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                sum += input.Pixels[x, y];
            }
        }

        double mean = sum / (width * height);

        double sumSqDiff = 0.0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double diff = input.Pixels[x, y] - mean;
                sumSqDiff += diff * diff;
            }
        }

        double variance = sumSqDiff / (width * height);
        double stdDev = Math.Sqrt(variance);

        double desiredStdDev = Math.Sqrt(desiredVariance);

        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                double normalized;
                if (stdDev > 1e-6)
                {
                    normalized = desiredMean + (input.Pixels[x, y] - mean) * (desiredStdDev / stdDev);
                }
                else
                {
                    normalized = desiredMean;
                }

                output.Pixels[x, y] = (byte)Math.Clamp(normalized, 0, 255);
            }
        });

        return output;
    }

    public static BinaryBitmap AdaptiveBinarize(GrayBitmap image, int windowSize = 15)
    {
        int width = image.Width;
        int height = image.Height;
        var binary = new BinaryBitmap(width, height);

        long[,] integral = new long[width, height];
        for (int y = 0; y < height; y++)
        {
            long sum = 0;
            for (int x = 0; x < width; x++)
            {
                sum += image.Pixels[x, y];
                integral[x, y] = (y == 0) ? sum : integral[x, y - 1] + sum;
            }
        }

        int s2 = windowSize / 2;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int x1 = Math.Max(0, x - s2);
                int y1 = Math.Max(0, y - s2);
                int x2 = Math.Min(width - 1, x + s2);
                int y2 = Math.Min(height - 1, y + s2);

                int count = (x2 - x1) * (y2 - y1);
                long sum = integral[x2, y2] - integral[x1, y2] - integral[x2, y1] + integral[x1, y1];

                if (image.Pixels[x, y] < sum / count * 0.95)
                    binary.Pixels[x, y] = true;
                else
                    binary.Pixels[x, y] = false;
            }
        }

        return binary;
    }
}