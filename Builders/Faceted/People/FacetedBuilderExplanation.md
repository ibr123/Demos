# PersonBuilder — The Faceted Builder Pattern Explained

This document walks through **every file** in the `Faceted/People` folder, explains
what each type and member does at the **code level**, and shows how a single facade
hands out several small *facet* sub-builders that all mutate **one shared `Person`**.

The goal is for **anyone** — even a junior developer who has never seen this
pattern — to read this file top to bottom and understand the design.

> There is also a prose companion, `FacetedBuilderNotes.txt`, in this same folder.
> That file is the conceptual summary; **this** file is the code-anchored walkthrough
> (member by member, with the real source). They overlap on purpose — read either
> alone and you're covered.

This is a *different* design goal from the other builders in this repo:

- The **Duck** builder (`RecursiveGeneric/Animals/Ducks/DuckBuilderExplanation.md`)
  optimizes for **any-order** chaining via a self-referential generic (CRTP).
- The **Chicken** builder (`Stepwise/Animals/Chickens/ChickenStepwiseBuilderExplanation.md`)
  optimizes for **forced-order** chaining via per-step interfaces.
- The **Shape** builder (`Functional/Shapes/FunctionalBuilderExplanation.md`)
  optimizes for **open/closed extensibility** via extension-method plugins.
- The **Person** builder (this one) optimizes for **ergonomic grouping** — many
  properties split into natural *aspects* (facets), each with its own tiny builder,
  reachable through one fluent chain.

> **Naming — this is the FACETED builder, not the GoF FACADE.** "Faceted builder"
> is the standard name: one product is built through several separate *aspects*
> (facets). The root `PersonBuilder` is loosely described as a "facade" (a single
> front door that hands out the facet sub-builders), but that is a description of its
> *role*, not a second name for the pattern — and it is unrelated to the GoF **Facade**
> pattern. The name similarity is a coincidence.

---

## 1. The Big Picture — What Are We Trying To Build?

From `Program.cs`, we construct a `Person` with a single fluent chain that *hops
between two facets* — address and job:

```csharp
PersonBuilder pb = new();
Person person = pb
    .Address.At(city: "Amman", postCode: "007799", streetName: "Yajooz")
    .Job.At("Microsoft")
        .ProfessionAs("Developer")
        .Makes(10);
Console.WriteLine(person);
```

The driving requirement:

1. **Group a wide object into aspects.** `Person` has six properties that fall into
   two natural groups — *address* (`StreetName`, `PostCode`, `City`) and *employment*
   (`CompanyName`, `JobField`, `AnnualIncome`). One flat fluent builder with six
   methods would be an unwieldy wall; facets keep each sub-builder small.
2. **Hop between facets in one chain.** `.Address...` then `.Job...` then back again
   if you like — all in a single expression.
3. **All facets build the *same* object.** Every facet writes into one shared
   `Person`; at the end you get that one composed object.

The pattern that delivers this is the **Faceted Builder**.

> **Deep-understanding note — the design DRIVER is *ergonomic grouping*, not
> enforcement.** The test: "If I delete the grouping benefit, does this design still
> make sense?" No — if `Person` had three properties, a plain fluent builder would be
> simpler and this facade+subclass machinery would buy nothing. Everything below (the
> facade, the facet sub-builders, the inheritance trick) exists to make *grouped,
> re-enterable* construction read like prose. Notably, the pattern enforces **nothing**
> about order or completeness (contrast the Stepwise/Chicken builder, whose *entire*
> reason for existing is enforcement).

---

## 2. The Core Idea — One Shared Product, Many Small Builders

A classic fluent builder is a single class with every setter on it. A **faceted**
builder splits those setters across several sub-builders, one per aspect, and uses a
**facade** to hand them out:

```
                     PersonBuilder (facade / root)
                     owns:  Person person   ← the ONE product
                       │
        ┌──────────────┴───────────────┐
        │ .Address                      │ .Job
        ▼                               ▼
PersonAddressFacetBuilder        PersonJobFacetBuilder
  .At(city, postCode, street)      .At(company)
                                   .ProfessionAs(profession)
                                   .Makes(income)
        │                               │
        └───────── both mutate the SAME Person ──────────┘
```

