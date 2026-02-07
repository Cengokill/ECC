# Guide de test pour monECC

Ce dossier contient des fichiers pour tester manuellement et automatiquement le programme monECC.

## Fichiers de test

- **`run_tests.sh`** : Script automatisé pour exécuter les tests essentiels
- **`TEST_README.md`** : Ce fichier

## Méthode 1 : Tests manuels

### Comment exécuter les tests manuels

1. Lancez le programme en mode interactif :

```bash
dotnet run
```

2. Copiez-collez les commandes ci-dessous une par une dans le terminal.
3. Vérifiez les résultats (création de fichiers, sorties console, messages d'erreur).

## Méthode 2 : Tests automatisés

### Utilisation du script `run_tests.sh`

Le script exécute automatiquement une série de tests et affiche les résultats.

```bash
# Exécuter tous les tests
./run_tests.sh

# Ou avec bash explicitement
bash run_tests.sh
```

e script génère automatiquement un rapport détaillé au format Markdown à la fin de l'exécution. Le rapport est enregistré sous le nom `RAPPORT_TESTS_YYYYMMDD_HHMMSS.md` avec l'horodatage de l'exécution.

### Tests effectués par le script

1. Génération de clés (basique, avec options)
2. Commande `test` (basique et verbose)
3. Chiffrement (basique, avec fichiers)
4. Déchiffrement (depuis fichiers)
5. Cycle complet (génération → chiffrement → déchiffrement)
6. Commande `help`

### Résultat attendu

Le script affiche :
- Un indicateur pour chaque test réussi
- Un indicateur pour chaque test échoué
- Un résumé final avec le nombre de tests passés/échoués
- Un message confirmant la génération du rapport

### Rapport généré automatiquement

À la fin de l'exécution, le script génère un rapport Markdown complet qui contient :

- **Résumé des tests** : nombre total, réussis, échoués, pourcentage, durée
- **Détails par catégorie** : tableaux avec résultats et descriptions
- **Fonctionnalités testées** : liste des features validées
- **Fichiers créés** : inventaire des fichiers générés pendant les tests
- **Vérifications effectuées** : checklist des validations
- **Conclusion** : statut global et recommandations

Le rapport est automatiquement nommé avec l'horodatage pour conserver un historique des tests.

**Exemple** : `RAPPORT_TESTS_20260203_152325.md`

## Tests à effectuer manuellement

### 1. Test de compatibilité ascendante

Vérifiez que les anciennes commandes fonctionnent toujours :

```bash
keygen
crypt monECC.pub "test"
decrypt monECC.priv "<texte_chiffré>"
help
```

### 2. Test du switch `-d`

```bash
# Créer des clés dans un répertoire spécifique
keygen -f testdir -d ./test_output/

# Vérifier que les fichiers sont créés dans le bon répertoire
ls -la ./test_output/
```

### 3. Tests de base : génération de clés

```bash
# Génération de clés par défaut
keygen

# Génération avec nom personnalisé
keygen -f mescles

# Génération avec taille personnalisée
keygen -f testkeys -s 5000

# Génération dans un répertoire spécifique
keygen -f testdir -d ./test_output/

# Génération avec tous les paramètres
keygen -f complet -s 2000 -d ./keys/
```

### 4. Tests de chiffrement

```bash
# Chiffrement basique (utilise monECC.pub généré précédemment)
crypt monECC.pub "Bonjour, ceci est un test"

# Chiffrement avec sortie dans un fichier
crypt monECC.pub "Message secret" -o message_chiffre.txt

# Chiffrement depuis un fichier d'entrée
echo "Message depuis fichier" > input.txt
crypt monECC.pub -i input.txt -o output.txt

# Chiffrement avec répertoire de sortie
crypt monECC.pub "Test avec répertoire" -o test.txt -d ./encrypted/

# Chiffrement complet (fichier entrée + répertoire sortie)
crypt monECC.pub -i input.txt -o result.txt -d ./encrypted/
```

### 5. Tests de déchiffrement

```bash
# Déchiffrement basique (remplacez <texte_chiffré> par le résultat d'un crypt affiché en console)
# decrypt monECC.priv "<texte_chiffré>"

# Déchiffrement depuis un fichier
decrypt monECC.priv -i message_chiffre.txt

# Déchiffrement avec sortie dans un fichier
decrypt monECC.priv -i message_chiffre.txt -o message_dechiffre.txt

# Déchiffrement avec répertoire de sortie
decrypt monECC.priv -i message_chiffre.txt -o result.txt -d ./decrypted/

# Déchiffrement complet
decrypt monECC.priv -i ./encrypted/test.txt -o final.txt -d ./decrypted/
```

### 6. Test de la commande `test`

```bash
# Test basique
test

# Test en mode verbose
test -v

# Alias
test --verbose
```

### 7. Test d'un cycle complet

```bash
# 1. Générer des clés
keygen -f cycle_test -d ./test_cycle/

# 2. Chiffrer un message
crypt ./test_cycle/cycle_test.pub "Message de test complet" -o ./test_cycle/chiffre.txt

# 3. Déchiffrer le message
decrypt ./test_cycle/cycle_test.priv -i ./test_cycle/chiffre.txt -o ./test_cycle/dechiffre.txt

# 4. Vérifier le résultat
cat ./test_cycle/dechiffre.txt
# Devrait afficher : "Message de test complet"
```

### 8. Tests d'erreurs

Vérifiez que les erreurs sont bien gérées :

```bash
# Fichier inexistant
crypt fichier_inexistant.pub "test"

# Répertoire invalide
keygen -d /chemin/inexistant/test

# Paramètres manquants
crypt
decrypt
keygen -f

# Taille invalide
keygen -s 0
keygen -s -5
```

### 9. Tests avancés (multi-destinataires)

```bash
# Génération multiple de clés dans différents répertoires
keygen -f alice -d ./users/alice/
keygen -f bob -d ./users/bob/
keygen -f charlie -d ./users/charlie/

# Chiffrement pour plusieurs destinataires
crypt ./users/alice/alice.pub "Message pour Alice" -o alice_msg.txt -d ./messages/
crypt ./users/bob/bob.pub "Message pour Bob" -o bob_msg.txt -d ./messages/
crypt ./users/charlie/charlie.pub "Message pour Charlie" -o charlie_msg.txt -d ./messages/

# Déchiffrement par chaque destinataire
decrypt ./users/alice/alice.priv -i ./messages/alice_msg.txt -o alice_read.txt
decrypt ./users/bob/bob.priv -i ./messages/bob_msg.txt -o bob_read.txt
decrypt ./users/charlie/charlie.priv -i ./messages/charlie_msg.txt -o charlie_read.txt
```

## Nettoyage après les tests

Pour supprimer tous les fichiers de test créés :

```bash
rm -rf ./test_output ./keys ./encrypted ./decrypted ./test_cycle ./users ./messages
rm -f input.txt output.txt message_chiffre.txt message_dechiffre.txt *.pub *.priv
```

## Notes importantes

- Les répertoires sont créés automatiquement si nécessaire avec le switch `-d`
- Les fichiers temporaires de la commande `test` sont supprimés automatiquement (sauf en mode `-v`)
- Le script `run_tests.sh` nettoie automatiquement les fichiers de test avant de commencer
- Pour tester en mode non-interactif, utilisez : `echo "commande" | dotnet run`

## Dépannage

### Le script ne s'exécute pas

```bash
chmod +x run_tests.sh
```

### Erreur "command not found"

Assurez-vous d'être dans le répertoire du projet :
```bash
cd /Users/user/Documents/ECC
```

### Tests qui échouent

Vérifiez que :
- Le programme compile correctement : `dotnet build`
- Les fichiers de clés existent avant de tester crypt/decrypt
- Vous avez les permissions d'écriture dans les répertoires de test
