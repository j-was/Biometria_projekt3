using System;

namespace FingerprintDecryptor.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private Bitmap? _mainImage;

    [ObservableProperty] private int _selectedIndex = -1;

    public ObservableCollection<Bitmap?> Thumbnails { get; } = new();
    public bool IsKMMSelected { get; set; }
    public bool IsK3MSelected { get; set; }
    public bool Is8NeighbourSelected { get; set; }

    public bool HasImage = false;

    public MainWindowViewModel()
    {
        Thumbnails.Add(CreateBlackBitmap());
    }

    public void LoadNewImage(Bitmap image)
    {
        ClearThumbnails(image);

        SelectedIndex = 0;
        MainImage = Thumbnails[0];
        HasImage = true;
    }

    public void ClearThumbnails(Bitmap? newImage = null)
    {
        var currentImage = newImage ?? Thumbnails[0];
        Thumbnails.Clear();
        Thumbnails.Add(currentImage);
    }
    
    partial void OnSelectedIndexChanged(int value)
    {
        if (value >= 0 && value < Thumbnails.Count)
        {
            MainImage = Thumbnails[value];
        }
    }

    public void LoadStageImage(Bitmap image)
    {
        int i = Thumbnails.Count;
        Thumbnails.Add(image);
        SelectedIndex = i;
        MainImage = Thumbnails[i];
    }
    
    private WriteableBitmap CreateBlackBitmap()
    {
        var size = new PixelSize(100, 100);
        var dpi = new Vector(96, 96);
        var wb = new WriteableBitmap(size, dpi, Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul);

        using (var buf = wb.Lock())
        {
            unsafe
            {
                uint* ptr = (uint*)buf.Address;
                int pixels = size.Width * size.Height;
                for (int i = 0; i < pixels; i++)
                {
                    ptr[i] = 0xFF000000;
                }
            }
        }

        return wb;
    }
}
