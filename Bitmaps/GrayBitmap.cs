namespace FingerprintDecryptor.Bitmaps;

public class GrayBitmap : IDisposable
{
    public int Width { get; }
    public int Height { get; }
    public byte[,] Pixels { get; }
    private bool _disposed;

    public GrayBitmap(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new byte[width, height];
    }

    public GrayBitmap Clone()
    {
        var clone = new GrayBitmap(Width, Height);
        Array.Copy(Pixels, clone.Pixels, Pixels.Length);
        return clone;
    }

    public GrayBitmap(Bitmap bitmap)
    {
        Width = bitmap.PixelSize.Width;
        Height = bitmap.PixelSize.Height;
        Pixels = new byte[Width, Height];

        using var ms = new MemoryStream();
        bitmap.Save(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var decodeBitmap = WriteableBitmap.Decode(ms);
        using var fb = decodeBitmap.Lock();
        unsafe
        {
            uint* ptr = (uint*)fb.Address;
            int stride = fb.RowBytes / 4;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    uint pixel = ptr[y * stride + x];
                    byte b = (byte)(pixel & 0xFF);
                    Pixels[x, y] = b;
                }
            }
        }
    }

    public Bitmap ToBitmap()
    {
        var writeableBitmap = new WriteableBitmap(
            new Avalonia.PixelSize(Width, Height),
            new Avalonia.Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888);

        using (var fb = writeableBitmap.Lock())
        {
            unsafe
            {
                uint* ptr = (uint*)fb.Address;
                int stride = fb.RowBytes / 4;

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var b = Pixels[x, y];
                        ptr[y * stride + x] = (uint)((255 << 24) | (b << 16) | (b << 8) | b);
                    }
                }
            }
        }

        using var ms = new MemoryStream();
        writeableBitmap.Save(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return new Bitmap(ms);
    }

    public void Dispose()
    {
        _disposed = true;
    }
}