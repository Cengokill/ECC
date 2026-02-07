using System.Security.Cryptography;
using System.Numerics;
using System.Text;
using System.Linq;

namespace ECC;

/// <summary>
/// Classe statique pour la génération de clés cryptographiques.
/// </summary>
public class GenerationClef
{
    public int _k { get; private set; }
    public Point? _Q { get; private set; }
    public string? _S { get; private set; }
    public int resultat;
    public readonly int _modulo = 101;
    public Point _p;
    public int _a;
    public int _b;
    public int _secret;
    private readonly RandomNumberGenerator _csp;

    public GenerationClef(Point p, int a, int b = 0)
    {
        _p = p;
        _a = a;
        _b = b;
        _S = null;
        _Q = null;
        _csp = RandomNumberGenerator.Create();
    }

    public void GenererClef(int maxValue = 1000)
    {
        // Vérifie que le point générateur P est sur la courbe
        if (!EstSurCourbe(_p))
        {
            throw new Exception($"Le point générateur P=({_p.x},{_p.y}) n'est pas sur la courbe y²=x³+{_a}x+{_b} (mod {_modulo})");
        }

        _k = Generer(maxValue);

        // Calcul de Q = kP
        _Q = DoubleAndAdd(_p, _k);
        
        // Vérifie que la clé publique générée est sur la courbe
        if (_Q != null && !EstSurCourbe(_Q))
        {
            throw new Exception($"La clé publique générée Q=({_Q.x},{_Q.y}) n'est pas sur la courbe");
        }
    }

    public void CalculerSecret()
    {
        // La formule pour obtenir le secret partagé S est : S = k * Qb.
        // Ici, _k est la clé privée (entier) et _Q est la clé publique de la cible (Point).
        
        // On utilise l'algorithme DoubleAndAdd pour effectuer cette multiplication sur la courbe.
        Point S = DoubleAndAdd(_Q, _k);

        // S est maintenant le point secret partagé sur la courbe.
        // Maintenant on hache le secret.
        _S = HacherSecret(S);
    }

    public string HacherSecret(Point S)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            // 1. Préparation des données : conversion des coordonnées x et y en octets.
            // On concatène les octets de x et de y pour représenter le point S.
            byte[] bits_x = BitConverter.GetBytes(S.x);
            byte[] bits_y = BitConverter.GetBytes(S.y);
            byte[] inputBytes = new byte[bits_x.Length + bits_y.Length];
            
            Buffer.BlockCopy(bits_x, 0, inputBytes, 0, bits_x.Length);
            Buffer.BlockCopy(bits_y, 0, inputBytes, bits_x.Length, bits_y.Length);

            // 2. Calcul du hash SHA256 pour obtenir un résultat de 32 octets (256 bits).
            byte[] hash = sha256.ComputeHash(inputBytes);

            // 3. Conversion du hash en chaîne hexadécimale pour une représentation lisible.
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Hache le secret partagé et retourne les bytes directement (pour le chiffrement).
    /// </summary>
    private byte[] HacherSecretBytes(Point S)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            // Préparation des données : conversion des coordonnées x et y en octets.
            byte[] bits_x = BitConverter.GetBytes(S.x);
            byte[] bits_y = BitConverter.GetBytes(S.y);
            byte[] inputBytes = new byte[bits_x.Length + bits_y.Length];
            
            Buffer.BlockCopy(bits_x, 0, inputBytes, 0, bits_x.Length);
            Buffer.BlockCopy(bits_y, 0, inputBytes, bits_x.Length, bits_y.Length);

