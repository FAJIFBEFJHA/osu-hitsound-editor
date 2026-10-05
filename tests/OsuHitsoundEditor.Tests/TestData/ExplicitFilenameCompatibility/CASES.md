# Explicit Filename Compatibility Fixture

Manual compatibility fixture for `osu-hitsound-editor`.

Verified on 2026-10-04 using the exact `.osz` artifact preserved in this directory.

## Artifact identity

```text
SHA-256: 5a0feb3533012a0d5d913c097c3124e233fc46efc750ca7d9491bb906c40093c
```

The `.osz` contains only synthetic audio created for this test.

## Audio signatures

| Sound | Audible signature |
|---|---|
| Normal | one low beep |
| Custom | two medium beeps |
| Whistle | one long, very high beep |
| Finish | three descending tones |
| Clap | short noise burst |

The background audio is silent.

## Exact input cases

| Case | Time | `hitSound` | `hitSample.filename` | Purpose |
|---|---:|---:|---|---|
| 1 | 2000 ms | `0` | empty | Normal control |
| 2 | 4000 ms | `0` | `custom.wav` | Explicit filename without additions |
| 3 | 6000 ms | `2` | `custom.wav` | Explicit filename + Whistle |
| 4 | 8000 ms | `4` | `custom.wav` | Explicit filename + Finish |
| 5 | 10000 ms | `8` | `custom.wav` | Explicit filename + Clap |
| 6 | 12000 ms | `14` | `custom.wav` | Explicit filename + Whistle + Finish + Clap |
| 7 | 14000 ms | `8` | empty | Clap control |

`14 = 2 + 4 + 8`.

All objects use:

```text
normalSet = 1
additionSet = 1
index = 1
volume = 100
```

## Manual result — osu!stable

| Case | Observed audible result |
|---|---|
| 1 | Normal |
| 2 | Custom only |
| 3 | Custom only |
| 4 | Custom only |
| 5 | Custom only |
| 6 | Custom only |
| 7 | Clap control |

Observed stable rule:

```text
explicit hitSample.filename
    -> custom sample only
```

## Manual result — osu!lazer

| Case | Observed audible result |
|---|---|
| 1 | Normal |
| 2 | Custom only |
| 3 | Custom + Whistle |
| 4 | Custom + Finish |
| 5 | Custom + Clap |
| 6 | Custom + Whistle + Finish + Clap |
| 7 | Clap control |

Observed lazer behavior:

```text
explicit hitSample.filename
    -> custom sample
    + applicable additions from hitSound flags
```

## Project compatibility decision

`osu-hitsound-editor` uses the legacy/osu!stable result as its canonical logical behavior:

```text
explicit hitSample.filename
    -> custom sample only
```

The original parsed `hitSound` flags remain preserved as source data.

The suppression belongs to logical hitsound resolution, not parsing.

## Reproduction

1. Import `explicit-filename-compatibility.osz`.
2. Use autoplay/automatic play when available.
3. Compare cases 2-6 against case 2.
4. Do not infer results from memory; record each case independently.
5. If recreating the package, compare its contents and behavior against the preserved artifact before replacing this fixture.
