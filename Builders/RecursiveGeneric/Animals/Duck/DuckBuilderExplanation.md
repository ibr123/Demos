# DuckBuilder — A Self-Referential Generic (Recursive Generic / CRTP) Explained

This document walks through every file in the `Duck` folder, explains what each
class does, why the inheritance chain looks the way it does, why the
`DuckBuilder` class is necessary, and finally how the generic type parameter
`Self` propagates through method calls at runtime.

The goal is for **anyone** — even a junior developer who has never seen this
pattern — to be able to read this file from top to bottom and understand the
design.

---

## 1. The Big Picture — What Are We Trying To Build?

We want to construct a `Duck` object using **fluent method chaining** like this:

```csharp
Duck duck = Duck.Builder
                .Origin("Germany")
                .Wight(2.5)
                .AgeInMonths(6)
                .Color(DuckColors.White)
                .Build();
```

Three requirements drive the design:

1. **Any order.** The user should be able to call `.Color()`, `.Origin()`,
   `.AgeInMonths()`, `.Wight()` in **any order they like** — for example
   `.Color().Origin()` should work just as well as `.Origin().Color()`.
2. **No method "disappears" after a call.** After calling any method, every
   *other* builder method must still be available in IntelliSense / at
   compile time.
3. **Strongly typed.** No `object`, no `dynamic`, no runtime reflection. Every
   method returns the most concrete type so chaining stays type-safe.

The trick that makes all three requirements work is a pattern called the
**Curiously Recurring Template Pattern (CRTP)**, also called a
**recursive generic** or **F-bounded polymorphism**.

---

## 2. The Cast of Classes

The Duck folder contains eight `.cs` files. Two are plain data, one is a base
class, four are individual property setters, one is a top-level aggregator,
and one is the concrete builder we actually instantiate.

| File | Type | Role |
|------|------|------|
| `Duck.cs` | data class | The "product" — the thing being built |
| `DuckFeatures.cs` | abstract base | Owns the `Duck` instance and exposes `Build()` |
| `DuckAgeBuilder.cs` | generic builder | Adds `.AgeInMonths(int)` |
| `DuckOriginBuilder.cs` | generic builder | Adds `.Origin(string)` |
| `DuckWeightBuilder.cs` | generic builder | Adds `.Wight(double)` |
| `DuckColorBuilder.cs` | generic builder | Adds `.Color(DuckColors)` |
| `DuckBuildAggregator.cs` | generic aggregator | Anchors the top of the chain |
| `DuckBuilder.cs` | sealed concrete class | "Closes" the generic — what you actually `new` |

### 2.1 `Duck.cs` — The Product

```csharp
public class Duck
{
    public int AgeInMonths { get; set; }
    public double Weight { get; set; }
    public string? Origin { get; set; }
    public DuckColors Color { get; set; }

    public static DuckBuilder Builder => new();
}
```

This is the **target object** we want to construct. The static
`Builder` property is a convenience entry point so callers can write
`Duck.Builder.Origin(...)...` without doing `new DuckBuilder()` themselves.

### 2.2 `DuckFeatures.cs` — The Base of the Chain

```csharp
public abstract class DuckFeatures
{
    protected Duck duck = new();
    public Duck Build()
    {
        return duck;
    }
}
```

This is the **root** of the inheritance chain. Everyone in the chain shares
one important piece of state:

* `protected Duck duck` — a single `Duck` instance that every builder method
  in the chain mutates.

It also exposes `Build()`, which simply returns the duck. Because every
builder in the chain eventually inherits from `DuckFeatures`, every builder
automatically gets `.Build()` and a shared `duck` field — no duplication.

It is `abstract` because there is no reason to ever instantiate
`DuckFeatures` directly; it exists only to be inherited.

### 2.3 The Four Property Builders

These four classes are nearly identical in shape — each one adds a single
fluent setter for one property of the duck.

