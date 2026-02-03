# Guide de test pour monECC

Ce dossier contient des fichiers pour tester manuellement et automatiquement le programme monECC.

## Fichiers de test

- **`test_commands.txt`** : Liste complète de commandes à tester manuellement
- **`run_tests.sh`** : Script automatisé pour exécuter les tests essentiels
- **`TEST_README.md`** : Ce fichier

## Méthode 1 : Tests manuels

### Utilisation du fichier `test_commands.txt`

1. Lancez le programme en mode interactif :
   ```bash
   dotnet run
   ```

2. Copiez-collez les commandes du fichier `test_commands.txt` une par une dans le terminal

3. Vérifiez les résultats de chaque commande

### Exemples de commandes à tester

```bash
# Génération de clés
keygen
keygen -f mescles -s 5000 -d ./keys/

# Chiffrement
crypt monECC.pub "Message secret" -o message.txt -d ./encrypted/

# Déchiffrement
decrypt monECC.priv -i message.txt -o resultat.txt -d ./decrypted/

# Test de validation
test
test -v
```

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

1. ✅ Génération de clés (basique, avec options)
2. ✅ Commande `test` (basique et verbose)
3. ✅ Chiffrement (basique, avec fichiers)
4. ✅ Déchiffrement (depuis fichiers)
5. ✅ Cycle complet (génération → chiffrement → déchiffrement)
6. ✅ Commande `help`

### Résultat attendu

Le script affiche :
- ✓ pour chaque test réussi
- ✗ pour chaque test échoué
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
```

### 2. Test du switch `-d`

```bash
# Créer des clés dans un répertoire spécifique
keygen -f testdir -d ./test_output/

# Vérifier que les fichiers sont créés dans le bon répertoire
ls -la ./test_output/
```

### 3. Test de la commande `test`

```bash
# Test basique
test

# Test en mode verbose
test -v
```

### 4. Test d'un cycle complet

```bash
# 1. Générer des clés
keygen -f cycle_test -d ./test_cycle/

# 2. Chiffrer un message
crypt ./test_cycle/cycle_test.pub "Message de test" -o ./test_cycle/chiffre.txt

# 3. Déchiffrer le message
decrypt ./test_cycle/cycle_test.priv -i ./test_cycle/chiffre.txt -o ./test_cycle/dechiffre.txt

# 4. Vérifier le résultat
cat ./test_cycle/dechiffre.txt
# Devrait afficher : "Message de test"
```

### 5. Tests d'erreurs

Vérifiez que les erreurs sont bien gérées :

```bash
# Fichier inexistant
crypt fichier_inexistant.pub "test"

# Paramètres manquants
crypt
decrypt

# Taille invalide
keygen -s 0
keygen -s -5
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
