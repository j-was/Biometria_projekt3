using FingerprintDecryptor.Bitmaps;

public class MinutiaePostProcessor
{
    private readonly BinaryBitmap _skeleton;
    private bool[,] _mask;
    
    private int RoiBorderMargin { get; init; } = 20;

    public MinutiaePostProcessor(BinaryBitmap skeleton, bool[,] roiMask)
    {
        _skeleton = skeleton;
        _mask = roiMask;
    }

    public void CleanSpuriousMinutiae(List<(int x, int y)> endings, List<(int x, int y)> bifurcations)
    {
        RemoveBorderEndings(endings, bifurcations);
    }

    private void RemoveBorderEndings(List<(int x, int y)> endings, List<(int x, int y)> bifurcations)
    {
        endings.RemoveAll(e => DistanceToBackground(e) <= RoiBorderMargin);
        bifurcations.RemoveAll(b => DistanceToBackground(b) <= RoiBorderMargin);
    }

    private double DistanceToBackground((int x, int y) p)
    {
        int r = RoiBorderMargin;
        double minDistance = double.MaxValue;

        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                int nx = p.x + dx;
                int ny = p.y + dy;

                if (!IsWithinBounds(nx, ny) || !_mask[nx, ny])
                {
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < minDistance)
                        minDistance = dist;
                }
            }
        }

        return minDistance;
    }

    private bool IsWithinBounds(int x, int y) =>
        x >= 0 &&
        x < _skeleton.Width &&
        y >= 0 &&
        y < _skeleton.Height;
}