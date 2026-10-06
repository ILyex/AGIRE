# Décisions d’architecture initiales

## ADR-001 — Interface partagée et serveur central

**État :** proposition fondée sur l’utilisation par plusieurs employés depuis plusieurs appareils.

**Décision :** application web centrale bilingue arabe/français avec ASP.NET Core Blazor Server et PostgreSQL. Hébergement sur un serveur de l’organisation ou un hébergement privé approuvé. Les appareils ne se connectent jamais directement à la base.

**Motifs :** source de données unique, accès simultané, droits centralisés, sauvegardes structurées et accès multi-appareils. Un EXE autonome ne fournit pas une synchronisation sûre ; un lanceur Windows pourra être ajouté plus tard pour la même interface.

**Limites :** le serveur et le réseau sont requis pour l’accès partagé. Un plan de continuité et de restauration doit être défini avant la mise en production.

## ADR-002 — Aucune installation sur les appareils employés

**Décision :** servir l’interface depuis le serveur en HTTPS ; chaque employé utilise un navigateur déjà installé. Pas d’extension, de runtime ni de client de base de données à installer. L’administrateur système gère le serveur et ses mises à jour.

**Motif :** réduire les étapes de démarrage et les écarts de version entre appareils. Un lanceur web facultatif pourra être ajouté si un besoin concret apparaît, sans déplacer la base sur les appareils.

## ADR-004 — Déploiement privé derrière un proxy de confiance

**Décision :** `docker-compose.production.yml` fournit PostgreSQL sur un réseau interne, le service web sans port public et Caddy pour terminer TLS automatiquement. ASP.NET Core n’accepte les en-têtes `X-Forwarded-*` que depuis l’adresse Caddy fixe configurée et exige `AllowedHosts`, PostgreSQL et un chemin persistant pour les clés Data Protection. Le contrôle de santé vérifie la connexion à la base.

**Limites :** un domaine dirigé vers le serveur et les ports 80/443 ouverts sont nécessaires à l’émission du certificat. Le compose ne configure ni pare-feu hôte, ni DNS, ni disque chiffré, ni alertes. La base et les clés restent sur l’hôte Docker ; celui-ci et ses volumes doivent être protégés. Stockez des sauvegardes chiffrées hors serveur et testez leur restauration. Plusieurs réplicas nécessitent un stockage partagé protégé des clés et une stratégie évitant les migrations simultanées.

## ADR-003 — Sessions web sûres et comptes individuels

**Décision :** comptes créés par un administrateur, ASP.NET Core Identity ou fournisseur d’identité fiable, rôles initiaux (administrateur, agent, lecture seule) et MFA obligatoire pour les administrateurs avant l’accès aux fonctions de gestion. Les pages de configuration de l’application d’authentification restent accessibles pour l’activation initiale. Sessions par cookie uniquement en HTTPS, avec expiration et révocation côté serveur, limitation des tentatives et protection CSRF des opérations modifiant les données.

Ne jamais stocker de secret ou jeton de session dans `localStorage`, et ne pas considérer le masquage d’un bouton comme un contrôle d’accès serveur. Appliquer `Secure`, `HttpOnly` et `SameSite` selon le flux de connexion. ([OWASP — Session Management](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html), [Microsoft — SameSite dans ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-10.0))

Les mots de passe utilisent un hachage adaptatif salé via une solution éprouvée, jamais du texte brut ni un chiffrement réversible. La MFA réduit l’impact du vol de mot de passe et OWASP recommande de limiter les tentatives. ([OWASP — Password Storage](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html), [OWASP — Authentication](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html))

HTTPS doit aussi être actif sur le réseau interne ; l’utilisateur ne doit pas contourner les avertissements de certificat. Les flux avec cookies doivent appliquer une protection CSRF adaptée. ([Microsoft — Protection CSRF](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0))

Le déploiement de production exige un serveur SMTP configuré et un transport TLS (`StartTls` ou `SslOnConnect`) pour confirmer les adresses et réinitialiser les mots de passe. Les identifiants restent dans l’environnement de déploiement, jamais dans Git.

## Flux actuel et couches cibles

La version actuelle exécute l’interface et les contrôles d’accès dans l’application Blazor Server ; les postes clients ne communiquent pas directement avec PostgreSQL. Les diagrammes et couches ci-dessous décrivent l’évolution visée, pas des projets séparés déjà présents.

```text
Navigateur employé (ar/fr, RTL/LTR)
                │ HTTPS
                ▼
         Application ASP.NET Core Blazor Server
      ┌─────────┼───────────┐
      ▼         ▼           ▼
Identité     Domaine      Export XLSX
                │
                ▼
        PostgreSQL central
                │
        Sauvegardes protégées
```

- **Web :** écrans, expérience et traductions ; aucun secret de base de données.
- **API :** identité, validation, autorisations, erreurs cohérentes et contrats versionnés.
- **Domaine/Application :** règles client, facture, paiement et consommation.
- **Infrastructure :** PostgreSQL, migrations, audit, export et sauvegardes.
- **Déploiement :** serveur, base, contrôles de disponibilité et sauvegardes, sans secrets dans Git.

## Cohérence financière

- Les paiements sont des enregistrements distincts ; le solde est calculé à partir des factures et paiements affectés.
- Transactions atomiques lors de la création d’une facture ou d’un paiement.
- Types décimaux adaptés à la monnaie ; pas de virgule flottante pour les calculs financiers.
- Chaque relevé conserve la période, les index et le tarif appliqué ; la facture générée conserve le montant calculé. Le tarif est historisé dans le relevé, pas dans un champ distinct de la facture.
- Les actions sensibles consignent l’utilisateur, la date et les valeurs nécessaires à l’audit.

## Dépôt et données

- Le futur dépôt GitHub sera créé en mode **Privé** ; la visibilité se choisit lors de sa création et ne se garantit pas dans Git.
- Bases de données, exports, sauvegardes, fichiers d’environnement et secrets sont ignorés par Git.
- Les exemples de développement utilisent exclusivement des données fictives.
- Pas de licence open source avant confirmation du propriétaire et des droits.

## Arborescence cible

```text
docs/                     besoins et décisions AR/FR
prototype/                prototype précédent
src/web/                  interface web
src/api/                  API et services
src/domain/               modèles et règles métier
src/infrastructure/       stockage et intégrations
deploy/                   hébergement
scripts/                  développement et maintenance
```

## Décisions en attente

Hébergement local ou cloud, notifications, taxes et règles tarifaires avancées, documents officiels, fonctionnement hors ligne et propriété de la licence. La devise opérationnelle est déjà fixée au DZD et le tarif courant est un montant administré par m³.


