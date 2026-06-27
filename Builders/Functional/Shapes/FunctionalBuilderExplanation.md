# ShapeBuilder — The Functional Builder Pattern Explained

This document walks through every file in the `Functional/Shapes` folder, explains
what each type does, and shows how the design lets **third parties add brand-new
build steps without ever modifying the builder class** — using nothing more than a
`List<Action<T>>` and C# extension methods.

The goal is for **anyone** — even a junior developer who has never seen this
pattern — to read this file top to bottom and understand the design.

This is a *different* design goal from the other two builders in this repo:

- The `Duck` builder (`RecursiveGeneric/Animals/Duck/DuckBuilderExplanation.md`)
  optimizes for **any-order** chaining via a self-referential generic.
- The `Chicken` builder (`Stepwise/Animals/Chicken/ChickenStepwiseBuilderExplanation.md`)
  optimizes for **forced-order** chaining via per-step interfaces.
- The `Shape` builder (this one) optimizes for **open/closed extensibility** — new
  steps are added from the *outside*, with no edit to `ShapeBuilder`.

---

## 1. The Big Picture — What Are We Trying To Build?

We want to construct a `Shape` using fluent chaining like this:

```csharp
ShapeBuilder shapeBuilder = new();

Shape circle = shapeBuilder.DefineShape("Circle")
                           .CalculateCircleArea(5)
                           .CalculateCirclePerimeter(5)
                           .Build();

Shape triangle = shapeBuilder.DefineShape("Triangle")
                             .CalculateTriangleArea(6, 3)
                             .CalculateTrianglePerimeter(4, 5, 7)
                             .Build();
```

Notice something odd already: `DefineShape` lives on `ShapeBuilder` itself, but
`CalculateCircleArea`, `CalculateTriangleArea`, etc. do **not**. They are defined
in *separate plugin classes* and yet chain seamlessly. That is the whole point of
this pattern.

The driving requirement:

1. **Extensible from the outside.** Anyone — including code in another assembly
   that you don't control — can add a new build step (a new shape's area, a new
   perimeter formula) **without touching `ShapeBuilder`**.
2. **Fluent chaining.** Every step returns the builder so calls can be chained.
3. **Deferred construction.** Nothing is built during the chain; the real work
   happens all at once inside `Build()`.

The pattern that delivers this is the **Functional Builder**.

> **Deep-understanding note — the design DRIVER is the Open/Closed Principle.**
> The test: "If I delete the OCP benefit, does this design still make sense?" No.
> If you didn't care about third parties adding steps without touching your class,
> a classic builder would be simpler and better encapsulated, and the
> `List<Action<T>>` machinery would buy you nothing. Every structural choice below
> (public action list, deferred `Build()`, extension methods) is bent toward making
> outside-in extensibility possible.

---

## 2. The Core Idea — State Is a List of Functions, Not a Half-Built Object

In a classic builder the builder *holds the half-built object* and mutates it
immediately on each call. In a **functional** builder the builder holds a
**recipe**: a `List<Action<Shape>>`. Each step doesn't change a `Shape` — it
*records a function* that will change a `Shape` later.

```
.DefineShape("Circle")        ──►  Actions += (s => s.ShapeType = "Circle")
.CalculateCircleArea(5)       ──►  Actions += (s => s.Area = π·5²)
.CalculateCirclePerimeter(5) ──►  Actions += (s => s.Perimeter = 2·π·5)
.Build()                      ──►  s = new Shape(); replay every action onto s; return s
```

Building is **function application**: create a fresh `Shape`, then apply each
recorded `Action<Shape>` to it in order. That FP flavor — state *is* a list of
functions, construction *is* running them — is where the name "functional builder"
comes from.

---

## 3. The Cast of Files

| File | Type | Role |
|------|------|------|
| `Shape.cs` | data class | The "product" — the thing being built |
| `ShapeBuilder.cs` | builder | Holds the action list, exposes `DefineShape` + `Build` |
| `Plugins/CirclePlugin.cs` | static ext. class | Adds `CalculateCircleArea` / `CalculateCirclePerimeter` |
| `Plugins/TrianglePlugin.cs` | static ext. class | Adds `CalculateTriangleArea` / `CalculateTrianglePerimeter` |

