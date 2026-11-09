# Dezentrales Bradley–Terry-Rating

Nach jedem Match lässt sich ein Rating allein aus den aktuellen Ratings der beiden Spieler und dem Ergebnis aktualisieren. Kein zentraler Server, kein globales Match-Verzeichnis, keine Spieler-IDs. Das Repository enthält zwei semantisch identische Implementierungen – TypeScript und C# –, beide auf Basis reiner Funktionen.

**Sprachen:** [English](../README.md) · [简体中文](zh-CN.md) · [繁體中文](zh-TW.md) · **Deutsch** · [Español](es-ES.md) · [Français](fr-FR.md) · [Italiano](it-IT.md) · [日本語](ja-JP.md) · [한국어](ko-KR.md) · [Русский](ru-RU.md) · [Tiếng Việt](vi-VN.md)

## Pakete

| Sprache | Paket | Dokumentation |
| --- | --- | --- |
| TypeScript | [`decentralized-bradley-terry-rating`](https://www.npmjs.com/package/decentralized-bradley-terry-rating) | [typescript/README.md](../typescript/README.md) |
| C# (.NET 8) | [`DecentralizedBradleyTerry`](https://www.nuget.org/packages/DecentralizedBradleyTerry) | [csharp/README.md](../csharp/README.md) |

## Das Modell

Ein Rating `r ∈ (0, 1)` ist die relative Siegquote des Spielers: die Wahrscheinlichkeit, gegen einen neutralen Gegner mit Rating `r = 0.5` zu gewinnen. Es entspricht einer Bradley–Terry-Stärke von

```
γ = r / (1 - r)
```

Die Vorhersage für ein Match zwischen zwei Spielern lautet

```
P(A gewinnt gegen B) = γA / (γA + γB)
                    = rA(1 - rB) / (rA(1 - rB) + rB(1 - rA))
```

Dabei sind `θ = logit(r) = log(γ)`, `p = P(A gewinnt gegen B)`, `s = 1`, wenn A gewinnt, sonst `0`, und `η` die Lernrate:

```
θA' = θA + η · (s - p)
θB' = θB - η · (s - p)
r'  = σ(θ')
```

Das ist ein Online-Schritt des Gradientenaufstiegs auf die globale Bradley–Terry-Log-Likelihood – Elo in Log-Odds-Form. Der Standardwert `η = 0.25` entspricht ungefähr einem Schach-`K ≈ 43` (`η = K · ln 10 / 400`).

Bei fester Lernrate pendelt sich das Online-Update in einer `O(η)`-Umgebung der globalen Lösung ein. Für echte Konvergenz zum Maximum-Likelihood-Schätzer lässt man die Rate mit der Zahl der gespielten Matches abklingen, etwa `η_t = η0 / (t + τ)`. Die Bibliothek ist zustandslos; den Abklingplan übergibt der Aufrufer.

## Eigenschaften

1. **Beschränkt** – Ein- und Ausgaben liegen immer strikt in `(0, 1)`. Keine negativen Siegquoten, keine Werte über 100 %.
2. **Neutral bei 0.5** – `r = 0.5` steht für ein ausgeglichenes Match gegen einen neutralen Gegner; jeder neue Spieler startet dort.
3. **Symmetrisch** – Vertauscht man die Spieler und dreht das Ergebnis um, vertauschen sich auch die neuen Ratings. `P(A gewinnt gegen B) + P(B gewinnt gegen A) = 1`.
4. **Monoton** – Das Rating des Siegers steigt, das des Verlierers sinkt.
5. **Überraschungen zählen mehr** – Ein unerwartetes Ergebnis bewegt die Ratings stärker als ein erwartetes. Ein Sieg gegen einen stärkeren Gegner bringt daher mehr als einer gegen einen schwächeren.
6. **Nullsumme in Log-Odds** – `θA' + θB' = θA + θB`. Ein Match verändert die Summe der Log-Odds des Paares nie, der Rating-Pool driftet also als Ganzes nicht. Starten alle bei 0.5, bleibt die Summe aller `θ` bei 0 – genau die Normalisierung der globalen Bradley–Terry-Maximum-Likelihood-Lösung.
7. **Konvergent** – Jedes Update folgt dem Gradienten der globalen Bradley–Terry-Log-Likelihood. Lässt man die Lernrate so abklingen, dass die Robbins–Monro-Bedingungen erfüllt sind, konvergieren die Ratings gegen den globalen MLE; die Tests prüfen das gegen eine Referenz-MLE aus dem MM-Algorithmus.

## Unentschieden

Ein Match hat genau zwei Ausgänge: A gewinnt oder B gewinnt. Braucht Ihre Anwendung Unentschieden, sollten Sie sie nicht unverändert an diese Bibliothek übergeben.

## Schnellstart

TypeScript:

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

// alice gewinnt
const { ratingA, ratingB } = updateRatings(a, b, true);
ratings.set('alice', ratingA);
ratings.set('bob', ratingB);

console.log(winProbability(ratingA, ratingB)); // aktualisierte Vorhersage
```

C#:

```csharp
using DecentralizedBradleyTerry;

var ratings = new Dictionary<string, double>
{
    ["alice"] = Ratings.InitialRating,
    ["bob"] = Ratings.InitialRating,
};

// alice gewinnt
var (newAlice, newBob) = Ratings.Update(ratings["alice"], ratings["bob"], aWon: true);
ratings["alice"] = newAlice;
ratings["bob"] = newBob;

double p = Ratings.WinProbability(newAlice, newBob);
```

Die vollständige API je Sprache steht in der README des jeweiligen Pakets.

## Repository-Struktur

```
.
├── typescript/   # npm-Paket
├── csharp/       # NuGet-Paket
└── docs/         # Übersetzungen dieser README
```

## Tests ausführen

TypeScript (Node ≥ 23.6, führt `.ts`-Dateien dank nativer Typentfernung direkt aus – kein Build-Schritt):

```bash
cd typescript
npm test
```

Optional:

```bash
npm install        # TypeScript und @types/node installieren
npm run typecheck  # Typprüfung
npm run build      # dist/ erzeugen
```

C# (.NET 8 SDK):

```bash
dotnet test csharp/test/DecentralizedBradleyTerry.Tests/DecentralizedBradleyTerry.Tests.csproj
```

Beide Testsuiten decken die Vorhersageformel, Symmetrie, Monotonie, Erhaltung der Log-Odds, den Einfluss der Lernrate, ungültige Eingaben sowie einen End-to-End-Test ab, dass Online-Updates gegen den globalen Bradley–Terry-MLE konvergieren.

## Lizenz

MIT
