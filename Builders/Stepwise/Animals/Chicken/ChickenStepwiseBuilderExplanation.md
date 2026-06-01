# ChickenBuilder — The Stepwise (Staged) Builder Pattern Explained

This document walks through every file in the `Chicken` folder, explains what
each type does, and shows how the design **forces the caller to call the builder
methods in a fixed order** — starting with `OfOrigin` and ending with `Build`.

The goal is for **anyone** — even a junior developer who has never seen this
pattern — to read this file top to bottom and understand the design.

This is the *opposite* design goal of the `Duck` builder (see
`RecursiveGeneric/Animals/Duck/DuckBuilderExplanation.md`). The Duck builder
lets you call methods in **any order**; the Chicken builder **enforces a strict
sequence**.

---

## 1. The Big Picture — What Are We Trying To Build?

We want to construct a `Chicken` using fluent chaining like this:

```csharp
Chicken chicken = Chicken.ChickenBuilder
                         .OfOrigin("Japan")
                         .Color(AnimalColors.Black)
                         .Weight(5.5)
                         .Age(3)
                         .Build();
```

The driving requirement is the exact opposite of the Duck builder:

1. **Order is mandatory.** The caller MUST call `OfOrigin` first, then `Color`,
   then `Weight`, then `Age`, then `Build`. Any other order is a
   **compile-time error**.
2. **You cannot skip a step.** You cannot call `.Age(2)` straight away, and you
   cannot reach `.Build()` until every prior step has run.
3. **Strongly typed.** No `object`, no `dynamic`, no runtime checks. The compiler
   itself is the gatekeeper.

The pattern that delivers this is the **Stepwise Builder** (also called the
**Staged Builder** or **Step Builder**).

---

## 2. The Core Idea — Each Step Returns a Different Type

In a normal builder every method returns the *same* builder type, so every
method is always available. In a stepwise builder **each method returns a
different interface that exposes only the *next* allowed method**.

```
IChickenOriginBuilder   → sees only OfOrigin
   │ OfOrigin(...) returns
   ▼
IChickenkColorBuilder   → sees only Color
   │ Color(...) returns
   ▼
IChickenWeightBuilder   → sees only Weight
   │ Weight(...) returns
   ▼
IChickenAgeBuilder      → sees only Age
   │ Age(...) returns
   ▼
IChickenBuilder         → sees only Build
   │ Build() returns
   ▼
Chicken                 (the finished product)
```

Because each interface only declares the single next method, the compiler's
IntelliSense literally walks you down one legal path. Out-of-order calls don't
exist as far as the compiler is concerned.

---

## 3. The Cast of Files

| File | Type | Role |
|------|------|------|
| `Chicken.cs` | data class | The "product" + the static entry point |
| `Interfaces/IChickenOriginBuilder.cs` | step 1 interface | Exposes `OfOrigin` → returns step 2 |
| `Interfaces/IChickenkColorBuilder.cs` | step 2 interface | Exposes `Color` → returns step 3 |
| `Interfaces/IChickenWeightBuilder.cs` | step 3 interface | Exposes `Weight` → returns step 4 |
| `Interfaces/IChickenAgeBuilder.cs` | step 4 interface | Exposes `Age` → returns final step |
| `Interfaces/IChickenBuilder.cs` | final interface | Exposes `Build` → returns `Chicken` |
| `Interfaces/IAggrigateChickenBuilder.cs` | aggregate interface | Inherits all five so one class can implement them |
| `ChickenBuilderStepwise.cs` | concrete builder | Implements every step (explicitly) |

> Note on naming: `IChickenkColorBuilder` and `IAggrigateChickenBuilder` contain
> typos (an extra `k`, and "Aggrigate" instead of "Aggregate"). They are kept
> here as-is to match the code; rename consistently if you clean them up.

---

## 4. The Step Interfaces

Each interface declares exactly one method, and that method's **return type is
the next interface in the chain**:

```csharp
public interface IChickenOriginBuilder
{
    IChickenkColorBuilder OfOrigin(string? origin);   // step 1 → step 2
}

public interface IChickenkColorBuilder
{
    IChickenWeightBuilder Color(AnimalColors color);  // step 2 → step 3
}

public interface IChickenWeightBuilder
{
    IChickenAgeBuilder Weight(double weight);         // step 3 → step 4
}

public interface IChickenAgeBuilder
{
    IChickenBuilder Age(int age);                     // step 4 → final
}

public interface IChickenBuilder
{
    Chicken Build();                                  // final → product
}
```

This chaining of return types is the entire enforcement mechanism. Nothing else
is doing the work.

---

## 5. The Aggregate Interface

```csharp
public interface IAggrigateChickenBuilder : IChickenBuilder,
                                            IChickenAgeBuilder,
                                            IChickenWeightBuilder,
                                            IChickenkColorBuilder,
                                            IChickenOriginBuilder
{
}
```

