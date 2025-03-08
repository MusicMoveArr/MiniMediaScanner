namespace MiniMediaScanner.Helpers;

public static class GuidHelper
{
    public static bool GuidHasValue(Guid value)
    {
        if (value == Guid.Empty)
        {
            return false;
        }

        return true;
    }
    
    public static bool GuidHasValue(Guid? value)
    {
        if (!value.HasValue)
        {
            return false;
        }

        return GuidHasValue(value.Value);
    }
}