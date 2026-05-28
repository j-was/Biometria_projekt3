using FingerprintDecryptor.ViewModels;

namespace FingerprintDecryptor.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OpenFile_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Wybierz obraz odcisku do analizy",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
        });

        if (files.Count >= 1)
        {
            try
            {
                using var stream = await files[0].OpenReadAsync();

                var bitmap = WriteableBitmap.Decode(stream);

                if (DataContext is MainWindowViewModel vm)
                {
                    vm.LoadNewImage(bitmap);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas ładowania obrazu: {ex.Message}");
            }
        }
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void SaveImage_Click(object sender, RoutedEventArgs e)
    {
        var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Zapisz obraz jako",
                DefaultExtension = ".png",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } },
                    new FilePickerFileType("JPEG Image") { Patterns = new[] { "*.jpg", "*.jpeg" } },
                    new FilePickerFileType("Bitmap Image") { Patterns = new[] { "*.bmp" } }
                }
            });

        if (file is not null && DataContext is MainWindowViewModel vm)
        {
            var bitmap = vm.MainImage;

            if (bitmap is not null)
            {
                await using var stream = await file.OpenWriteAsync();

                if (bitmap is Bitmap bmp)
                {
                    bmp.Save(stream);
                }
            }
        }
    }

    private void _8Neighbour_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Is8NeighbourSelected = true;
            vm.IsK3MSelected = false;
            vm.IsKMMSelected = false;

            Selector.Algorithm = SkeletonizeAlgorithm.Morphological8Neigbour;
        }
    }

    private void KMM_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Is8NeighbourSelected = false;
            vm.IsK3MSelected = false;
            vm.IsKMMSelected = true;

            Selector.Algorithm = SkeletonizeAlgorithm.KMM;
        }
    }

    private void K3M_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Is8NeighbourSelected = false;
            vm.IsK3MSelected = true;
            vm.IsKMMSelected = false;

            Selector.Algorithm = SkeletonizeAlgorithm.K3M;
        }
    }

    private void Decrypt_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (DataContext is MainWindowViewModel vm && vm.HasImage && vm.Thumbnails.Count > 0)
            {
                vm.ClearThumbnails();
                _ = Task.Run(() => AnalyzeFingerprint(vm, vm.Thumbnails[0]!));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd analizy: {ex.Message}");
        }
    }

    private async Task AnalyzeFingerprint(MainWindowViewModel vm, Bitmap originalBitmap)
    {
        try
        {
            var skeletonizer = new FingerprintSkeletonizer(originalBitmap);
            skeletonizer.StageImageAdded += (stageBitmap) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { vm.LoadStageImage(stageBitmap); });
            };
            await skeletonizer.SkeletonizeAsync();
            var finder = new MinutiaeFinder(skeletonizer.ResultImage, skeletonizer.OriginalImage);
            finder.StageImageAdded += (stageBitmap) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { vm.LoadStageImage(stageBitmap); });
            };
            await finder.FindAsync();
        }
        catch (Exception ex)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.WriteLine($"Błąd analizy: {ex.Message}");
            });
        }
    }
}