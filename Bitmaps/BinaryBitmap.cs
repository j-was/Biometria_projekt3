namespace FingerprintDecryptor.Bitmaps;

public class BinaryBitmap : IDisposable
{
    public int Width
    {
        get => Pixels.GetLength(0);
    }

    public int Height
    {
        get => Pixels.GetLength(1);
    }

    public bool[,] Pixels { get; }
    private bool _disposed;

    // TRUE - black, FALSE - white

    public BinaryBitmap(int width, int height)
    {
        Pixels = new bool[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Pixels[i, j] = false;
            }
        }
    }

    public BinaryBitmap Clone()
    {
        var clone = new BinaryBitmap(Width, Height);
        Array.Copy(Pixels, clone.Pixels, Pixels.Length);
        return clone;
    }

    public DirectBitmap ToDirectBitmap()
    {
        var dst = new DirectBitmap(Width, Height);
        for (int y = 0; y < dst.Height; y++)
        {
            for (int x = 0; x < dst.Width; x++)
            {
                byte v = Pixels[x, y] ? (byte)0 : (byte)255;
                dst.Pixels[x, y] = (v, v, v);
            }
        }

        return dst;
    }

    public GrayBitmap ToGrayBitmap()
    {
        var dst = new GrayBitmap(Width, Height);
        for (int y = 0; y < dst.Height; y++)
        {
            for (int x = 0; x < dst.Width; x++)
            {
                dst.Pixels[x, y] = Pixels[x, y] ? (byte)0 : (byte)255;
            }
        }

        return dst;
    }

    public Bitmap ToBitmap()
    {
        return this.ToDirectBitmap().ToBitmap();
    }

    public void Dispose()
    {
        _disposed = true;
    }
}