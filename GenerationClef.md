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
    public int _secret;
    private readonly RandomNumberGenerator _csp;

    public GenerationClef(Point p, int a)
    {
        _p = p;
        _a = a;
        _S = null;
        _Q = null;
        _csp = RandomNumberGenerator.Create();
    }

    public void GenererClef()
    {
        _k = Generer();

        // Calcul de Q = kP
        _Q = DoubleAndAdd(_p, _k);
    }

    public void CalculerSecret()
    {
        // La formule pour obtenir le secret partagé S est : S = k * Qb [1]
        // Ici, _k est la clé privée (entier) et _Q est la clé publique de la cible (Point) [2].
        
        // On utilise l'algorithme DoubleAndAdd pour effectuer cette multiplication sur la courbe [3].
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

            // 2. Calcul du hash SHA256 pour obtenir un résultat de 32 octets (256 bits) [1].
            byte[] hash = sha256.ComputeHash(inputBytes);

            // 3. Conversion du hash en chaîne hexadécimale pour une représentation lisible [1].
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
        // 1. Générer une clé privée éphémère
        int kEph = Generer();
        Console.WriteLine($"[DEBUG Chiffrer] Clé privée éphémère kEph: {kEph}");
        Console.WriteLine($"[DEBUG Chiffrer] Clé publique destinataire: ({clePublique.x}, {clePublique.y})");
        
        // 2. Calculer la clé publique éphémère R = kEph * P
        Point R = DoubleAndAdd(_p, kEph);
        // S'assurer que R est dans le modulo
        R.x = Mod(R.x);
        R.y = Mod(R.y);
        Console.WriteLine($"[DEBUG Chiffrer] R = kEph * P = ({R.x}, {R.y})");
        
        // 3. Calculer le secret partagé S = kEph * clePublique
        Point S = DoubleAndAdd(clePublique, kEph);
        Console.WriteLine($"[DEBUG Chiffrer] S = kEph * clePublique = ({S.x}, {S.y})");
        
        // 4. Hacher le secret partagé pour obtenir une clé de chiffrement
        byte[] cleBytes = HacherSecretBytes(S);
        Console.WriteLine($"[DEBUG Chiffrer] Clé de chiffrement générée: {BitConverter.ToString(cleBytes).Replace("-", "")}");
        
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
            Console.WriteLine($"[DEBUG Chiffrer] R avant sauvegarde: ({R.x}, {R.y})");
            byte[] rx = BitConverter.GetBytes(R.x);
            byte[] ry = BitConverter.GetBytes(R.y);
            Console.WriteLine($"[DEBUG Chiffrer] Bytes de R.x sauvegardés: {BitConverter.ToString(rx)}");
            Console.WriteLine($"[DEBUG Chiffrer] Bytes de R.y sauvegardés: {BitConverter.ToString(ry)}");
            
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
            Console.WriteLine($"[DEBUG Dechiffrer] Texte chiffré reçu: '{texteChiffreBase64}'");
            Console.WriteLine($"[DEBUG Dechiffrer] Longueur: {texteChiffreBase64.Length}");
            
            // 1. Nettoyer le texte base64 (enlever les espaces et caractères blancs)
            string texteAvantNettoyage = texteChiffreBase64;
            texteChiffreBase64 = texteChiffreBase64.Trim().Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
            texteChiffreBase64 = texteChiffreBase64.Trim('"').Trim('\'').Trim();
            
            Console.WriteLine($"[DEBUG Dechiffrer] Texte après nettoyage: '{texteChiffreBase64}'");
            Console.WriteLine($"[DEBUG Dechiffrer] Longueur après nettoyage: {texteChiffreBase64.Length}");
            
            // Vérifier les caractères valides en base64
            bool contientCaractereInvalide = false;
            foreach (char c in texteChiffreBase64)
            {
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/' || c == '='))
                {
                    Console.WriteLine($"[DEBUG Dechiffrer] Caractère invalide trouvé: '{c}' (code: {(int)c})");
                    contientCaractereInvalide = true;
                }
            }
            if (contientCaractereInvalide)
            {
                Console.WriteLine("[DEBUG Dechiffrer] ATTENTION: La chaîne contient des caractères non-base64!");
            }
            
            // 2. Décoder le base64
            Console.WriteLine("[DEBUG Dechiffrer] Tentative de décodage base64...");
            byte[] donnees = Convert.FromBase64String(texteChiffreBase64);
            Console.WriteLine($"[DEBUG Dechiffrer] Décodage réussi! Taille des données: {donnees.Length} bytes");
            
            // 3. Extraire IV (16 bytes), R.x (8 bytes), R.y (8 bytes) et le texte chiffré
            if (donnees.Length < 32)
            {
                throw new Exception($"Données insuffisantes: {donnees.Length} bytes (minimum 32 requis)");
            }
            
            Console.WriteLine($"[DEBUG Dechiffrer] Extraction des données: IV(0-15), R.x(16-23), R.y(24-31), texte(32-{donnees.Length-1})");
            
            byte[] iv = new byte[16];
            byte[] rxBytes = new byte[8];
            byte[] ryBytes = new byte[8];
            
            Buffer.BlockCopy(donnees, 0, iv, 0, 16);
            Buffer.BlockCopy(donnees, 16, rxBytes, 0, 8);
            Buffer.BlockCopy(donnees, 24, ryBytes, 0, 8);
            
            // Afficher les bytes bruts pour déboguer
            Console.WriteLine($"[DEBUG Dechiffrer] Bytes de R.x: {BitConverter.ToString(rxBytes)}");
            Console.WriteLine($"[DEBUG Dechiffrer] Bytes de R.y: {BitConverter.ToString(ryBytes)}");
            
            long rx = BitConverter.ToInt64(rxBytes, 0);
            long ry = BitConverter.ToInt64(ryBytes, 0);
            Console.WriteLine($"[DEBUG Dechiffrer] Point R extrait (avant modulo): ({rx}, {ry})");
            
            // Appliquer le modulo pour s'assurer que les valeurs sont dans [0, 101[
            rx = Mod(rx);
            ry = Mod(ry);
            Console.WriteLine($"[DEBUG Dechiffrer] Point R extrait (après modulo): ({rx}, {ry})");
            Point R = new Point(rx, ry);
            
            int longueurTexteChiffre = donnees.Length - 16 - 8 - 8;
            Console.WriteLine($"[DEBUG Dechiffrer] Longueur du texte chiffré: {longueurTexteChiffre} bytes");
            if (longueurTexteChiffre <= 0)
            {
                throw new Exception($"Longueur du texte chiffré invalide: {longueurTexteChiffre}");
            }
            
            byte[] texteChiffre = new byte[longueurTexteChiffre];
            Buffer.BlockCopy(donnees, 32, texteChiffre, 0, longueurTexteChiffre);
            
            // 4. Calculer la clé publique Q = clePrivee * P
            Console.WriteLine($"[DEBUG Dechiffrer] Calcul de Q = {clePrivee} * P");
            Point Q = DoubleAndAdd(_p, clePrivee);
            Console.WriteLine($"[DEBUG Dechiffrer] Q calculé: ({Q.x}, {Q.y})");
            
            // 5. Calculer le secret partagé S = clePrivee * R
            Console.WriteLine($"[DEBUG Dechiffrer] Calcul de S = {clePrivee} * R");
            Console.WriteLine($"[DEBUG Dechiffrer] R reçu: ({R.x}, {R.y})");
            Point S = DoubleAndAdd(R, clePrivee);
            Console.WriteLine($"[DEBUG Dechiffrer] S calculé: ({S.x}, {S.y})");
            
            // 6. Hacher le secret partagé pour obtenir la clé de déchiffrement
            Console.WriteLine("[DEBUG Dechiffrer] Hachage du secret partagé...");
            byte[] cleBytes = HacherSecretBytes(S);
            Console.WriteLine($"[DEBUG Dechiffrer] Clé de déchiffrement générée: {BitConverter.ToString(cleBytes).Replace("-", "")}");
            
            // 7. Déchiffrer avec AES
            Console.WriteLine("[DEBUG Dechiffrer] Déchiffrement AES...");
            using (Aes aes = Aes.Create())
            {
                aes.Key = cleBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.IV = iv;
                
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] texteClairBytes = decryptor.TransformFinalBlock(texteChiffre, 0, texteChiffre.Length);
                    Console.WriteLine($"[DEBUG Dechiffrer] Déchiffrement réussi! Texte clair: {texteClairBytes.Length} bytes");
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
        Console.WriteLine($"[DEBUG DoubleAndAdd] k={k}, binaire={bits_k}, longueur={bits_k.Length}");
        
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
        Console.WriteLine($"[DEBUG DoubleAndAdd] Initialisation avec premier bit '{bits_k[0]}': ({resultat.x}, {resultat.y})");
        
        // 3. Traiter les bits restants (du 2e au dernier)
        for(int i = 1; i < bits_k.Length; i++)
        {
            // Toujours doubler d'abord
            if (resultat.x != 0 || resultat.y != 0)
            {
                resultat = Double(resultat);
            }
            Console.WriteLine($"[DEBUG DoubleAndAdd] Après double (bit {i}='{bits_k[i]}'): ({resultat.x}, {resultat.y})");
            
            // Puis ajouter P si le bit est 1
            if(bits_k[i] == '1')
            {
                if (resultat.x == 0 && resultat.y == 0)
                {
                    resultat = new Point(p.x, p.y);
                }
                else
                {
                    resultat = Add(resultat, p);
                }
                Console.WriteLine($"[DEBUG DoubleAndAdd] Après add: ({resultat.x}, {resultat.y})");
            }
        }
        Console.WriteLine($"[DEBUG DoubleAndAdd] Résultat final: ({resultat.x}, {resultat.y})");
        return resultat;
    }

    // Fonction pour doubler un point
    private Point Double(Point p)
    {
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
        // Cas particulier : si p1 == p2, il faut utiliser Double, sinon division par 0 !
        if (p1.x == p2.x && p1.y == p2.y){
            return Double(p1);
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
    public int Generer()
    {
        int nombreAleat = this.Next(0, 1000);
        return nombreAleat;
    }
}