---

## 4. `Shape.cs` — The Product

```csharp
public class Shape
{
    public string ShapeType { get; set; }
    public double Area { get; set; }
    public double Perimeter { get; set; }
}
```

This is the **target object** we want to construct. It is a plain data holder; it
knows nothing about builders, actions, or plugins. Every build step ultimately just
sets one of these three properties.

---

## 5. `ShapeBuilder.cs` — The Core Builder

```csharp
public sealed class ShapeBuilder
{
    public readonly List<Action<Shape>> ShapeBuildingActions = [];

    public ShapeBuilder DefineShape(string shapeType)
    {
        ShapeBuildingActions.Add(shapeTypeBuilder =>
        {
            shapeTypeBuilder.ShapeType = shapeType;
        });

        return this;
    }

    public Shape Build()
    {
        Shape shape = new();
        ShapeBuildingActions.ForEach(buildingAction => buildingAction(shape));
        ShapeBuildingActions.Clear();   // reset the recipe so the next product starts clean
        return shape;
    }
}
```

> **Deep-understanding note — why `sealed`, and why *only* for intent.** `ShapeBuilder`
> is `sealed`, so no class can inherit from it. The honest reason here is **design
> intent, not safety or performance**: the functional builder's contract is "extend me
> by adding *extension methods* from the outside, never by subclassing." Sealing
> enforces that contract instead of merely implying it.
> The usual textbook arguments for sealing *don't* carry weight in this class: there
> are **no `virtual` members**, so a subclass could not `override` anything or break an
> invariant anyway (the most it could do is *hide* a method with `new`, which doesn't
> change behavior for callers holding a `ShapeBuilder`). And devirtualization perf only
> helps virtual call sites, of which there are none. So the single load-bearing reason
> is the **communicated convention**: plug in new steps via `CirclePlugin`-style
> extension methods, not inheritance. (The plugins are `static` classes, which are
> already implicitly sealed — you can't write `sealed` on them and can't inherit them.)

Three pieces, each with a deliberate access choice:

- **`public readonly List<Action<Shape>> ShapeBuildingActions`** — the recipe.
  - `readonly` means you cannot **reassign** the list reference (no
    `ShapeBuildingActions = somethingElse`). It does **not** make the contents
    immutable.
  - `public` is the load-bearing decision: extension methods in *other* classes
    must be able to call `.Add(...)` on this list. If it were `private`, plugins
    couldn't contribute steps and the whole pattern collapses.

  > **Deep-understanding note — the encapsulation trade-off is the price of OCP.**
  > Exposing the action list publicly is normally a smell. Here it is the *cost*
  > you knowingly pay to let outside code extend the builder. Classic builders keep
  > state private (stronger encapsulation) precisely because they don't need this.

- **`DefineShape(string)`** — records a lambda that sets `ShapeType`, then
  `return this;` for fluent chaining. The lambda **captures** `shapeType` in a
  closure and remembers it until `Build()` runs.

- **`Build()`** — the only place real work happens. Creates a fresh `Shape`, then
  `ForEach` replays every recorded action onto it **in insertion order**, clears the
  recipe (`ShapeBuildingActions.Clear()`) so the next product starts clean, and
  returns it. (See §8 for why the clear is there.)

  > **Deep-understanding note — deferred execution + last-write-wins.** The lambdas
  > were *recorded* during the chain but only *run* here, in order. If two actions
  > set the same field, the **last one added wins**. Ordering is the only thing
  > that disambiguates conflicting steps — this pattern enforces nothing else about
  > sequence.

---

## 6. The Plugins — Adding Steps From the Outside

Here is the magic. `CalculateCircleArea` is **not** a method on `ShapeBuilder`. It
is an **extension method** in a completely separate static class:

