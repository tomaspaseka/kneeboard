using Kneeboard.ViewModels;
using Xunit;

namespace Kneeboard.Tests.ViewModels;

/// <summary>
/// A framing is read back off a platform view as the pilot's gesture settles, so it has to survive
/// whatever that view reports at the limits of a pinch or a fling rather than throwing mid-flight.
/// </summary>
public class FramingTests
{
    [Fact]
    public void Fit_IsTheWholePageCentred()
    {
        Assert.Equal(1.0, Framing.Fit.Factor);
        Assert.Equal(0.5, Framing.Fit.CentreX);
        Assert.Equal(0.5, Framing.Fit.CentreY);
    }

    [Theory]
    [InlineData(0.4, 1.0)]  // further out than the whole page
    [InlineData(9.0, 3.0)]  // closer in than the screen allows
    [InlineData(2.5, 2.5)]
    public void Factor_IsHeldBetweenTheFitAndTheClosestTheScreenAllows(double factor, double expected) =>
        Assert.Equal(expected, new Framing(factor, 0.5, 0.5).Factor);

    [Theory]
    [InlineData(-0.4, 0.0)] // off the top-left of the page
    [InlineData(1.7, 1.0)]  // off the bottom-right
    [InlineData(0.25, 0.25)]
    public void Centre_IsHeldOnThePage(double centre, double expected)
    {
        var framing = new Framing(1.0, centre, centre);

        Assert.Equal(expected, framing.CentreX);
        Assert.Equal(expected, framing.CentreY);
    }

    /// <summary>
    /// Value equality is what stops an echo: the binder compares the framing reported back against the
    /// one it published, and only a real change is worth a notification.
    /// </summary>
    [Fact]
    public void Framings_OfTheSamePage_AreEqual() =>
        Assert.Equal(new Framing(2.0, 0.3, 0.4), new Framing(2.0, 0.3, 0.4));

    /// <summary>
    /// The looser comparison, for asking a platform view where it is: a framing handed to one and read
    /// straight back out of its own arithmetic returns a hair off, and that is not a new framing.
    /// </summary>
    [Fact]
    public void Matches_AFramingAHairOff_IsTheSameFraming() =>
        Assert.True(new Framing(2.0, 0.3, 0.4).Matches(new Framing(2.0, 0.30001, 0.39999)));

    [Fact]
    public void Matches_ADifferentPatchOfThePage_IsNot() =>
        Assert.False(new Framing(2.0, 0.3, 0.4).Matches(new Framing(2.0, 0.5, 0.4)));

    [Fact]
    public void Matches_TheSamePatchAtADifferentZoom_IsNot() =>
        Assert.False(new Framing(2.0, 0.3, 0.4).Matches(new Framing(2.5, 0.3, 0.4)));
}
