Project architecture (recommended)

This document summarizes the assembly/project layout and responsibilities for the ZomboZ codebase. Keep Core engine-free, put Unity-specific wiring in Runtime and Editor code in Editor assemblies.

1) ZomboZ.Core (noEngineReferences = true)
- Purpose: pure domain, DTOs, interfaces/ports and exceptions. No UnityEngine or Editor APIs.
- Typical folders: Domain/, Ports/, DTOs/, Exceptions/
- Examples of contents: ICache<TKey,TValue>, IPool<T>, IRepository<TId,TEntity>, domain entities, value objects.
- asmdef guidance: set "noEngineReferences" = true and do NOT reference Unity assemblies.

2) ZomboZ.Infrastructure.* (noEngineReferences = true if they do not use Unity; reference ZomboZ.Core)
- Purpose: concrete implementations of Core ports, adapters and shared, engine-independent helpers.
- Example subprojects: ZomboZ.Infrastructure.Cache, ZomboZ.Infrastructure.Persistence
- Typical contents: InMemoryLruCache, repository implementations that use pure .NET (e.g., in-memory or file-based), shared adapters.
- asmdef guidance: references: ["ZomboZ.Core"], set noEngineReferences = true when implementations don't call Unity APIs.

3) Implementations (examples)
- InMemoryLruCache<TKey,TValue> : ICache<TKey,TValue>
- SqlitePlayerRepository : IRepository<Guid, Player> (persistence only; can be in Infrastructure.Persistence)
- GameObjectPool (if Unity-specific) should live outside Core and be placed in Infrastructure assemblies that allow engine refs or in Runtime.

4) ZomboZ.Runtime (noEngineReferences = false)
- Purpose: composition root and all Unity-engine code that interacts with scenes, MonoBehaviours, Addressables, physics, etc.
- Typical folders: Bootstrapper, SceneControllers, UI, Services (Addressables wrappers)
- Contents: Bootstrapper MonoBehaviour (registers implementations into DI/service-locator), MonoBehaviours that consume Core interfaces via the composition root.
- asmdef guidance: references: ["ZomboZ.Core", "ZomboZ.Infrastructure.*"], set noEngineReferences = false.

5) ZomboZ.Editor (editor-only code, exclude from builds)
- Purpose: Editor tools, custom inspectors, Editor Windows, validation scripts.
- Place under Assets/Scripts/Editor or similar and set the asmdef to include only the Editor platform (includePlatforms: ["Editor"]).
- Can reference Core and Runtime asmdefs where helpful.

Notes and practices
- Keep dependency direction: Core <- Infrastructure <- Runtime. Core must not reference Infrastructure or Unity.
- Use asmdefs to enforce boundaries. For engine-free code use noEngineReferences=true so Core is reusable outside Unity (.NET tests or other hosts).
- Use small, focused assemblies: Core for abstractions, Infrastructure for adapters, Runtime for composition and Unity specifics.
- For testing: unit-test Core using a .NET test project targeting .NET Standard 2.1 or add test-only asmdefs that reference Core.
- For packages/sharing: libraries meant to be reused outside Unity should target .NET Standard 2.1 and live either as separate projects or as asmdefs with no engine references.

Quick checklist
- [ ] ZomboZ.Core asmdef with noEngineReferences = true
- [ ] ZomboZ.Infrastructure.Cache asmdef referencing ZomboZ.Core (noEngineReferences = true if no Unity types used)
- [ ] ZomboZ.Runtime asmdef referencing ZomboZ.Core + ZomboZ.Infrastructure.* (noEngineReferences = false)
- [ ] ZomboZ.Editor asmdef with includePlatforms = ["Editor"]

If you want I can add the exact asmdef JSON snippets into this repo (already applied for some assemblies) or scaffold more concrete examples (GameObjectPool, Sqlite repo, Addressables wrapper, DI container).