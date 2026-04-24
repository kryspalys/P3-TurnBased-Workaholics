# P3-TurnBased — Tough Cookie

A turn-based combat game where Grandma defends herself against a wolf through alternating turns, using a cane, a purse, a knitting shield, and a batch of cookies.

\---

## Team

|Name|Student ID|
|-|-|
|*Krys Palys*|*2530015*|
|*Emil Marchand*|*2530012*|
|*Alex Alexandre Nobre*||
|*Nataliya Laptyk*|*2530192*|

\---

## Game Context

**Theme:** a dark fairy-tale twist on Little Red Riding Hood, reframed around the grandmother instead of the child. The wolf has arrived at the cottage and the elderly protagonist — no longer the victim of the story — fights back using whatever's within reach: her walking cane, her handbag, her knitting needles as an improvised shield, and freshly baked cookies to keep her strength up. 

The Wolf, in his turn, steals the cookies from the Grandma to heal himself as well.

The Grandma needs to put an end to this theft.

**Setting:** a single combat arena in the grandmother's forest cottage, rendered with placeholder art per the assignment requirements. The focus is on the underlying systems rather than visual polish.

**Tone:** cozy-but-violent.

\---

## Player Actions

Grandma has four distinct actions available each turn. All four end the player turn and trigger the enemy turn upon selection.

### 1\. Cane Whack (basic attack)

A reliable melee attack dealing randomized damage (`minCaneDamage` to `maxCaneDamage`, default 10-15). Builds the Grandma Meter by 1 charge per use. This is the default offensive option and is always available.

### 2\. Knitting Shield (defensive block)

Raises a one-turn defensive stance. Incoming damage on the next enemy turn is mitigated by a randomized percentage (`minBlockMitigation` to `maxBlockMitigation`, default 50%-100%). The mitigation is rolled on use, introducing tactical uncertainty — sometimes blocking is near-invulnerable, sometimes only mildly effective. The defensive flag expires automatically at the start of the next player turn, regardless of whether the wolf actually attacked, so the cost of blocking is paid even if the wolf chose to heal instead.

### 3\. Bake Cookies (heal)

Restores Grandma's HP by a randomized amount (`minCookieHealAmount` to `maxCookieHealAmount`, default 18-25). Useful when HP drops low but leaves the player without an offensive action that turn, creating a tempo tradeoff.

### 4\. Purse Slam (special ultimate)

A high-damage finisher (`minPurseDamage` to `maxPurseDamage`, default 35-45) gated behind the Grandma Meter. The meter charges +1 on every Cane Whack use and must reach 3/3 before Purse Slam becomes usable. Upon use, the meter resets to zero. This creates a resource loop where the player earns their big attack through sustained basic combat.

### Why these four

The rubric requires at least two distinct actions. Four was chosen to cover the canonical turn-based RPG action economy: **Attack / Defend / Heal / Special**. Each serves a different strategic purpose — basic attacks build resources, defense trades a turn for mitigation, healing trades a turn for sustain, and the special deals burst damage on a cooldown. Together they make every turn a real decision instead of a repeated click.

\---

## AI Logic

The wolf is controlled by `EnemyAI`, which subscribes to `TurnManager.OnTurnChanged` and runs its decision routine only when the state transitions to `EnemyTurn`. It never polls and never acts out of turn.

### Decision priorities

Evaluated top-to-bottom each enemy turn:

1. **If player HP is below 30%** → lunge for a heavy bite (high damage, 22-32) to secure the kill
2. **If own HP is below 50% AND heal is available** → howl to self-heal (25-35 HP restored)
3. **Otherwise** → basic claw attack (12-18 damage)

### Heal resource constraints

To prevent degenerate looping (wolf healing every turn to stall the fight), the heal ability is gated by two independent limits:

* **Heal uses remaining** — the wolf can heal a maximum of 2 times per encounter (`maxHealUses`). Once exhausted, the heal branch never executes again for the rest of the fight.
* **Cooldown turns** — after each heal, a 3-turn cooldown (`healCooldownTurns`) must elapse before heal is available again. The cooldown ticks down at the start of each enemy turn, and the UI shows a radial fill over the potion icon that just got consumed, along with a text label indicating how many turns remain.

