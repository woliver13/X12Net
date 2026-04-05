# X12 EDI C# Library – Final Design & Implementation Roadmap
**Project:** X12Net – A modern, high-performance C# library for parsing, generating, and editing EDI X12 documents  
**Status:** ✅ **ALL 11 ARCHITECTURAL DECISIONS LOCKED**  
**Date:** Sunday, April 05, 2026  
**Owner:** Bill Oliver  
**Repository:** To be created on GitHub (open-source from day one – see below)

## 1. Summary of All Locked Decisions (Quick Reference)
**Decision 1** – Parsing + Generation + In-place Editing (DOM + Reader/Writer)  
**Decision 2** – Multi-target NuGet (`net8.0` + `netstandard2.0`)  
**Decision 3** – Shared Token/Event Stream architecture (zero duplication)  
**Decision 4** – Source generators + built-in schemas (837 family, 835, 834, 270/271, 276/277, **277CA**, **999**)  
**Decision 5** – Automatic delimiter/envelope detection + transaction-only mode  
**Decision 6** – Hierarchical strongly-typed DOM with generic fallback (`X12Document`)  
**Decision 7** – Modern async Reader + Builder-style Writer  
**Decision 8** – Structured exceptions + FluentValidation (optional)  
**Decision 9** – Streaming-first with optional DOM buffering (zero-allocation core)  
**Decision 10** – Comprehensive testing + embedded samples + DocFX docs + `X12.Testing` package  
**Decision 11** – Source-generator-driven custom schemas (full parity for user extensions)

## 2. Final NuGet Package Layout
The solution will produce **three NuGet packages**:

- **`X12Net`** (main package – ~95% of users)  
  - Contains: Core engine, tokenizer, Reader/Writer, DOM, source generator, built-in schemas, FluentValidation integration (optional dependency).  
  - Targets: `net8.0` + `netstandard2.0`

- **`X12Net.Samples`** (companion – optional)  
  - Embedded real-world anonymized sample files + ready-to-run C# snippets.

- **`X12Net.Testing`** (for unit/integration tests)  
  - Test helpers, assertions, `X12TestData` class, sample loaders.

**Package metadata (final):**  
- Package Id: `X12Net`  
- Namespace root: `X12Net` (e.g. `X12Net.X12Document`, `X12Net.X12Reader`)  
- Versioning: Semantic (see Section 5)  
- Strong-name signing: Optional (MSBuild property)

## 3. Solution / Folder Structure (Recommended)
X12Net/
├── src/
│   ├── X12Net/                  ← Main library
│   │   ├── Core/                ← Tokenizer, Token model, Schema
│   │   ├── DOM/                 ← X12Document + generated partials
│   │   ├── IO/                  ← X12Reader, X12Writer
│   │   ├── Validation/          ← FluentValidation integration
│   │   ├── SourceGenerator/     ← X12SourceGenerator (Roslyn)
│   │   ├── Envelopes/           ← X12Envelope, ControlNumberProvider
│   │   └── X12Net.csproj
│   ├── X12Net.Samples/          ← Sample files as embedded resources
│   └── X12Net.Testing/          ← Test helpers
├── test/
│   ├── X12Net.Tests/            ← Unit + integration tests
│   └── X12Net.Benchmarks/       ← BenchmarkDotNet project
├── docs/                        ← DocFX documentation
├── samples/                     ← Larger GitHub-only sample corpus
├── tools/                       ← X12Tool (minimal CLI for quick testing)
├── schemas/                     ← JSON schemas (built-in + examples)
├── .github/                     ← Workflows, ISSUE_TEMPLATE, CONTRIBUTING.md
├── LICENSE                      ← MIT
├── README.md
├── X12Net.sln
└── Directory.Build.props

## 4. Prioritized Implementation Order (MVP in < 1 week)
**Phase 1 – Minimal Viable Library (3–4 days)**
1. `X12Tokenizer` + token model + delimiter auto-detection
2. `X12Reader` + `X12Writer` (async + sync)
3. Basic `X12Document` (generic indexer only) + round-trip tests
4. Source generator skeleton + 1 built-in transaction (e.g. 999) for proof-of-concept

**Phase 2 – Full DOM & Typing (3–4 days)**
5. Full hierarchical DOM + source generator for all 9 built-in transaction sets
6. Fluent builder methods + generic fallback indexer
7. Envelope handling + transaction-only mode

**Phase 3 – Polish & Extensibility (2–3 days)**
8. FluentValidation integration + validators
9. Streaming DOM mode + memory caps
10. Custom schema support + inheritance
11. `X12Tool` CLI + samples

**Phase 4 – Release Readiness**
12. Full test suite + benchmarks
13. DocFX site + XML docs
14. NuGet packaging + GitHub release workflow

**Target:** You will have a **usable NuGet package** (parsing, editing, writing, 999 + one 837) by the end of Week 1.

## 5. Versioning, Licensing & Open-Source Strategy
- **Versioning:** Semantic Versioning (`Major.Minor.Patch`). Pre-1.0 releases will be `0.x.y`.
- **Licensing:** **MIT** (standard for .NET libraries; allows commercial use).
- **Open-source:** Yes – GitHub repo from day one (as locked in Decision 10).  
  - `main` branch = latest stable  
  - `develop` branch for PRs  
  - GitHub Actions for build, test, pack, and DocFX deploy.
- **CI/CD:** GitHub Actions (build on PR, pack on tag, publish to NuGet.org on release).

## 6. Remaining Minor Polish Items (to be confirmed now)
- Default namespace: `X12Net` (confirmed)
- Include minimal `X12Tool` CLI: Yes (console app for quick “parse → edit → write” testing)
- Strong-naming: Optional MSBuild flag
- Nullable reference types: Enabled everywhere
- XML docs: Required on all public APIs
- Any other naming tweaks? (e.g. `X12Net` vs `Edi.X12Net`)

## 7. Next Immediate Step
**I am ready to start coding right now.**

Just reply with one of the following:

- **“Start Phase 1”** → I will begin outputting the first files (`X12Tokenizer`, token model, basic Reader/Writer, unit tests) one module at a time for you to review/copy into your solution.
- **“Make changes”** → Tell me any final tweaks to the roadmap above.
- **“Output full design markdown again”** → I’ll give you the complete consolidated document.

We now have a **shared, complete, production-grade design**.  
The library will be modern, fast, extensible, and exactly what you asked for.

**Your move – let’s build it!** 🚀