```csharp
public static class CirclePlugin
{
    public static ShapeBuilder CalculateCircleArea(this ShapeBuilder shapeBuilder, float radius)
    {
        shapeBuilder.ShapeBuildingActions.Add(circle =>
        {
            circle.Area = Math.PI * Math.Pow(radius, 2);
        });

        return shapeBuilder;
    }

    public static ShapeBuilder CalculateCirclePerimeter(this ShapeBuilder shapeBuilder, float radius)
    {
        shapeBuilder.ShapeBuildingActions.Add(circle =>
        {
            circle.Perimeter = 2 * Math.PI * radius;
        });

        return shapeBuilder;
    }
}
```

`TrianglePlugin` follows the exact same recipe for triangles:

```csharp
public static class TrianglePlugin
{
    public static ShapeBuilder CalculateTriangleArea(this ShapeBuilder shapeBuilder, float height, float @base)
    {
        shapeBuilder.ShapeBuildingActions.Add(triangle =>
        {
            triangle.Area = height * @base / 2;
        });
        return shapeBuilder;
    }

    public static ShapeBuilder CalculateTrianglePerimeter(this ShapeBuilder shapeBuilder, float sideA, float sideB, float sideC)
    {
        shapeBuilder.ShapeBuildingActions.Add(triangle =>
        {
            triangle.Perimeter = sideA + sideB + sideC;
        });
        return shapeBuilder;
    }
}
```

What makes this work:

- **`this ShapeBuilder shapeBuilder`** — the `this` modifier on the first parameter
  is what turns a static method into an extension method. It lets you *call* it as
  if it were an instance method: `shapeBuilder.CalculateCircleArea(5)` is just sugar
  for `CirclePlugin.CalculateCircleArea(shapeBuilder, 5)`.
- **It reaches into the public `ShapeBuildingActions`** and adds its own lambda —
  exactly like `DefineShape` does, but from a class the builder has never heard of.
- **It returns the same `ShapeBuilder`** so the chain keeps flowing.

> **Deep-understanding note — this is the Open/Closed Principle made physical.**
> `ShapeBuilder` is *closed for modification* (you never edit it to add a shape) yet
> *open for extension* (new shapes arrive as new plugin classes). A classic builder
> would force you to crack open the builder class and add a method for every new
> shape. Here, adding `SquarePlugin` is a brand-new file that touches nothing
> existing.

> **Deep-understanding note — `@base` is not a typo.** `base` is a C# keyword, so the
> triangle parameter is written `@base`. The `@` is a verbatim-identifier prefix
> that is erased at compile time — the parameter's real name is `base`, and a caller
> using named arguments would write `base:` (no `@`).

---

## 7. How It Works — A Step-By-Step Trace

Let's trace the circle from `Program.cs`:

```csharp
ShapeBuilder shapeBuilder = new();
Shape circle = shapeBuilder.DefineShape("Circle")
                           .CalculateCircleArea(5)
                           .CalculateCirclePerimeter(5)
                           .Build();
```

> **Key idea:** during the chain, **no `Shape` exists yet**. Each call only appends a
> function to `ShapeBuildingActions` and returns the same `ShapeBuilder`. The `Shape`
> is born inside `Build()`.

### Step 1 — `DefineShape("Circle")`

Records `Actions[0] = (s => s.ShapeType = "Circle")`. Returns the builder.

```text
ShapeBuildingActions: [ set ShapeType="Circle" ]
```

### Step 2 — `.CalculateCircleArea(5)`

Extension method appends `Actions[1] = (s => s.Area = π·5²)`. The value `5` is
captured in the closure. Returns the builder.

```text
ShapeBuildingActions: [ set ShapeType, set Area ]
```

### Step 3 — `.CalculateCirclePerimeter(5)`

Appends `Actions[2] = (s => s.Perimeter = 2·π·5)`. Returns the builder.

```text
ShapeBuildingActions: [ set ShapeType, set Area, set Perimeter ]
```

### Step 4 — `.Build()` — the only place anything is constructed

```csharp
Shape shape = new();                                  // empty shell: "", 0, 0
ShapeBuildingActions.ForEach(action => action(shape)); // replay in order
ShapeBuildingActions.Clear();                          // reset so the next product starts clean
return shape;
```

