# Explicit Filename Remaining Components

Manual compatibility fixture for `osu-hitsound-editor`.

Verified using the exact `.osz` artifact preserved in this directory.

## Artifact identity

```text
SHA-256: d23d3a64187230dc66cc14dff9559256d1ddaf4d17565b0659b9af746deea39a
```

## Exact cases

### Spinner

```text
start = 2000 ms
end = 4000 ms
hitSound = 14
hitSample = 1:1:1:100:custom.wav
```

### Slider

```text
start = 6000 ms
slides = 2
length = 400
hitSound = 2
edgeSounds = 8 | 4 | 2
edgeSets = 1:1 | 1:1 | 1:1
hitSample = 1:1:1:100:custom.wav
```

Important edge times:

```text
6000 ms = head   -> Clap
8000 ms = repeat -> Finish
10000 ms = tail  -> Whistle
```

## Manual result — osu!stable

Spinner end:

```text
Custom only
```

No Whistle, Finish, or Clap additions were audible with the explicit filename.

Slider:

```text
head   -> Clap
repeat -> Finish
tail   -> Whistle
```

`custom.wav` was not audible at any slider edge.

The custom beatmap slider body/tick files included in this fixture were not observed during the slider.

## Manual result — osu!lazer

Spinner end:

```text
Custom + applicable additions
```

There was also additional sound during the spinner traversal before spinner end. This is treated as osu!lazer-specific behavior and is intentionally deferred.

Slider:

```text
head   -> Clap
repeat -> Finish
tail   -> Whistle
```

`custom.wav` was not audible at any slider edge.

The custom beatmap slider body/tick files included in this fixture were not observed during the slider.

## Project interpretation

For circles and spinners, the project uses the chosen legacy/osu!stable rule:

```text
explicit hitSample.filename
    -> custom sample only
```

For legacy sliders, the trailing `hitSample` is preserved as source data, but the official legacy parser applies only the sample-bank fields to slider sample generation. `index`, `volume`, and `filename` must not be treated as ordinary hit-object overrides for slider edge/body/tick logical resolution.

Slider edge sounds remain controlled by `edgeSounds` / `edgeSets` and timing-point sample index/volume behavior.

The osu!lazer-specific spinner traversal sound is outside the current canonical legacy model and is deferred.
