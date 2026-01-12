# 🚀 Système RAG + LLM avec Cache Redis

## 📋 Vue d'ensemble

Projet ASP.NET Core avec système de recommandations intelligent utilisant :
- **LLM** : Ollama (gemma:2b) pour génération de réponses
- **RAG** : Vector Database (Qdrant) pour recherche sémantique
- **Cache** : Redis pour optimisation des performances

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────┐
│           Pages Razor (Interface)               │
└─────────────┬───────────────────────────────────┘
              │
┌─────────────▼───────────────────────────────────┐
│     OllamaRecommendationService (LLM)           │
│     • Orchestration du flux RAG                 │
│     • Génération réponse via Ollama             │
└────┬─────────────────────────┬──────────────────┘
     │                         │
     │ utilise                 │ cache avec
     │                         │
┌────▼────────────────┐   ┌────▼─────────────────┐
│  VectorRAGService   │   │  Redis Cache         │
│  • Recherche        │   │  • Cache embeddings  │
│    vectorielle      │   │  • Cache réponses    │
│  • Indexation       │   │  • TTL: 30min        │
└────┬────────────┬───┘   └──────────────────────┘
     │            │
     │            │
┌────▼────────┐ ┌▼──────────────────────┐
│   Qdrant    │ │  SemanticKernel       │
│  Vector DB  │ │  • Embeddings (384d)  │
│  Port 6333  │ │  • Ollama: all-minilm │
└─────────────┘ └───────────────────────┘
```

---

## 🔧 Services principaux

### **1. OllamaRecommendationService (LLM)**
```csharp
// Orchestration complète du processus RAG
public async Task<string> GetRecommendationsAsync(string userQuery)
{
    // 1. RETRIEVAL : Récupérer produits pertinents
    var relevantProducts = _ragService.RetrieveRelevantProducts(userQuery, allProducts);
    
    // 2. AUGMENTATION : Enrichir le prompt
    var enrichedPrompt = BuildPrompt(userQuery, relevantProducts);
    
    // 3. GENERATION : Appeler Ollama
    var response = await CallOllamaAsync(enrichedPrompt);
    
    return response;
}
```

### **2. VectorRAGService (RAG)**
```csharp
// Recherche vectorielle avec Qdrant
public List<Produit> RetrieveRelevantProducts(string query, List<Produit> allProducts)
{
    // 1. Convertir la question en vecteur (embedding)
    var queryEmbedding = _embeddingService.GenerateEmbeddingAsync(query);
    
    // 2. Indexer les produits dans Qdrant (si nécessaire)
    IndexProductsIfNeeded(allProducts);
    
    // 3. Rechercher les produits similaires dans Qdrant
    var similarIds = _qdrantService.SearchAsync(queryEmbedding, topK: 10);
    
    return allProducts.Where(p => similarIds.Contains(p.Id)).ToList();
}
```

### **3. Redis Cache**
```csharp
// Configuration dans Program.cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "ProjetTestDotNet_";
});
```

---

## 📦 Installation

### **1. Packages NuGet**
```bash
dotnet add package Microsoft.SemanticKernel --version 1.28.0
dotnet add package Microsoft.SemanticKernel.Connectors.Ollama --version 1.29.0-alpha
```

### **2. Lancer Qdrant (Vector Database)**
```bash
docker-compose up -d
```

### **3. Lancer Redis (si pas déjà actif)**
```bash
docker run -d -p 6379:6379 --name redis redis:alpine
```

### **4. Installer modèle Ollama**
```bash
ollama pull gemma:2b
ollama pull all-minilm
```

---

## 🚀 Lancer l'application

```bash
dotnet build
dotnet run
```

Accéder : `http://localhost:5000`

---

## 🧪 Test

Page recommandations : `/Recommendations`

Exemples de questions :
- "Produits moins de 50€"
- "Produit pour développeur débutant"
- "Formation en Python"

---

## ⚡ Performance avec Redis

| Sans cache | Avec cache Redis |
|-----------|------------------|
| ~3000ms   | ~50ms           |
| ❌ Requête LLM à chaque fois | ✅ Réponse instantanée |

---

## 🔑 Points clés

### **Flux RAG complet :**
1. **RETRIEVAL** : VectorRAGService récupère produits pertinents via Qdrant
2. **AUGMENTATION** : Enrichissement du contexte avec métadonnées produits
3. **GENERATION** : Ollama génère réponse personnalisée en français
4. **CACHE** : Redis stocke réponse (TTL: 30min)

### **Vector Database (Qdrant) :**
- Stockage de vecteurs (embeddings 384 dimensions)
- Recherche par similarité cosinus
- Dashboard : `http://localhost:6333/dashboard`

### **Cache Redis :**
- Réduction temps réponse : 3s → 50ms
- TTL configurable (défaut: 30 minutes)
- Cache invalidation automatique

---

## 📂 Structure services

```
Services/
├── IRAGService.cs                    # Interface RAG
├── VectorRAGService.cs               # RAG avec Vector DB
├── IEmbeddingService.cs              # Interface embeddings
├── SemanticKernelEmbeddingService.cs # Génération embeddings
├── IQdrantService.cs                 # Interface Qdrant
├── QdrantService.cs                  # API REST Qdrant
└── OllamaRecommendationService.cs    # Service LLM principal
```

---

## 🐳 Docker

```yaml
# docker-compose.yml
services:
  qdrant:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333"
```

---

## 📊 Configuration

**appsettings.json :**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProjetTest;..."
  }
}
```

**Program.cs :**
```csharp
// Services RAG + LLM
builder.Services.AddScoped<IEmbeddingService, SemanticKernelEmbeddingService>();
builder.Services.AddScoped<IQdrantService, QdrantService>();
builder.Services.AddScoped<IRAGService, VectorRAGService>();
builder.Services.AddScoped<IRecommendationService, OllamaRecommendationService>();

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = "localhost:6379";
});
```

---

**🎓 Projet prêt pour démonstration professionnelle !**