```text
action 0  →  shape.ShapeType = "Circle"
action 1  →  shape.Area      = 78.539...
action 2  →  shape.Perimeter = 31.415...

return    →  Shape { ShapeType="Circle", Area=78.54, Perimeter=31.42 }
```

Every closure replays the value it captured back during the chain, against the one
fresh `Shape` created in `Build()`.

---

## 8. A Real Gotcha — The Builder Is Reused Across Products

> **Status:** this section describes the trap as it existed *before* the fix.
> Solution (b) below — `ShapeBuildingActions.Clear()` inside `Build()` — has since
> been applied to `ShapeBuilder`, so the current code no longer accumulates. The
> walkthrough is kept because the trap itself is the lesson.

In `Program.cs` the **same** `shapeBuilder` instance builds both the circle and the
triangle:

```csharp
ShapeBuilder shapeBuilder = new();
Shape circle   = shapeBuilder.DefineShape("Circle")...Build();
Shape triangle = shapeBuilder.DefineShape("Triangle")...Build();   // SAME builder
```

If `Build()` **doesn't clear** `ShapeBuildingActions` (the original code), the
triangle chain *appends* to the list that already contains the circle's three
actions:

```text
After both chains, ShapeBuildingActions =
  [ ShapeType="Circle", Area=circle, Perimeter=circle,     ← left over from circle
    ShapeType="Triangle", Area=triangle, Perimeter=triangle ]
```

So `triangle = ...Build()` replays **all six** actions on its fresh `Shape`.

