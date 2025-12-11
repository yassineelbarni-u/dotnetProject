# Système de Panier  - Explication Complète

## 📊 Modélisation de la Base de Données

### Table `Paniers`
```sql
Id              INT PRIMARY KEY      -- Identifiant unique de chaque ligne
SessionId       VARCHAR(100) NULL    -- ID unique du visiteur (cookie)
UserId          VARCHAR(100) NULL    -- ID utilisateur si connecté (NULL pour l'instant)
ProduitId       INT                  -- Référence au produit
Quantite        INT                  -- Nombre d'exemplaires
PrixUnitaire    DECIMAL              -- Prix au moment de l'ajout ⚠️ IMPORTANT
DateAjout       DATETIME             -- Date d'ajout au panier
DateExpiration  DATETIME             -- 90 jours après DateAjout
```

---

## 🔑 Concepts Clés

### 1. **SessionId - L'identité du visiteur**
- **Qu'est-ce que c'est ?** Un ID unique généré pour chaque visiteur (comme un numéro de ticket)
- **Comment ça marche ?** 
  - Première visite → Génération d'un `Guid.NewGuid()` → Stocké dans la session
  - Retour du visiteur → Récupération du même SessionId
- **Pourquoi ?** Pour que chaque visiteur ait son propre panier, même sans compte

```csharp
// Générer ou récupérer le SessionId
var sessionId = HttpContext.Session.GetString("SessionId");
if (string.IsNullOrEmpty(sessionId))
{
    sessionId = Guid.NewGuid().ToString(); // Ex: "a3f5c2d8-1234-5678-90ab-cdef12345678"
    HttpContext.Session.SetString("SessionId", sessionId);
}
```

---

### 2. **UserId - Pour les utilisateurs connectés**
- **Actuellement :** `NULL` car pas de système de compte utilisateur
- **Futur :** Si vous ajoutez un système de login, vous stockerez l'ID de l'utilisateur ici
- **Avantage :** Le panier suit l'utilisateur sur tous ses appareils

---

### 3. **PrixUnitaire - Prix au moment de l'ajout ⚠️ TRÈS IMPORTANT**

#### Pourquoi stocker le prix ?
Imaginez ce scénario :
1. **Lundi** : Client ajoute un produit à 100€
2. **Mardi** : Admin change le prix à 150€
3. **Mercredi** : Client passe commande

**Sans PrixUnitaire stocké :**
- Le client paie 150€ → Il est furieux, c'était 100€ quand il l'a ajouté ! 😡

**Avec PrixUnitaire stocké :**
- Le client paie 100€ → Prix garanti au moment de l'ajout ✅

```csharp
// Lors de l'ajout au panier
var nouveauPanier = new PanierModel
{
    ProduitId = id,
    Quantite = 1,
    PrixUnitaire = produit.Prix,  // ⚠️ Stocker le prix ACTUEL
    DateAjout = DateTime.Now
};
```

---

### 4. **Quantité - Gestion intelligente**

#### Comment ça fonctionne ?
1. **Première fois** : Créer une nouvelle ligne avec `Quantite = 1`
2. **Deuxième fois** : Augmenter `Quantite++` sur la ligne existante
3. **Pas de doublons** : Un produit = Une ligne dans le panier

```csharp
// Vérifier si le produit est déjà dans le panier
var panierExistant = await _context.Paniers
    .FirstOrDefaultAsync(p => p.ProduitId == id && p.SessionId == sessionId);

if (panierExistant != null)
{
    panierExistant.Quantite++;  // Augmenter la quantité
}
else
{
    // Créer nouvelle ligne
    _context.Paniers.Add(nouveauPanier);
}
```

---

### 5. **DateExpiration - Nettoyage automatique (90 jours)**

#### Pourquoi 90 jours ?
- Amazon garde les paniers 90 jours
- Évite d'avoir des millions de paniers abandonnés en base de données

```csharp
DateExpiration = DateTime.Now.AddDays(90)
```

**Nettoyage automatique (à implémenter plus tard) :**
```csharp
// Supprimer les paniers expirés
var paniersExpires = await _context.Paniers
    .Where(p => p.DateExpiration < DateTime.Now)
    .ToListAsync();
_context.Paniers.RemoveRange(paniersExpires);
await _context.SaveChangesAsync();
```

