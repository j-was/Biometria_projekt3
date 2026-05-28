namespace FingerprintDecryptor;

public enum SkeletonizeAlgorithm
{
    Morphological8Neigbour,
    KMM,
    K3M
}

public static class Selector
{
    public static SkeletonizeAlgorithm Algorithm {get; set;} = SkeletonizeAlgorithm.KMM;
}