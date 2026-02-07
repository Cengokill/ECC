# monECC - Cryptographie sur Courbes Elliptiques

Programme de cryptographie basé sur les courbes elliptiques (ECC) en ligne de commande, permettant de générer des clés, chiffrer et déchiffrer des messages de manière sécurisée.

## Description

monECC est une implémentation en C# d'un système de cryptographie à courbes elliptiques utilisant :
- **Courbe elliptique** : y² = x³ + 35x + 3 (mod 101)
- **Point générateur** : P = (2, 9) (J'ai gardé le point (2,9))
- **Chiffrement** : AES en mode CBC avec clé dérivée par SHA256
- **Échange de clés** : Basé sur le principe de Diffie-Hellman sur courbe elliptique (ECDH)

## Démarrage rapide

### Compilation et exécution

```bash
cd /xxx/ECC
dotnet build
dotnet run
```

### Utilisation de base

Le programme fonctionne en mode interactif. Une fois lancé, vous pouvez utiliser les commandes suivantes :

```bash
# Générer une paire de clés
keygen

# Chiffrer un message
crypt monECC.pub "Message secret"

# Déchiffrer un message
decrypt monECC.priv "message_chiffré_en_base64"

# Valider le système
test

# Afficher l'aide
help

# Quitter
exit
```

## Commandes disponibles

### `keygen` - Génération de clés

Génère une paire de clés (publique et privée) dans le répertoire courant.

**Fichiers générés :**
- `monECC.pub` : Clé publique (point Q sur la courbe)
- `monECC.priv` : Clé privée (entier k)

**Options :**
- `-f <nom>` : Personnaliser le nom des fichiers (ex: `-f alice` → `alice.pub`, `alice.priv`)
- `-s <taille>` : Modifier la plage de génération de k (défaut: 1000)
- `-d <répertoire>` : Spécifier le répertoire de sortie

**Exemples :**
```bash
keygen                                    # Génère monECC.pub et monECC.priv
keygen -f alice -s 5000                   # Génère alice.pub et alice.priv avec k entre 1 et 5000
keygen -f bob -d ./keys/                  # Génère bob.pub et bob.priv dans ./keys/
```

### `crypt` - Chiffrement

Chiffre un message en utilisant la clé publique du destinataire.

**Syntaxe :**
```bash
crypt <fichier_clé_publique> "<message>"
```

**Options :**
- `-i <fichier>` : Lire le message depuis un fichier
- `-o <fichier>` : Sauvegarder le résultat dans un fichier
- `-d <répertoire>` : Spécifier le répertoire de sortie

**Exemples :**
```bash
crypt monECC.pub "Hello World"                    # Affiche le résultat en console
crypt alice.pub "Secret" -o message.txt           # Sauvegarde dans message.txt
crypt bob.pub -i input.txt -o encrypted.txt       # Chiffre depuis input.txt
```

### `decrypt` - Déchiffrement

Déchiffre un message en utilisant sa propre clé privée.

**Syntaxe :**
```bash
decrypt <fichier_clé_privée> "<cryptogramme>"
```

**Options :**
- `-i <fichier>` : Lire le cryptogramme depuis un fichier
- `-o <fichier>` : Sauvegarder le résultat dans un fichier
- `-d <répertoire>` : Spécifier le répertoire de sortie

**Exemples :**
```bash
decrypt monECC.priv "cryptogramme_base64"         # Affiche le résultat en console
decrypt alice.priv -i message.txt                 # Déchiffre depuis message.txt
decrypt bob.priv -i encrypted.txt -o result.txt   # Déchiffre et sauvegarde dans result.txt
```

### `test` - Validation du système

Exécute une série de tests pour valider le bon fonctionnement du système cryptographique.

**Options :**
- `-v` ou `--verbose` : Affichage détaillé avec informations de débogage

**Exemples :**
```bash
test           # Test rapide avec résumé
test -v        # Test détaillé avec toutes les informations
```

### `help` - Aide

Affiche le manuel d'utilisation complet avec tous les détails sur les commandes et options.

## Fonctionnement technique

### Génération de clés (keygen)

1. Génère un entier aléatoire k (clé privée) entre 1 et 1000
2. Calcule Q = k × P sur la courbe (clé publique) via l'algorithme "Double-and-Add"
3. Vérifie que Q est bien sur la courbe
4. Encode les clés en Base64 et les sauvegarde dans des fichiers

### Chiffrement (crypt)

1. Lit la clé publique du destinataire (Q)
2. Génère une clé privée éphémère (r)
3. Calcule le secret partagé S = r × Q
4. Dérive une clé AES et un IV via SHA256 du secret S
5. Chiffre le message avec AES en mode CBC
6. Calcule R = r × P (point éphémère public)
7. Retourne R et le cryptogramme encodés en Base64

### Déchiffrement (decrypt)

1. Lit la clé privée (k)
2. Extrait R du message chiffré
3. Recalcule le secret partagé S = k × R
4. Dérive la même clé AES et IV via SHA256
5. Déchiffre le message avec AES en mode CBC
6. Retourne le texte en clair

## Tests

Le projet inclut une suite complète de tests automatisés qui valident toutes les fonctionnalités.

### Exécution des tests automatisés

```bash
./run_tests.sh
```

Le script :
- Exécute 12 tests couvrant toutes les fonctionnalités
- Affiche les résultats en temps réel avec code couleur
- Génère automatiquement un rapport détaillé au format Markdown
- Valide l'intégrité des opérations de chiffrement/déchiffrement

**Pour plus de détails sur les tests, consultez [TEST_README.md](TEST_README.md)**

## Structure du projet

```
ECC/
├── Program.cs             # Point d'entrée et gestion des commandes
├── GenerationClef.cs      # Arithmétique des courbes elliptiques
├── TP-ECC.md              # Spécifications du projet
├── README.md              # Ce fichier
├── TEST_README.md         # Documentation des tests
├── run_tests.sh           # Script de tests automatisés
└── RAPPORT_TESTS_*.md     # Rapports de tests générés
```

## Format des fichiers de clés

### Clé privée (*.priv)

```
---begin monECC private key---
<valeur_k_en_base64>
---end monECC key---
```

### Clé publique (*.pub)

```
---begin monECC public key---
<Qx;Qy_en_base64>
---end monECC key---
```

## Exemple complet

```bash
# Démarrer le programme
dotnet run

# Alice génère ses clés
monECC> keygen -f alice

# Bob génère ses clés
monECC> keygen -f bob

# Alice chiffre un message pour Bob
monECC> crypt bob.pub "Message secret pour Bob" -o message_pour_bob.txt

# Bob déchiffre le message d'Alice
monECC> decrypt bob.priv -i message_pour_bob.txt

# Résultat : "Message secret pour Bob"
```

## Prérequis

- .NET SDK 6.0 ou supérieur
- Bash (pour l'exécution des tests automatisés)

## Fonctionnalités

- Génération de paires de clés ECC
- Chiffrement/déchiffrement de messages
- Support des fichiers d'entrée/sortie
- Répertoires personnalisables
- Mode interactif avec historique de commandes
- Validation automatique du système
- Tests automatisés complets
- Génération de rapports de tests

## Références

Pour plus d'informations :
- [TP-ECC.md](TP-ECC.md) : Spécifications techniques détaillées
- [TEST_README.md](TEST_README.md) : Guide complet des tests
- `help` dans le programme : Manuel d'utilisation intégré

## Sécurité

**Note importante** : Cette implémentation est à vocation éducative. Pour une utilisation en production, il est recommandé d'utiliser des bibliothèques cryptographiques établies et auditées.

Points de sécurité :
- Utilisation d'une courbe elliptique sur un corps fini petit (F₁₀₁) - suffisant pour la démonstration
- Algorithme Double-and-Add pour l'efficacité
- SHA256 pour la dérivation de clé
- AES-CBC pour le chiffrement symétrique
- Point éphémère pour chaque chiffrement
