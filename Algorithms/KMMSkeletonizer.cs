using FingerprintDecryptor.Bitmaps;

namespace FingerprintDecryptor.Algorithms;

public class KMMSkeletonizer : ISkeletonizer
{
    public BinaryBitmap InputImage { get; set; }
    public BinaryBitmap OutputImage { get; set; }

    public KMMSkeletonizer(BinaryBitmap inputImage)
    {
        InputImage = inputImage.Clone();
        OutputImage = inputImage.Clone();
    }

    public void Skeletonize()
    {
        int width = InputImage.Width;
        int height = InputImage.Height;

        int[,] pixels = new int[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                pixels[i, j] = 0;
            }
        }

        for (int i = 1; i < width - 1; i++)
        {
            for (int j = 1; j < height - 1; j++)
            {
                if (InputImage.Pixels[i, j])
                {
                    pixels[i, j] = 1;
                }
            }
        }

        bool modified = true;
        while (modified)
        {
            modified = false;
            for (int i = 1; i < width - 1; i++)
            {
                for (int j = 1; j < height - 1; j++)
                {
                    if (pixels[i, j] == 1)
                    {
                        if (pixels[i, j + 1] == 0 || pixels[i, j - 1] == 0 ||
                            pixels[i + 1, j] == 0 || pixels[i - 1, j] == 0)
                        {
                            pixels[i, j] = 2;
                        }
                    }
                }
            }

            for (int i = 1; i < width - 1; i++)
            {
                for (int j = 1; j < height - 1; j++)
                {
                    if (pixels[i, j] == 2)
                    {
                        if (pixels[i + 1, j + 1] == 0 || pixels[i - 1, j - 1] == 0 ||
                            pixels[i + 1, j - 1] == 0 || pixels[i - 1, j + 1] == 0)
                        {
                            pixels[i, j] = 3;
                        }
                    }
                }
            }

            for (int i = 1; i < width - 1; i++)
            {
                for (int j = 1; j < height - 1; j++)
                {
                    if (pixels[i, j] > 1)
                    {
                        if (IsValue4(i, j))
                        {
                            pixels[i, j] = 4;
                        }
                    }
                }
            }

            for (int i = 1; i < width - 1; i++)
            {
                for (int j = 1; j < height - 1; j++)
                {
                    if (pixels[i, j] == 4)
                    {
                        pixels[i, j] = 0;
                        modified = true;
                    }
                }
            }

            int N = 2;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (pixels[i, j] == N)
                    {
                        if (CalculateWeightAndDelete(i, j))
                        {
                            pixels[i, j] = 0;
                            modified = true;
                        }
                        else
                        {
                            pixels[i, j] = 1;
                        }
                    }
                }
            }

            N = 3;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (pixels[i, j] == N)
                    {
                        if (CalculateWeightAndDelete(i, j))
                        {
                            pixels[i, j] = 0;
                            modified = true;
                        }
                        else
                        {
                            pixels[i, j] = 1;
                        }
                    }
                }
            }
        }

        OutputImage = new BinaryBitmap(width, height);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (pixels[i, j] > 0)
                {
                    OutputImage.Pixels[i, j] = true;
                }
            }
        }

        bool CalculateWeightAndDelete(int x, int y)
        {
            int sum = 0;
            if (pixels[x, y + 1] > 0) sum += 1;
            if (pixels[x + 1, y + 1] > 0) sum += 2;
            if (pixels[x + 1, y] > 0) sum += 4;
            if (pixels[x + 1, y - 1] > 0) sum += 8;
            if (pixels[x, y - 1] > 0) sum += 16;
            if (pixels[x - 1, y - 1] > 0) sum += 32;
            if (pixels[x - 1, y] > 0) sum += 64;
            if (pixels[x - 1, y + 1] > 0) sum += 128;
            if (deleteSums.Contains(sum)) return true;
            return false;
        }
        
        bool IsValue4(int x, int y)
        {
            int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

            var neighbors = new List<(int nx, int ny)>();
            for (int i = 0; i < 8; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];

                if (pixels[nx, ny] > 0)
                    neighbors.Add((nx, ny));
            }

            int count = neighbors.Count;

            if (count < 2 || count > 4)
                return false;

            int[] dx4 = { 0, 1, 0, -1 };
            int[] dy4 = { -1, 0, 1, 0 };

            var visited = new HashSet<(int, int)>();
            var queue = new Queue<(int, int)>();
            queue.Enqueue(neighbors[0]);
            visited.Add(neighbors[0]);

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();

                for (int i = 0; i < 4; i++)
                {
                    int nx = cx + dx4[i];
                    int ny = cy + dy4[i];

                    var candidate = (nx, ny);

                    if (neighbors.Contains(candidate))
                    {
                        visited.Add(candidate);
                        queue.Enqueue(candidate);
                    }
                }
            }

            bool isOneBlock = visited.Count == neighbors.Count;
            return isOneBlock;
        }
    }

    private int[] deleteSums =
    [
        3, 5, 7, 12, 13, 14, 15, 20, 21, 22, 23, 28, 29, 30, 31, 48, 52, 53, 54, 55, 56, 60, 61, 62, 63, 65, 67, 69, 71,
        77, 79, 80,
        81, 83, 84, 85, 86, 87, 88, 89, 91, 93, 94, 95, 97, 99, 101, 103, 109, 111, 112, 113, 115, 116, 117, 118,
        119, 120, 121, 123, 124, 125, 126,
        127, 131, 133, 135, 141, 143, 149, 151, 157, 159, 181, 183, 189, 191, 192, 193, 195, 197, 199, 205, 207, 208,
        209, 211, 212, 213, 214, 215, 216, 217, 219, 220,
        221, 222, 223, 224, 225, 227, 229, 231, 237, 239, 240, 241, 243, 244, 245, 246, 247, 248, 249, 251, 252, 253,
        254, 255
    ];
}