# Classement Bradley–Terry décentralisé

Chaque match se met à jour à partir des seuls classements actuels des deux joueurs et du résultat. Pas de serveur central, pas de registre global des matchs, pas d'identifiants de joueur. Le dépôt contient deux implémentations sémantiquement identiques — TypeScript et C# —, toutes deux fondées sur des fonctions pures.

**Langues :** [English](../README.md) · [简体中文](zh-CN.md) · [繁體中文](zh-TW.md) · [Deutsch](de-DE.md) · [Español](es-ES.md) · **Français** · [Italiano](it-IT.md) · [日本語](ja-JP.md) · [한국어](ko-KR.md) · [Русский](ru-RU.md) · [Tiếng Việt](vi-VN.md)

## Paquets

| Langage | Paquet | Documentation |
| --- | --- | --- |
| TypeScript | [`decentralized-bradley-terry-rating`](https://www.npmjs.com/package/decentralized-bradley-terry-rating) | [typescript/README.md](../typescript/README.md) |
| C# (.NET 8) | [`DecentralizedBradleyTerry`](https://www.nuget.org/packages/DecentralizedBradleyTerry) | [csharp/README.md](../csharp/README.md) |

## Le modèle

Un classement `r ∈ (0, 1)` est le taux de victoire relatif du joueur : la probabilité de battre un adversaire neutre classé `r = 0.5`. Il correspond à une force Bradley–Terry de

```
γ = r / (1 - r)
```

La prédiction d'un match entre deux joueurs s'écrit

```
P(A bat B) = γA / (γA + γB)
           = rA(1 - rB) / (rA(1 - rB) + rB(1 - rA))
```

Avec `θ = logit(r) = log(γ)`, `p = P(A bat B)`, `s = 1` si A gagne et `0` sinon, et `η` le taux d'apprentissage :

```
θA' = θA + η · (s - p)
θB' = θB - η · (s - p)
r'  = σ(θ')
```

Il s'agit d'une étape d'ascension de gradient en ligne sur la log-vraisemblance globale de Bradley–Terry — Elo sous forme de log-odds. La valeur par défaut `η = 0.25` équivaut à peu près à un facteur `K ≈ 43` aux échecs (`η = K · ln 10 / 400`).

Avec un taux d'apprentissage fixe, la mise à jour en ligne se stabilise dans un voisinage `O(η)` de la solution globale. Pour converger strictement vers la solution du maximum de vraisemblance, faites décroître le taux avec le nombre de matchs joués, par exemple `η_t = η0 / (t + τ)`. La bibliothèque ne conserve aucun état : c'est à l'appelant de fournir la décroissance.

## Propriétés

1. **Borné** — les entrées et les sorties restent toujours strictement dans `(0, 1)`. Pas de taux de victoire négatif, pas de valeur au-dessus de 100 %.
2. **Neutre à 0.5** — `r = 0.5` correspond à un match équilibré contre un adversaire neutre, et tout nouveau joueur part de là.
3. **Symétrique** — échangez les joueurs et inversez le résultat : les deux nouveaux classements s'échangent aussi. `P(A bat B) + P(B bat A) = 1`.
4. **Monotone** — le classement du vainqueur monte, celui du perdant descend.
5. **Les surprises comptent davantage** — un résultat inattendu fait bouger les classements plus qu'un résultat attendu : battre un joueur plus fort rapporte donc plus que battre un joueur plus faible.
6. **Somme nulle en log-odds** — `θA' + θB' = θA + θB`. Un match ne modifie jamais la somme des log-odds du couple, donc l'ensemble des classements ne dérive pas. Si tout le monde part de 0.5, la somme totale des `θ` reste à 0, exactement la normalisation de la solution globale du maximum de vraisemblance de Bradley–Terry.
7. **Convergent** — chaque mise à jour suit le gradient de la log-vraisemblance globale de Bradley–Terry. Un taux d'apprentissage qui décroît en respectant les conditions de Robbins–Monro fait converger les classements vers le MLE global ; les tests le vérifient par rapport à un MLE de référence calculé avec l'algorithme MM.

## Matchs nuls

Un match a exactement deux issues : A gagne ou B gagne. Si votre domaine a besoin de matchs nuls, ne les passez pas tels quels à cette bibliothèque.

## Démarrage rapide

TypeScript :

```ts
import {
  INITIAL_RATING,
  updateRatings,
  winProbability,
} from 'decentralized-bradley-terry-rating';

const ratings = new Map<string, number>([
  ['alice', INITIAL_RATING],
  ['bob', INITIAL_RATING],
]);

const a = ratings.get('alice')!;
const b = ratings.get('bob')!;

// alice gagne
const { ratingA, ratingB } = updateRatings(a, b, true);
ratings.set('alice', ratingA);
ratings.set('bob', ratingB);

console.log(winProbability(ratingA, ratingB)); // prédiction mise à jour
```

C# :

```csharp
using DecentralizedBradleyTerry;

var ratings = new Dictionary<string, double>
{
    ["alice"] = Ratings.InitialRating,
    ["bob"] = Ratings.InitialRating,
};

// alice gagne
var (newAlice, newBob) = Ratings.Update(ratings["alice"], ratings["bob"], aWon: true);
ratings["alice"] = newAlice;
ratings["bob"] = newBob;

double p = Ratings.WinProbability(newAlice, newBob);
```

L'API complète de chaque langage se trouve dans le README de son paquet.

## Organisation du dépôt

```
.
├── typescript/   # paquet npm
├── csharp/       # paquet NuGet
└── docs/         # traductions de ce README
```

## Lancer les tests

TypeScript (Node ≥ 23.6, qui exécute directement les fichiers `.ts` grâce à l'effacement natif des types — aucune étape de compilation) :

```bash
cd typescript
npm test
```

En option :

```bash
npm install        # installe TypeScript et @types/node
npm run typecheck  # vérification des types
npm run build      # génère dist/
```

C# (SDK .NET 8) :

```bash
dotnet test csharp/test/DecentralizedBradleyTerry.Tests/DecentralizedBradleyTerry.Tests.csproj
```

Les deux suites couvrent la formule de prédiction, la symétrie, la monotonie, la conservation des log-odds, l'effet du taux d'apprentissage, les entrées invalides et une vérification de bout en bout que les mises à jour en ligne convergent vers le MLE global de Bradley–Terry.

## Licence

MIT