---

## 🔄 Workflow Complet

### **Scénario 1 : Visiteur Anonyme**

```
1. Utilisateur arrive sur le site
   ↓
2. Première action "Ajouter au panier"
   → Génération SessionId: "abc-123-def"
   → Stocké dans Session
   ↓
3. Base de données:
   | Id | SessionId   | ProduitId | Quantite | PrixUnitaire |
   |----|-------------|-----------|----------|--------------|
   | 1  | abc-123-def | 5         | 1        | 99.99        |
   ↓
4. Ajoute encore le même produit
   → Même SessionId détecté
   → Quantite devient 2
   ↓
5. Base de données:
   | Id | SessionId   | ProduitId | Quantite | PrixUnitaire |
   |----|-------------|-----------|----------|--------------|
   | 1  | abc-123-def | 5         | 2        | 99.99        |
```

---

### **Scénario 2 : Deux Visiteurs Différents**

```
Visiteur A:
SessionId = "aaa-111"
Panier: Produit 5 (x2)

Visiteur B:
SessionId = "bbb-222"
Panier: Produit 5 (x1), Produit 8 (x3)

Base de données:
| Id | SessionId | ProduitId | Quantite |
|----|-----------|-----------|----------|
| 1  | aaa-111   | 5         | 2        |  ← Visiteur A
| 2  | bbb-222   | 5         | 1        |  ← Visiteur B
| 3  | bbb-222   | 8         | 3        |  ← Visiteur B
```

**Chacun voit uniquement son panier !**

---

## 💻 Code Expliqué Ligne par Ligne

### **1. Ajouter au Panier (OnPostAjouterPanierAsync)**

```csharp
public async Task<IActionResult> OnPostAjouterPanierAsync(int id)
{
    // Récupérer le produit depuis la base de données
    var produit = await _context.Produits.FindAsync(id);
    if (produit == null) return NotFound();

    // ÉTAPE 1: Obtenir ou créer SessionId
    var sessionId = HttpContext.Session.GetString("SessionId");
    if (string.IsNullOrEmpty(sessionId))
    {
        sessionId = Guid.NewGuid().ToString(); // Créer ID unique
        HttpContext.Session.SetString("SessionId", sessionId); // Stocker dans session
    }

    // ÉTAPE 2: Vérifier si produit déjà dans le panier
    var panierExistant = await _context.Paniers
        .FirstOrDefaultAsync(p => 
            p.ProduitId == id &&           // Même produit
            p.SessionId == sessionId       // ET même visiteur
        );

    if (panierExistant != null)
    {
        // ÉTAPE 3A: Produit déjà présent → Augmenter quantité
        panierExistant.Quantite++;
    }
    else
    {
        // ÉTAPE 3B: Nouveau produit → Créer ligne
        var nouveauPanier = new PanierModel
        {
            SessionId = sessionId,           // Lier au visiteur
            ProduitId = id,                  // Lier au produit
            Quantite = 1,                    // Première unité
            PrixUnitaire = produit.Prix,     // ⚠️ Prix actuel
            DateAjout = DateTime.Now,
            DateExpiration = DateTime.Now.AddDays(90)
        };
        _context.Paniers.Add(nouveauPanier);
    }

    // ÉTAPE 4: Sauvegarder en base de données
    await _context.SaveChangesAsync();
    return RedirectToPage();
}
```

---

### **2. Afficher le Panier (OnGetAsync dans Panier/Index)**

```csharp
public async Task OnGetAsync()
{
    // Récupérer le SessionId du visiteur
    var sessionId = HttpContext.Session.GetString("SessionId");
    
    if (!string.IsNullOrEmpty(sessionId))
    {
        // Récupérer UNIQUEMENT les articles de CE visiteur
        ArticlesPanier = await _context.Paniers
            .Include(p => p.Produit)              // Charger les infos produit
            .Where(p => p.SessionId == sessionId) // ⚠️ Filtrer par SessionId
            .ToListAsync();

        // Calculer le total avec le prix stocké
        Total = ArticlesPanier.Sum(p => p.PrixUnitaire * p.Quantite);
    }
}
```