The two moving parts that make this work:

1. **A single shared `Person`.** The facade allocates it once; every facet builder
   receives *that same reference* and mutates it. (See §7 — this is the crux, and
   exactly where the pattern silently breaks if you get it wrong.)
2. **Facet builders inherit the facade.** Because each facet builder *is-a*
   `PersonBuilder`, each one *also* exposes `.Address` and `.Job` — which is what lets
   you jump from one facet to another mid-chain. (See §6 — this is the load-bearing
   trick.)

---

## 3. The Cast of Files

| File | Type | Role |
|------|------|------|
| `Person.cs` | data class | The **product** — a plain data holder with a `ToString` |
| `PersonBuilder.cs` | class | The **facade / root** — owns the `Person`, exposes `.Address` / `.Job`, and the implicit conversion back to `Person` |
| `Facets/PersonAddressFacetBuilder.cs` | class : `PersonBuilder` | Address **facet** — `.At(city, postCode, streetName)` |
| `Facets/PersonJobFacetBuilder.cs` | class : `PersonBuilder` | Job **facet** — `.At(company)`, `.ProfessionAs(profession)`, `.Makes(income)` |

All four live in namespace `Builders.Faceted.People` (the facets in
`...Person.Facets`).

---

## 4. `Person.cs` — The Product

```csharp
namespace Builders.Faceted.People;

public class Person
{
    public string? StreetName { get; set; }
    public string? PostCode { get; set; }
    public string? City { get; set; }
    public string? CompanyName { get; set; }
    public string? JobField { get; set; }
    public int AnnualIncome { get; set; }

    public override string ToString()
    {
        return
        "Faceted Builder Example"
        + "\n" +
        $"{nameof(StreetName)}: {StreetName}, {nameof(PostCode)}: {PostCode}, {nameof(City)}: {City}, "
        + "\n" +
        $"{nameof(CompanyName)}: {CompanyName}, {nameof(JobField)}: {JobField}, {nameof(AnnualIncome)}: {AnnualIncome}";
    }
}
```

This is the **target object**. It knows nothing about builders or facets — every
build step ultimately just sets one of these six properties. Two natural groups:
`StreetName`/`PostCode`/`City` (address) and `CompanyName`/`JobField`/`AnnualIncome`
(employment). That grouping is the whole reason the pattern is worth using here.

> **Deep-understanding note — the product being a reference type is load-bearing.**
> `Person` is a `class`, so passing it into each facet builder shares *the one object*.
> If `Person` were a `struct`, `new PersonAddressFacetBuilder(person)` would pass a
> **copy**; each facet would edit its own copy, the facets would never merge, and the
> final object would be missing whatever the other facet set. The pattern silently
> **depends** on `Person` being a class. (See §7.)

> **Deep-understanding note — `int AnnualIncome` behaves differently from the string
> fields.** The five string properties are `string?`, so "not set" reads back as
> `null`. `AnnualIncome` is a non-nullable `int`, so an unset income is
> **indistinguishable from a real `0`** — there is no "wasn't provided" state. If that
> distinction ever matters, make it `int?` (or `decimal?`).

### Flags on the product (code review)

