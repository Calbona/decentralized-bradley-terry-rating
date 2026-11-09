# Puntuación Bradley–Terry descentralizada

Cada partida se actualiza solo con la puntuación actual de los dos jugadores y el resultado. Sin servidor central, sin registro global de partidas y sin identificadores de jugador. El repositorio incluye dos implementaciones semánticamente idénticas —TypeScript y C#—, ambas basadas en funciones puras.

**Idiomas:** [English](../README.md) · [简体中文](zh-CN.md) · [繁體中文](zh-TW.md) · [Deutsch](de-DE.md) · **Español** · [Français](fr-FR.md) · [Italiano](it-IT.md) · [日本語](ja-JP.md) · [한국어](ko-KR.md) · [Русский](ru-RU.md) · [Tiếng Việt](vi-VN.md)

## Paquetes

| Lenguaje | Paquete | Documentación |
| --- | --- | --- |
| TypeScript | [`decentralized-bradley-terry-rating`](https://www.npmjs.com/package/decentralized-bradley-terry-rating) | [typescript/README.md](../typescript/README.md) |
| C# (.NET 8) | [`DecentralizedBradleyTerry`](https://www.nuget.org/packages/DecentralizedBradleyTerry) | [csharp/README.md](../csharp/README.md) |

## El modelo

Una puntuación `r ∈ (0, 1)` es la tasa de victorias relativa del jugador: la probabilidad de ganar a un rival neutro con puntuación `r = 0.5`. Se corresponde con una fuerza Bradley–Terry de

```
γ = r / (1 - r)
```

La predicción de una partida entre dos jugadores es

```
P(A gana a B) = γA / (γA + γB)
              = rA(1 - rB) / (rA(1 - rB) + rB(1 - rA))
```

Con `θ = logit(r) = log(γ)`, `p = P(A gana a B)`, `s = 1` si gana A y `0` en caso contrario, y `η` la tasa de aprendizaje:

```
θA' = θA + η · (s - p)
θB' = θB - η · (s - p)
r'  = σ(θ')
```

Se trata de un paso de ascenso de gradiente en línea sobre la log-verosimilitud global de Bradley–Terry: la forma en log-odds de Elo. El valor predeterminado `η = 0.25` equivale aproximadamente a un `K ≈ 43` en ajedrez (`η = K · ln 10 / 400`).

Con una tasa de aprendizaje fija, la actualización en línea se estabiliza en un entorno `O(η)` de la solución global. Para converger estrictamente a la solución de máxima verosimilitud, haz que la tasa decaiga con el número de partidas jugadas, por ejemplo `η_t = η0 / (t + τ)`. La biblioteca no guarda estado, así que el calendario de decaimiento lo aporta quien la llama.

## Propiedades

1. **Acotada** — las entradas y las salidas siempre quedan estrictamente dentro de `(0, 1)`. No hay tasas de victoria negativas ni valores por encima del 100 %.
2. **Neutra en 0.5** — `r = 0.5` es una partida igualada contra un rival neutro, y todo jugador nuevo parte de ahí.
3. **Simétrica** — si intercambias los jugadores y das la vuelta al resultado, las nuevas puntuaciones se intercambian igualmente. `P(A gana a B) + P(B gana a A) = 1`.
4. **Monótona** — la puntuación del ganador sube y la del perdedor baja.
5. **Las sorpresas pesan más** — un resultado inesperado mueve más la puntuación que uno esperado, así que ganar a un rival más fuerte renta más que ganar a uno más débil.
6. **Suma cero en log-odds** — `θA' + θB' = θA + θB`. Una partida nunca altera la suma de los log-odds de la pareja, así que el conjunto de puntuaciones no se desplaza. Si todos parten de 0.5, la suma total de `θ` se mantiene en 0, que es exactamente la normalización de la solución global de máxima verosimilitud de Bradley–Terry.
7. **Convergente** — cada actualización avanza en la dirección del gradiente de la log-verosimilitud global de Bradley–Terry. Si la tasa de aprendizaje decae cumpliendo las condiciones de Robbins–Monro, las puntuaciones convergen al MLE global; las pruebas lo comprueban contra una MLE de referencia calculada con el algoritmo MM.

## Empates

Cada partida tiene exactamente dos resultados: gana A o gana B. Si tu dominio necesita empates, no los pases a esta biblioteca tal cual.

## Inicio rápido

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

// gana alice
const { ratingA, ratingB } = updateRatings(a, b, true);
ratings.set('alice', ratingA);
ratings.set('bob', ratingB);

console.log(winProbability(ratingA, ratingB)); // predicción actualizada
```

C#:

```csharp
using DecentralizedBradleyTerry;

var ratings = new Dictionary<string, double>
{
    ["alice"] = Ratings.InitialRating,
    ["bob"] = Ratings.InitialRating,
};

// gana alice
var (newAlice, newBob) = Ratings.Update(ratings["alice"], ratings["bob"], aWon: true);
ratings["alice"] = newAlice;
ratings["bob"] = newBob;

double p = Ratings.WinProbability(newAlice, newBob);
```

La API completa de cada lenguaje está en el README de su paquete.

## Estructura del repositorio

```
.
├── typescript/   # paquete de npm
├── csharp/       # paquete de NuGet
└── docs/         # traducciones de este README
```

## Ejecutar las pruebas

TypeScript (Node ≥ 23.6, que ejecuta los archivos `.ts` directamente mediante la eliminación nativa de tipos, sin paso de compilación):

```bash
cd typescript
npm test
```

Opcional:

```bash
npm install        # instala TypeScript y @types/node
npm run typecheck  # comprobación de tipos
npm run build      # genera dist/
```

C# (SDK de .NET 8):

```bash
dotnet test csharp/test/DecentralizedBradleyTerry.Tests/DecentralizedBradleyTerry.Tests.csproj
```

Ambas suites cubren la fórmula de predicción, la simetría, la monotonía, la conservación de los log-odds, el efecto de la tasa de aprendizaje, las entradas no válidas y una comprobación de extremo a extremo de que las actualizaciones en línea convergen a la MLE global de Bradley–Terry.

## Licencia

MIT
