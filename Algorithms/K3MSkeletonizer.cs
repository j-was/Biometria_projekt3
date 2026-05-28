using FingerprintDecryptor.Bitmaps;

namespace FingerprintDecryptor.Algorithms;

public class K3MSkeletonizer : ISkeletonizer
{
    public BinaryBitmap InputImage { get; set; }
    public BinaryBitmap OutputImage { get; set; }

    public K3MSkeletonizer(BinaryBitmap inputImage)
    {
        InputImage = inputImage.Clone();
        OutputImage = inputImage.Clone();
    }

    private int[,] N = { { 128, 1, 2 }, { 64, 0, 4 }, { 32, 16, 8 } };

    private int[][] A = new[]
    {
        new int[]
        {
            3, 6, 7, 12, 14, 15, 24, 28, 30, 31, 48, 56, 60,
            62, 63, 96, 112, 120, 124, 126, 127, 129, 131, 135,
            143, 159, 191, 192, 193, 195, 199, 207, 223, 224,
            225, 227, 231, 239, 240, 241, 243, 247, 248, 249,
            251, 252, 253, 254
        },
        new int[] { 7, 14, 28, 56, 112, 131, 193, 224 },
        new int[]
        {
            7, 14, 15, 28, 30, 56, 60, 112, 120, 131, 135,
            193, 195, 224, 225, 240
        },
        new int[]
        {
            7, 14, 15, 28, 30, 31, 56, 60, 62, 112, 120,
            124, 131, 135, 143, 193, 195, 199, 224, 225, 227,
            240, 241, 248
        },
        new int[]
        {
            7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120,
            124, 126, 131, 135, 143, 159, 193, 195, 199, 207,
            224, 225, 227, 231, 240, 241, 243, 248, 249, 252
        },
        new int[]
        {
            7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120,
            124, 126, 131, 135, 143, 159, 191, 193, 195, 199,
            207, 224, 225, 227, 231, 239, 240, 241, 243, 248,
            249, 251, 252, 254
        }
    };

    private int[] A1pix = new int[]
    {
        3, 6, 7, 12, 14, 15, 24, 28, 30, 31, 48, 56,
        60, 62, 63, 96, 112, 120, 124, 126, 127, 129, 131,
        135, 143, 159, 191, 192, 193, 195, 199, 207, 223,
        224, 225, 227, 231, 239, 240, 241, 243, 247, 248,
        249, 251, 252, 253, 254
    };

    public void Skeletonize()
    {
        int width = InputImage.Width;
        int height = InputImage.Height;

        int[,] pixels = new int[width, height];
        bool[,] borders = new bool[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (InputImage.Pixels[i, j])
                {
                    pixels[i, j] = 1;
                }
                else
                {
                    pixels[i, j] = 0;
                }

                borders[i, j] = false;
            }
        }

        int numIt = 0;
        bool modified = true;
        while (modified)
        {
            modified = false;
            // PHASE 0
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (pixels[i, j] > 0)
                    {
                        if (A[0].Contains(weight(i, j)))
                        {
                            borders[i, j] = true;
                        }
                    }
                }
            }

            // PHASE 1-5
            for (int k = 1; k <= 5; k++)
            {
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        if (borders[i, j])
                        {
                            if (A[k].Contains(weight(i, j)))
                            {
                                pixels[i, j] = 0;
                                modified = true;
                            }
                        }
                    }
                }
            }

            // PHASE 6
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (borders[i, j])
                    {
                        borders[i, j] = false;
                    }
                }
            }


            numIt++;
            if (numIt > 100)
            {
                modified = false;
            }
        }

        // Thinning phase
        modified = true;
        while (modified)
        {
            modified = false;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (pixels[i, j] > 0)
                    {
                        if (A1pix.Contains(weight(i, j)))
                        {
                            pixels[i, j] = 0;
                            modified = true;
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


        int weight(int x, int y)
        {
            int res = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    int nx = x + i;
                    int ny = y + j;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        res += N[i + 1, j + 1] * pixels[x + i, y + j];
                    }
                }
            }

            return res;
        }
    }
}