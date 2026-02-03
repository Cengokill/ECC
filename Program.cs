using ECC;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        // Si des arguments sont fournis, exécuter la commande une fois puis entrer en mode interactif
        if (args.Length > 0)
        {
            TraiterCommande(args);
        }
        else
        {
            // Afficher le manuel au démarrage
            AfficherManuel();
        }

        // Mode interactif : boucle jusqu'à ce que l'utilisateur tape "exit" ou "quit"
        Console.WriteLine("\nMode interactif. Tapez 'exit' ou 'quit' pour quitter.");
        while (true)
        {
            Console.Write("monECC> ");
            string? ligne = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(ligne))
                continue;
            
            // Quitter le programme
            if (ligne.Trim().ToLower() == "exit" || ligne.Trim().ToLower() == "quit")
            {
                Console.WriteLine("Au revoir !");
                break;
            }

            // Parser la ligne de commande
            string[] arguments = ParseLigneCommande(ligne);
            if (arguments.Length == 0)
                continue;

            TraiterCommande(arguments);
        }
    }

    static string[] ParseLigneCommande(string ligne)
    {
        List<string> args = new List<string>();
        bool dansGuillemets = false;
        StringBuilder currentArg = new StringBuilder();

        foreach (char c in ligne)
        {
            if (c == '"')
            {
                dansGuillemets = !dansGuillemets;
            }
            else if (char.IsWhiteSpace(c) && !dansGuillemets)
            {
                if (currentArg.Length > 0)
                {
                    args.Add(currentArg.ToString());
                    currentArg.Clear();
                }
            }
            else
            {
                currentArg.Append(c);
            }
        }

        if (currentArg.Length > 0)
        {
            args.Add(currentArg.ToString());
        }

        return args.ToArray();
    }

    static void TraiterCommande(string[] args)
    {
        // Récupère la commande (obligatoire)
        string commande = args[0].ToLower();

        // Valide que la commande est une des valeurs autorisées
        string[] commandesValides = { "keygen", "crypt", "decrypt", "help", "test" };
        if (!commandesValides.Contains(commande))
        {
            Console.WriteLine($"Erreur : Commande '{commande}' invalide.");
            Console.WriteLine("Commandes valides : keygen, crypt, decrypt, help, test");
            Console.WriteLine("Utilisez 'help' pour afficher le manuel.");
            return;
        }

        switch (commande)
        {
            case "keygen":
                ExecuterKeygen(args);
                break;
            case "crypt":
                ExecuterCrypt(args);
                break;
            case "decrypt":
                ExecuterDecrypt(args);
                break;
            case "help":
                AfficherManuel();
                break;
            case "test":
                ExecuterTest(args);
                break;
        }
    }

    static void AfficherManuel()
    {
        Console.WriteLine("Script monECC par Killian Cengo");
        Console.WriteLine("Syntaxe :");
        Console.WriteLine("monECC <commande> [<clé>] [<texte>] [switchs]");
        Console.WriteLine();
        Console.WriteLine("Commande :");
        Console.WriteLine("  keygen  : Génère une paire de clé");
        Console.WriteLine("  crypt   : Chiffre <texte> pour la clé publique <clé>");
        Console.WriteLine("  decrypt : Déchiffre <texte> pour la clé privée <clé>");
        Console.WriteLine("  test    : Valide le fonctionnement du système");
        Console.WriteLine("  help    : Affiche ce manuel");
        Console.WriteLine();
        Console.WriteLine("Clé :");
        Console.WriteLine("  Un fichier qui contient une clé publique monECC (\"crypt\") ou une clé");
        Console.WriteLine("  privée (\"decrypt\")");
        Console.WriteLine();
        Console.WriteLine("Texte :");
        Console.WriteLine("  Une phrase en clair (\"crypt\") ou une phrase chiffrée (\"decrypt\")");
        Console.WriteLine();
        Console.WriteLine("Switchs :");
        Console.WriteLine("  -f <file>    : Choisir le nom des fichiers générés (keygen), monECC par défaut");
        Console.WriteLine("  -s <size>    : Plage d'aléa pour la clé privée (keygen), 1000 par défaut");
        Console.WriteLine("  -d <dir>     : Répertoire de sortie pour les fichiers générés (keygen/crypt/decrypt)");
        Console.WriteLine("  -i <file>    : Lire le texte depuis un fichier (crypt/decrypt)");
        Console.WriteLine("  -o <file>    : Écrire le résultat dans un fichier (crypt/decrypt)");
        Console.WriteLine();
        Console.WriteLine("Commande supplémentaire :");
        Console.WriteLine("  test         : Valide le fonctionnement du système cryptographique");
        Console.WriteLine("  test -v      : Mode verbose pour afficher plus de détails");
    }

    static void ExecuterKeygen(string[] args)
    {
        // Parser les arguments pour les switchs -f, -s et -d
        string nomFichier = "monECC";
        int tailleMax = 1000;
        string? repertoireSortie = null;
        
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "-f" && i + 1 < args.Length)
            {
                nomFichier = args[i + 1];
                i++; // Passer au suivant car on a consommé l'argument
            }
            else if (args[i] == "-s" && i + 1 < args.Length)
            {
                if (!int.TryParse(args[i + 1], out tailleMax) || tailleMax <= 1)
                {
                    Console.WriteLine("Erreur : La taille doit être un entier supérieur à 1.");
                    return;
                }
                i++; // Passer au suivant car on a consommé l'argument
            }
            else if (args[i] == "-d" && i + 1 < args.Length)
            {
                repertoireSortie = args[i + 1];
                i++; // Passer au suivant car on a consommé l'argument
            }
        }

        // Paramètres par défaut pour la courbe elliptique
        // Courbe: y² = x³ + 35x + 3 (mod 101), Point générateur P = (2, 9)
        Point p = new Point(2, 9);
        int a = 35;
        int b = 3;

        // Générer la paire de clés
        GenerationClef generateur = new GenerationClef(p, a, b);
        generateur.GenererClef(tailleMax);

        // Vérifier que la génération a réussi
        if (generateur._Q == null)
        {
            Console.WriteLine("Erreur : Échec de la génération de la clé publique.");
            return;
        }

        // Déterminer le répertoire de sortie
        string repertoireFinal = repertoireSortie ?? Directory.GetCurrentDirectory();
        
        // Créer le répertoire s'il n'existe pas
        try
        {
            Directory.CreateDirectory(repertoireFinal);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur : Impossible de créer le répertoire '{repertoireFinal}': {ex.Message}");
            return;
        }

        // Sauvegarde la clé publique dans monECC.pub
        // Format avec 3 lignes :
        // ---begin monECC public key---
        // (x,y)
        // ---end monECC key---
        string fichierPub = Path.Combine(repertoireFinal, $"{nomFichier}.pub");
        string contenuPub = $"---begin monECC public key---\n({generateur._Q.x};{generateur._Q.y})\n---end monECC key---";
        File.WriteAllText(fichierPub, contenuPub);
        Console.WriteLine($"Clé publique sauvegardée dans : {fichierPub}");

        // Sauvegarde la clé privée dans monECC.priv
        // Format avec 3 lignes :
        // ---begin monECC private key---
        // base64_encode(k)
        // ---end monECC key---
        string fichierPriv = Path.Combine(repertoireFinal, $"{nomFichier}.priv");
        // Encoder k en base64
        byte[] kBytes = Encoding.UTF8.GetBytes(generateur._k.ToString());
        string kBase64 = Convert.ToBase64String(kBytes);
        string contenuPriv = $"---begin monECC private key---\n{kBase64}\n---end monECC key---";
        File.WriteAllText(fichierPriv, contenuPriv);
        Console.WriteLine($"Clé privée sauvegardée dans : {fichierPriv}");
    }

    static void ExecuterCrypt(string[] args)
    {
        // Valide les paramètres obligatoires : commande (args[0]), clé (args[1])
        if (args.Length < 2)
        {
            Console.WriteLine("Erreur : Paramètres manquants.");
            Console.WriteLine("Usage : monECC crypt <fichier_clé_publique> <texte> [-i <fichier_entrée>] [-o <fichier_sortie>]");
            Console.WriteLine("Exemple : monECC crypt monECC.pub \"Mon message secret\"");
            return;
        }

        // Parser les switchs -i, -o et -d
        string? fichierEntree = null;
        string? fichierSortie = null;
        string? repertoireSortie = null;
        List<string> texteParts = new List<string>();
        
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "-i" && i + 1 < args.Length)
            {
                fichierEntree = args[i + 1];
                i++; // Passer au suivant
            }
            else if (args[i] == "-o" && i + 1 < args.Length)
            {
                fichierSortie = args[i + 1];
                i++; // Passer au suivant
            }
            else if (args[i] == "-d" && i + 1 < args.Length)
            {
                repertoireSortie = args[i + 1];
                i++; // Passer au suivant
            }
            else if (fichierEntree == null && args[i] != "-i" && args[i] != "-o" && args[i] != "-d")
            {
                // Si pas de -i, le texte est dans les arguments restants (sauf les switchs)
                texteParts.Add(args[i]);
            }
        }
        
        string texte = string.Join(" ", texteParts);

        // Vérifie que le paramètre clé (args[1]) n'est pas vide
        if (string.IsNullOrWhiteSpace(args[1]))
        {
            Console.WriteLine("Erreur : Le fichier de clé publique est obligatoire.");
            return;
        }

        // Lit la clé publique depuis le fichier
        string fichierCle = args[1];
        if (!File.Exists(fichierCle))
        {
            Console.WriteLine($"Erreur : Le fichier '{fichierCle}' n'existe pas.");
            return;
        }

        // Lit toutes les lignes du fichier
        string[] lignes = File.ReadAllLines(fichierCle);
        
        // Vérifie que le fichier a le bon format (3 lignes)
        if (lignes.Length < 3)
        {
            Console.WriteLine("Erreur : Format de fichier de clé publique invalide.");
            Console.WriteLine("Le fichier doit contenir 3 lignes.");
            return;
        }

        // Vérifie que le fichier commence par l'en-tête attendu
        if (!lignes[0].Trim().Equals("---begin monECC public key---", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Erreur : Format de fichier de clé publique invalide.");
            Console.WriteLine("Le fichier doit commencer par '---begin monECC public key---'");
            return;
        }

        // Vérifie que le fichier se termine par le footer attendu
        if (!lignes[2].Trim().Equals("---end monECC key---", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Erreur : Format de fichier de clé publique invalide.");
            Console.WriteLine("Le fichier doit se terminer par '---end monECC key---'");
            return;
        }

        // Extrait Qx et Qy de la ligne 2
        string ligne2 = lignes[1].Trim();
        Point? clePublique = ParserClePublique(ligne2);
        
        if (clePublique == null)
        {
            Console.WriteLine("Erreur : Format de clé publique invalide dans le fichier.");
            Console.WriteLine("La ligne 2 doit contenir le format (x,y)");
            return;
        }

        // Lire le texte depuis un fichier si -i est spécifié, sinon depuis les arguments
        if (fichierEntree != null)
        {
            if (!File.Exists(fichierEntree))
            {
                Console.WriteLine($"Erreur : Le fichier d'entrée '{fichierEntree}' n'existe pas.");
                return;
            }
            texte = File.ReadAllText(fichierEntree);
        }
        
        // Vérifie que le texte n'est pas vide
        if (string.IsNullOrWhiteSpace(texte))
        {
            Console.WriteLine("Erreur : Le texte à chiffrer est obligatoire.");
            return;
        }

        // Paramètres par défaut pour la courbe elliptique
        // Courbe: y² = x³ + 35x + 3 (mod 101), Point générateur P = (2, 9)
        Point p = new Point(2, 9);
        int a = 35;
        int b = 3;

        // Chiffre le texte
        GenerationClef generateur = new GenerationClef(p, a, b);
        string texteChiffre = generateur.Chiffrer(texte, clePublique);

        // Écrire dans un fichier si -o est spécifié, sinon afficher
        if (fichierSortie != null)
        {
            // Déterminer le chemin complet du fichier de sortie
            string cheminComplet;
            if (repertoireSortie != null)
            {
                // Créer le répertoire s'il n'existe pas
                try
                {
                    Directory.CreateDirectory(repertoireSortie);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur : Impossible de créer le répertoire '{repertoireSortie}': {ex.Message}");
                    return;
                }
                cheminComplet = Path.Combine(repertoireSortie, fichierSortie);
            }
            else
            {
                cheminComplet = fichierSortie;
            }
            
            File.WriteAllText(cheminComplet, texteChiffre);
            Console.WriteLine($"Texte chiffré sauvegardé dans : {cheminComplet}");
        }
        else
        {
            Console.WriteLine(texteChiffre);
        }
    }

    static void ExecuterDecrypt(string[] args)
    {
        // Valide les paramètres obligatoires : commande (args[0]), clé (args[1])
        if (args.Length < 2)
        {
            Console.WriteLine("Erreur : Paramètres manquants.");
            Console.WriteLine("Usage : monECC decrypt <fichier_clé_privée> <texte_chiffré> [-i <fichier_entrée>] [-o <fichier_sortie>]");
            Console.WriteLine("Exemple : monECC decrypt monECC.priv \"base64_texte_chiffré\"");
            return;
        }

        // Parser les switchs -i, -o et -d
        string? fichierEntree = null;
        string? fichierSortie = null;
        string? repertoireSortie = null;
        List<string> texteParts = new List<string>();
        
        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "-i" && i + 1 < args.Length)
            {
                fichierEntree = args[i + 1];
                i++; // Passer au suivant
            }
            else if (args[i] == "-o" && i + 1 < args.Length)
            {
                fichierSortie = args[i + 1];
                i++; // Passer au suivant
            }
            else if (args[i] == "-d" && i + 1 < args.Length)
            {
                repertoireSortie = args[i + 1];
                i++; // Passer au suivant
            }
            else if (fichierEntree == null && args[i] != "-i" && args[i] != "-o" && args[i] != "-d")
            {
                // Si pas de -i, le texte est dans les arguments restants (sauf les switchs)
                texteParts.Add(args[i]);
            }
        }
        
        string texteChiffre = string.Join(" ", texteParts);

        // Vérifie que le paramètre clé (args[1]) n'est pas vide
        if (string.IsNullOrWhiteSpace(args[1]))
        {
            Console.WriteLine("Erreur : Le fichier de clé privée est obligatoire.");
            return;
        }

        // Lit la clé privée depuis le fichier
        string fichierCle = args[1];
        if (!File.Exists(fichierCle))
        {
            Console.WriteLine($"Erreur : Le fichier '{fichierCle}' n'existe pas.");
            return;
        }

        // Lit toutes les lignes du fichier
        string[] lignes = File.ReadAllLines(fichierCle);
        
        // Vérifie que le fichier a le bon format (3 lignes)
        if (lignes.Length < 3)
        {
            Console.WriteLine("Erreur : Format de fichier de clé privée invalide.");
            Console.WriteLine("Le fichier doit contenir 3 lignes.");
            return;
        }

        // Vérifie que le fichier commence par l'en-tête attendu
        if (!lignes[0].Trim().Equals("---begin monECC private key---", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Erreur : Format de fichier de clé privée invalide.");
            Console.WriteLine("Le fichier doit commencer par '---begin monECC private key---'");
            return;
        }

        // Vérifie que le fichier se termine par le footer attendu
        if (!lignes[2].Trim().Equals("---end monECC key---", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Erreur : Format de fichier de clé privée invalide.");
            Console.WriteLine("Le fichier doit se terminer par '---end monECC key---'");
            return;
        }

        // Extrait k de la ligne 2 (décodage base64)
        string ligne2 = lignes[1].Trim();
        int clePrivee;
        try
        {
            byte[] kBytes = Convert.FromBase64String(ligne2);
            string kStr = Encoding.UTF8.GetString(kBytes);
            if (!int.TryParse(kStr, out clePrivee))
            {
                Console.WriteLine("Erreur : Format de clé privée invalide dans le fichier.");
                Console.WriteLine("La ligne 2 doit contenir un entier encodé en base64");
                return;
            }
        }
        catch (FormatException)
        {
            Console.WriteLine("Erreur : Format base64 invalide dans le fichier de clé privée.");
            return;
        }

        // Lire le texte chiffré depuis un fichier si -i est spécifié
        if (fichierEntree != null)
        {
            if (!File.Exists(fichierEntree))
            {
                Console.WriteLine($"Erreur : Le fichier d'entrée '{fichierEntree}' n'existe pas.");
                return;
            }
            texteChiffre = File.ReadAllText(fichierEntree);
        }
        
        // Nettoyer le texte chiffré : enlever les espaces et autres caractères blancs
        texteChiffre = texteChiffre.Trim().Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "");
        
        // Vérifie que le texte chiffré n'est pas vide
        if (string.IsNullOrWhiteSpace(texteChiffre))
        {
            Console.WriteLine("Erreur : Le texte chiffré est obligatoire.");
            return;
        }

        // Paramètres par défaut pour la courbe elliptique
        // Courbe: y² = x³ + 35x + 3 (mod 101), Point générateur P = (2, 9)
        Point p = new Point(2, 9);
        int a = 35;
        int b = 3;

        // Déchiffrer le texte
        GenerationClef generateur = new GenerationClef(p, a, b);
        string texteClair = generateur.Dechiffrer(texteChiffre, clePrivee);

        // Écrire dans un fichier si -o est spécifié, sinon afficher
        if (fichierSortie != null)
        {
            // Déterminer le chemin complet du fichier de sortie
            string cheminComplet;
            if (repertoireSortie != null)
            {
                // Créer le répertoire s'il n'existe pas
                try
                {
                    Directory.CreateDirectory(repertoireSortie);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur : Impossible de créer le répertoire '{repertoireSortie}': {ex.Message}");
                    return;
                }
                cheminComplet = Path.Combine(repertoireSortie, fichierSortie);
            }
            else
            {
                cheminComplet = fichierSortie;
            }
            
            File.WriteAllText(cheminComplet, texteClair);
            Console.WriteLine($"Texte déchiffré sauvegardé dans : {cheminComplet}");
        }
        else
        {
            Console.WriteLine(texteClair);
        }
    }

    static Point? ParserClePublique(string cleStr)
    {
        try
        {
            // Enlever les parenthèses si présentes
            cleStr = cleStr.Trim().TrimStart('(').TrimEnd(')');
            
            // Séparer par la virgule ou le point-virgule (pour compatibilité)
            string[] parts = cleStr.Split(new char[] { ',', ';' });
            if (parts.Length != 2)
                return null;

            long x = long.Parse(parts[0].Trim());
            long y = long.Parse(parts[1].Trim());
            
            return new Point(x, y);
        }
        catch
        {
            return null;
        }
    }

    static void ExecuterTest(string[] args)
    {
        // Parser le switch -v pour le mode verbose
        bool verbose = false;
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "-v" || args[i] == "--verbose")
            {
                verbose = true;
            }
        }

        Console.WriteLine("=== Test de validation monECC ===");
        Console.WriteLine();

        // Paramètres de la courbe elliptique
        Point p = new Point(2, 9);
        int a = 35;
        int b = 3;

        // Créer un répertoire temporaire pour les fichiers de test
        string repertoireTest = Path.Combine(Path.GetTempPath(), "monECC_test_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        string nomFichierTest = "test";

        try
        {
            // Créer le répertoire temporaire
            Directory.CreateDirectory(repertoireTest);
            if (verbose)
            {
                Console.WriteLine($"[VERBOSE] Répertoire de test créé : {repertoireTest}");
            }

            // Étape 1 : Générer une paire de clés de test
            Console.WriteLine("Étape 1/4 : Génération d'une paire de clés...");
            var startTime = DateTime.Now;
            
            GenerationClef generateur = new GenerationClef(p, a, b);
            generateur.GenererClef(1000);
            
            if (generateur._Q == null)
            {
                Console.WriteLine("❌ ÉCHEC : Impossible de générer la clé publique.");
                return;
            }

            var keygenTime = (DateTime.Now - startTime).TotalMilliseconds;
            Console.WriteLine($"✓ Clés générées avec succès (temps: {keygenTime:F2}ms)");
            
            if (verbose)
            {
                Console.WriteLine($"  Clé privée k = {generateur._k}");
                Console.WriteLine($"  Clé publique Q = ({generateur._Q.x}, {generateur._Q.y})");
            }

            // Sauvegarder les clés dans le répertoire temporaire
            string fichierPubTest = Path.Combine(repertoireTest, $"{nomFichierTest}.pub");
            string fichierPrivTest = Path.Combine(repertoireTest, $"{nomFichierTest}.priv");
            
            string contenuPub = $"---begin monECC public key---\n({generateur._Q.x};{generateur._Q.y})\n---end monECC key---";
            File.WriteAllText(fichierPubTest, contenuPub);
            
            byte[] kBytes = Encoding.UTF8.GetBytes(generateur._k.ToString());
            string kBase64 = Convert.ToBase64String(kBytes);
            string contenuPriv = $"---begin monECC private key---\n{kBase64}\n---end monECC key---";
            File.WriteAllText(fichierPrivTest, contenuPriv);

            // Étape 2 : Chiffrer un message de test
            Console.WriteLine("Étape 2/4 : Chiffrement d'un message de test...");
            startTime = DateTime.Now;
            
            string messageTest = $"Test monECC - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            string texteChiffre = generateur.Chiffrer(messageTest, generateur._Q);
            
            var cryptTime = (DateTime.Now - startTime).TotalMilliseconds;
            Console.WriteLine($"✓ Message chiffré avec succès (temps: {cryptTime:F2}ms)");
            
            if (verbose)
            {
                Console.WriteLine($"  Message original : {messageTest}");
                Console.WriteLine($"  Taille du texte chiffré : {texteChiffre.Length} caractères");
            }

            // Étape 3 : Déchiffrer le message
            Console.WriteLine("Étape 3/4 : Déchiffrement du message...");
            startTime = DateTime.Now;
            
            string texteDechiffre = generateur.Dechiffrer(texteChiffre, generateur._k);
            
            var decryptTime = (DateTime.Now - startTime).TotalMilliseconds;
            Console.WriteLine($"✓ Message déchiffré avec succès (temps: {decryptTime:F2}ms)");
            
            if (verbose)
            {
                Console.WriteLine($"  Message déchiffré : {texteDechiffre}");
            }

            // Étape 4 : Vérifier l'intégrité
            Console.WriteLine("Étape 4/4 : Vérification de l'intégrité...");
            
            if (texteDechiffre == messageTest)
            {
                Console.WriteLine($"✓ Intégrité vérifiée : le message déchiffré correspond au message original");
                Console.WriteLine();
                Console.WriteLine("✅ TOUS LES TESTS SONT PASSÉS AVEC SUCCÈS !");
                Console.WriteLine();
                
                if (verbose)
                {
                    Console.WriteLine($"Temps total : {keygenTime + cryptTime + decryptTime:F2}ms");
                    Console.WriteLine($"  - Génération de clés : {keygenTime:F2}ms");
                    Console.WriteLine($"  - Chiffrement : {cryptTime:F2}ms");
                    Console.WriteLine($"  - Déchiffrement : {decryptTime:F2}ms");
                    Console.WriteLine();
                    Console.WriteLine($"Fichiers de test conservés dans : {repertoireTest}");
                    Console.WriteLine("  (Ils seront supprimés automatiquement au prochain redémarrage)");
                }
                else
                {
                    Console.WriteLine("Utilisez 'test -v' pour plus de détails.");
                }
            }
            else
            {
                Console.WriteLine("❌ ÉCHEC : Le message déchiffré ne correspond pas au message original !");
                Console.WriteLine($"  Message original : {messageTest}");
                Console.WriteLine($"  Message déchiffré : {texteDechiffre}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERREUR lors du test : {ex.Message}");
            if (verbose)
            {
                Console.WriteLine($"Stack trace : {ex.StackTrace}");
            }
        }
        finally
        {
            // Nettoyer les fichiers temporaires (sauf en mode verbose où on les garde pour inspection)
            if (!verbose)
            {
                try
                {
                    if (File.Exists(Path.Combine(repertoireTest, $"{nomFichierTest}.pub")))
                        File.Delete(Path.Combine(repertoireTest, $"{nomFichierTest}.pub"));
                    if (File.Exists(Path.Combine(repertoireTest, $"{nomFichierTest}.priv")))
                        File.Delete(Path.Combine(repertoireTest, $"{nomFichierTest}.priv"));
                    if (Directory.Exists(repertoireTest))
                        Directory.Delete(repertoireTest);
                }
                catch
                {
                    // Ignorer les erreurs de suppression (fichiers peut-être verrouillés)
                }
            }
        }
    }
}