- **⚠ Type name matches its namespace.** The class is `Person` inside namespace
  `Builders.Faceted.People`, so its fully-qualified name is
  `Builders.Faceted.People.Person`. This trips Microsoft analyzer **CA1724**
  ("Type names should not match namespaces") and the Framework Design Guidelines
  ("do not use the same name for a namespace and a type in it"). It works here only
  because every consumer is *inside* or `using` the namespace, so bare `Person`
  resolves to the type — but from outside you'd need the awkward double `Person.Person`.
  *Why it matters:* it invites ambiguity and forces qualification. A clearer product
  name (or a namespace that isn't `...Person`) removes the friction.
- **⚠ Money as `int`.** `AnnualIncome` (and `Makes(int)`) model currency as `int`. The
  .NET convention for money is **`decimal`** — `int` can't hold fractional currency and
  `float`/`double` carry binary-rounding error. The demo value `10` is obviously just
  illustrative, but the *type* is the wrong shape for real money.
- **Fixed — `"Facated"` → `"Faceted"`** in the `ToString` header literal. (Was a
  cosmetic typo that printed verbatim; now corrected.)

---

## 5. `PersonBuilder.cs` — The Facade / Root

```csharp
using Builders.Faceted.People.Facets;

namespace Builders.Faceted.People;

public class PersonBuilder
{
    protected Person person = new();

    public PersonAddressFacetBuilder Address => new(person);

    public PersonJobFacetBuilder Job => new(person);

    public static implicit operator Person(PersonBuilder pb)
    {
        return pb.person;
    }
}
```

Four members, each a deliberate choice:

- **`protected Person person = new();`** — allocates the product **once per facade**
  (this field initializer is where the real `Person` is born). It is `protected`
  (not `private`) precisely so the facet **subclasses** can read *and reassign* it —
  which their constructors do (see §6). It is **not** `readonly`: the facet ctors need
  to overwrite it with the shared instance.

- **`public PersonAddressFacetBuilder Address => new(person);`** and
  **`public PersonJobFacetBuilder Job => new(person);`** — the two facet entry points.
  Deliberate details:
  - They are **expression-bodied get-only *properties*** (`=>`), not methods, so the
    chain reads as prose: `pb.Address.At(...)`, not `pb.Address().At(...)`.
  - **Each access returns a brand-new facet builder** (`new(...)`), cheap, and every
    one is handed the **shared `person`** via `new(person)`. That argument is the
    whole ballgame — see the §7 bug.

- **`public static implicit operator Person(PersonBuilder pb) => pb.person;`** — the
  invisible `Build()`. There is **no `.Build()` method** in this pattern; the terminal
  step is this user-defined conversion. See §8.

> **Deep-understanding note — `protected` + mutable is the pattern's requirement, not
> sloppiness.** A classic builder hides its state `private`. Here the facet subclasses
> must inherit and rewrite `person`, so `private` would break the design and `readonly`
> would block the ctor overwrite. The looser access is the *price* of the
> inheritance-based facet trick — the same shape of trade-off the Functional builder
> makes when it exposes its action list `public` for extension.

---

## 6. The Facet Builders — And the Load-Bearing Inheritance Trick

Both facet builders are tiny, and both **inherit `PersonBuilder`**.

`Facets/PersonAddressFacetBuilder.cs`:

```csharp
namespace Builders.Faceted.People.Facets;

public class PersonAddressFacetBuilder : PersonBuilder
{
    public PersonAddressFacetBuilder(Person person)
    {
        this.person = person;         // overwrite inherited field with the SHARED person
    }

    public PersonAddressFacetBuilder At(string city, string postCode, string streetName)
    {
        person.City = city;
        person.PostCode = postCode;
        person.StreetName = streetName;
        return this;                  // fluent chaining WITHIN the address facet
    }
}
```

`Facets/PersonJobFacetBuilder.cs`:

```csharp
namespace Builders.Faceted.People.Facets;

public class PersonJobFacetBuilder : PersonBuilder
{
    public PersonJobFacetBuilder(Person person)
    {
        this.person = person;
    }

    public PersonJobFacetBuilder At(string companyName)
    {
        person.CompanyName = companyName;
        return this;
    }

    public PersonJobFacetBuilder ProfessionAs(string profession)
    {
        person.JobField = profession;
        return this;
    }

    public PersonJobFacetBuilder Makes(int annualIncome)
    {
        person.AnnualIncome = annualIncome;
        return this;
    }
}
```

Anatomy of both:

- **`: PersonBuilder`** — each facet builder *is-a* `PersonBuilder`. **This is the
  trick that makes facet switching possible.** Because a `PersonAddressFacetBuilder`
  inherits `.Address` and `.Job`, you can be mid-address-facet and call `.Job` to hop
  into the job facet — and vice-versa.
- **`ctor(Person person) { this.person = person; }`** — overwrites the *inherited*
  `person` field with the shared instance the facade passed in. (See §9 for why an
  empty `Person` gets allocated and discarded here first.)
- **Facet methods** set one or more fields on the shared `person`, then `return this;`
  for chaining *within* the facet.

> **Deep-understanding note — delete the inheritance and the pattern collapses.** If
> `.Address` returned a *bare* `PersonAddressFacetBuilder` that did **not** inherit
> `PersonBuilder`, then `.Job` would not exist on it and the chain could not cross
> facets. The facets are re-enterable and order-free *only because* every facet builder
> also carries the facade's `.Address` / `.Job` entry points:
>
> ```csharp
> // all legal — bounce back and forth; every hop returns a builder over the SAME Person
> pb.Job.At("Microsoft").Address.At("Amman", "007799", "Yajooz").Job.Makes(10);
> ```

> **Deep-understanding note — `At` on *both* facets is reuse, not collision.** Both
> facet builders define a method named `At`, with different signatures and meanings:
>
> ```csharp
> .Address.At("Amman", "007799", "Yajooz")   // → City / PostCode / StreetName
> .Job.At("Microsoft")                        // → CompanyName
> ```
>
> Legal because they live on different types. The facet acts as a **namespace/context**
> that lets you reuse a natural verb (`At`) without clashing — a genuine *benefit* of
> splitting into facets, not an accident.

> **Deep-understanding note — the two facets have different *granularity* on purpose.**
> Address bundles all three fields into one `.At(...)`; Job chains three separate steps
> (`.At(...).ProfessionAs(...).Makes(...)`). A facet's API can be as coarse or as fine
> as reads best for that aspect.

> **Named arguments on `.Address.At(...)` are not decoration.** `At(string, string,
> string)` takes three same-typed strings — a positional call is trivially transposed
> without any compiler complaint. Naming each argument pins each value to its field.
> The demo does this (`city:`, `postCode:`, `streetName:`); keep it.

---

## 7. The Shared Reference — One `Person`, Many Builders (The Crux)

This is the heart of the whole pattern, and the single place it silently breaks.

- `protected Person person = new();` in `PersonBuilder` allocates the `Person` **exactly
  once** per facade.
- `.Address => new(person)` and `.Job => new(person)` pass **that same reference** into
  each facet builder's ctor, which stores it in the inherited `person` field
  (`this.person = person;`).
- So every facet builder mutates the **one** `Person`, and the two facets compose into
  a single object. `Person` being a reference type is what makes "pass it around"
  mean "share it," never "copy it."

> **Deep-understanding note — this is EXACTLY where the pattern silently breaks.**
> An earlier version of this code wrote `.Address => new()` (no argument). Because the
> facet builders inherit `PersonBuilder`, `new()` ran the **base** field initializer
> `person = new()` and handed each facet its **own fresh `Person`**. Result: address
> and job wrote to *different* objects and never merged — **no compile error, no
> exception, just a half-empty `Person`.** The fix was to pass the shared reference:
> `new(person)` plus the ctor `this.person = person;`. (See §9 for *why* the inherited
> initializer made the bug so quiet.)

---

## 8. The Implicit Operator — The Invisible `Build()`

```csharp
public static implicit operator Person(PersonBuilder pb) => pb.person;
```

The last call in the chain, `.Makes(10)`, returns a **`PersonJobFacetBuilder`**, *not*
a `Person`. Yet this compiles:

```csharp
Person person = pb.Address.At(...).Job.At(...).ProfessionAs(...).Makes(10);
```

…because assigning a builder to a `Person`-typed variable fires this user-defined
**implicit conversion**, which returns `pb.person`. So the **terminal operation of the
build is the assignment's type coercion** — the build "finishes" the moment you store
it into a `Person`.

> **Deep-understanding note — one operator on the base covers both facets.** The
> operator is declared once, on `PersonBuilder`, converting `PersonBuilder → Person`.
> C#'s user-defined-conversion lookup considers operators on the source expression's
> type *and its base classes*, so because `PersonJobFacetBuilder` (and
> `PersonAddressFacetBuilder`) *is-a* `PersonBuilder`, the base operator applies to a
> facet-typed expression too. You don't need an operator per facet.

> **⚠ PITFALL — `var` defeats it.** `var person = pb.Address.At(...)....Makes(10);`
> infers **`PersonJobFacetBuilder`** — there's no target type, so no conversion runs.
> You now hold a *builder*, not a `Person`, and `Console.WriteLine(person)` prints the
> type name (`...Facets.PersonJobFacetBuilder`, the default `ToString`) instead of the
> person string — a silent, confusing bug. The demo works precisely because it declares
> **`Person person = ...`**; the explicit target type is what pulls the trigger.

> **⚠ Aliasing gotcha.** The operator returns the *same* reference the builder holds —
> not a copy. If you keep `pb` and mutate it after extracting `person` (e.g.
> `pb.Address.At("Paris", ...)`), you also mutate the already-extracted `person`. A
> builder here is **single-use by intent**; treat it as spent after conversion.

> **⚠ Throws on a null builder.** `PersonBuilder pb = null; Person p = pb;` still runs
> the operator, and `pb.person` throws `NullReferenceException`. Harmless here (a null
> builder is nonsensical), but it means this `implicit` conversion *can* throw — which
> the guideline says implicit conversions shouldn't. A small point in favour of
> `explicit`.

> **Style aside (beyond the author):** some style guides distrust `implicit`
> conversions because they hide work (here, "finish the build"). `explicit` would make
> the finish visible — `var person = (Person)pb.Address.At(...)...;` — and would also
> defuse the `var` pitfall above (an explicit cast forces the conversion). The author
> chose `implicit` for the seamless `Person person = ...` finish line; that's a
> reasonable ergonomic call, just know the trade-off.

> **Beyond the author — pair it with a named method (CA2225).** Analyzer **CA2225**
> ("operator overloads have named alternates") suggests exposing a named equivalent —
> e.g. `Person Build()` or `ToPerson()` — next to the operator, so callers in languages
> without operator overloading can still finish the build. Ironic here (the whole point
> was to skip `Build()`), but it's the standard convention.

---

## 9. Field-Initializer Ordering — Why Two Throwaway `Person`s Get Allocated

Constructing a facet builder allocates **and immediately discards** one empty `Person`.
Here's why, precisely, for `new PersonAddressFacetBuilder(person)`:

1. `PersonAddressFacetBuilder` declares no fields, so it has no field initializers of
   its own to run.
2. The base `PersonBuilder` constructor runs — **including its field initializer**
   `person = new()`. *An empty throwaway `Person` is allocated here.*
3. The `PersonAddressFacetBuilder` ctor **body** runs: `this.person = person;` —
   overwriting the field with the shared instance. The throwaway is now garbage.

So the demo allocates **3 `Person`s total**: 1 real (in `new PersonBuilder()`) + 2
throwaways (one each for `.Address` and `.Job`). Harmless here — but note the
connection:

> **Deep-understanding note — the throwaway *is* what made the §7 bug silent.** That
> base initializer always runs and always gives the field a valid `Person`. When the
> properties were `new()` instead of `new(person)`, step 3 simply never replaced the
> throwaway with the shared one — so every facet quietly kept its own object. The bug
> wasn't a null or a crash; it was a *valid-but-wrong* object, which is why nothing
> complained.

---

## 10. How It Works — A Step-By-Step Trace

Tracing the exact chain from `Program.cs`:

```csharp
PersonBuilder pb = new();
Person person = pb
    .Address.At(city: "Amman", postCode: "007799", streetName: "Yajooz")
    .Job.At("Microsoft").ProfessionAs("Developer").Makes(10);
```

**Step 0 — `new PersonBuilder()`**
`PersonBuilder.person = new Person()` → one empty shell (all strings `null`,
`AnnualIncome = 0`).

**Step 1 — `pb.Address`**
Returns `new PersonAddressFacetBuilder(person)` — new facet builder, **same** person
ref (plus one discarded throwaway, per §9).

**Step 2 — `.At(city: "Amman", postCode: "007799", streetName: "Yajooz")`**
```text
person.City       = "Amman"
person.PostCode   = "007799"
person.StreetName = "Yajooz"
```
Returns `this` (the address facet builder).

**Step 3 — `.Job`** (inherited property, called on the address facet builder)
Returns `new PersonJobFacetBuilder(person)` — another facet builder, **same** person.

**Step 4 — `.At("Microsoft").ProfessionAs("Developer").Makes(10)`**
```text
person.CompanyName  = "Microsoft"
person.JobField     = "Developer"
person.AnnualIncome = 10
```
Each returns `this` (the job facet builder).

**Step 5 — assignment `Person person = <PersonJobFacetBuilder>`**
The implicit operator fires → returns `pb`'s shared `person`:
```text
Person { StreetName="Yajooz", PostCode="007799", City="Amman",
         CompanyName="Microsoft", JobField="Developer", AnnualIncome=10 }
```

**Console output** (`WriteLine(person)` → `Person.ToString`):
```text
Faceted Builder Example
StreetName: Yajooz, PostCode: 007799, City: Amman,
CompanyName: Microsoft, JobField: Developer, AnnualIncome: 10
```

> **Key timing note — there is no deferral here** (unlike the Functional builder's
> `List<Action<T>>`). Each facet call mutates the shared `Person` **immediately**. The
> only thing that happens "at the end" is the implicit conversion handing back that
> already-built object. Also note: the chain sets `City` before `StreetName`, but
> `ToString` prints `StreetName` first — **chain order is independent of print order.**

---

## 11. Why `PersonBuilder` Is *Not* Sealed (Contrast the Functional Builder)

`PersonBuilder` is intentionally **inheritable** — the facet builders *are* its
subclasses, and they rely on inheriting `.Address` / `.Job` to enable facet switching.
Sealing the base would break the pattern.

Compare the Functional builder (`FunctionalBuilderExplanation.md` §5), which *is*
`sealed`: there the extension point is **extension methods**, so sealing communicates
"extend me from the outside, never by subclassing."

> **One-line takeaway — the extension mechanism dictates the seal decision.**
> Functional builder → extend via extension methods → **seal** it.
> Faceted builder → extend via **subclass** facet builders → must stay **open**.

---

## 12. Code Smells & "Beyond the Author" Notes (Consolidated)

Pulled together for a quick review pass. None break the demo; all are worth knowing.

- **⚠ Namespace / folder mismatch.** The folder is `Faceted` but the namespace is
  `Builders.Faceted.People` (segment `FacetedBuilder`). Every *other* builder
  folder in this repo matches its namespace (`Functional/Shapes` →
  `Builders.Functional.Shapes`, `Stepwise/Animals/Chickens` →
  `Builders.Stepwise.Animals.Chickens`, etc.), so this one is the odd one out. This
  trips analyzer **IDE0130** ("namespace does not match folder structure"). The most
  recent commit — *"Fixation for Facet builder folder's name"* — strongly suggests the
  folder was renamed (from `FacetedBuilder` → `Faceted`) without realigning the
  namespaces. *Recommendation:* rename the namespaces to `Builders.Faceted.Person`
  (and `...Person.Facets`) to match the folder and the rest of the repo.
- **⚠ Type name == namespace** (`Person` in `...Person`) — CA1724; see §4.
- **⚠ Money as `int`** (`AnnualIncome`, `Makes(int)`) — prefer `decimal`; see §4.
- **`int` can't express "unset" income** — defaults to `0`, unlike the `string?`
  fields which default to `null`; see §4.
- **No completeness/validation.** Nothing forces both facets to be set, or any field
  to be non-null. You can build an all-`null` `Person`. That's *by design* for a
  faceted builder (it optimizes for grouping, not enforcement) — if you need required
  fields in a required order, that's the **Stepwise** builder's job.
- **Mild naming drift** — `ProfessionAs(...)` sets `JobField` (profession vs. job
  field). Harmless, but the method and the property don't use the same noun.
- **Facet ctors are `public`**, so a caller *could* `new PersonAddressFacetBuilder(p)`
  directly and bypass the facade. Not dangerous (it still needs a `Person`), just
  broader surface than necessary; `internal` would keep facets an implementation detail.
- **⚠ Implicit operator throws on `null`.** Converting a null `PersonBuilder` runs the
  operator and hits `pb.person` → `NullReferenceException`; an `implicit` conversion is
  conventionally expected never to throw. See §8.
- **⚠ No named alternate for the operator (CA2225).** Convention is to pair a conversion
  operator with a named method (`Build()` / `ToPerson()`) for non-C# callers. See §8.
- **Cosmetic** — the implicit operator is written block-bodied
  (`{ return pb.person; }`) while the notes file shows the equivalent
  expression-bodied form — both compile identically. (The former `"Facated"`
  `ToString` typo has been fixed to `"Faceted"`.)

---

## 13. Faceted vs. Other Builder Variants

| Aspect | Faceted (this) | Functional (Shape) | Stepwise (Chicken) | Recursive-Generic (Duck) |
|--------|----------------|--------------------|--------------------|--------------------------|
| Builder state | one shared `Person`, mutated | `List<Action<T>>` (a recipe) | one half-built object | one half-built object |
| Structure | facade + subclass facets | 1 class + extension-method plugins | chained per-step interfaces | self-referential generic base |
| Mutation timing | immediate, per call | deferred to `Build()` | immediate, per call | immediate, per call |
| Enforces order? | **No** (facets free / re-enterable) | No | **Yes** (interfaces force it) | No (any order) |
| Finish step | implicit operator → `Person` | explicit `.Build()` | explicit `.Build()` | explicit `.Build()` |
| Design driver | ergonomic **grouping** | **open/closed** extensibility | **forced order** / no-skip | **any-order** fluent chaining |
| Best when | many props in clear groups | third parties add new steps | a required sequence of fields | all fields optional, order-free |

---

## 14. When To Use / When Not

- **Use** when a single object has many properties that fall into natural aspects and
  one flat fluent API would be an unwieldy wall of methods. Facets keep each
  sub-builder small and let a verb like `At` mean different things per context.
- **Don't** reach for it when:
  - there are only a few properties → a plain fluent builder is simpler;
  - you must **enforce** an order or a required set → use **Stepwise** (Chicken);
  - third parties must bolt on new steps without touching you → use **Functional**
    (Shape).

---

## 15. Relation to the Other Builders in This Repo

- **Faceted (this)** → a facade hands out multiple facet builders, one per aspect, all
  mutating one shared product; hop between them via inherited entry points. Fluent
  *within* a facet (each method returns `this`); the facade adds *cross-facet* hops.
- **Functional (Shape)** → state is a `List<Action<T>>`; optimized for open/closed
  extension via extension-method plugins.
- **Stepwise (Chicken)** → per-step interfaces force call **order** and completeness at
  compile time.
- **Recursive-Generic / CRTP (Duck)** → every method returns `Self`, giving
  strongly-typed **any-order** chaining across an inheritance hierarchy.

---

## 16. Continuation / TODO

- **Align namespaces with the folder** — rename `Builders.Faceted.People`
  → `Builders.Faceted.Person` (and `...Person.Facets`) to match the folder and the
  rest of the repo (fixes IDE0130). See §12.
- **Reconsider the `Person` type name** to stop it matching its namespace (CA1724).
- **Model money as `decimal`** for `AnnualIncome` / `Makes(...)` if this ever moves
  beyond a demo.
- **Optional:** make the facet constructors `internal` so the facade is the only
  documented way to reach a facet.
- **Optional:** demonstrate the re-enterable nature explicitly in `Program.cs` (a
  chain that hops `Address → Job → Address`) to show facets are order-free.
