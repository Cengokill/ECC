# Travaux Pratiques : Cryptographie sur Courbes Elliptiques (monECC)

## 2. Rappel du fonctionnement de ECC

Pour ce TP, l'implémentation repose sur une courbe elliptique spécifique dénie sur un corps ni **$F_{101}$**.

### 2.1 Paramètres de la courbe
La courbe utilisée suit l'équation de Weierstrass réduite : **$y^2 = x^3 + 35x + 3 \pmod{101}$**. 
Le point de départ (générateur) est **$P = (2, 9)$**. 

### 2.2 Génération des clés
1.  **Clé privée :** Choisir un nombre entier aléatoire **$k$** compris entre **1 et 1000**.
2.  **Clé publique :** Calculer le point **$Q = kP$** sur la courbe.
    *   Le calcul doit être effectué en utilisant la méthode **"Double-and-Add"** pour être ecace.
    *   Connaître $k$ et $P$ permet de trouver $Q$ rapidement, mais il est pratiquement impossible de retrouver $k$ à partir de $P$ et $Q$ (problème du logarithme discret).

### 2.3 Calcul du secret partagé et chiffrement
Pour communiquer avec une cible, les étapes sont les suivantes :
1.  **Secret partagé ($S$) :** Multiplier sa propre clé privée $k$ par la clé publique de la cible $Q_b$ ($S = k \cdot Q_b$).
2.  **Hachage :** Le secret $S$ obtenu (ses coordonnées) doit être haché avec l'algorithme **SHA256** pour produire une clé de 256 bits robuste.
3.  **Chiffrement symétrique :** 
    *   Utiliser les **16 premiers caractères** du secret haché comme **Vecteur d'Initialisation (IV)**.
    *   Utiliser le reste du hash comme clé pour un chiffrement **AES en mode CBC**.

---

## 3. Le programme en détail

Le programme doit être une application en ligne de commande permettant de gérer les clés et les messages.

### 3.1 Paramètres et syntaxe
Si le programme est lancé sans paramètres ou avec `help`, il doit acher un manuel d'utilisation.
**Syntaxe attendue :** `monECC <commande> [<clé>] [<texte>] [switchs]`.

**Règles de commande :**
*   **`keygen`** : Génère une paire de fichiers (clé publique et clé privée).
*   **`crypt`** : Chiffre le texte fourni en utilisant le fichier de clé publique de la cible.
*   **`decrypt`** : Déchiffre le cryptogramme fourni en utilisant le fichier de clé privée de l'utilisateur.

### 3.2 La fonction Keygen
Cette commande doit générer deux fichiers texte dans le dossier courant. 
**Important :** Le contenu des clés doit être encodé en **Base64**.

*   **Fichier `monECC.priv` (Clé privée) :**
    *   Ligne 1 : `---begin monECC private key---`
    *   Ligne 2 : La valeur de l'entier $k$ encodée en Base64.
    *   Ligne 3 : `---end monECC key---`.

*   **Fichier `monECC.pub` (Clé publique) :**
    *   Ligne 1 : `---begin monECC public key---`
    *   Ligne 2 : Les coordonnées $Q_x$ et $Q_y$ séparées par un point-virgule (ex: `x;y`), le tout encodé en Base64.
    *   Ligne 3 : `---end monECC key---`.

### 3.3 La fonction Crypt
1.  Lire le fichier de clé publique fourni en paramètre.
2.  Vérifier la présence des balises de début et extraire les coordonnées $Q_x$ et $Q_y$.
3.  Calculer le secret partagé $S$, le hacher, et chiffrer le message en **AES/CBC**.
4.  Acher le résultat (le cryptogramme) dans la console.

### 3.4 La fonction Decrypt
1.  Lire le fichier de clé privée fourni en paramètre.
2.  Extraire la valeur $k$.
3.  Utiliser les coordonnées du point éphémère (si inclus dans le message) ou de la clé publique correspondante pour recalculer le secret $S$.
4.  Déchiffrer le message et acher le texte en clair.

### 3.5 Options facultatives (Switchs)
Le programme peut accepter des options supplémentaires :
*   **`-f <file>`** : Personnaliser le nom des fichiers de clés générés.
*   **`-s <size>`** : Modifier la plage de l'aléa pour $k$ (par défaut 1000).
*   **`-i`** : Utiliser un fichier texte comme entrée à la place d'une chaîne de caractères.
*   **`-o`** : Enregistrer le résultat dans un fichier de sortie au lieu de l'acher.