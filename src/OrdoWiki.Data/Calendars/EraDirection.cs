namespace OrdoWiki.Data.Calendars;

public enum EraDirection
{
    /// <summary>Counts up from <c>AnchorYear</c>. Display year = absoluteYear − AnchorYear.</summary>
    Forward = 0,

    /// <summary>Counts down toward <c>AnchorYear</c>. Display year = AnchorYear − absoluteYear.</summary>
    Backward = 1,
}
