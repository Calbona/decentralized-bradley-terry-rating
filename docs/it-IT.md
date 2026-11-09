# Rating Bradley–Terry decentralizzato

Ogni partita si aggiorna usando solo i punteggi attuali dei due giocatori e il risultato. Niente server centrale, niente registro globale delle partite, niente ID giocatore. Il repository contiene due implementazioni semanticamente identiche — TypeScript e C# —, entrambe basate su funzioni pure.

**Lingue:** [English](../README.md) · [简体中文](zh-CN.md) · [繁體中文](zh-TW.md) · [Deutsch](de-DE.md) · [Español](es-ES.md) · [Français](fr-FR.md) · **Italiano** · [日本語](ja-JP.md) · [한국어](ko-KR.md) · [Русский](ru-RU.md) · [Tiếng Việt](vi-VN.md)

## Pacchetti

| Linguaggio | Pacchetto | Documentazione |
| --- | --- | --- |
| TypeScript | [`decentralized-bradley-terry-rating`](https://www.npmjs.com/package/decentralized-bradley-terry-rating) | [typescript/README.md](../typescript/README.md) |
| C# (.NET 8) | [`DecentralizedBradleyTerry`](https://www.nuget.org/packages/DecentralizedBradleyTerry) | [csharp/README.md](../csharp/README.md) |

## Il modello

Un punteggio `r ∈ (0, 1)` è il tasso di vittoria relativo del giocatore: la probabilità di battere un avversario neutro con punteggio `r = 0.5`. Corrisponde a una forza Bradley–Terry di

```
γ = r / (1 - r)
```

La previsione di una partita tra due giocatori è

```
P(A batte B) = γA / (γA + γB)
             = rA(1 - rB) / (rA(1 - rB) + rB(1 - rA))
```

Con `θ = logit(r) = log(γ)`, `p = P(A batte B)`, `s = 1` se vince A e `0` altrimenti, e `η` il tasso di apprendimento:

```
θA' = θA + η · (s - p)
θB' = θB - η · (s - p)
r'  = σ(θ')
```

È un passo di salita al gradiente online sulla log-verosimiglianza globale di Bradley–Terry: la forma in log-odds di Elo. Il valore predefinito `η = 0.25` equivale all'incirca a un `K ≈ 43` degli scacchi (`η = K · ln 10 / 400`).

Con un tasso di apprendimento fisso, l'aggiornamento online si stabilizza in un intorno `O(η)` della soluzione globale. Per convergere rigorosamente alla soluzione di massima verosimiglianza, fai decadere il tasso con il numero di partite giocate, ad esempio `η_t = η0 / (t + τ)`. La libreria non mantiene stato: la pianificazione del decadimento spetta a chi la chiama.

## Proprietà

1. **Limitato** — input e output restano sempre strettamente in `(0, 1)`. Niente tassi di vittoria negativi, niente valori sopra il 100%.
2. **Neutro a 0.5** — `r = 0.5` indica una partita equilibrata contro un avversario neutro, ed è il punto di partenza di ogni nuovo giocatore.
3. **Simmetrico** — scambia i giocatori e inverti il risultato: anche i nuovi punteggi si scambiano. `P(A batte B) + P(B batte A) = 1`.
4. **Monotono** — il punteggio di chi vince sale, quello di chi perde scende.
5. **Le sorprese pesano di più** — un risultato inatteso muove i punteggi più di uno atteso, quindi battere un avversario più forte rende più che batterne uno più debole.
6. **Somma zero nei log-odds** — `θA' + θB' = θA + θB`. Una partita non cambia mai la somma dei log-odds della coppia, quindi l'insieme dei punteggi non va alla deriva. Se tutti partono da 0.5, la somma totale dei `θ` resta 0, esattamente la normalizzazione della soluzione globale di massima verosimiglianza di Bradley–Terry.
7. **Convergente** — ogni aggiornamento segue il gradiente della log-verosimiglianza globale di Bradley–Terry. Con un tasso di apprendimento che decade rispettando le condizioni di Robbins–Monro, i punteggi convergono al MLE globale; i test lo verificano rispetto a un MLE di riferimento calcolato con l'algoritmo MM.

## Pareggi

Una partita ha esattamente due esiti: vince A o vince B. Se il tuo dominio ha bisogno dei pareggi, non passarli così come sono a questa libreria.

## Avvio rapido

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

// vince alice
const { ratingA, ratingB } = updateRatings(a, b, true);
ratings.set('alice', ratingA);
ratings.set('bob', ratingB);

console.log(winProbability(ratingA, ratingB)); // previsione aggiornata
```

C#:

```csharp
using DecentralizedBradleyTerry;

var ratings = new Dictionary<string, double>
{
    ["alice"] = Ratings.InitialRating,
    ["bob"] = Ratings.InitialRating,
};

// vince alice
var (newAlice, newBob) = Ratings.Update(ratings["alice"], ratings["bob"], aWon: true);
ratings["alice"] = newAlice;
ratings["bob"] = newBob;

double p = Ratings.WinProbability(newAlice, newBob);
```

L'API completa di ciascun linguaggio è nel README del relativo pacchetto.

## Struttura del repository

```
.
├── typescript/   # pacchetto npm
├── csharp/       # pacchetto NuGet
└── docs/         # traduzioni di questo README
```

## Eseguire i test

TypeScript (Node ≥ 23.6, che esegue direttamente i file `.ts` tramite la rimozione nativa dei tipi, senza fase di build):

```bash
cd typescript
npm test
```

Facoltativo:

```bash
npm install        # installa TypeScript e @types/node
npm run typecheck  # controllo dei tipi
npm run build      # genera dist/
```

C# (SDK .NET 8):

```bash
dotnet test csharp/test/DecentralizedBradleyTerry.Tests/DecentralizedBradleyTerry.Tests.csproj
```

Entrambe le suite coprono la formula di previsione, la simmetria, la monotonia, la conservazione dei log-odds, l'effetto del tasso di apprendimento, gli input non validi e una verifica end-to-end che gli aggiornamenti online convergono al MLE globale di Bradley–Terry.

## Licenza

MIT
