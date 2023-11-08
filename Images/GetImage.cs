using System.Collections.Immutable;
using EyeAuras.OpenCVAuras.ML.Yolo;
using EyeAuras.OpenCVAuras.Scaffolding;

namespace EyeAuras.Web.Repl.Component.Image;

public static class GetImage
{
    public static async Task<(bool? IsActive, Rectangle? Bounds)> RefreshAndGet(IImageSearchTrigger aura)
    {
        var result = await aura.FetchNextResult();
        return (result.Success, aura.BoundsWindow);
    }
    
    public static async Task<(bool? IsActive, ImmutableArray<YoloPrediction> Predictions)> RefreshAndGetML(IMLSearchTrigger aura)
    {
        var result = await aura.FetchNextResult();
        return (result.Success, aura.Predictions);
    }
    
    public static Point GetImgCenter(Rectangle? rectangle)
    {
        if (rectangle == null)
        {
            throw new ArgumentNullException(nameof(rectangle), "Rectangle cannot be null.");
        }

        int centerX = rectangle.Value.Left + rectangle.Value.Width / 2;
        int centerY = rectangle.Value.Top + rectangle.Value.Height / 2;

        return new Point(centerX, centerY);
            
    }
}