            // Calcul du hash SHA256 pour obtenir un résultat de 32 octets (256 bits).
            return sha256.ComputeHash(inputBytes);
        }
    }

    /// <summary>
    /// Chiffre un texte avec une clé publique ECC.
    /// </summary>
    /// <param name="texte">Le texte à chiffrer</param>
    /// <param name="clePublique">La clé publique du destinataire (Point)</param>
    /// <returns>Le texte chiffré en base64</returns>
    public string Chiffrer(string texte, Point clePublique)
    {
        // Vérifier que la clé publique est sur la courbe
        if (!EstSurCourbe(clePublique))
        {
            throw new Exception($"La clé publique ({clePublique.x},{clePublique.y}) n'est pas sur la courbe y²=x³+{_a}x+{_b} (mod {_modulo})");
        }

        // 1. Générer une clé privée éphémère
        int kEph = Generer();
        
        // 2. Calculer la clé publique éphémère R = kEph * P
        Point R = DoubleAndAdd(_p, kEph);
        // S'assurer que R est dans le modulo
        R.x = Mod(R.x);
        R.y = Mod(R.y);
        
        // 3. Calculer le secret partagé S = kEph * clePublique
        Point S = DoubleAndAdd(clePublique, kEph);
        
        // 4. Hacher le secret partagé pour obtenir une clé de chiffrement
        byte[] cleBytes = HacherSecretBytes(S);
        
        // 5. Utiliser AES pour chiffrer le texte avec la clé dérivée
        byte[] texteBytes = Encoding.UTF8.GetBytes(texte);
        byte[] texteChiffre;
        
        using (Aes aes = Aes.Create())
        {
            // Utiliser les 32 premiers octets de la clé pour AES-256
            aes.Key = cleBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();
            
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                texteChiffre = encryptor.TransformFinalBlock(texteBytes, 0, texteBytes.Length);
            }
            
            // 6. Combiner IV + R (clé publique éphémère) + texte chiffré
            // Format : IV (16 bytes) + R.x (8 bytes) + R.y (8 bytes) + texte chiffré
            byte[] iv = aes.IV;
            byte[] rx = BitConverter.GetBytes(R.x);
            byte[] ry = BitConverter.GetBytes(R.y);
            
            byte[] resultat = new byte[iv.Length + rx.Length + ry.Length + texteChiffre.Length];
            Buffer.BlockCopy(iv, 0, resultat, 0, iv.Length);
            Buffer.BlockCopy(rx, 0, resultat, iv.Length, rx.Length);
            Buffer.BlockCopy(ry, 0, resultat, iv.Length + rx.Length, ry.Length);
            Buffer.BlockCopy(texteChiffre, 0, resultat, iv.Length + rx.Length + ry.Length, texteChiffre.Length);
            
            // 7. Convertir en base64 pour une représentation lisible
            return Convert.ToBase64String(resultat);
        }
    }

    /// <summary>
    /// Déchiffre un texte avec une clé privée ECC.
    /// </summary>
    /// <param name="texteChiffreBase64">Le texte chiffré en base64</param>
    /// <param name="clePrivee">La clé privée du destinataire (entier)</param>
    /// <returns>Le texte déchiffré</returns>
    public string Dechiffrer(string texteChiffreBase64, int clePrivee)
    {
        try
        {
            // 1. Nettoyer le texte base64 (enlever les espaces et caractères blancs)
            texteChiffreBase64 = texteChiffreBase64.Trim().Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
            texteChiffreBase64 = texteChiffreBase64.Trim('"').Trim('\'').Trim();
            
            // 2. Décoder le base64
            byte[] donnees = Convert.FromBase64String(texteChiffreBase64);
            
            // 3. Extraire IV (16 bytes), R.x (8 bytes), R.y (8 bytes) et le texte chiffré
            if (donnees.Length < 32)
            {
                throw new Exception($"Données insuffisantes : {donnees.Length} bytes (minimum 32 requis)");
            }
            
            byte[] iv = new byte[16];
            byte[] rxBytes = new byte[8];
            byte[] ryBytes = new byte[8];
            
            Buffer.BlockCopy(donnees, 0, iv, 0, 16);
            Buffer.BlockCopy(donnees, 16, rxBytes, 0, 8);
            Buffer.BlockCopy(donnees, 24, ryBytes, 0, 8);
            
            long rx = BitConverter.ToInt64(rxBytes, 0);
            long ry = BitConverter.ToInt64(ryBytes, 0);
            
            // Appliquer le modulo pour s'assurer que les valeurs sont dans [0, 101[
            rx = Mod(rx);
            ry = Mod(ry);
            Point R = new Point(rx, ry);
            
            int longueurTexteChiffre = donnees.Length - 16 - 8 - 8;
            if (longueurTexteChiffre <= 0)
            {
                throw new Exception($"Longueur du texte chiffré invalide : {longueurTexteChiffre}");
            }
            
            byte[] texteChiffre = new byte[longueurTexteChiffre];
            Buffer.BlockCopy(donnees, 32, texteChiffre, 0, longueurTexteChiffre);
            
            // 4. Calculer le secret partagé S = clePrivee * R
            Point S = DoubleAndAdd(R, clePrivee);
            
            // 5. Hacher le secret partagé pour obtenir la clé de déchiffrement
            byte[] cleBytes = HacherSecretBytes(S);
            
            // 6. Déchiffrer avec AES
            using (Aes aes = Aes.Create())
            {
                aes.Key = cleBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.IV = iv;
                
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] texteClairBytes = decryptor.TransformFinalBlock(texteChiffre, 0, texteChiffre.Length);
                    return Encoding.UTF8.GetString(texteClairBytes);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Erreur lors du déchiffrement : {ex.Message}", ex);
        }
    }

    // Calcul de Q = kP
    // Le nombre entier (clé privée).
    // P : Le point de départ (x, y)
    // Q : Le point final (clé publique).
    public Point DoubleAndAdd(Point p, int k)
    {
        // Cas spéciaux
        if (k == 0)
            return new Point(0, 0);
        if (k == 1)
            return new Point(p.x, p.y);
        
        // 1. Conversion de k en binaire
        string bits_k = Convert.ToString(k, 2);
        
        // 2. Initialisation : commencer avec le premier bit
        Point resultat;
        if (bits_k[0] == '1')
        {
            resultat = new Point(p.x, p.y);
        }
        else
        {
            resultat = new Point(0, 0);
        }
        
        // 3. Traiter les bits restants (du 2e au dernier)
        for(int i = 1; i < bits_k.Length; i++)
        {
            // Toujours doubler d'abord (même si c'est le point à l'infini)
            resultat = Double(resultat);
            
            // Puis ajouter P si le bit est 1
            if(bits_k[i] == '1')
            {
                if (resultat.x == 0 && resultat.y == 0)
                {
                    // (0,0) + P = P (élément neutre)
                    resultat = new Point(p.x, p.y);
                }
                else
                {
                    resultat = Add(resultat, p);
                }
            }
        }
        return resultat;
    }

    // Fonction pour doubler un point
    private Point Double(Point p)
    {
        // Si c'est le point à l'infini (0,0), il reste le point à l'infini
        if (p.x == 0 && p.y == 0)
        {
            return new Point(0, 0);
        }
        
        // Si y = 0, le point doublé est le point à l'infini (représenté par (0,0))
        if (p.y == 0) {
            return new Point(0, 0);
        }

        long numerateur = Mod(3 * p.x * p.x + _a);
        long denominateur = Mod(2 * p.y);
        long pente = Mod(numerateur * ModInverse(denominateur));

        Point p3 = new Point();
        p3.x = Mod(pente * pente - 2 * p.x);
        p3.y = Mod(pente * (p.x - p3.x) - p.y);

        return p3;
    }

    // Fonction pour ajouter deux points
    private Point Add(Point p1, Point p2)
    {
        // Gérer le point à l'infini (0,0) comme élément neutre
        if (p1.x == 0 && p1.y == 0)
            return new Point(p2.x, p2.y);
        if (p2.x == 0 && p2.y == 0)
            return new Point(p1.x, p1.y);
        
        // Cas particulier : si p1 == p2, il faut utiliser Double, sinon division par 0 !
        if (p1.x == p2.x && p1.y == p2.y){
            return Double(p1);
        }
        
        // Cas où p1.x == p2.x mais p1.y != p2.y : ce sont des points opposés, résultat = point à l'infini
        if (p1.x == p2.x)
        {
            return new Point(0, 0);
        }

        long numerateur = Mod(p2.y - p1.y);
        long denominateur = Mod(p2.x - p1.x);
        long pente = Mod(numerateur * ModInverse(denominateur));

        Point p3 = new Point();
        p3.x = Mod(pente * pente - p1.x - p2.x);
        p3.y = Mod(pente * (p1.x - p3.x) - p1.y);

        return p3;
    }

    // Fonction magique pour remplacer la division
    // Au lieu de diviser par N, on multiplie par ModInverse(N)
    private long ModInverse(long number)
    {
        return (long)BigInteger.ModPow(number, _modulo - 2, _modulo);
    }

    // Fonction pour gérer le modulo proprement (évite les négatifs)
    private long Mod(long x)
    {
        return ((x % _modulo) + _modulo) % _modulo;
    }

    /// <summary>
    /// Vérifie si un point est sur la courbe elliptique y² = x³ + ax + b (mod p).
    /// </summary>
    public bool EstSurCourbe(Point point)
    {
        // Le point à l'infini (0,0) est considéré comme valide
        if (point.x == 0 && point.y == 0)
            return true;
            
        // Vérifier l'équation : y² = x³ + ax + b (mod p)
        long gauche = Mod(point.y * point.y);
        long droite = Mod(point.x * point.x * point.x + _a * point.x + _b);
        
        return gauche == droite;
    }

    /// <summary>
    /// Génère un entier non signé aléatoire.
    /// </summary>
    /// <returns>Un entier non signé aléatoire</returns>
    private uint GetRandomUInt()
    {
        byte[] bytes = new byte[4];
        _csp.GetBytes(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>
    /// Renvoie une valeur aléatoire
    /// </summary>
    /// <param name="minValue"></param>
    /// <param name="maxExclusiveValue"></param>
    /// <returns></returns>
    public int Next(int minValue, int maxExclusiveValue)
    {
        if (minValue == maxExclusiveValue)
            return minValue;

        if (minValue > maxExclusiveValue)
        {
            throw new ArgumentOutOfRangeException($"{nameof(minValue)} doit être inférieur à {nameof(maxExclusiveValue)}");
        }

        var diff = (long)maxExclusiveValue - minValue;
        var upperBound = uint.MaxValue / diff * diff;

        uint ui;
        do
        {
            ui = GetRandomUInt();
        } while (ui >= upperBound);

        return (int)(minValue + (ui % diff));
    }

    /// <summary>
    /// Méthode principale de génération de clé.
    /// </summary>
    /// <param name="maxValue">Valeur maximale exclusive pour la génération (par défaut 1000)</param>
    public int Generer(int maxValue = 1000)
    {
        int nombreAleat = this.Next(1, maxValue);
        return nombreAleat;
    }
}