> **Deep-understanding note — why the triangle is still correct (and when it wouldn't
> be).** It works *only* because of last-write-wins: the triangle's three actions
> come last and overwrite all three fields the circle had set. It is doing wasted
> work (re-running the circle's actions), and it is one missing field away from a
> bug — if the triangle set `Area` but not `Perimeter`, the result would silently
> leak the circle's perimeter. This is the classic shared-mutable-recipe trap.

### Solutions

**(a) Best — a fresh builder per product.** Eliminate the shared state instead of
managing it. Match the Duck/Chicken style with a static entry point on `Shape`:

```csharp
public static ShapeBuilder Builder => new();   // fresh recipe every time

Shape circle   = Shape.Builder.DefineShape("Circle")...Build();
Shape triangle = Shape.Builder.DefineShape("Triangle")...Build();
```

**(b) Fallback — reset inside `Build()`** when you genuinely must reuse one instance.
**This is the fix currently applied in the code:**

```csharp
public Shape Build()
{
    Shape shape = new();
    ShapeBuildingActions.ForEach(action => action(shape));
    ShapeBuildingActions.Clear();   // next product starts clean
    return shape;
}
```

It was chosen here because it makes reuse safe **without changing any call site** and
works no matter how the instance is shared (including the DI case in §9). The
downside: `Build()` now has a **hidden side effect** — it mutates the builder, which
is mildly surprising for a method that ostensibly "just builds."

> **Deep-understanding note:** both fix the symptom. Fresh-instance **eliminates**
> shared state (nothing to leak); `Clear()` carefully **manages** shared state (leak,
> then wipe). Removing a hazard beats remembering to defuse it — so default to fresh.

---

## 9. Using This Builder Under Dependency Injection

The shared-recipe trap from §8 gets sharper under DI, and the obvious "just make it
transient" reflex **does not fix it**.

Suppose `ShapeBuilder` is injected and used to build two shapes in one class:

```csharp
public class ShapeService
{
    private readonly ShapeBuilder _builder;          // resolved ONCE, at construction

    public ShapeService(ShapeBuilder builder) => _builder = builder;

    public (Shape, Shape) BuildBoth()
    {
        var circle   = _builder.DefineShape("Circle").CalculateCircleArea(5)...Build();
        var triangle = _builder.DefineShape("Triangle").CalculateTriangleArea(6, 3)...Build();
        //             ▲ same _builder → §8 accumulation bug is back
        return (circle, triangle);
    }
}
```

Registering `services.AddTransient<ShapeBuilder>()` does **not** help.

> **Deep-understanding note — DI lifetime ≠ per-use freshness.** A DI lifetime
> controls how often the *container creates* the instance, not how often *you reuse*
> it. `Transient` means "new per **resolution**." A constructor parameter is resolved
> exactly **once** (at construction), so you get one builder in a field and reuse it
> for both builds. To get "new per **use**" you must **resolve per use** — no object
> lifetime can deliver per-call freshness through a plain injected field. Picking
> `Transient` and thinking "new instance, so I'm safe" *ships* the bug, because the
> safety you imagined (per-use) isn't the safety the lifetime gives (per-resolution).

### Solution — inject a factory, not the builder

Inject `Func<ShapeBuilder>`; every call resolves a brand-new builder:

```csharp
public class ShapeService
{
    private readonly Func<ShapeBuilder> _builderFactory;

    public ShapeService(Func<ShapeBuilder> builderFactory) => _builderFactory = builderFactory;

    public (Shape, Shape) BuildBoth()
    {
        var circle   = _builderFactory().DefineShape("Circle").CalculateCircleArea(5)...Build();
        var triangle = _builderFactory().DefineShape("Triangle").CalculateTriangleArea(6, 3)...Build();
        //             ▲ fresh builder each call → no accumulation
        return (circle, triangle);
    }
}
```

Registration (Microsoft.Extensions.DI doesn't auto-generate `Func<T>`; Autofac and
others do):

```csharp
services.AddTransient<ShapeBuilder>();
services.AddTransient<Func<ShapeBuilder>>(sp => () => sp.GetRequiredService<ShapeBuilder>());
```

> **Deep-understanding note — a builder is the wrong shape for a long-lived
> dependency.** A builder is short-lived, single-use, and stateful — at odds with
> being injected and held in a field. So don't inject the builder; inject the
> *ability to make* builders (`Func<ShapeBuilder>` or a small `IShapeBuilderFactory`).
> The factory is safe to be long-lived; each builder it hands out is fresh. The
> static `Shape.Builder => new()` from §8 also fixes the bug, but a static `new()`
> can't be mocked/substituted, so it fights testability in a DI codebase — prefer the
> factory there.

---

## 10. "Functional" vs. Classic Builder

| Aspect | Classic Builder | Functional Builder (this) |
|--------|-----------------|---------------------------|
| Builder state | a half-built object | `List<Action<T>>` (a recipe) |
| Mutation timing | immediately, on each call | all at once, inside `Build()` |
| Add a new step | edit the builder class | write an extension method (new file) |
| Encapsulation | strong (state hidden) | weaker (action list is `public`) |
| Design driver | construct complex objects step by step | Open/Closed extensibility |
| Best for | a fixed, known set of steps | steps third parties must extend |

> **Caveat:** OCP is the reason behind *this variant*, not the Builder pattern as a
> whole. Builders in general exist to construct complex objects step by step; only
> the functional variant is *driven* by open/closed extensibility.

---

## 11. Relation to the Other Builders in This Repo

- **Functional (this)** → optimizes for **extension without modification**. Enforces
  nothing about order; new steps plug in from outside via extension methods.
- **Recursive generic / CRTP (Duck)** → optimizes for **any-order** fluent chaining;
  every method returns the most-derived `Self` type.
- **Stepwise (Chicken)** → optimizes for **forced-order, no-skip** construction;
  every method returns the *next* interface in a fixed chain.

Use a **functional** builder when the set of build steps is open-ended and you want
others to extend it. Use a **recursive generic** when all properties are optional and
order is free. Use a **stepwise** builder when certain fields are mandatory and must
be supplied in a specific order, checked at compile time.

---

## 12. Continuation / TODO

- ~~**Reset between products:** make `Build()` clear `ShapeBuildingActions`, or create
  a fresh `ShapeBuilder` per product, to remove the shared-recipe gotcha in §8.~~
  **Done** — `Build()` now calls `ShapeBuildingActions.Clear()` (the §8(b) fix). A
  fresh-builder-per-product entry point (§8(a)) remains a possible future improvement.
- **Remove boilerplate:** every step is `Actions.Add(...); return ...;`. The next
  evolution is a *generic* functional builder (recursive generic / CRTP) with a
  single `Do(...)` helper that records the action and returns `Self`, so plugins
  shrink to one line.
```