---

### **3. Compter les Articles (Badge panier)**

```csharp
var sessionId = HttpContext.Session.GetString("SessionId");
if (!string.IsNullOrEmpty(sessionId))
{
    NombreArticlesPanier = await _context.Paniers
        .Where(p => p.SessionId == sessionId)  // Filtrer par SessionId
        .SumAsync(p => p.Quantite);            // Additionner toutes les quantités
}
```

**Exemple :**
```
Panier:
- Produit A : Quantite = 2
- Produit B : Quantite = 3
- Produit C : Quantite = 1

NombreArticlesPanier = 2 + 3 + 1 = 6
```

---

### **4. Sécurité - Vérifier le SessionId**

```csharp
public async Task<IActionResult> OnPostSupprimerAsync(int id)
{
    var sessionId = HttpContext.Session.GetString("SessionId");
    var panier = await _context.Paniers.FindAsync(id);
    
    // ⚠️ IMPORTANT: Vérifier que l'article appartient à CE visiteur
    if (panier != null && panier.SessionId == sessionId)
    {
        _context.Paniers.Remove(panier);
        await _context.SaveChangesAsync();
    }
    
    return RedirectToPage();
}
```

**Pourquoi ?**
- Sans cette vérification, un visiteur pourrait supprimer les articles d'un autre !
- Amazon vérifie toujours que vous êtes le propriétaire

---

## 📈 Avantages de ce Système

### ✅ **1. Isolation des Paniers**
- Chaque visiteur a son propre panier
- Pas de confusion entre clients

### ✅ **2. Prix Garanti**
- Le prix est fixé au moment de l'ajout
- Pas de mauvaise surprise pour le client

### ✅ **3. Persistance**
- Le panier reste même si le visiteur ferme le navigateur
- Session stockée côté serveur pendant 30 minutes par défaut

### ✅ **4. Scalabilité**
- Fonctionne pour des milliers de visiteurs simultanés
- Chaque SessionId est unique

### ✅ **5. Extensible**
- Facile d'ajouter un système de compte utilisateur (UserId)
- Fusion de paniers possible (session + compte)

---

## 🔮 Évolutions Futures

### **1. Système de Compte Utilisateur**
```csharp
// Lors du login
var userId = "user@email.com";
HttpContext.Session.SetString("UserId", userId);

// Fusion des paniers
var paniersSession = await _context.Paniers
    .Where(p => p.SessionId == sessionId)
    .ToListAsync();

foreach (var p in paniersSession)
{
    p.UserId = userId;      // Lier au compte
    p.SessionId = null;     // Retirer SessionId
}
await _context.SaveChangesAsync();
```

### **2. Vérification de Stock**
```csharp
if (produit.Stock < panierExistant.Quantite + 1)
{
    TempData["Error"] = "Stock insuffisant !";
    return RedirectToPage();
}
```

### **3. Nettoyage Automatique**
```csharp
// Tâche planifiée quotidienne
var paniersExpires = await _context.Paniers
    .Where(p => p.DateExpiration < DateTime.Now)
    .ToListAsync();
_context.Paniers.RemoveRange(paniersExpires);
```

---

## 📚 Résumé des Concepts

| Concept        | Rôle                                    | Exemple              |
|----------------|-----------------------------------------|----------------------|
| **SessionId**  | Identifiant unique du visiteur          | "abc-123-def"        |
| **UserId**     | ID utilisateur connecté (futur)         | "user@email.com"     |
| **Quantite**   | Nombre d'exemplaires du produit         | 1, 2, 3...           |
| **PrixUnitaire** | Prix au moment de l'ajout             | 99.99 €              |
| **DateExpiration** | Nettoyage automatique               | 90 jours             |

---

## 🎯 Points Clés à Retenir

1. **Un SessionId = Un Visiteur = Un Panier**
2. **PrixUnitaire évite les changements de prix**
3. **Quantite évite les doublons (1 produit = 1 ligne)**
4. **Filtrer TOUJOURS par SessionId pour la sécurité**
5. **90 jours d'expiration = comme Amazon**

---

Votre système de panier fonctionne maintenant exactement comme Amazon ! 🚀
