using FingerprintDecryptor.Bitmaps;
using System;
using System.Collections.Generic;

namespace FingerprintDecryptor.Algorithms;

public class MorphologicalSkeletonizer : ISkeletonizer
{
    public BinaryBitmap InputImage { get; set; }
    public BinaryBitmap OutputImage { get; set; }

    public MorphologicalSkeletonizer(BinaryBitmap inputImage)
    {
        InputImage = inputImage.Clone();
        OutputImage = inputImage.Clone();
    }

    private readonly int[][,] Masks = new int[8][,]
    {
        new int[3, 3]
        {
            { 0, 0, 0 },
            { -1, 1, -1 },
            { 1, 1, 1 }
        },
        new int[3, 3]
        {
            { -1, 0, 0 },
            { 1, 1, 0 },
            { -1, 1, -1 }
        },
        new int[3, 3]
        {
            { 1, -1, 0 },
            { 1, 1, 0 },
            { 1, -1, 0 }
        },
        new int[3, 3]
        {
            { -1, 1, -1 },
            { 1, 1, 0 },
            { -1, 0, 0 }
        },
        new int[3, 3]
        {
            { 1, 1, 1 },
            { -1, 1, -1 },
            { 0, 0, 0 }
        },
        new int[3, 3]
        {
            { -1, 1, -1 },
            { 0, 1, 1 },
            { 0, 0, -1 }
        },
        new int[3, 3]
        {
            { 0, -1, 1 },
            { 0, 1, 1 },
            { 0, -1, 1 }
        },
        new int[3, 3]
        {
            { 0, 0, -1 },
            { 0, 1, 1 },
            { -1, 1, -1 }
        }
    };

    public void Skeletonize()
    {
        int width = InputImage.Width;
        int height = InputImage.Height;

        var pixels = new bool[width, height];
        Array.Copy(InputImage.Pixels, pixels, width * height);

        bool changed;

        do
        {
            changed = false;

            for (int m = 0; m < 8; m++)
            {
                var toRemove = new List<(int x, int y)>();

                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        if (!pixels[x, y]) continue;

                        if (MatchesMask(pixels, x, y, Masks[m]))
                        {
                            toRemove.Add((x, y));
                        }
                    }
                }

                if (toRemove.Count > 0)
                {
                    foreach (var (x, y) in toRemove)
                    {
                        pixels[x, y] = false;
                    }

                    changed = true;
                }
            }
        } while (changed);

        var result = new BinaryBitmap(width, height);
        Array.Copy(pixels, result.Pixels, width * height);
        OutputImage = result;
    }

    private static bool MatchesMask(bool[,] pixels, int cx, int cy, int[,] mask)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int maskValue = mask[dy + 1, dx + 1];

                if (maskValue == -1) continue;

                bool pixelValue = pixels[cx + dx, cy + dy];
                bool expectedValue = maskValue == 1;

                if (pixelValue != expectedValue) return false;
            }
        }

        return true;
    }
}