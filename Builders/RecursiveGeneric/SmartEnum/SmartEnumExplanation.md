# SmartEnum — A Type-Safe, Behavior-Rich Alternative to C# `enum`

This document walks through every file in the `SmartEnum` folder, explains what
each class does, why the design uses a self-referential generic
(CRTP / recursive generic), and how the pieces work together at compile time
and at runtime.

It is written so that a developer who has never seen the pattern can read it
end-to-end and understand the design. It is also written so that **new
sections can be appended later** without breaking the existing flow — see the
"Continuation" placeholder at the bottom.

If you have not yet read `Builders/RecursiveGeneric/Animals/Duck/DuckBuilderExplanation.md`,
that file covers the same recursive-generic mechanic in a different context
(builder pattern). Reading it first is helpful but not required; the relevant
mechanics are re-explained here in the enum context.

---

## 1. The Big Picture — What Problem Does SmartEnum Solve?

C#'s built-in `enum` is a thin wrapper over an integer. That gives us
exhaustive switch coverage and a nice name in the debugger, but it has real
weaknesses:

1. **No behavior.** You cannot attach methods to an `enum` value. To compute
   "what comes after `Pending`?" you typically write a switch or extension
   method *outside* the enum.
2. **No extra data.** An `enum` value carries one integer. If you need a
   display name, an associated icon, or a numeric weight, you have to pair
   the enum with parallel arrays, attributes, or lookup tables.
3. **Weak invalid-value protection.** Any `int` can be cast to any enum,
   even values that aren't declared. `(OrderStatus)999` compiles and runs.
