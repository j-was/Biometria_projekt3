using FingerprintDecryptor.Bitmaps;

namespace FingerprintDecryptor;

public class MinutiaeFinder
{
    public DirectBitmap Original { get; set; }
    public BinaryBitmap Input { get; set; }
    private BinaryBitmap Skeleton { get; set; }
    public DirectBitmap Results { get; set; }
    public DirectBitmap SkeletonMarked { get; set; }

    private int markRadius = 12;

    public event Action<Bitmap>? StageImageAdded;

    private void AddStageImage(Bitmap bitmap)
    {
        StageImageAdded?.Invoke(bitmap);
    }

    public MinutiaeFinder(BinaryBitmap skeleton, DirectBitmap original)
    {
        Original = original;
        Input = skeleton;
        Skeleton = skeleton.Clone();
        Results = original.Clone();
    }

    public Task FindAsync()
    {
        return Task.Run(() => { FindMinutions(); });
    }

    private List<(int x, int y)> endings = new List<(int x, int y)>();
    private List<(int x, int y)> bifurcations = new List<(int x, int y)>();

    private void FindMinutions()
    {
        for (int i = 2; i < Skeleton.Width - 2; i++)
        {
            for (int j = 2; j < Skeleton.Height - 2; j++)
            {
                if (Skeleton.Pixels[i, j])
                {
                    if (Selector.Algorithm == SkeletonizeAlgorithm.K3M)
                    {
                        FindMinutionInLocalArea(i, j);
                    }
                    else
                    {
                        int n = CountNeighbours(i, j);
                        if (n >= 3)
                        {
                            bifurcations.Add((i, j));
                        }
                        
                        if (n ==1)
                        {
                            endings.Add((i, j));
                        }
                    }
                   
                }
            }
        }
        
        VerifyMinutions();

        SkeletonMarked = Skeleton.ToDirectBitmap();

        MarkBifurcations();
        MarkEndings();
        AddStageImage(Results.ToBitmap());
    }

    private void FindMinutionInLocalArea(int x, int y, int areaHalfWidth = 2)
    {
        if (!Skeleton.Pixels[x, y])
            return;
        
        int width  = Skeleton.Width;
        int height = Skeleton.Height;

        if (x < areaHalfWidth || y < areaHalfWidth || x >= width - areaHalfWidth || y >= height - areaHalfWidth)
            return;
        
        bool[] perimeter = new bool[16];
        int perimeterIndex = 0;

        for (int i = -areaHalfWidth; i < areaHalfWidth; i++)
            perimeter[perimeterIndex++] = Skeleton.Pixels[x + i, y - areaHalfWidth];
        for (int i = -areaHalfWidth; i < areaHalfWidth; i++)
            perimeter[perimeterIndex++] = Skeleton.Pixels[x + areaHalfWidth, y + i];
        for (int i = areaHalfWidth; i > -areaHalfWidth; i--)
            perimeter[perimeterIndex++] = Skeleton.Pixels[x + i, y + areaHalfWidth];
        for (int i = areaHalfWidth; i > -areaHalfWidth; i--)
            perimeter[perimeterIndex++] = Skeleton.Pixels[x - areaHalfWidth, y + i];

        int transitions = 0;
        bool next, current = perimeter[0];
        for (int i = 1; i <= perimeter.Length; i++)
        {
            next = perimeter[i % perimeter.Length];

            if (!current && next)
            {
                transitions++;
            }

            current = next;
        }

        switch (transitions)
        {
            case 0:
            case 2:
                break;
            case 1:
                endings.Add((x, y));
                break;
            default:
                bifurcations.Add((x, y));
                break;
        }
    }


    private void VerifyMinutions()
    {
        var mask = ImageProcessing.CreateMask(new GrayBitmap(Original.ToBitmap()));
        var mpp = new MinutiaePostProcessor(Skeleton, mask);
        mpp.CleanSpuriousMinutiae(endings, bifurcations);
    }

    
    private int CountNeighbours(int x, int y)
    {
        int res = 0;
        int width = Skeleton.Width;
        int height = Skeleton.Height;
    
        if (x + 1 < width && y - 1 >= 0 && Skeleton.Pixels[x + 1, y - 1]) res++;
        if (x + 1 < width && Skeleton.Pixels[x + 1, y]) res++;
        if (x + 1 < width && y + 1 < height && Skeleton.Pixels[x + 1, y + 1]) res++;
        if (y - 1 >= 0 && Skeleton.Pixels[x, y - 1]) res++;
        if (y + 1 < height && Skeleton.Pixels[x, y + 1]) res++;
        if (x - 1 >= 0 && y - 1 >= 0 && Skeleton.Pixels[x - 1, y - 1]) res++;
        if (x - 1 >= 0 && Skeleton.Pixels[x - 1, y]) res++;
        if (x - 1 >= 0 && y + 1 < height && Skeleton.Pixels[x - 1, y + 1]) res++;
    
        return res;
    }

    private void MarkEndings()
    {
        foreach (var end in endings)
        {
            DrawMarking(end.x, end.y, (0, 255, 0));
        }
    }

    private void MarkBifurcations()
    {
        foreach (var bif in bifurcations)
        {
            DrawMarking(bif.x, bif.y, (255, 0, 0));
        }
    }

    private void DrawMarking(int X, int Y, (byte, byte, byte) color)
    {
        int r = markRadius;
        var bmp = Results;
        var bmp2 = SkeletonMarked;
        for (int i = -r; i < r; i++)
        {
            if (X + i > 0 && X + i < Skeleton.Width)
            {
                int y = (int)Math.Sqrt(r * r - i * i);
                if (Y + y < bmp.Height)
                {
                    bmp.Pixels[X + i, Y + y] = color;
                    bmp.Pixels[X + i, Y + y - 1] = color;
                    bmp2.Pixels[X + i, Y + y] = color;
                    bmp2.Pixels[X + i, Y + y - 1] = color;
                }

                if (Y - y >= 0)
                {
                    bmp.Pixels[X + i, Y - y] = color;
                    bmp.Pixels[X + i, Y - y + 1] = color;
                    bmp2.Pixels[X + i, Y - y] = color;
                    bmp2.Pixels[X + i, Y - y + 1] = color;
                }
            }
        }
    }
}