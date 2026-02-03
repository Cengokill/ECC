namespace ECC;

/// <summary>
/// Représente un point sur une courbe elliptique avec des coordonnées x et y.
/// </summary>
public class Point
{
    public long x { get; set; }
    public long y { get; set; }

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public Point()
    {
        x = 0;
        y = 0;
    }

    /// <summary>
    /// Constructeur avec coordonnées x et y.
    /// </summary>
    /// <param name="x">Coordonnée x du point</param>
    /// <param name="y">Coordonnée y du point</param>
    public Point(long x, long y)
    {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// Permet la conversion implicite depuis un tuple (long, long) vers Point.
    /// Permet d'utiliser la syntaxe : Point p = (3, 9);
    /// </summary>
    public static implicit operator Point((long x, long y) tuple)
    {
        return new Point(tuple.x, tuple.y);
    }
}