4. **No polymorphism.** Two enums that share a concept (e.g., "states with
   transitions") can't share an interface or base method.

The **SmartEnum pattern** addresses all four by modeling each "enum value" as
a `static readonly` instance of a sealed class. The class itself can carry
fields, methods, validation, and inheritance. We then use a recursive generic
base type (`Enumeration<Self>`) so the base can provide rich behavior —
`GetAll()`, `FromId()`, `FromName()`, `Next()` — that automatically returns
the **concrete derived type**, not the base.

The example in this folder demonstrates the pattern with an `OrderStatus`
smart enum that supports:

```csharp
foreach (OrderStatus s in OrderStatus.GetAll()) { ... }     // discover all values
OrderStatus shipped   = OrderStatus.FromId(3);              // lookup by id
OrderStatus delivered = OrderStatus.FromName("Delivered");  // lookup by name
OrderStatus next      = OrderStatus.Pending.Next();         // ordered traversal
```

All four of those methods are defined **once** on the base class
`Enumeration<Self>` and return the concrete `OrderStatus` type — not an
`object`, not an `Enumeration<OrderStatus>`. That is the whole point of the
recursive generic.

---

## 2. The Cast of Classes

The folder contains two `.cs` files. One is the reusable base, one is a
concrete smart enum that demonstrates it.

| File | Type | Role |
|------|------|------|
| `Enumeration.cs` | abstract generic base | Provides `Id`, `Name`, equality, lookup, `Next()`, `GetAll()` |
| `OrderStatus.cs` | sealed concrete class | A specific smart enum with five values |

### 2.1 `Enumeration.cs` — The Reusable Base

```csharp
public abstract class Enumeration<Self> : IComparable
    where Self : Enumeration<Self>
{
    public int    Id   { get; }
    public string Name { get; }

    protected Enumeration(int id, string name) { Id = id; Name = name; }
    ...
}
```

Four things to notice up front:

1. **It is generic in `Self`.** `Self` is a placeholder for "whatever concrete
   smart enum is deriving from me" — `OrderStatus`, `Priority`, `Country`,
   whatever.
2. **It is constrained with `where Self : Enumeration<Self>`.** This is the
   recursive constraint: `Self` must itself derive from `Enumeration<Self>`.
   That is what allows the base class to return `Self` from its methods
   safely.
3. **It is `abstract`.** It cannot be instantiated directly. The only way to
   use it is to derive a concrete class from it. (See section 3 for why
   `abstract` matters here.)
4. **Its constructor is `protected`.** Only subclasses can call it, which
   means smart enum *values* can only be created from inside their own class.

The class provides four kinds of behavior:

* **Identity:** `Id` and `Name` (read-only — set once at construction).
* **Discovery:** `GetAll()` uses reflection to find every `static`,
  `public`, `Self`-typed field declared on the concrete type, and returns
  them as `IEnumerable<Self>`.
* **Lookup:** `FromId(int)` and `FromName(string)` walk `GetAll()` and throw
  if no value matches — preventing the "invalid cast" problem of C# enums.
* **Equality, comparison, traversal:** `Equals`/`GetHashCode` based on
  `Id` + concrete `GetType()`, `CompareTo` based on `Id`, and `Next()` which
  walks the values in `Id`-ordered cyclic order.

### 2.2 `OrderStatus.cs` — A Concrete Smart Enum

```csharp
public sealed class OrderStatus : Enumeration<OrderStatus>
{
    public static readonly OrderStatus Pending   = new(1, "Pending");
    public static readonly OrderStatus Paid      = new(2, "Paid");
    public static readonly OrderStatus Shipped   = new(3, "Shipped");
    public static readonly OrderStatus Delivered = new(4, "Delivered");
    public static readonly OrderStatus Cancelled = new(5, "Cancelled");

    private OrderStatus(int id, string name) : base(id, name) { }
}
```

This tiny class is everything a "smart enum" needs:

* **`sealed`** — nobody can derive further. That guarantees `OrderStatus` is
  the leaf type and matches the `Self : Enumeration<Self>` constraint
  cleanly.
* **`: Enumeration<OrderStatus>`** — closes the recursive generic by
  substituting `OrderStatus` for `Self`. From now on every inherited method
  whose return type was `Self` becomes a method returning `OrderStatus`.
* **`static readonly` fields** — the *values*. They are created exactly once,
  when the type is first touched, and never change afterward.
* **`private` constructor** — outsiders cannot create new `OrderStatus`
  instances. The set of values is fixed and lives inside the class.

The whole file is twelve lines. That is the productivity payoff of building
the heavy machinery once in `Enumeration<Self>`.

---

## 3. Why Is `Enumeration<Self>` Abstract and Not a Plain Class?

Making `Enumeration<Self>` abstract adds **zero runtime behavior** — it has no
abstract methods that subclasses are forced to implement. A plain `class`
would compile and run identically. The sole effect of `abstract` here is to
make `new Enumeration<...>(...)` a compile-time error.

### Why that matters

`Enumeration<Self>` on its own represents nothing meaningful — there is no
such concept as "an enumeration value that doesn't belong to a specific
enum." Every actual smart enum value belongs to *some* concrete derived type
(`OrderStatus.Pending`, `Priority.High`, etc.). Allowing
`new Enumeration<OrderStatus>(99, "Bogus")` from anywhere would silently
let callers fabricate values outside the closed set the class defines.

The `protected` constructor already restricts construction to subclasses, but
`abstract` makes the intent explicit at the *type* level: this is
infrastructure, not an API.

| | `abstract class` | plain `class` |
|---|---|---|
| Can be instantiated? | No (compile error) | Yes (but pointless / dangerous) |
| Pattern still works? | Yes | Yes |
| Communicates intent? | Clearly | Only by convention |

---

## 4. Why a Recursive Generic? Why Not Just `Enumeration` (Non-Generic)?

A plausible-looking alternative is:

```csharp
public abstract class Enumeration
{
    public int Id { get; }
    public string Name { get; }
    public static IEnumerable<Enumeration> GetAll() { ... }
    public static Enumeration FromId(int id) { ... }
}
```

That works — but every API returns `Enumeration`, not `OrderStatus`. Callers
have to cast:

```csharp
OrderStatus s = (OrderStatus)Enumeration.FromId(3);  // ugh
```

Worse, the static `GetAll()` on the base can't know which concrete type the
caller meant — there is no `Self` to look up. You'd have to pass a `typeof`,
or expose only an instance method, or push `GetAll` down into every subclass.

The recursive generic fixes both at once. By declaring:

```csharp
public abstract class Enumeration<Self> : IComparable
    where Self : Enumeration<Self>
```

…the base class learns the concrete type as a compile-time parameter. Inside
the base class, **`Self` means `OrderStatus` whenever the call is made from
an `OrderStatus` context**, so:

* `GetAll()` returns `IEnumerable<OrderStatus>` — no cast required.
* `FromId(3)` returns `OrderStatus` — no cast required.
* `Pending.Next()` returns `OrderStatus` — no cast required.

The constraint `Self : Enumeration<Self>` is the only thing that makes the
internal `(Self)f.GetValue(null)!` cast type-safe and the method bodies
expressible at all.

If the constraint surprises you, see section 5 of
`DuckBuilderExplanation.md` for a step-by-step trace of how `Self` gets
resolved when the compiler sees `class OrderStatus : Enumeration<OrderStatus>`.
The mechanics are identical here — `Self` is fixed to `OrderStatus` at the
point of declaration, and every method on the base "sees" that substitution
permanently.

---

## 5. Inside `Enumeration<Self>` — Each Method, In Detail

### 5.1 `GetAll()` — Reflective Discovery

```csharp
public static IEnumerable<Self> GetAll()
{
    return typeof(Self)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(f => typeof(Self).IsAssignableFrom(f.FieldType))
        .Select(f => (Self)f.GetValue(null)!);
}
```

What it does, line by line:

* `typeof(Self)` — at runtime this is `typeof(OrderStatus)` (or whatever
  concrete type closed the generic).
* `.GetFields(...)` with `Public | Static | DeclaredOnly` — finds the
  `public static readonly OrderStatus Pending = ...;` style fields.
  * `Public` — only fields a caller could see.
  * `Static` — only class-level fields, not instance fields.
  * `DeclaredOnly` — only fields declared on `OrderStatus` itself, not
    anything inherited. (Without this you'd accidentally pick up base-class
    statics too.)
* `.Where(f => typeof(Self).IsAssignableFrom(f.FieldType))` — keeps only
  fields whose type is `OrderStatus` (or a subtype). Filters out unrelated
  static fields you may have declared.
* `.Select(f => (Self)f.GetValue(null)!)` — reads each static field's value
  and casts it to `Self`. `null` is the "no instance" target because the
  fields are `static`. The `!` suppresses the nullable warning — these
  fields are initialized at type load, so they cannot be null in practice.

**Why fields and not properties?** A `static readonly` field initializer
runs exactly once when the class is loaded, in declaration order, and
produces a single canonical instance per value. A static property would
either run code every call (wrong) or back onto a field anyway (same
thing). The pattern is universally implemented with fields.

**Cost note.** `GetFields` uses reflection on every call to `GetAll()`,
`FromId`, `FromName`, and `Next()`. For an `OrderStatus` with five values
this is negligible; if you ever need it to be free, cache the result in a
private static array. (See section "Continuation" for ideas.)

### 5.2 `FromId(int)` and `FromName(string)` — Validated Lookup

```csharp
public static Self FromId(int id) {
    Self? match = GetAll().FirstOrDefault(item => item.Id == id);
    if (match is null)
        throw new InvalidOperationException($"No {typeof(Self).Name} with Id = {id}");
    return match;
}
```

* Returns the matching value or throws — no silent "unknown value" like a
  cast-from-int would produce.
* `typeof(Self).Name` is included in the error message so the exception
  pinpoints both *which* smart enum failed and *which* id/name was
  unrecognized.
* `FromName` is the symmetric counterpart, matching on `Name` instead of
  `Id`. It is case-sensitive by design; if you want case-insensitive lookup,
  change the comparison to `string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)`.

### 5.3 `Equals` / `GetHashCode` — Value Equality, Type-Safe

```csharp
public override bool Equals(object? obj) {
    if (obj is not Enumeration<Self> other) return false;
    return GetType() == obj.GetType() && Id == other.Id;
}
public override int GetHashCode() => Id.GetHashCode();
```

Two values are equal iff they have the **same concrete type** *and* the
**same `Id`**. The `GetType()` check matters: without it, an
`OrderStatus` with `Id == 1` and a hypothetical `Priority` with `Id == 1`
would compare equal at the base-class level. Comparing `GetType()` keeps
different smart enums in different equivalence classes.

In practice you rarely need `Equals` because every smart enum value is a
singleton — reference equality (`==`) is already correct between
`OrderStatus.Pending` and `OrderStatus.Pending`. But the override is
defensive: if a value ever comes back from serialization or reflection as a
*different* instance with the same `Id`, equality still works.

### 5.4 `CompareTo` — Ordering By `Id`

```csharp
public int CompareTo(object? other) => Id.CompareTo(((Enumeration<Self>)other!).Id);
```

Makes smart enums sortable, so `OrderStatus.GetAll().OrderBy(x => x)` works.
The cast assumes you compare like with like — comparing an `OrderStatus` to
a `Priority` will throw `InvalidCastException`, which is the right behavior.

### 5.5 `Next()` — Cyclic Traversal

```csharp
public Self Next() {
    List<Self> all = GetAll().OrderBy(e => e.Id).ToList();
    int currentIndex = all.FindIndex(e => e.Id == Id);
    return all[(currentIndex + 1) % all.Count];
}
```

Returns the next value in `Id` order, wrapping around at the end:
`Cancelled.Next() == Pending`. Useful for state-machine demos and round-robin
selection.

The wraparound is intentional — if you want a state machine where
`Cancelled` is a terminal state, override `Next()` in the derived class to
return `this`, or model transitions explicitly as a dictionary on the
derived type (a natural extension; see "Continuation").

---

## 6. How `Self` Resolves At Runtime — Short Trace

Tracing the example from `Program.cs`:

```csharp
foreach (OrderStatus status in OrderStatus.GetAll()) { ... }
OrderStatus current = OrderStatus.Pending;
OrderStatus next    = current.Next();
```

* `OrderStatus.GetAll()`. Static method inherited from
  `Enumeration<OrderStatus>` (because `OrderStatus : Enumeration<OrderStatus>`).
  Inside the method, `typeof(Self)` is `typeof(OrderStatus)`. Reflection
  picks up the five `static readonly` fields and casts each to `OrderStatus`.
  The return type is `IEnumerable<OrderStatus>`, so the `foreach` variable
  can be typed `OrderStatus` with no cast.
* `OrderStatus.Pending`. A direct field read — same instance every time. No
  allocation. The static initializer for `OrderStatus` ran once, the first
  time the class was touched, and built all five singletons.
* `current.Next()`. Inherited instance method. Internally calls `GetAll()`,
  sorts by `Id`, finds `current` at index 0, and returns the element at
  index 1 — `OrderStatus.Paid`. The compile-time return type is `Self`,
  which (because `Self = OrderStatus` for this class) is `OrderStatus`. No
  cast needed by the caller.

At **runtime** there is nothing exotic happening. Every smart enum value is
a singleton object on the heap, created during static initialization. Every
"smart" method either walks those singletons (`GetAll`/`FromId`/`FromName`/
`Next`) or asks a single one about itself (`Equals`, `CompareTo`,
`ToString`).

---

## 7. Comparison Cheat Sheet — `enum` vs SmartEnum

| Concern | C# `enum` | SmartEnum |
|---|---|---|
| Storage | one `int` (or backing integral) | a singleton object reference |
| Behavior on the value | none — must be external | any method you write on the class |
| Extra data per value | none (or via attributes) | any field on the class |
| Invalid values | `(MyEnum)999` is legal | only the declared `static readonly` values exist |
| Discovery (`GetAll`) | `Enum.GetValues(typeof(T))` (boxed) | `T.GetAll()` (typed) |
| Lookup by id/name | `Enum.Parse`/`Enum.IsDefined` | `T.FromId`/`T.FromName` (throws on miss) |
| Polymorphism | none | inherits from a generic base, can implement interfaces |
| Allocations per value | 0 | 1 at type load, then reused forever |
| Switch exhaustiveness | compiler-checked with `enum` | not checked — handle the default case yourself |

The big trade-off is the loss of compiler-checked switch exhaustiveness.
When you switch on an `OrderStatus`, the compiler will not warn you if you
forget `Cancelled`. The usual mitigation is a switch expression with a
default branch that throws, plus a unit test that exercises every value.

---

## 8. Summary Cheat Sheet

* `Enumeration<Self>` — abstract generic base, recursive-generic constrained
  with `where Self : Enumeration<Self>`. Holds `Id`/`Name`, equality,
  comparison, reflective `GetAll`, validated `FromId`/`FromName`, cyclic
  `Next()`.
* `OrderStatus : Enumeration<OrderStatus>` — sealed concrete class that
  closes the generic to itself. Declares its values as `public static
  readonly OrderStatus` fields with a `private` constructor so no outside
  code can mint new values.
* The recursive generic lets the base's static and instance methods return
  **the concrete derived type** (no casts on the caller side).
* At runtime there is no magic: each value is a singleton built at type
  load; the "smart" methods walk those singletons via reflection.
* SmartEnum trades the compiler-checked switch exhaustiveness of `enum` for
  arbitrary behavior, extra data, validation, and polymorphism.

---

## 9. Continuation

This file is intentionally structured so that future material can be
appended below this line without renumbering or rewriting earlier sections.
Likely extensions, in rough order of usefulness:

* **Caching `GetAll`** to avoid repeated reflection.
* **Adding extra fields** per value (e.g., a display string, an allowed
  transitions set) and exposing them through the base.
* **Implementing `IEquatable<Self>`** for allocation-free equality and
  better generic-collection performance.
* **State-machine variant** — overriding `Next()` per derived class, or
  modeling transitions explicitly via a `Dictionary<Self, Self[]>`.
* **JSON / EF Core serialization** — converting smart enums to and from
  their `Id` or `Name` across persistence boundaries.
* **Comparison with `Ardalis.SmartEnum`** and other library
  implementations.

Add new sections under `## 10. …`, `## 11. …`, etc. — do not edit sections
1–8 unless the underlying example code changes.