```csharp
public class DuckAgeBuilder<Self> : DuckFeatures
    where Self : DuckAgeBuilder<Self>
{
    public Self AgeInMonths(int ageInMonths)
    {
        duck.AgeInMonths = ageInMonths;
        return (Self)this;
    }
}
```

Three things to notice:

1. **It's generic in `Self`.** `Self` is a placeholder type that represents
   "whatever the most-derived class in this hierarchy actually is."
2. **It has a self-referential constraint** —
   `where Self : DuckAgeBuilder<Self>`. This says: "`Self` must itself
   inherit from `DuckAgeBuilder<Self>`." That is what makes the cast
   `(Self)this` legal — the compiler is told `this` really is a `Self`.
3. **It returns `Self`, not `DuckAgeBuilder<Self>`.** This is the whole
   point of the pattern. The return type lets you keep chaining as if you
   never went up the inheritance hierarchy. We'll see why in section 5.

#### What does "valid subclass" mean in the constraint?

The constraint `where Self : DuckAgeBuilder<Self>` is a **rule the compiler
enforces** about which types are allowed to be substituted for `Self`. A type
is **valid** only if it satisfies that rule — i.e. a type `X` is valid only if
`X : DuckAgeBuilder<X>` is true (read: `X` **IS-A** `DuckAgeBuilder<X>`).

```csharp
// ✅ VALID — DuckBuilder inherits from the chain that closes on itself.
//    Is DuckBuilder a DuckAgeBuilder<DuckBuilder>? Yes → allowed.
public sealed class DuckBuilder : DuckBuildAggregator<DuckBuilder> { }

// ❌ INVALID — these are the "arbitrary types".
DuckAgeBuilder<int>          // Is int a DuckAgeBuilder<int>?        No → compile error
DuckAgeBuilder<string>       // Is string a DuckAgeBuilder<string>? No → compile error
DuckAgeBuilder<HttpClient>   // Unrelated type                      → compile error
```

**Why the rule exists:** every setter ends with `return (Self)this;`. At
runtime `this` is always a `DuckBuilder`-family object. Without the constraint,
`Self` could be `int`, `string`, or `HttpClient`, and `(int)this` /
`(string)this` would be meaningless — a guaranteed runtime failure. The
constraint bans those types *up front*, so `Self` can only ever be a type that
`this` actually **is**, making the cast always safe.

> **In one line:** "valid" = a type that lives inside this builder's own
> inheritance family (it satisfies `Self : DuckXxxBuilder<Self>`), as opposed
> to an "arbitrary type" like `int` or `string` that has nothing to do with the
> builder and would make `(Self)this` nonsense. The constraint is what turns
> `Self` from "any type at all" (like a plain `T`) into "specifically a type
> from my own hierarchy" — which is the entire reason this trick is type-safe
> rather than a pile of unchecked casts.

The other three (`DuckOriginBuilder`, `DuckWeightBuilder`, `DuckColorBuilder`)
follow the exact same recipe but each one stacks on top of the previous one:

```text
DuckColorBuilder<Self>  : DuckWeightBuilder<Self>
DuckWeightBuilder<Self> : DuckOriginBuilder<Self>
DuckOriginBuilder<Self> : DuckAgeBuilder<Self>
DuckAgeBuilder<Self>    : DuckFeatures
```

So the **linear chain** of single inheritance is:

```text
DuckFeatures
  ▲
  │
DuckAgeBuilder<Self>
  ▲
  │
DuckOriginBuilder<Self>
  ▲
  │
DuckWeightBuilder<Self>
  ▲
  │
DuckColorBuilder<Self>
  ▲
  │
DuckBuildAggregator<Self>
  ▲
  │
DuckBuilder            ← sealed, concrete, no generic
```

C# does **not** support multiple inheritance of classes — you can only
inherit from one base class. But by **stacking single inheritance vertically**
we get something that *behaves* like multiple inheritance: a single
`DuckBuilder` instance ends up with every method (`AgeInMonths`, `Origin`,
`Wight`, `Color`, `Build`) glued together into one type.

