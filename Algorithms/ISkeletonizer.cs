using FingerprintDecryptor.Bitmaps;

namespace FingerprintDecryptor.Algorithms;

public interface ISkeletonizer
{
    public BinaryBitmap InputImage { get; set; }
    public BinaryBitmap OutputImage { get; set; }

    public void Skeletonize();
}