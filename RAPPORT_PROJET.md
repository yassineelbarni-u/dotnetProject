# 📋 Rapport de Projet - Système de Gestion E-Commerce
## Application Web ASP.NET Core avec Cache Redis et Recommandations IA

---

## 📑 Table des Matières

1. [Introduction](#introduction)
2. [Architecture Globale](#architecture-globale)
3. [Partie 1 : Gestion du Panier avec DTO](#partie-1-gestion-du-panier-avec-dto)
4. [Partie 2 : Système de Cache avec Redis](#partie-2-système-de-cache-avec-redis)
5. [Partie 3 : Système de Recommandation IA (LLM)](#partie-3-système-de-recommandation-ia-llm)
6. [Technologies Utilisées](#technologies-utilisées)
7. [Diagrammes et Schémas](#diagrammes-et-schémas)
8. [Conclusion](#conclusion)

---

## 1. Introduction

### 1.1 Contexte du Projet

Ce projet est une **application web e-commerce** développée avec **ASP.NET Core 10** (Razor Pages). L'application permet aux utilisateurs de :
- Parcourir un catalogue de produits
- Ajouter des produits au panier
- Gérer leur panier (modifier quantités, supprimer articles)
- Obtenir des recommandations de produits via Intelligence Artificielle

### 1.2 Objectifs du Projet

L'objectif principal était de créer une application moderne intégrant :
1. **Pattern DTO (Data Transfer Object)** pour optimiser le transfert de données
2. **Cache distribué Redis** pour améliorer les performances
3. **Intelligence Artificielle (LLM)** pour des recommandations personnalisées

### 1.3 Fonctionnalités Principales

✅ **Gestion de produits** : CRUD complet (Create, Read, Update, Delete)  
✅ **Système de panier** : Gestion par session utilisateur  
✅ **Cache Redis** : Stockage distribué pour les paniers  
✅ **Recommandations IA** : Utilisation de modèle LLM (Ollama Gemma 2B)  
✅ **Interface admin** : Gestion des produits et catégories

---

## 2. Architecture Globale

### 2.1 Stack Technique

```
┌─────────────────────────────────────────────────────────┐
│                   Frontend (Razor Pages)                 │
│                  HTML + CSS + JavaScript                 │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              ASP.NET Core 10 (Backend)                   │
│  ┌────────────┐  ┌────────────┐  ┌────────────────────┐ │
│  │   Pages    │  │    DTOs    │  │     Services       │ │
│  │ (Razor)    │  │            │  │ - Recommendation   │ │
│  └────────────┘  └────────────┘  └────────────────────┘ │
└────────────┬───────────────┬──────────────┬─────────────┘
             │               │              │
     ┌───────▼──────┐  ┌────▼─────┐  ┌────▼──────────┐
     │ SQL Server   │  │  Redis   │  │  Ollama LLM   │
     │ (Database)   │  │  (Cache) │  │  (Gemma 2B)   │
     └──────────────┘  └──────────┘  └───────────────┘
```

### 2.2 Pattern Architectural : MVC / Razor Pages

L'application utilise le pattern **Razor Pages** (variante simplifiée de MVC) :
- **Pages** : Fichiers `.cshtml` (Vue) + `.cshtml.cs` (Code-behind)
- **Models** : Entités de base de données (`Produit`, `Panier`, etc.)
- **Services** : Logique métier réutilisable (`OllamaRecommendationService`)
- **DTOs** : Objets de transfert de données optimisés

---

## 3. Partie 1 : Gestion du Panier avec DTO

### 3.1 Qu'est-ce qu'un DTO (Data Transfer Object) ?

#### 3.1.1 Définition

Un **DTO** est un objet simple utilisé pour **transférer des données** entre différentes couches de l'application. Il contient **uniquement les données nécessaires** pour une opération spécifique, sans logique métier complexe.

#### 3.1.2 Pourquoi utiliser un DTO ?

**Problème sans DTO :**
```csharp
// Sans DTO : on charge toute l'entité Panier avec ses relations
public class Panier
{
    public int Id { get; set; }
    public string SessionId { get; set; }
    public int ProduitId { get; set; }
    public Produit Produit { get; set; }  // ❌ Relation lourde
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public DateTime DateAjout { get; set; }
    public DateTime DateExpiration { get; set; }
}
// Problème : sérialisation complexe, données inutiles en cache
```

**Solution avec DTO :**
```csharp
// Avec DTO : uniquement les données affichées à l'utilisateur
public class PanierItemDTO
{
    public int Id { get; set; }
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public int ProduitId { get; set; }
    public string? ProduitNom { get; set; }
    public string? ProduitImage { get; set; }
    public string? ProduitDescription { get; set; }
    public int ProduitStock { get; set; }
    public decimal SousTotal => PrixUnitaire * Quantite;  // Propriété calculée
}
```

#### 3.1.3 Avantages du DTO

✅ **Performance** : Moins de données transférées (pas de relations inutiles)  
✅ **Sécurité** : On expose uniquement les champs nécessaires  
✅ **Flexibilité** : La structure du DTO peut différer du modèle DB  
✅ **Cache optimisé** : Sérialisation JSON simplifiée pour Redis  
✅ **Découplage** : Le frontend ne dépend pas de la structure DB  

### 3.2 Implémentation du DTO dans le Projet

#### 3.2.1 Structure du DTO

```csharp
namespace ProjetTestDotNet.DTOs
{
    /// <summary>
    /// DTO pour un article dans le panier.
    /// Contient UNIQUEMENT les données nécessaires pour l'affichage.
    /// </summary>
    public class PanierItemDTO
    {
        public int Id { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        
        // Informations produit dénormalisées
        public int ProduitId { get; set; }
        public string? ProduitNom { get; set; }
        public string? ProduitImage { get; set; }
        public string? ProduitDescription { get; set; }
        public int ProduitStock { get; set; }
        
        // Propriété calculée (pas stockée en DB)
        public decimal SousTotal => PrixUnitaire * Quantite;
    }
}
```

**Points clés :**
- `SousTotal` est calculé dynamiquement (pas stocké)
- Les informations produit sont **dénormalisées** (copiées) pour éviter les jointures
- Pas de relations complexes (navigation properties)

#### 3.2.2 Utilisation du DTO dans le Panier

**Lecture du panier depuis Redis (Méthode `OnGetAsync`) :**

```csharp
public async Task OnGetAsync()
{
    // 1. Récupérer l'identifiant de session
    var sessionId = Request.Cookies["SessionId"];
    
    // 2. Créer un SessionId si inexistant
    if (string.IsNullOrEmpty(sessionId))
    {
        sessionId = Guid.NewGuid().ToString();
        Response.Cookies.Append("SessionId", sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }
    
    if (!string.IsNullOrEmpty(sessionId))
    {
        // 3. Lire le panier depuis Redis
        var panierKey = $"Panier_{sessionId}";
        var cachedData = await _cache.GetStringAsync(panierKey);
        
        if (cachedData != null)
        {
            // 4. Désérialiser JSON → DTO
            ArticlesPanier = JsonSerializer.Deserialize<List<PanierItemDTO>>(cachedData) ?? new();
            Console.WriteLine($" REDIS CACHE HIT - {ArticlesPanier.Count} articles");
        }
        else
        {
            ArticlesPanier = new List<PanierItemDTO>();
            Console.WriteLine($"❌ Panier vide");
        }
        
        // 5. Calculer le total
        Total = ArticlesPanier.Sum(a => a.SousTotal);
    }
}
```

**Workflow :**
1. **Récupération SessionId** → Cookie HTTP identifie l'utilisateur
2. **Construction clé Redis** → `Panier_{sessionId}`
3. **Désérialisation JSON** → Convertir le JSON Redis en objets C# (DTO)
4. **Calcul total** → Somme des sous-totaux

### 3.3 Gestion du SessionId

#### 3.3.1 Qu'est-ce que le SessionId ?

Le **SessionId** est un identifiant unique généré pour chaque visiteur du site. Il permet de :
- Identifier l'utilisateur de manière anonyme (sans compte)
- Associer un panier à un visiteur spécifique
- Persister le panier entre les visites (30 jours)

#### 3.3.2 Génération et Stockage

```csharp
// Génération d'un UUID (Universally Unique Identifier)
var sessionId = Guid.NewGuid().ToString();
// Exemple : "a3f5c2d8-1234-5678-90ab-cdef12345678"

// Stockage dans un cookie HTTP
Response.Cookies.Append("SessionId", sessionId, new CookieOptions
{
    HttpOnly = true,       // ✅ Pas accessible via JavaScript (sécurité XSS)
    Secure = true,         // ✅ Transmis uniquement en HTTPS
    SameSite = SameSiteMode.Lax,  // ✅ Protection CSRF
    Expires = DateTimeOffset.UtcNow.AddDays(30)  // ⏳ Expire après 30 jours
});
```

**Sécurité :**
- `HttpOnly = true` → Empêche les attaques XSS (Cross-Site Scripting)
- `Secure = true` → Force HTTPS (données chiffrées)
- `SameSite = Lax` → Limite les requêtes cross-site (protection CSRF)

### 3.4 Opérations CRUD sur le Panier

#### 3.4.1 Ajouter un Produit

```csharp
// 1. Récupérer le panier depuis Redis
var cachedData = await _cache.GetStringAsync($"Panier_{sessionId}");
var articles = JsonSerializer.Deserialize<List<PanierItemDTO>>(cachedData) ?? new();

// 2. Vérifier si le produit existe déjà
var article = articles.FirstOrDefault(a => a.ProduitId == produitId);
if (article != null)
{
    article.Quantite++;
}
else
{
    // Ajouter nouveau produit
    articles.Add(new PanierItemDTO
    {
        ProduitId = produit.Id,
        ProduitNom = produit.Nom,
        Quantite = 1,
        PrixUnitaire = produit.Prix
    });
}

// 3. Sauvegarder dans Redis (JSON sérialisé)
var serialized = JsonSerializer.Serialize(articles);
await _cache.SetStringAsync($"Panier_{sessionId}", serialized, new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
});
```

#### 3.4.2 Supprimer un Produit

```csharp
public async Task<IActionResult> OnPostSupprimerAsync(int id)
{
    var sessionId = Request.Cookies["SessionId"];
    var panierKey = $"Panier_{sessionId}";
    var cachedData = await _cache.GetStringAsync(panierKey);
    
    if (cachedData != null)
    {
        var articles = JsonSerializer.Deserialize<List<PanierItemDTO>>(cachedData) ?? new();
        
        // Supprimer l'article
        var article = articles.FirstOrDefault(a => a.Id == id);
        if (article != null)
        {
            articles.Remove(article);
            
            // Mettre à jour Redis
            var serialized = JsonSerializer.Serialize(articles);
            await _cache.SetStringAsync(panierKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)
            });
            
            // Invalider le cache du compteur
            await _cache.RemoveAsync($"PanierCount_{sessionId}");
        }
    }
    return RedirectToPage();
}
```

#### 3.4.3 Modifier la Quantité

```csharp
public async Task<IActionResult> OnPostModifierQuantiteAsync(int id, int quantite)
{
    if (quantite <= 0) return RedirectToPage();
    
    var sessionId = Request.Cookies["SessionId"];
    var panierKey = $"Panier_{sessionId}";
    var cachedData = await _cache.GetStringAsync(panierKey);
    
    if (cachedData != null)
    {
        var articles = JsonSerializer.Deserialize<List<PanierItemDTO>>(cachedData) ?? new();
        
        var article = articles.FirstOrDefault(a => a.Id == id);
        if (article != null)
        {
            article.Quantite = quantite;
            
            // Sauvegarder
            var serialized = JsonSerializer.Serialize(articles);
            await _cache.SetStringAsync(panierKey, serialized);
        }
    }
    return RedirectToPage();
}
```

---

## 4. Partie 2 : Système de Cache avec Redis

### 4.1 Introduction au Cache Distribué

#### 4.1.1 Qu'est-ce que Redis ?

**Redis** (Remote Dictionary Server) est une base de données **clé-valeur en mémoire** ultra-rapide. Elle est utilisée comme :
- **Cache distribué** (partagé entre plusieurs serveurs)
- **File d'attente** (message queuing)
- **Session store** (stockage de sessions)

#### 4.1.2 Pourquoi Redis plutôt qu'un Cache en Mémoire ?

| Critère | IMemoryCache (in-process) | Redis (distributed) |
|---------|---------------------------|---------------------|
| **Vitesse** | ⚡ Très rapide (RAM locale) | 🚀 Rapide (réseau local) |
| **Partage multi-serveurs** | ❌ Non (chaque serveur a son propre cache) | ✅ Oui (cache centralisé) |
| **Persistance** | ❌ Perdu au redémarrage | ✅ Optionnel (sauvegarde disque) |
| **Scalabilité** | ❌ Limité à un serveur | ✅ Cluster Redis possible |
| **Usage** | Petites apps, dev local | Production, load balancing |

**Choix pour ce projet :** Redis → Permet de tester un cache distribué professionnel.

### 4.2 Configuration de Redis

#### 4.2.1 Installation de Redis (Windows)

```bash
# Option 1 : Via Chocolatey
choco install redis-64

# Option 2 : Télécharger depuis GitHub
# https://github.com/microsoftarchive/redis/releases

# Démarrer Redis
redis-server
```

#### 4.2.2 Configuration dans Program.cs

```csharp
// Enregistrer le service Redis dans le conteneur d'injection de dépendances
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";  // Adresse du serveur Redis
    options.InstanceName = "ProjetTestDotNet_"; // Préfixe pour les clés
});
```

**Explication :**
- `localhost:6379` → Connexion locale à Redis (port par défaut)
- `InstanceName` → Préfixe ajouté à toutes les clés (`ProjetTestDotNet_Panier_xyz`)
- `AddStackExchangeRedisCache` → Implémente `IDistributedCache`

### 4.3 Utilisation de Redis dans le Panier

#### 4.3.1 Injection de Dépendance

```csharp
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;  // Interface Redis
    
    public IndexModel(AppDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }
}
```

**Pattern utilisé :** Injection de dépendance (Dependency Injection)
- ASP.NET Core injecte automatiquement l'implémentation Redis
- Le code dépend de l'interface `IDistributedCache` (pas de couplage fort)

#### 4.3.2 Stockage dans Redis

**Structure des clés Redis :**
```
ProjetTestDotNet_Panier_{sessionId}       → JSON du panier
ProjetTestDotNet_PanierCount_{sessionId}  → Nombre d'articles (cache)
```

**Exemple de données stockées (JSON) :**
```json
[
  {
    "Id": 1,
    "Quantite": 2,
    "PrixUnitaire": 99.99,
    "ProduitId": 5,
    "ProduitNom": "Laptop Dell XPS",
    "ProduitImage": "/images/laptop.jpg",
    "ProduitStock": 10,
    "SousTotal": 199.98
  },
  {
    "Id": 2,
    "Quantite": 1,
    "PrixUnitaire": 29.99,
    "ProduitId": 8,
    "ProduitNom": "Souris Logitech",
    "ProduitStock": 50,
    "SousTotal": 29.99
  }
]
```

#### 4.3.3 Opérations Redis

**Écriture dans Redis :**
```csharp
var articles = new List<PanierItemDTO> { /* ... */ };
var serialized = JsonSerializer.Serialize(articles);

await _cache.SetStringAsync($"Panier_{sessionId}", serialized, new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)  // Expire après 7 jours
});
```

**Lecture depuis Redis :**
```csharp
var cachedData = await _cache.GetStringAsync($"Panier_{sessionId}");
if (cachedData != null)
{
    var articles = JsonSerializer.Deserialize<List<PanierItemDTO>>(cachedData);
    // Utiliser les données
}
```

**Suppression de clé Redis :**
```csharp
await _cache.RemoveAsync($"PanierCount_{sessionId}");
```

### 4.4 Stratégie de Cache

#### 4.4.1 Expiration des Données

```csharp
new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7)  // 7 jours
}
```

**Types d'expiration Redis :**
- `AbsoluteExpirationRelativeToNow` → Expire après X temps (fixe)
- `SlidingExpiration` → Renouvelle l'expiration à chaque accès

**Choix :** 7 jours fixes → Le panier persiste une semaine sans accès.

#### 4.4.2 Invalidation du Cache

**Quand invalider le cache ?**
- ✅ Après suppression d'un article
- ✅ Après modification de quantité
- ✅ Après ajout d'un produit (pour mettre à jour le compteur)

```csharp
// Invalider le compteur d'articles après modification
await _cache.RemoveAsync($"PanierCount_{sessionId}");
```

### 4.5 Avantages du Cache Redis

✅ **Performance** : Lecture ultra-rapide (en mémoire)  
✅ **Scalabilité** : Cache partagé entre plusieurs serveurs web  
✅ **Persistance** : Les paniers survivent aux redémarrages du serveur  
✅ **TTL automatique** : Nettoyage automatique après expiration  
✅ **Simplicité** : Sérialisation JSON native via `IDistributedCache`  

---

## 5. Partie 3 : Système de Recommandation IA (LLM)

### 5.1 Architecture du Système de Recommandation

#### 5.1.1 Choix Technologiques

**LLM utilisé :** Ollama avec le modèle **Gemma 2B**

**Pourquoi Ollama ?**
- ✅ **Local** : Exécution sur le serveur (pas de dépendance externe)
- ✅ **Gratuit** : Pas de coûts API (contrairement à OpenAI)
- ✅ **Rapide** : Modèle 2B léger (réponses en quelques secondes)
- ✅ **Privé** : Données produits ne quittent pas le serveur

**Alternatives considérées :**
- OpenAI GPT-4 → Coûteux, requiert API key
- Azure OpenAI → Configuration complexe
- Hugging Face → Intégration plus technique

#### 5.1.2 Workflow du Système

```
┌─────────────────────────────────────────────────────────┐
│  1. Utilisateur pose une question                       │
│     "Quel laptop recommandes-tu pour gaming ?"          │
└─────────────────┬───────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────┐
│  2. Backend récupère les produits depuis SQL Server     │
│     SELECT * FROM Produits                              │
└─────────────────┬───────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────┐
│  3. Construction du prompt (contexte + question)        │
│     "Voici les produits : ..."                          │
│     "Question : Quel laptop recommandes-tu ?"           │
└─────────────────┬───────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────┐
│  4. Appel API Ollama (HTTP POST)                        │
│     POST http://localhost:11434/api/generate            │
└─────────────────┬───────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────┐
│  5. LLM analyse et génère une recommandation            │
│     "Je recommande le Dell XPS Gaming à 1299€..."      │
└─────────────────┬───────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────────┐
│  6. Retour de la réponse à l'utilisateur (frontend)     │
└─────────────────────────────────────────────────────────┘
```

### 5.2 Implémentation du Service

#### 5.2.1 Interface du Service

```csharp
namespace ProjetTestDotNet.Services
{
    public interface IRecommendationService
    {
        Task<string> GetRecommendationsAsync(string userMessage);
    }
}
```

**Design Pattern :** Interface-based programming
- Permet de changer l'implémentation (OpenAI, Ollama, etc.) sans modifier le code appelant
- Facilite les tests unitaires (mock du service)

#### 5.2.2 Classe OllamaRecommendationService

```csharp
public class OllamaRecommendationService : IRecommendationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppDbContext _context;
    private readonly string _ollamaUrl = "http://localhost:11434/api/generate";
    
    public OllamaRecommendationService(
        IHttpClientFactory httpClientFactory,
        AppDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
    }
    
    public async Task<string> GetRecommendationsAsync(string userMessage)
    {
        try
        {
            // 1. Récupérer les produits depuis la DB
            var produits = await _context.Produits.ToListAsync();
            
            if (!produits.Any())
            {
                return "❌ Aucun produit disponible.";
            }
            
            // 2. Construire le contexte produits
            var produitsContext = BuildProductContext(produits);
            
            // 3. Construire le prompt
            var prompt = $@"Tu es un assistant e-commerce. Voici les produits disponibles :

{produitsContext}

Question : {userMessage}

Réponds en français ou anglais depend de la question, sois concis (3-5 lignes max). 
Liste les produits pertinents avec leur prix.";
            
            // 4. Appeler Ollama
            var response = await CallOllamaAsync(prompt);
            return response;
        }
        catch (Exception ex)
        {
            return $"❌ Erreur : {ex.Message}";
        }
    }
}
```

#### 5.2.3 Construction du Contexte Produits

```csharp
private string BuildProductContext(List<Models.Produit> produits)
{
    var sb = new StringBuilder();
    
    // Limiter à 20 produits (éviter un prompt trop long)
    var produitsLimites = produits.Take(20).ToList();
    
    foreach (var p in produitsLimites)
    {
        sb.AppendLine($"- {p.Nom} | {p.Prix:F0}€ | {p.Categorie ?? "Autre"} | Stock: {p.Stock}");
    }
    
    return sb.ToString();
}
```

**Exemple de contexte généré :**
```
- Laptop Dell XPS | 1299€ | Electronique | Stock: 5
- Souris Logitech MX | 89€ | Accessoires | Stock: 25
- Clavier Mécanique | 149€ | Accessoires | Stock: 10
...
```

**Optimisation :**
- Limite à 20 produits → Évite les prompts trop longs (limite tokens)
- Format structuré → Le LLM comprend facilement

### 5.3 Communication avec Ollama

#### 5.3.1 Appel API HTTP

```csharp
private async Task<string> CallOllamaAsync(string prompt)
{
    // 1. Créer le client HTTP
    var client = _httpClientFactory.CreateClient();
    client.Timeout = TimeSpan.FromMinutes(3);  // Timeout 3 minutes
    
    // 2. Construire le corps de la requête JSON
    var requestBody = new
    {
        model = "gemma:2b",          // Modèle LLM utilisé
        prompt = prompt,             // Texte d'entrée
        stream = false,              // Réponse complète (pas de streaming)
        options = new
        {
            temperature = 0.7,       // Créativité (0 = déterministe, 1 = créatif)
            num_predict = 200,       // Nombre max de tokens générés
            num_ctx = 1024           // Taille du contexte (tokens)
        }
    };
    
    var jsonContent = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
    
    try
    {
        // 3. Envoyer la requête POST
        var response = await client.PostAsync(_ollamaUrl, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return $"❌ Erreur Ollama ({response.StatusCode}): {errorContent}";
        }
        
        // 4. Parser la réponse JSON
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonDocument.Parse(responseContent);
        
        var recommendation = jsonResponse.RootElement
            .GetProperty("response")
            .GetString();
        
        return recommendation ?? "Aucune recommandation générée.";
    }
    catch (HttpRequestException ex)
    {
        return $"❌ Impossible de se connecter à Ollama. Vérifiez : `ollama serve`";
    }
}
```

#### 5.3.2 Paramètres de Génération

| Paramètre | Valeur | Description |
|-----------|--------|-------------|
| `model` | `gemma:2b` | Modèle LLM (2 milliards de paramètres) |
| `temperature` | `0.7` | Contrôle la créativité (0 = déterministe, 1 = aléatoire) |
| `num_predict` | `200` | Nombre max de tokens générés (~ 150 mots) |
| `num_ctx` | `1024` | Taille du contexte (prompt + réponse) |
| `stream` | `false` | Réponse complète d'un coup (pas de streaming) |

**Choix de `temperature = 0.7` :**
- Trop bas (0.2) → Réponses répétitives et robotiques
- Trop haut (0.9) → Réponses créatives mais parfois incorrectes
- **0.7 = Équilibre** → Réponses naturelles et pertinentes

### 5.4 Enregistrement du Service

#### 5.4.1 Configuration dans Program.cs

```csharp
// Service HTTP Client (pour appeler Ollama)
builder.Services.AddHttpClient();

// Service de recommandation IA
builder.Services.AddScoped<IRecommendationService, OllamaRecommendationService>();
```

**Durée de vie du service :**
- `AddScoped` → Une instance par requête HTTP
- Alternative : `AddSingleton` (une instance globale) ou `AddTransient` (nouvelle instance à chaque injection)

### 5.5 Utilisation dans une Page Razor

#### 5.5.1 Page de Chat (Chat.cshtml.cs)

```csharp
public class ChatModel : PageModel
{
    private readonly IRecommendationService _recommendationService;
    
    public ChatModel(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }
    
    [BindProperty]
    public string UserMessage { get; set; } = "";
    
    public string AiResponse { get; set; } = "";
    
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(UserMessage))
        {
            AiResponse = "⚠️ Veuillez poser une question.";
            return Page();
        }
        
        // Appeler le service de recommandation
        AiResponse = await _recommendationService.GetRecommendationsAsync(UserMessage);
        
        return Page();
    }
}
```

#### 5.5.2 Interface Utilisateur (Chat.cshtml)

```html
<form method="post">
    <div class="mb-3">
        <label for="userMessage" class="form-label">Votre question :</label>
        <input type="text" 
               class="form-control" 
               id="userMessage" 
               asp-for="UserMessage" 
               placeholder="Ex: Quel laptop recommandes-tu pour gaming ?" />
    </div>
    
    <button type="submit" class="btn btn-primary">Envoyer</button>
</form>

@if (!string.IsNullOrEmpty(Model.AiResponse))
{
    <div class="alert alert-info mt-3">
        <h5>Recommandation IA :</h5>
        <p>@Model.AiResponse</p>
    </div>
}
```

### 5.6 Exemple de Conversation

**Utilisateur :**
> "Quel laptop recommandes-tu pour le gaming ?"

**LLM (Gemma 2B) :**
> "Pour le gaming, je vous recommande le **Dell XPS Gaming** à **1299€**. Il offre un excellent rapport qualité-prix avec un processeur puissant et une carte graphique dédiée. Sinon, le **HP Omen 15** à **999€** est une alternative plus abordable avec de bonnes performances."

**Utilisateur :**
> "Quels sont les produits en stock ?"

**LLM :**
> "Voici les produits actuellement disponibles :
> - Laptop Dell XPS (5 en stock)
> - Souris Logitech MX (25 en stock)
> - Clavier Mécanique (10 en stock)"

### 5.7 Avantages du Système de Recommandation

✅ **Contextuel** : Le LLM connaît tous les produits disponibles  
✅ **Naturel** : L'utilisateur pose des questions en langage naturel  
✅ **Intelligent** : Le LLM comprend les intentions (gaming → produits puissants)  
✅ **Local** : Pas de dépendance à des APIs externes payantes  
✅ **Flexible** : Répond en français ou anglais selon la question  

---

## 6. Technologies Utilisées

### 6.1 Backend

| Technologie | Version | Usage |
|-------------|---------|-------|
| **ASP.NET Core** | 10.0 | Framework web (Razor Pages) |
| **Entity Framework Core** | 10.0 | ORM pour SQL Server |
| **SQL Server** | 2022 | Base de données relationnelle |
| **Redis** | 7.x | Cache distribué (Stack Exchange) |
| **System.Text.Json** | 10.0 | Sérialisation JSON (DTOs) |

### 6.2 Intelligence Artificielle

| Technologie | Version | Usage |
|-------------|---------|-------|
| **Ollama** | Latest | Runtime LLM local |
| **Gemma 2B** | 2.0 | Modèle de langage (Google) |
| **HttpClient** | .NET 10 | Communication HTTP avec Ollama |

### 6.3 Frontend

| Technologie | Usage |
|-------------|-------|
| **Razor Pages** | Moteur de templates (HTML + C#) |
| **Bootstrap 5** | Framework CSS responsive |
| **JavaScript** | Interactions dynamiques |

### 6.4 Patterns et Principes

✅ **DTO (Data Transfer Object)** : Optimisation du transfert de données  
✅ **Dependency Injection** : Couplage faible entre composants  
✅ **Interface-based Programming** : Flexibilité et testabilité  
✅ **Repository Pattern** : Abstraction de l'accès aux données (Entity Framework)  
✅ **MVC/Razor Pages** : Séparation présentation/logique  

---

## 7. Diagrammes et Schémas

### 7.1 Diagramme de Flux - Ajout au Panier

```
┌─────────────────────────────────────────────────────────────┐
│  Utilisateur clique "Ajouter au panier"                     │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│  Backend vérifie SessionId dans le cookie                   │
│    ├─ Existe ? → Récupérer                                  │
│    └─ Inexistant ? → Créer (Guid.NewGuid())                │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│  Récupérer le panier depuis Redis                           │
│  Clé : "Panier_{sessionId}"                                 │
└────────────────────┬────────────────────────────────────────┘
                     │
              ┌──────▼──────┐
              │ Panier vide ? │
              └──────┬────┬──┘
                 OUI │    │ NON
        ┌───────────▼─   ▼───────────┐
        │ Créer nouvelle  │ Ajouter   │
        │ liste DTO       │ au panier │
        │                 │ existant  │
        └───────────┬─────┴───────────┘
                    │
┌───────────────────▼─────────────────────────────────────────┐
│  Produit déjà dans le panier ?                              │
│    ├─ OUI → Incrémenter Quantite++                         │
│    └─ NON → Ajouter nouveau PanierItemDTO                  │
└───────────────────┬─────────────────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────────────────┐
│  Sérialiser la liste DTO en JSON                            │
│  JsonSerializer.Serialize(articles)                         │
└───────────────────┬─────────────────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────────────────┐
│  Sauvegarder dans Redis                                     │
│  _cache.SetStringAsync("Panier_{sessionId}", json)         │
│  Expiration : 7 jours                                       │
└───────────────────┬─────────────────────────────────────────┘
                    │
┌───────────────────▼─────────────────────────────────────────┐
│  Retourner confirmation à l'utilisateur                     │
│  "Produit ajouté au panier ✅"                              │
└─────────────────────────────────────────────────────────────┘
```

### 7.2 Diagramme de Séquence - Recommandation IA

```
Utilisateur          Frontend           Backend          Redis           SQL Server        Ollama
    │                    │                  │               │                  │               │
    │  "Quel laptop ?"   │                  │               │                  │               │
    │───────────────────>│                  │               │                  │               │
    │                    │  POST /Chat      │               │                  │               │
    │                    │─────────────────>│               │                  │               │
    │                    │                  │  SELECT       │                  │               │
    │                    │                  │  Produits     │                  │               │
    │                    │                  │──────────────────────────────────>│               │
    │                    │                  │               │                  │               │
    │                    │                  │  Liste        │                  │               │
    │                    │                  │  produits     │                  │               │
    │                    │                  │<──────────────────────────────────│               │
    │                    │                  │               │                  │               │
    │                    │                  │  BuildProductContext()           │               │
    │                    │                  │  (Format prompt)                 │               │
    │                    │                  │               │                  │               │
    │                    │                  │  POST /api/generate              │               │
    │                    │                  │  (prompt + question)             │               │
    │                    │                  │──────────────────────────────────────────────────>│
    │                    │                  │               │                  │               │
    │                    │                  │               │                  │      LLM      │
    │                    │                  │               │                  │   Analyse     │
    │                    │                  │               │                  │      ...      │
    │                    │                  │               │                  │               │
    │                    │                  │  Recommandation                  │               │
    │                    │                  │  (JSON)                          │               │
    │                    │                  │<──────────────────────────────────────────────────│
    │                    │                  │               │                  │               │
    │                    │  Réponse HTML    │               │                  │               │
    │                    │  (avec recommand)│               │                  │               │
    │                    │<─────────────────│               │                  │               │
    │  Affichage         │                  │               │                  │               │
    │  recommandation    │                  │               │                  │               │
    │<───────────────────│                  │               │                  │               │
```

### 7.3 Architecture des Données

```
┌──────────────────────────────────────────────────────────────┐
│                     SQL SERVER (Base de données)             │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌───────────────┐      ┌───────────────┐                  │
│  │   Produits    │      │   Admins      │                  │
│  ├───────────────┤      ├───────────────┤                  │
│  │ Id (PK)       │      │ Id (PK)       │                  │
│  │ Nom           │      │ Username      │                  │
│  │ Prix          │      │ Password      │                  │
│  │ Description   │      └───────────────┘                  │
│  │ Image         │                                          │
│  │ Stock         │      ┌───────────────┐                  │
│  │ Categorie     │      │  Categories   │                  │
│  └───────────────┘      ├───────────────┤                  │
│                         │ Id (PK)       │                  │
│                         │ Nom           │                  │
│                         └───────────────┘                  │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│                     REDIS (Cache distribué)                  │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Clé : "Panier_{sessionId}"                                 │
│  Valeur : JSON (List<PanierItemDTO>)                        │
│  TTL : 7 jours                                              │
│                                                              │
│  Exemple :                                                   │
│  {                                                           │
│    "Panier_abc-123-def": [                                  │
│      { "ProduitId": 5, "Quantite": 2, "PrixUnitaire": 99.99 }│
│    ]                                                         │
│  }                                                           │
└──────────────────────────────────────────────────────────────┘
```

---

## 8. Conclusion

### 8.1 Résumé des Fonctionnalités

Ce projet démontre l'intégration de **trois composants avancés** :

1. **Pattern DTO** : Optimisation du transfert de données entre couches
   - Réduction de la taille des données (pas de relations inutiles)
   - Sérialisation JSON simplifiée pour Redis
   - Sécurité (exposition contrôlée des données)

2. **Cache distribué Redis** : Performance et scalabilité
   - Stockage ultra-rapide des paniers (en mémoire)
   - Persistance des sessions utilisateur (30 jours)
   - Prêt pour le load balancing (multi-serveurs)

3. **Intelligence Artificielle (LLM)** : Recommandations contextuelles
   - Modèle local Ollama (Gemma 2B) sans coûts API
   - Compréhension du langage naturel
   - Recommandations basées sur le catalogue réel

### 8.2 Compétences Démontrées

✅ **Architecture logicielle** : Pattern DTO, Dependency Injection, Interface-based  
✅ **Performance** : Cache distribué, sérialisation optimisée  
✅ **Sécurité** : Cookies HTTP-only, validation serveur  
✅ **Intelligence Artificielle** : Intégration LLM, construction de prompts  
✅ **DevOps** : Configuration Redis, gestion des services  
✅ **Base de données** : Entity Framework, migrations, requêtes optimisées  

### 8.3 Perspectives d'Amélioration

**Fonctionnalités futures :**
- 🔐 Système d'authentification utilisateur (Identity)
- 💳 Intégration paiement (Stripe, PayPal)
- 📧 Notifications email (confirmation de commande)
- 📊 Dashboard analytics (ventes, produits populaires)
- 🔍 Recherche avancée (Elasticsearch)
- 🌐 Internationalisation (multi-langues)

**Optimisations techniques :**
- ⚡ Cache des produits (IMemoryCache + Redis)
- 🔄 Stratégie de cache hiérarchique (L1/L2)
- 📈 Monitoring Redis (Redis Commander)
- 🧪 Tests unitaires (xUnit, Moq)
- 🔍 Recherche sémantique (embeddings + vector DB)

### 8.4 Conclusion Finale

Ce projet illustre l'intégration harmonieuse de technologies modernes (ASP.NET Core, Redis, IA) pour créer une application e-commerce performante et évolutive. L'utilisation du **pattern DTO** garantit une architecture propre, le **cache Redis** assure la scalabilité, et l'**intelligence artificielle** offre une expérience utilisateur innovante.

Le code est structuré, commenté, et suit les bonnes pratiques du développement logiciel professionnel.

---

## 📚 Références

- **ASP.NET Core Documentation** : https://learn.microsoft.com/aspnet/core
- **Redis Documentation** : https://redis.io/docs/
- **Ollama Documentation** : https://ollama.ai/docs
- **Entity Framework Core** : https://learn.microsoft.com/ef/core
- **Design Patterns** : Gang of Four (GoF)

---

**Auteur :** Votre Nom  
**Date :** Janvier 2026  
**Technologies :** ASP.NET Core 10, Redis, Ollama Gemma 2B  
**Licence :** Usage éducatif

---

*Fin du rapport*