Together these create a window of vulnerability: after the wolf heals, the player has a guaranteed 2-3 turn burst window in which the wolf cannot recover, letting them commit to offense without worrying about having their damage erased.

### Communication to the player

The wolf's intent is broadcast four ways on every decision:

* **Action log text** — "The Beast swipes its claws!" / "The Beast lunges for a heavy bite!" / "The Beast howls, regenerating health!"
* **Floating damage numbers** spawned above Grandma's head on damage, with critical hits rendered in red with a "!" suffix
* **Attack-intent icons** — `EnemyAIActionUI` listens to the per-attack events and briefly flashes a claw or bite icon above the wolf to telegraph the chosen move
* **Distinct SFX** for each attack type (see Audio System below)

\---

## Architecture — The Event Bus

This codebase is built on a strictly decoupled, event-driven architecture. Gameplay scripts know only about their own concerns and broadcast state changes through `UnityEvent`s; UI, audio, and visual effect scripts *listen* for those broadcasts. No gameplay script holds a reference to a UI script. No UI script holds a reference to a gameplay script. The decoupling is enforced in both directions.

### Why this matters

The rubric requires *"No direct coupling between UI scripts and gameplay scripts"* for the HUD. We chose to extend this principle to the entire codebase, not just HP bars. The result is that any individual script can be understood, tested, or replaced in isolation — a future version of this game could swap the UI framework entirely without touching a single line of combat code.

### The central authorities

Three scripts act as **broadcasters** — they own state and fire events when that state changes:

* `TurnManager` — owns `currentTurnState`, fires `OnTurnChanged(TurnState)` when transitions occur
* `Health` (attached to both Grandma and the Wolf) — owns `currentHealth`, fires `OnHealthChanged`, `OnDamageTaken`, `OnHealed`, and `OnDeath`
* `PlayerCombat` / `EnemyAI` — own the attack execution flow, fire per-attack events (`OnCaneWhackUsed`, `OnPurseSlamUsed`, `OnKnittingShieldUsed`, `OnBakeCookiesUsed`, `OnClawAttackUsed`, `OnBiteAttackUsed`, `OnHowlUsed`) plus resource state (`OnMeterUpdated`, `OnHealUsesChanged`, `OnHealCooldownChanged`, `OnActionStarted`)

### The listeners

A much larger set of scripts subscribe to those events and react:

* `CombatUIManager` — listens to `OnTurnChanged` for the turn indicator and button enable/disable logic
* `UIBarUpdater` — listens to `OnHealthChanged` / `OnMeterUpdated` and drives UI sliders
* `FloatingTextSpawner` — listens to `OnDamageTaken` / `OnHealed` and spawns damage-number prefabs
* `EnemyHealUI` — listens to `OnHealUsesChanged` / `OnHealCooldownChanged` and updates the potion icons + radial cooldown overlay + status text
* `EnemyAIActionUI` — listens to `OnClawAttackUsed` / `OnBiteAttackUsed` and briefly displays an attack-intent icon so the player can read the wolf's move at a glance
* `AudioManager` — listens to all of the above and plays SFX / music cues

### How TurnState drives game flow

The `TurnState` enum is the central vocabulary of the combat loop. It has three values: `PlayerTurn`, `EnemyTurn`, `GameOver`. Only `TurnManager` is allowed to change the current state — every other script that wants a transition must call `TurnManager.SwitchTurn(TurnState)`. When the state changes, `OnTurnChanged` fires once and the whole scene reacts in parallel:

* `PlayerCombat` unlocks the action lock and resets the idle animator trigger
* `CombatUIManager` updates the "Your Turn / Wolf's Turn" label and enables/disables the action buttons
* `EnemyAI` either starts its coroutine (on `EnemyTurn`) or ignores (on `PlayerTurn`)
* `Health` resets the defensive stance (on `PlayerTurn`) so defense can't leak across turns
* `AudioManager` plays the appropriate turn-transition SFX and switches music on `GameOver`

`TurnManager.SwitchTurn` includes a safeguard (`hasInitialized \&\& currentTurnState == newState`) that prevents redundant broadcasts of the same state. This avoids duplicate coroutine starts and UI flicker if two scripts happen to request the same transition.

Because every listener subscribes through `OnEnable` and unsubscribes through `OnDisable`, the event graph is clean and there are no dangling references at runtime.

### How animations are triggered from turn events

