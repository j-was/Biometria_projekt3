using FingerprintDecryptor.Algorithms;
using FingerprintDecryptor.Bitmaps;
using FingerprintDecryptor.Preprocessing;

namespace FingerprintDecryptor;

public class FingerprintSkeletonizer
{
    public DirectBitmap OriginalImage { get; private set; }
    private GrayBitmap FingerprintImage { get; set; }
    private BinaryBitmap BinaryImage { get; set; }
    public BinaryBitmap ResultImage { get; private set; }

    private List<GaborFilter> filterBank = new List<GaborFilter>();
    private int nOrientations = 16;
    private double[] frequencies = [1.0 / 7.0, 1.0 / 9.0, 1.0 / 11.0, 1.0 / 13.0, 1.0 / 15.0];
    private int kernelSize = 23;
    private int blockSize = 10;


    private ISkeletonizer SkeletonizerAlgorithm { get; set; }

    public event Action<Bitmap>? StageImageAdded;

    private void AddStageImage(Bitmap bitmap)
    {
        StageImageAdded?.Invoke(bitmap);
    }

    public FingerprintSkeletonizer(Bitmap originalImage)
    {
        OriginalImage = new DirectBitmap(originalImage);
        FingerprintImage = new GrayBitmap(originalImage);
    }

    public Task SkeletonizeAsync()
    {
        return Task.Run(() =>
        {
            Enhance();
            Skeletonize();
        });
    }

    private void Enhance()
    {
        FingerprintImage = ImageProcessing.Normalize(FingerprintImage, 127, 10000);


        filterBank = GaborBank.GenerateBank(nOrientations, kernelSize, frequencies);
        var fingerMask = ImageProcessing.CreateMask(FingerprintImage);
        var blockOrientation = Estimators.EstimateOrientation(FingerprintImage, blockSize);
        var pixelOrientation = Estimators.Upsample(blockOrientation, blockSize,
            FingerprintImage.Width, FingerprintImage.Height);
        var blockFrequency = Estimators.EstimateFrequency(FingerprintImage, blockOrientation, blockSize);
        var pixelFrequency = Estimators.Upsample(blockFrequency, blockSize,
            FingerprintImage.Width, FingerprintImage.Height);

        FingerprintImage =
            GaborEnhancer.Enhance(FingerprintImage, pixelOrientation, pixelFrequency, filterBank, fingerMask);
        AddStageImage(FingerprintImage.ToBitmap());

        BinaryImage = ImageProcessing.AdaptiveBinarize(FingerprintImage);
        AddStageImage(BinaryImage.ToBitmap());
    }

    private void Skeletonize()
    {
        switch (Selector.Algorithm)
        {
            case SkeletonizeAlgorithm.Morphological8Neigbour:
                SkeletonizerAlgorithm = new MorphologicalSkeletonizer(BinaryImage);
                break;
            case SkeletonizeAlgorithm.K3M:
                SkeletonizerAlgorithm = new K3MSkeletonizer(BinaryImage);
                break;
            case SkeletonizeAlgorithm.KMM:
                SkeletonizerAlgorithm = new KMMSkeletonizer(BinaryImage);
                break;
        }

        SkeletonizerAlgorithm.Skeletonize();

        BinaryImage = SkeletonizerAlgorithm.OutputImage;


        ResultImage = BinaryImage.Clone();
        AddStageImage(ResultImage.ToBitmap());
    }
}