### 2.4 `DuckBuildAggregator.cs` — The Top of the Generic Chain

```csharp
public class DuckBuildAggregator<Self> : DuckColorBuilder<Self>
    where Self : DuckBuildAggregator<Self>
{
}
```

This class adds no new methods. Its job is purely structural — it provides
**one single named "top" of the generic chain** so you don't have to remember
which specific builder happens to be the topmost one when you write
`DuckBuilder`. If tomorrow someone adds `DuckHabitatBuilder` and slots it in,
only `DuckBuildAggregator` needs to be updated to inherit from it; the rest
of the world doesn't care.

It also tightens the constraint: `where Self : DuckBuildAggregator<Self>` —
ensuring `Self` is "all the way up" the chain, not stuck partway.

### 2.5 `DuckBuilder.cs` — The Concrete Class

```csharp
public sealed class DuckBuilder : DuckBuildAggregator<DuckBuilder>
{
}
```

This tiny class is the linchpin of the entire design.

* It is **sealed** — nobody can inherit from it. That guarantees `DuckBuilder`
  really is the "leaf" of the inheritance tree.
* It is **non-generic** — you can write `new DuckBuilder()` directly.
* It **closes the generic loop** by substituting itself for `Self`:
  `DuckBuildAggregator<DuckBuilder>`.

We will dedicate section 4 to explaining why this class is necessary.

---

## 3. Why Is `DuckFeatures` Abstract and Not a Plain Class?

Making `DuckFeatures` abstract adds **zero runtime behavior** — it has no abstract
methods that subclasses are forced to implement. A plain `class` would compile and
run identically. The sole effect of `abstract` here is to make
`new DuckFeatures()` a **compile-time error**.

### Why that matters

`DuckFeatures` on its own is useless to a caller:

```csharp
var f = new DuckFeatures(); // if this were allowed...
f.Build();                  // returns an empty Duck with all default values
                            // no .Origin(), .Color(), etc. — those live on subclasses
```

You'd get a `Duck` with nothing set and no way to set anything. Allowing that call
would silently succeed and produce a broken result.

### `abstract` as a design signal

It tells anyone reading the code: **this class is infrastructure, not an API.**
Don't instantiate it, don't expose it — just inherit from it. That is a stronger
signal than a comment, enforced by the compiler.

| | `abstract class` | plain `class` |
|---|---|---|
| Can be instantiated? | No (compile error) | Yes (but pointless) |
| Pattern still works? | Yes | Yes |
| Communicates intent? | Clearly | Only by convention |

---

## 4. Why Use This Chain of Inheritance At All?

"Why not just put all four setter methods in one big builder class?"

You absolutely could. The reason the design is split into one builder per
property is **modularity and reuse**:

* Each builder class is a tiny, focused unit responsible for **exactly one
  field**. Easy to read, easy to test, easy to delete.
* If you later add a new property (say, `BeakLength`), you only write a new
  `DuckBeakBuilder<Self> : DuckColorBuilder<Self>` and slot it into the
  chain. You don't touch any of the other builder classes.
* The chain also documents the design: each link is one responsibility.

The pattern combines two ideas:

1. **Chain of single inheritance** so all behaviors stack into one final
   type (gives you the "multiple inheritance" effect without C#'s multiple
   inheritance restriction).
2. **Self-referential generic (`Self`)** so every builder method returns
   *the most-derived type* — which means the chain doesn't "narrow" as you
   call methods. (More on this in section 5.)

---

## 4. Why is `DuckBuilder` Necessary?

A reasonable question is: "Can't we just use `DuckBuildAggregator<Self>`
directly?"

No, and there are two compelling reasons.

### Reason 1 — You Cannot Instantiate an Open Generic

