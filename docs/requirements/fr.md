# Étude des besoins — AGIRE TAIRET, Agence nationale de gestion intégrée des ressources en eau

## Objectif

Centraliser la gestion des clients, de la facturation, des encaissements et de la consommation d’eau. Plusieurs employés doivent pouvoir consulter et modifier les mêmes données depuis différents appareils.

## Accès et connexion

- Aucun module complémentaire ni application à installer sur les appareils employés ; un navigateur récent déjà présent et l’adresse du serveur suffisent.
- Installation, mises à jour et base de données sont gérées sur le serveur. L’accès partagé nécessite une connexion au réseau hébergeant le serveur.
- Chaque employé possède son propre compte ; aucun compte partagé. L’administrateur crée et désactive les comptes.
- Pas d’inscription publique ; la création de comptes est réservée aux personnes autorisées.
- Connexion, déconnexion, changement et récupération de mot de passe sécurisés, avec des erreurs qui ne révèlent pas si le compte existe.

## Utilisateurs et rôles proposés

| Rôle | Droits initiaux |
|---|---|
| Administrateur | Utilisateurs, paramètres, consultation complète et validation des corrections |
| Agent de facturation | Clients, relevés, factures et paiements |
| Consultation / comptabilité | Consultation et rapports sans modification, si souhaité |

Chaque modification doit enregistrer son auteur et son horodatage. Les mouvements financiers ne sont pas effacés sans trace ; une correction suit la politique de l’organisation.

## Modules fonctionnels

1. **Clients :** identifiant unique, nom, téléphone, adresse ou secteur/parcelle, état du compte (actif/suspendu), notes et pièces jointes facultatives.
2. **Factures :** numéro unique, client, période ou prestation, dates d’émission et d’échéance, lignes, total et état.
3. **Paiements actuellement disponibles :** montant, date et référence facultative ; paiement partiel rattaché à une seule facture. Le mode de paiement et la répartition d’un versement sur plusieurs factures ne sont pas encore gérés.
4. **Consommation :** index précédent et courant, date, volume calculé, tarif appliqué et facture correspondante.
5. **Tableau de bord :** clients actifs, créances ouvertes et en retard, encaissements par période, consommation et tendances mensuelles.
6. **Recherche et rapports :** recherche par nom, code, téléphone et facture ; filtres par période, état et secteur.
7. **Export :** classeur Excel `.xlsx` avec clients, factures, paiements, consommation et synthèse, selon les droits et la langue sélectionnée.
8. **Paramètres actuellement disponibles :** tarif central fixe par m³, modifiable par un administrateur. Chaque relevé conserve le tarif utilisé ; la période est mensuelle et l’échéance de la facture de consommation est fixée à 30 jours. Les interfaces prennent en charge l’arabe et le français.

## Arabe et français

- Traduction complète des libellés, validations, alertes et rapports.
- Le changement de langue règle RTL/LTR et l’alignement des menus, tableaux et icônes directionnelles.
- Les données saisies par l’utilisateur ne sont pas traduites ; elles peuvent contenir arabe et français.
- Les dates, nombres et montants suivent la langue et les paramètres, sans modifier les valeurs enregistrées.

## Règles financières à valider

- La devise opérationnelle est le dinar algérien (DZD / دج), avec deux décimales pour les factures et paiements. Des devises historiques peuvent apparaître dans les rapports, mais aucun taux de conversion n’est calculé.
- Prix TTC ou hors taxes, types de taxes et exonérations.
- Le tarif actuel est un prix fixe administré par m³ ; les tranches, abonnements, taxes et dates d’effet restent à définir avant toute évolution de cette règle.
- Pénalités et traitement des retards, le cas échéant.
- Format des numéros de factures/reçus et mentions obligatoires de l’organisation.
- Mode de paiement, correction, annulation, remboursement et répartition d’un versement sur plusieurs factures.

## Exigences de qualité

- Utilisation simultanée sans conflits ni perte de mises à jour.
- Transactions financières atomiques.
- Autorisations vérifiées côté serveur à chaque requête, pas uniquement par masquage des boutons.
- HTTPS obligatoire ; cookie de session `Secure`, `HttpOnly` et `SameSite` adapté, protection CSRF lorsque nécessaire.
- Mots de passe stockés avec un hachage adaptatif salé, jamais en clair ou avec un chiffrement réversible.
- Limitation des tentatives, temporisation/blocage après échecs répétés, révocation des sessions à la déconnexion ou à la désactivation du compte.
- Authentification multifacteur obligatoire pour les administrateurs et recommandée pour les autres comptes.
- Journal des connexions et opérations financières ; pas de mots de passe, jetons ou données inutiles dans les journaux.
- Pas de données financières ou jetons d’accès dans `localStorage` des appareils ; limiter la mise en cache des pages sensibles.
- Base isolée de l’Internet public, accès réseau limité et sauvegardes protégées.
- Sauvegardes protégées et tests de restauration ; la synchronisation ne remplace pas les sauvegardes.
- Aucune donnée de production ni secret dans le dépôt.
- Interface adaptée aux ordinateurs et tablettes, recherche rapide et erreurs compréhensibles.

## Périmètre par étapes

- **Version initiale :** comptes et rôles, clients, factures, paiements, relevés, tableau de bord, Excel et sauvegardes.
- **Évolutions éventuelles :** documents officiels, pièces jointes, alertes d’échéance, import Excel et lanceur de bureau.
