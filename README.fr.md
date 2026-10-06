# AGIRE TAIRET — Gestion intégrée des ressources en eau et des clients

Application web bilingue arabe/français pour gérer les clients, factures, paiements, créances et relevés de consommation d’eau. Elle est conçue pour plusieurs employés connectés à un serveur central ; chaque employé utilise son navigateur sans installer de client.

## Fonctionnalités présentes

- Connexion avec comptes individuels, rôles (administrateur, facturation, lecture seule) et verrouillage temporaire après plusieurs échecs.
- Gestion des clients et de leur statut, avec journal d’audit.
- Factures, paiements partiels et calcul du solde restant.
- Archives mensuelles des factures selon la date d’émission, avec un récapitulatif et une feuille Excel dédiée.
- La devise par défaut des nouvelles factures est le dinar algérien (DZD) ; la devise des factures existantes est conservée sans conversion automatique.
- Saisie mensuelle des relevés et création automatique d’une facture de consommation.
- Tableau de bord, graphiques et classeur Excel multifeuille en arabe ou français.
- Thèmes clair/sombre aux tons aquatiques, animations liquides discrètes, préférence mémorisée localement et respect de la réduction des animations du système.
- PostgreSQL pour le serveur partagé ; SQLite uniquement pour le développement local.

## Architecture

- ASP.NET Core Blazor Server sur .NET 10.
- ASP.NET Core Identity pour les comptes et les sessions.
- Entity Framework Core et PostgreSQL. Les postes clients n’accèdent jamais directement à la base.
- Interface RTL/LTR, ressources front-end incluses localement.

```text
docs/                  exigences et décisions en arabe et français
prototype/             prototype visuel précédent
src/web/Hawdh.Portal   application, données et interface
tests/                 tests d’intégration des comptes, e-mails, autorisations et exports Excel
Hawdh.Platform.slnx    solution .NET
```

## Développement sur un poste

Le poste de développement nécessite le SDK .NET 10. Depuis PowerShell à la racine :

```powershell
$env:SeedAdmin__Email = 'admin@example.local'
$env:SeedAdmin__Password = 'choisir-un-mot-de-passe-de-test-robuste'
dotnet run --project src/web/Hawdh.Portal
```

Ouvrez l’adresse affichée dans le terminal. SQLite est stocké localement dans `src/web/Hawdh.Portal/App_Data/` ; ce mode ne convient pas au partage ni aux données réelles.

L’envoi des liens de confirmation et de récupération est facultatif en développement. Configurez `Smtp__Host`, `Smtp__Port`, `Smtp__Security` et `Smtp__FromAddress`, puis `Smtp__UserName` et `Smtp__Password` si le serveur exige une authentification. En production, seuls `StartTls` et `SslOnConnect` sont acceptés.

## PostgreSQL avec Docker Compose

Cette configuration sert au développement local seulement. Copiez `.env.example` vers `.env`, remplacez les secrets d’exemple par des valeurs uniques, puis lancez :

```powershell
Copy-Item .env.example .env
# Modifier .env avec des secrets forts et uniques
docker compose up -d --build
```

Ouvrez `http://localhost:5080`. La base n’est pas publiée sur un port externe et l’application est volontairement limitée à `localhost`. Ce fichier reste réservé au développement local.

## Intégration continue

GitHub Actions s’exécute à chaque push ou pull request : restauration avec audit NuGet, vérification syntaxique des scripts de sauvegarde/restauration, validation de la configuration Compose de production, compilation Release, tests d’intégration de la MFA administrateur, des autorisations du rôle facturation, du changement d’adresse avec jeton de confirmation, de l’envoi SMTP, des formules Excel et des traductions utilisées, puis construction de l’image Docker de production. Les bases SQLite et SMTP de test sont isolées et temporaires. Dependabot surveille chaque semaine les mises à jour NuGet et GitHub Actions.

## Déploiement privé multi-utilisateur avec HTTPS

Pointez un enregistrement DNS A de votre domaine vers le serveur et ouvrez les ports 80 et 443. Sur le serveur, copiez `deploy/.env.production.example` vers `deploy/.env.production`, remplacez le domaine et tous les mots de passe par des valeurs uniques, puis lancez :

```bash
docker compose --env-file deploy/.env.production -f docker-compose.production.yml up -d --build
```

Caddy obtient et renouvelle le certificat TLS. PostgreSQL n’expose aucun port externe et le service web est accessible uniquement derrière Caddy. N’ouvrez pas le port 8080. Après la première connexion réussie du compte administrateur, supprimez `SEED_ADMIN_PASSWORD` du fichier de production puis recréez le conteneur web. Gardez le fichier de secrets hors de Git. Protégez le volume Data Protection et incluez-le dans les sauvegardes.

Créez une sauvegarde avec `bash deploy/backup-postgres.sh` ; le script valide l’archive avant de l’enregistrer. Testez une restauration sur une base séparée avec `BACKUP_FILE=/path/to/hawdh-backup.dump TARGET_DB=hawdh_restore_check bash deploy/restore-postgres.sh`. Le script refuse d’écraser la base de production. Conservez l’archive et son empreinte dans un emplacement chiffré hors du serveur et testez régulièrement la restauration. Si le sous-réseau Docker `172.31.245.0/24` est déjà utilisé, modifiez-le dans Compose ainsi que l’adresse `ReverseProxy__KnownProxy`.

## Sécurité et limites de préparation

La version actuelle applique les rôles, des mots de passe de 12 caractères minimum, un verrouillage après cinq échecs, un cookie HttpOnly/SameSite, la désactivation du cache des pages authentifiées et des contrôles d’autorisation sur les opérations financières. La MFA est obligatoire pour les administrateurs ; les pages de configuration de l’application d’authentification restent accessibles pour son activation initiale. Compose de production configure HTTPS, un proxy de confiance, la persistance des clés de session et le contrôle de disponibilité PostgreSQL. Configurez SMTP avec TLS dans `deploy/.env.production` pour l’envoi des confirmations d’adresse, changements d’e-mail et réinitialisations de mot de passe ; l’application de production refuse de démarrer si ces paramètres obligatoires manquent. Avant des données financières réelles, configurez des sauvegardes chiffrées hors serveur avec restauration testée, la supervision, une revue indépendante et faites valider les règles de tarification/facturation par l’organisation.

Cette première version est exécutable et révisable, mais n’est pas prête pour une exposition publique ou des données financières réelles. Il reste à configurer un serveur HTTPS et un certificat valide, les identifiants SMTP, les sauvegardes/restaurations, la supervision, une revue sécurité et opérationnelle, et à faire valider les tarifs, taxes et factures officielles par l’organisation.

## Confidentialité du dépôt

Le projet est prêt pour un dépôt GitHub **privé**, mais aucun dépôt GitHub n’a été créé et aucun fichier n’a été publié. Ne versionnez jamais les données réelles, mots de passe ou secrets ; `.gitignore` exclut les données locales et `.env`. Aucun choix de licence open source n’est fait avant clarification du propriétaire et des droits.

## Documentation

- [Étude des besoins en arabe](docs/requirements/ar.md) et [en français](docs/requirements/fr.md)
- [Décisions d’architecture en arabe](docs/architecture/ar.md) et [en français](docs/architecture/fr.md)
- [Contribution](CONTRIBUTING.md)
- [Signalement de vulnérabilité](SECURITY.md)