This exists purely so that **one concrete class** can satisfy all five step
interfaces at once. It is an *implementation detail*. The caller must never be
handed a reference of this type — if they were, they would see all five methods
simultaneously and the ordering would be lost.

---

## 6. The Concrete Builder

```csharp
public class ChickenBuilderStepwise : IAggrigateChickenBuilder
{
    private Chicken chicken = new();

    IChickenkColorBuilder IChickenOriginBuilder.OfOrigin(string? origin)
    {
        chicken.Origin = origin;
        return this;          // same object, but typed as the next step
    }

    IChickenWeightBuilder IChickenkColorBuilder.Color(AnimalColors color)
    {
        chicken.Color = color;
        return this;
    }

    IChickenAgeBuilder IChickenWeightBuilder.Weight(double weight)
    {
        chicken.Weight = weight;
        return this;
    }

    IChickenBuilder IChickenAgeBuilder.Age(int age)
    {
        chicken.AgeInMonths = age;
        return this;
    }

    Chicken IChickenBuilder.Build()
    {
        return chicken;
    }
}
```

Two things to notice:

1. **Every method does `return this;`** — it is always the *same* object. Only
   the *static return type* changes step to step. The object is fully capable
   the whole time; the compiler just shows the caller a narrower and narrower
   "view" of it.
2. **The methods are implemented explicitly** (`IInterface.Method` form, no
   `public`). This means the methods are NOT part of the concrete class's public
   surface — see Section 8.

---

## 7. The Entry Point

```csharp
// Chicken.cs
public static IChickenOriginBuilder ChickenBuilder => new ChickenBuilderStepwise();
```

This is the linchpin. Two deliberate choices:

- **The return type is `IChickenOriginBuilder`** — the *first step only*. Even
  though the object is a fully-capable `ChickenBuilderStepwise`, the caller is
  handed the narrowest possible view, so the only method they can call is
  `OfOrigin`. This is **subtype polymorphism**: an interface reference pointing
  at a concrete implementation.
- **It is a `=>` property (returns `new` each time)**, not a shared `static`
  field. Each call gets a fresh builder, so two independent `Build()` chains
  never share the same mutable `Chicken`.

> Earlier version had `public static ChickenBuilderStepwise ChickenBuilder = new();`
> — typed as the **concrete class**, which exposed every method and let callers
> start anywhere (e.g. `Chicken.ChickenBuilder.Age(2)`). That was the bug this
> design fixes.

---

## 8. Why Explicit Interface Implementation?

Explicit implementation (`IChickenOriginBuilder.OfOrigin` instead of
`public OfOrigin`) removes the methods from the concrete type's public surface.
They become reachable **only through the matching interface reference**:

```csharp
var raw = new ChickenBuilderStepwise();
raw.OfOrigin("X");          // ❌ won't compile — not a public member of the class
raw.Age(2);                 // ❌ won't compile

IChickenOriginBuilder view = new ChickenBuilderStepwise();
view.OfOrigin("X");         // ✅ only reachable via the interface
```

This closes the loophole where someone could grab the concrete type directly and
call methods out of order. Combined with the entry point handing out only
`IChickenOriginBuilder`, the ordered chain becomes the *only* way to use the
builder.

The one remaining theoretical bypass is a deliberate cast to
`IAggrigateChickenBuilder` — but that requires writing obviously-wrong code, so
it's considered "you're holding it wrong" rather than an open door.

---

## 9. Stepwise vs. Recursive-Generic (Duck) — When to Use Which

| | Duck (Recursive Generic / CRTP) | Chicken (Stepwise) |
|---|---|---|
| Call order | Any order | Fixed, enforced order |
| Can skip a property? | Yes | No — every step is mandatory |
| Mechanism | Every method returns `Self` (the full type) | Every method returns the *next* interface |
| Best for | Optional / order-independent config | Required fields that must all be set |
| Downside | No way to require fields | More boilerplate (one interface per step) |

Use a **recursive generic** builder when all properties are optional and order
doesn't matter. Use a **stepwise** builder when you must guarantee certain
fields are provided, in a certain order, at compile time.

---

## 10. Continuation / TODO

- Consider renaming the typo'd interfaces: `IChickenkColorBuilder` →
  `IChickenColorBuilder`, `IAggrigateChickenBuilder` → `IAggregateChickenBuilder`.
- Optionally remove the leftover commented-out `Builder` line that was copied
  from the Duck template.
- The pattern could be extended with optional steps (an interface that returns
  *itself* for repeatable calls, or branches that allow skipping a step) if some
  fields should become optional.
- A mid-chain validation step could throw if a value is invalid before `Build()`.