`DuckBuildAggregator<Self>` has an **unresolved type parameter** `Self`.
At some point a concrete value has to be substituted for `Self`. You cannot
write `new DuckBuildAggregator<Self>()` in your `Main` method because `Self`
is not a real type — it is a placeholder.

You also can't substitute `DuckBuildAggregator<DuckBuildAggregator<...>>`
because that would expand forever — the type would never terminate.

So we **need one concrete type** that says "I am the final answer to
`Self`." That type is `DuckBuilder`:

```csharp
public sealed class DuckBuilder : DuckBuildAggregator<DuckBuilder> { }
```

By doing this, `Self` is fully resolved (`Self = DuckBuilder`), and every
generic method in the chain finally has a concrete return type.

### Reason 2 — The Constraint Has To Be Satisfied

Every builder in the chain has a constraint of the form
`where Self : SomeBuilder<Self>`. The compiler verifies, at the point where
you close the generic, that this constraint actually holds.

When we write `DuckBuildAggregator<DuckBuilder>`:

* The constraint requires `DuckBuilder : DuckBuildAggregator<DuckBuilder>`.
* That is true — because `DuckBuilder` is *literally defined as*
  `: DuckBuildAggregator<DuckBuilder>`.

The constraint is satisfied **only because `DuckBuilder` exists and inherits
correctly**. Without `DuckBuilder`, no concrete type satisfies the
constraint, and the chain is unusable.

### Bonus — A Clean Public API

There's also an ergonomic benefit. Users of your library don't write
`new DuckBuildAggregator<DuckBuilder>()`; they write `new DuckBuilder()` (or
`Duck.Builder`). The recursive-generic mechanics are *implementation
detail*, hidden behind one short, friendly name.

---

## 5. How `Self` Propagates At Runtime — A Step-By-Step Trace

This is the part that most people find confusing. Let's trace exactly what
happens when this code runs:

```csharp
Duck duck = Duck.Builder
                .Origin("Germany")
                .Wight(2.5)
                .AgeInMonths(6)
                .Color(DuckColors.White)
                .Build();
```

> **Key idea:** At runtime there is only **one object** — a `DuckBuilder`
> instance. The chain of method calls returns *the same instance* every
> time, just typed differently to the *compiler*. There is no copying,
> no recursion, no expanding type tree at runtime — those concerns are
> 100 % compile-time.

### Step 0 — Resolving `Self`

When the compiler sees `DuckBuilder : DuckBuildAggregator<DuckBuilder>`, it
substitutes `Self = DuckBuilder` into **every base class** in the chain.
Inside `DuckBuilder`, the inherited definitions effectively become:

```text
DuckFeatures                               // no generic
DuckAgeBuilder<DuckBuilder>                // Self = DuckBuilder
DuckOriginBuilder<DuckBuilder>             // Self = DuckBuilder
DuckWeightBuilder<DuckBuilder>             // Self = DuckBuilder
DuckColorBuilder<DuckBuilder>              // Self = DuckBuilder
DuckBuildAggregator<DuckBuilder>           // Self = DuckBuilder
DuckBuilder
```

Notice that `Self` is **always `DuckBuilder`** — it does *not* change as you
go down the chain. That's the whole reason every method ends up returning
`DuckBuilder`.

This was actually the **bug** in the previous (broken) version: each level
wrapped `Self` in a new generic instead of forwarding it. That made `Self`
mean something different at every level, and methods returned narrow types
that couldn't see the full builder surface. The fix was to pass `Self`
straight through.

### Step 1 — `Duck.Builder`

```csharp
public static DuckBuilder Builder => new();
```

This creates exactly one heap object of type `DuckBuilder`. Let's call it
`b`. The static type of `Duck.Builder` is `DuckBuilder`.

```text
Stack frame after step 1:
    b : DuckBuilder   ───►   (heap)  DuckBuilder { duck = new Duck() }
```

### Step 2 — `.Origin("Germany")`