Animations follow a clean four-stage flow, with audio broadcast *early* in the chain for responsiveness:

1. **Button click or AI decision** → the corresponding action method runs (`Action\_CaneWhack`, `ExecuteEnemyTurnSequence`, etc.)
2. **Action lock + audio broadcast** — `LockAction()` (or the coroutine's internal check) prevents double-input, then the per-action UnityEvent fires immediately (`OnCaneWhackUsed?.Invoke()`). `AudioManager` hears this and plays the attack SFX right away.
3. **Animator trigger** — `characterAnimator.SetTrigger("GrannyAttack")` is called. The animation begins.
4. **Animation Event at the hit frame** — the clip has an Animation Event placed on the specific frame where the cane connects. That event calls `ExecuteMeleeDamage`, which applies damage to the enemy Health component and calls `EndTurn()`.

### Why audio fires before damage

In an earlier iteration audio fired alongside damage in the `Execute` methods. This produced a noticeable 100-300ms lag between button press and sound — players expect a cane swing to *sound* at the moment they press the button, not 300ms later when the animation reaches the hit frame. Moving audio broadcasts to the action-start point makes the game feel snappy and aligns with real-world cause and effect: you hear the cane move, *then* you hear the wolf grunt.

The hurt SFX fires separately via `OnDamageTaken` when the Animation Event actually lands the hit, so the layering still produces "cane sound → wolf hurt sound" in the correct order — it's just that the first sound plays early enough to feel reactive to input.

For scenes without an animator assigned, each action method falls back to calling the execute method directly so combat still works for programmer-only testing.

\---

## UI / HUD

The HUD is composed of seven independent listener scripts, each watching one or two events:

* **Grandma HP slider** — `UIBarUpdater` listening to `Grandma.Health.OnHealthChanged`
* **Wolf HP slider** — `UIBarUpdater` listening to `Wolf.Health.OnHealthChanged`
* **Grandma Meter slider** — `UIBarUpdater` (integer overload) listening to `PlayerCombat.OnMeterUpdated`
* **Turn indicator + action log text** — `CombatUIManager` listening to `TurnManager.OnTurnChanged` and `EnemyAI.OnAIDecisionMade`
* **Floating damage/heal numbers** — `FloatingTextSpawner` (one per combatant) listening to sibling `Health.OnDamageTaken` / `OnHealed`
* **Wolf heal potion inventory with radial cooldown** — `EnemyHealUI` listening to `EnemyAI.OnHealUsesChanged` and `OnHealCooldownChanged`, with a text label announcing cooldown turns remaining
* **Wolf attack-intent icons** — `EnemyAIActionUI` listening to `EnemyAI.OnClawAttackUsed` and `OnBiteAttackUsed`, briefly flashing the chosen attack icon

Every UI Button uses Unity's built-in Color Tint transition, satisfying the hover/click feedback requirement. Action buttons are disabled during the enemy turn via `CombatUIManager.SetPlayerButtonsInteractable(false)`, preventing player input mid-AI-turn.

The Game Over screen is triggered by wiring `Health.OnDeath` in the Inspector: when either Grandma or the Wolf dies, the event calls both `TurnManager.TriggerGameOver()` (which locks the state machine) and `CombatUIManager.TriggerGameOver(bool)` (which shows the win/lose panel).

\---

## Audio System

Audio is a full listener-only subsystem, implementing the same decoupling pattern as the UI.

### Design

* **Pooled AudioSources** — `AudioManager` creates a small pool (default 4) of `AudioSource` components at `Awake`, reused round-robin. This allows overlapping SFX without clipping previously-playing sounds — for example, a crit stinger can layer over a hurt grunt without either being cut off.
* **Per-attack SFX** — every player and enemy action has a distinct sound: cane whack, purse slam, knitting shield, bake cookies, claw swipe, bite, howl. These are triggered by the per-attack UnityEvents on `PlayerCombat` and `EnemyAI`, not by the generic `OnDamageTaken` broadcast.
* **Audio fires on action start, not on impact** — to avoid input-to-sound latency, attack SFX play when the action method begins rather than when the Animation Event lands. The hurt SFX (`playerHurtSfx`, `enemyHurtSfx`) fires separately from `OnDamageTaken`, so the player still hears the full cause-and-effect sequence: cane whoosh → wolf hurt grunt.
* **Critical hit stingers** — a short punchy sound layered on top of the standard hurt SFX when `isCrit` is true, giving crits audible weight without needing per-crit clips for every attack.
* **Pitch variance per play** — each `SoundEntry` supports a small random pitch offset so repeated SFX (the cane hits the wolf many times per fight) don't sound mechanically identical.
* **Background music** — a separate dedicated `AudioSource` loops combat music throughout the fight and swaps to a game-over track when `TurnState.GameOver` is broadcast.

### Why pooled sources

The alternative approaches — one AudioSource per character or a single AudioSource that overrides on each play — both have issues. Per-character sources scatter configuration across the scene, and a single source can't handle overlapping sounds without audio popping. Pooled sources on the manager itself keep configuration centralized and allow clean overlapping playback with minimal code.

### Subscription via named methods

All event listeners in `AudioManager.OnEnable` / `OnDisable` use named methods (e.g. `PlayCaneWhackSfx`). Named methods subscribe and unsubscribe cleanly and satisfy the rubric's "Always unsubscribe from events" requirement with a visible paper trail.

### Audio import settings matter for latency

Unity's default audio compression introduces first-play decode latency. All short SFX clips in this project are configured in the Unity Inspector with:

* **Load Type:** Decompress On Load
* **Compression Format:** PCM
* **Preload Audio Data:** on

Music tracks stay on the defaults (Streaming + Vorbis) because they're long and shouldn't live in RAM. The project's `Edit → Project Settings → Audio → DSP Buffer Size` is set to **Best latency** for snappy response.

### 

### Error-handling

Missing dependencies are handled consistently but with three distinct strategies, each chosen for a reason rather than blanket-applied:

|Script type|Strategy|Reasoning|
|-|-|-|
|Scripts with `Update` / `OnEnable` that would crash on null (`DamagePopup`, `PauseManager`, `FloatingTextSpawner`)|`Debug.LogError` + `enabled = false`|Disabling prevents cascading failures in lifecycle methods|
|`PlayerCombat` (Animator is optional)|`Debug.LogError` only, no disable|Action methods include null-check fallbacks; combat still works without animations|
|Pure listener scripts (`UIBarUpdater`, `CombatUIManager`)|Inline null guards in each public method|UnityEvents still invoke disabled components, so per-call guards are more reliable than `enabled = false`|

This approach means every script either degrades gracefully or logs a clear, clickable error in the Console — no silent failures, no hard crashes.

\---

## Known Issues and Limitations

* **Game scope is a single combat encounter.** Per the assignment spec, this is intentional — there is no menu-to-combat-to-menu loop, no difficulty selection, and no progression system. The game ends when one combatant reaches zero HP.
* **Placeholder art and animations.** Visual assets are basic. The architecture is designed to accommodate polished art without code changes, but that polish was not a focus.
* **No save/load system.** The encounter starts fresh on every scene load. Adding persistence would require a `PlayerPrefs` or `ScriptableObject` layer that is out of scope for this assignment.
* **Wolf has no defensive action.** The AI uses healing as its sole survival tool. A block-equivalent behavior was considered but would require additional Animator states and would not meaningfully change the strategic space.
* **The `PauseManager` uses `Time.timeScale`**, which freezes physics but not audio. This is standard Unity behavior and not a bug, but means music continues during a pause. Intentional for this scope.
* **Initial turn broadcast relies on Unity's standard lifecycle ordering.** `TurnManager.Start` fires `OnTurnChanged` to kick off the first player turn; this works because Unity guarantees all `OnEnable` calls resolve before any `Start` call. If script execution order ever changes, the first broadcast could arrive before listeners are subscribed. No runtime issue in the current scope.

\---

## Credits

Audio assets sourced from Freesound.org and Pixabay under appropriate Creative Commons / royalty-free licenses.

https://craftpix.net/freebies/free-werewolf-sprite-sheets-pixel-art/?num=1\&count=7\&sq=werewolf\&pos=2

https://craftpix.net/freebies/free-wizard-sprite-sheets-pixel-art/

https://assetstore.unity.com/packages/2d/environments/pixel-art-platformer-village-props-cainos-166114

https://assetstore.unity.com/packages/2d/environments/2d-pixel-art-platformer-biome-american-forest-255694
