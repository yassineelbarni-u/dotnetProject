# 📦 Implémentation du Cache avec IMemoryCache

## ✅ Ce qui a été fait

### 1. Enregistrement du service `IMemoryCache` dans `Program.cs`
```csharp
// Mémoire cache pour optimiser les lectures (produits, catégories)
builder.Services.AddMemoryCache();
```

### 2. Injection et utilisation dans `Pages/Produit/Index.cshtml.cs`
- **Cache des catégories** : clé `Produits_Categories` (expiration 10 minutes)
- **Cache des produits** : clé `Produits_Categorie_{categorie}` ou `Produits_Categorie_TOUTES` (expiration 5 minutes)
- La première requête charge depuis la base de données et met en cache
- Les requêtes suivantes lisent directement depuis le cache

### 3. Invalidation du cache après modifications
- **`Pages/Produit/Create.cshtml.cs`** : après ajout d'un produit, suppression des clés :
  - `Produits_Toutes`
  - `Produits_Categories`
  - `Produits_Categorie_{categorie}` (si applicable)
  
- **`Pages/Produit/Edit.cshtml.cs`** : après modification d'un produit, suppression des clés :
  - `Produits_Toutes`
  - `Produits_Categories`
  - `Produits_Categorie_{ancienneCategorie}`
  - `Produits_Categorie_{nouvelleCategorie}`

---

## 🧪 Comment tester le cache

### Test 1 : Vérifier la mise en cache initiale
1. **Démarrer l'application**
   ```bash
   dotnet run
   ```

2. **Ouvrir la page des produits** : `/Produit/Index`
   - La première requête charge les données depuis SQL Server
   - Les données sont mises en cache

3. **Recharger la page plusieurs fois (F5)**
   - Les requêtes suivantes lisent depuis le cache (pas de requête SQL)
   - **Performance** : temps de réponse plus rapide

4. **Vérifier dans les logs** (si vous activez le logging SQL) :
   - La première requête affiche des logs SQL
   - Les rechargements suivants n'affichent pas de logs SQL

### Test 2 : Vérifier l'expiration du cache
1. **Accéder à la page produits** : `/Produit/Index`
2. **Attendre 6 minutes** (expiration cache produits : 5 min)
3. **Recharger la page**
   - Le cache expire → nouvelle requête SQL
   - Les données sont remises en cache

### Test 3 : Vérifier l'invalidation du cache (après création)
1. **Se connecter en tant qu'admin** : `/Admin/Login`
2. **Créer un nouveau produit** : `/Produit/Create`
3. **Retourner à la page produits** : `/Produit/Index`
   - Le cache a été invalidé automatiquement
   - Les nouvelles données (incluant le nouveau produit) sont chargées depuis SQL
   - Le nouveau produit apparaît immédiatement

### Test 4 : Vérifier l'invalidation du cache (après modification)
1. **Se connecter en tant qu'admin** : `/Admin/Login`
2. **Modifier un produit existant** : `/Produit/Edit?id={id}`
   - Changer le prix, le nom, ou la catégorie
3. **Retourner à la page produits** : `/Produit/Index`
   - Le cache a été invalidé
   - Les modifications apparaissent immédiatement

### Test 5 : Tester le cache par catégorie
1. **Aller sur** : `/Produit/Index?Categorie=Electronique`
   - Cache créé pour clé `Produits_Categorie_Electronique`
2. **Aller sur** : `/Produit/Index?Categorie=Vetements`
   - Cache créé pour clé `Produits_Categorie_Vetements` (indépendant)
3. **Recharger chaque catégorie**
   - Chaque catégorie a son propre cache

---

## 🔍 Comment observer le cache en action (avec logs)

### Option 1 : Activer le logging SQL dans `appsettings.Development.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

- Avec ce paramètre, vous verrez les requêtes SQL dans la console
- Si vous rechargez la page et ne voyez pas de requête SQL → **le cache fonctionne** ✅

### Option 2 : Ajouter des logs manuels dans `Index.cshtml.cs`
```csharp
public async Task<IActionResult> OnGetAsync()
{
    var adminId = HttpContext.Session.GetString("AdminId");
    if (!string.IsNullOrEmpty(adminId))
    {
        return RedirectToPage("/Admin/Dashboard");
    }

    // Cacher les catégories (10 minutes)
    var categoriesKey = "Produits_Categories";
    if (!_cache.TryGetValue(categoriesKey, out List<string> categories))
    {
        Console.WriteLine("🔴 CACHE MISS - Chargement des catégories depuis la base");
        categories = await _context.Produits
            .Where(p => !string.IsNullOrEmpty(p.Categorie))
            .Select(p => p.Categorie!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        _cache.Set(categoriesKey, categories, TimeSpan.FromMinutes(10));
    }
    else
    {
        Console.WriteLine("🟢 CACHE HIT - Catégories lues depuis le cache");
    }
    Categories = categories;

    // Même chose pour les produits...
}
```

---

## 📊 Avantages du cache implémenté

✅ **Performance** : réduction du temps de réponse (pas de requête SQL à chaque rechargement)  
✅ **Charge DB réduite** : moins de pression sur SQL Server  
✅ **Invalidation automatique** : modifications des produits invalident le cache  
✅ **Cache par catégorie** : chaque filtre a son propre cache indépendant  
✅ **Expiration temporelle** : les données sont rafraîchies automatiquement après 5-10 minutes  

---

## 🚀 Améliorations possibles (optionnel)

### 1. Cache distribué avec Redis (pour multi-serveurs)
```csharp
// Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

### 2. Cache de détail produit (page individuelle)
```csharp
var produitKey = $"Produit_Detail_{id}";
if (!_cache.TryGetValue(produitKey, out Produit produit))
{
    produit = await _context.Produits.FindAsync(id);
    _cache.Set(produitKey, produit, TimeSpan.FromMinutes(15));
}
```

### 3. Stratégie d'invalidation plus fine
- Invalider uniquement les clés affectées (déjà fait ✅)
- Utiliser des tags de cache (avec bibliothèque tierce)

---

## 📝 Résumé pour le professeur

**Implémentation réalisée** :
- ✅ Service `IMemoryCache` enregistré dans `Program.cs`
- ✅ Cache appliqué sur la liste des produits et catégories (page `/Produit/Index`)
- ✅ Expiration automatique (5-10 minutes)
- ✅ Invalidation du cache après création/modification de produits
- ✅ Cache par catégorie (clés différentes par filtre)

**Pourquoi pas de cache pour le panier** :
- Le panier est lié à la session utilisateur (données personnelles)
- Il doit rester en base/session pour garantir la cohérence et la sécurité
- Le cache est pour des données partagées/réutilisables (produits, catégories)

**Tests recommandés** :
1. Vérifier la performance (rechargement rapide après première requête)
2. Vérifier l'invalidation (modifier un produit → changement visible immédiatement)
3. Observer les logs SQL (activer logging EF Core pour voir les requêtes)

---

**Date d'implémentation** : 28 décembre 2025  
**Technologie** : ASP.NET Core 10 + IMemoryCache + Entity Framework Core
