# Android build — رماد الرافدين

The game remains the existing custom WebGL/JavaScript project. Capacitor wraps the built game for Android; it does not replace the renderer or rewrite gameplay.

## Requirements

- Node.js 22 or newer
- Python 3
- Android Studio with an Android SDK (API 24 or newer)
- A physical Android device with an up-to-date Android System WebView, or an Android emulator

## First-time setup

From the repository root:

```bash
npm install
npm run android:init
```

The first command installs the pinned major-version Capacitor dependencies. The second builds `dist/index.html` and creates the native `android/` project.

Then open Android Studio:

```bash
npm run android:open
```

Wait for Gradle sync, select an emulator or connected Android device, and run the app.

## Updating the game

After changing any game source file:

```bash
npm run android:sync
```

This rebuilds the single-page game and copies the updated web assets into the native Android project.

To launch on a connected device from the command line:

```bash
npm run android:run
```

## Creating an APK

In Android Studio, use **Build → Build Bundle(s) / APK(s) → Build APK(s)** for a test APK.

For a release APK, use **Build → Generate Signed Bundle / APK**, create or select a release keystore, and keep the keystore and passwords outside the repository. Do not commit signing credentials.

## Important validation before release

- Test WebGL rendering and frame pacing on a real Android phone; CI's SwiftShader tests are not a substitute for device testing.
- Verify touch controls, safe-area layout, audio unlock after the first tap, Arabic speech availability, save/load, background/resume, and Android back-button behavior.
- Test at least one lower-memory device and one current device.
- Add final launcher icons, splash artwork, version code/name, and release signing configuration before publishing.

## Build scripts

- `npm run build`: creates `dist/index.html` using the existing Python build.
- `npm run android:init`: first-time Android platform generation.
- `npm run android:sync`: rebuilds and syncs web assets.
- `npm run android:open`: opens the native project in Android Studio.
- `npm run android:run`: builds, syncs, and launches on a connected device/emulator.
