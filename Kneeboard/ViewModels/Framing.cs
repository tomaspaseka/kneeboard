namespace Kneeboard.ViewModels;

/// <summary>
/// How far into a page the pilot is zoomed, and which point of the page sits at the centre of the
/// screen. Expressed relative to the page rather than in screen pixels, so it means the same thing at
/// any window size — a framing outlives a resize or a rotation without being recalculated.
/// </summary>
public readonly record struct Framing
{
    /// <summary>The whole page on screen at once. Nothing is ever framed further out than this.</summary>
    public const double FitFactor = 1.0;

    /// <summary>As close in as the pilot can get; the same limit the platform view enforces.</summary>
    public const double MaxFactor = 3.0;

    /// <summary>
    /// The page fitted to the screen and centred: an untouched page, and where every section starts.
    /// Named rather than left to <c>default</c>, which is not a framing at all — its factor is zero.
    /// Everything that produces a framing names this, so the default never reaches the screen.
    /// </summary>
    public static readonly Framing Fit = new(FitFactor, 0.5, 0.5);

    /// <summary>
    /// Clamps rather than rejects. Both the factor and the centre are read back off a platform view
    /// as the pilot's gesture settles, where overshoot at the limits is ordinary and a throw would
    /// take the kneeboard down mid-flight.
    /// </summary>
    public Framing(double factor, double centreX, double centreY)
    {
        Factor = Math.Clamp(factor, FitFactor, MaxFactor);
        CentreX = Math.Clamp(centreX, 0, 1);
        CentreY = Math.Clamp(centreY, 0, 1);
    }

    /// <summary>
    /// How far two framings may differ and still be the same one. A framing reaches the screen through a
    /// platform view's own zoom and scroll arithmetic and is read back out of the same arithmetic, so
    /// what comes back is a hair off what went in. Compared exactly, the screen and the view would spend
    /// the flight handing that difference back and forth.
    /// </summary>
    public const double Tolerance = 0.001;

    /// <summary>Magnification: 1 fits the page to the screen, <see cref="MaxFactor"/> is as far in as it goes.</summary>
    public double Factor { get; }

    /// <summary>How far across the page the centre of the screen sits, as a fraction of its width.</summary>
    public double CentreX { get; }

    /// <summary>How far down the page the centre of the screen sits, as a fraction of its height.</summary>
    public double CentreY { get; }

    /// <summary>
    /// Whether <paramref name="other"/> frames the page the same way, give or take the rounding a
    /// platform view introduces. Distinct from <c>==</c>, which is exact and is what the binder uses to
    /// tell a real change from an echo: this is for asking a platform view where it currently is.
    /// </summary>
    public bool Matches(Framing other) =>
        Math.Abs(Factor - other.Factor) < Tolerance
        && Math.Abs(CentreX - other.CentreX) < Tolerance
        && Math.Abs(CentreY - other.CentreY) < Tolerance;
}