Compile time:
* The compiler looks up `Origin` on type `DuckBuilder`. It finds it inherited
  from `DuckOriginBuilder<DuckBuilder>` (because `Self = DuckBuilder`).
* The signature it sees is `DuckBuilder Origin(string origin)`.

Run time:
* Method body runs: `duck.Origin = "Germany";`
* `(Self)this` — i.e. `(DuckBuilder)this` — is just a *cast*, not a copy.
  The same object `b` is returned.

```text
After step 2:
    return value : DuckBuilder
    (heap)       :  DuckBuilder { duck = { Origin = "Germany" } }
                    ▲
                    └── still the same single object `b`
```

### Step 3 — `.Wight(2.5)`

Compile time:
* `Wight` is found on `DuckWeightBuilder<DuckBuilder>`.
* Signature: `DuckBuilder Wight(double weight)`.

Because the return type is **still `DuckBuilder`**, the compiler is happy to
let you chain *any* other builder method next — including ones from
"higher" or "lower" in the chain. This is the property that the broken
version lost.

Run time:
* `duck.Weight = 2.5;`
* Returns `(DuckBuilder)this` → same object `b`.

### Step 4 — `.AgeInMonths(6)`

Compile time:
* `AgeInMonths` is on `DuckAgeBuilder<DuckBuilder>`.
* Signature: `DuckBuilder AgeInMonths(int ageInMonths)`.

Run time:
* `duck.AgeInMonths = 6;`
* Returns the same object `b`.

### Step 5 — `.Color(DuckColors.White)`

Compile time:
* `Color` is on `DuckColorBuilder<DuckBuilder>`.
* Signature: `DuckBuilder Color(DuckColors color)`.

Run time:
* `duck.Color = DuckColors.White;`
* Returns the same object `b`.

### Step 6 — `.Build()`

Compile time:
* `Build` is on `DuckFeatures`, inherited unchanged.
* Signature: `Duck Build()`.

Run time:
* Returns the `duck` field that has been mutated through every step.

### The Resulting Picture

```text
                    ┌─────────────────────────────────────────────────┐
                    │              ONE DuckBuilder object             │
                    │                                                 │
                    │   duck = Duck {                                 │
                    │       Origin      = "Germany"                   │
                    │       Weight      = 2.5                         │
                    │       AgeInMonths = 6                           │
                    │       Color       = DuckColors.White            │
                    │   }                                             │
                    └─────────────────────────────────────────────────┘
                                          │
                                  Build() │ returns duck
                                          ▼
                                       Duck
```

Every step returned **the same object**. The "magic" of the recursive
generic is entirely at *compile time* — it teaches the compiler that the
type didn't narrow, so chaining keeps working.

---

## 6. Summary Cheat Sheet

* `Duck` — the product. Holds the data.
* `DuckFeatures` — abstract base. Owns the shared `Duck duck` field and
  `Build()`.
* `DuckAgeBuilder<Self>` / `DuckOriginBuilder<Self>` /
  `DuckWeightBuilder<Self>` / `DuckColorBuilder<Self>` — each adds **one**
  fluent setter. Each one inherits from the next in the chain, so a
  `DuckColorBuilder<Self>` transitively has all four setters plus `Build()`.
* `DuckBuildAggregator<Self>` — empty class that acts as the named "top"
  of the generic chain and tightens the `Self` constraint.
* `DuckBuilder` — the **concrete, sealed, non-generic** class that closes
  `Self` to itself. Without it, you couldn't use the chain — `Self` would
  forever be a placeholder.
* The chain of single inheritance gives you **all methods composed into
  one type** (the C# substitute for multiple inheritance).
* The recursive generic (`Self : DuckXxxBuilder<Self>`) ensures every
  builder method returns the **most-derived** type, so chaining never
  narrows — every method stays callable in any order.
* At **runtime**, all the type acrobatics vanish: there is exactly one
  `DuckBuilder` instance, every method mutates its internal `duck`, and
  `Build()` hands that `duck` back at the end.
