<p align="center">
  <img src="src/HomiKey.App/Assets/homikey.png" width="96" height="96" alt="HomiKey icon">
</p>

<h1 align="center">HomiKey</h1>

<p align="center">
  Fix text typed in the wrong keyboard layout, Persian ↔ English, with one hotkey.<br>
  <a href="README.fa.md">فارسی</a> ·
  <a href="https://github.com/amir2139/HomiKey/releases/latest">Download</a> ·
  <a href="https://amir2139.github.io/HomiKey/">Web version</a>
</p>

<p align="center">
  <a href="https://github.com/amir2139/HomiKey/actions/workflows/ci.yml"><img src="https://github.com/amir2139/HomiKey/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="MIT license"></a>
</p>

You start typing, look up, and the keyboard was in the other language:

| You typed  | You meant |
|------------|-----------|
| `sghl o,fd?` | `سلام خوبی؟` |
| `اثممخ`    | `hello`   |

Select it, press **Ctrl+Shift+Space**, and HomiKey retypes it as if the right layout had been on.

## Features

- **One hotkey, any app.** Converts the selected text in Notepad, Word, browsers, chat apps and anything else that supports copy and paste. The default hotkey is Ctrl+Shift+Space, and you can change it.
- **Detects the direction.** HomiKey looks at which script most of the text is in and converts Latin → Persian or Persian → Latin on its own. In mixed text, only the wrong-layout part changes.
- **Press again to undo.** A second press within 30 seconds puts back exactly what was there before.
- **Nothing selected? It finds the mistake.** With nothing selected, the hotkey goes back from the cursor over every word typed in the wrong layout and fixes them together: `چطوری sghl phgj` becomes `چطوری سلام حالت`. It stops at a word in the other language, the start of the line, or a word ending in `.` or `!`. A separate last-word hotkey (Pause by default) fixes only the last word.
- **Persian layouts.** Supports the standard Persian layout (ISIRI 9147), the old Windows Persian layout and US QWERTY, including Shift keys, the half-space (ZWNJ), Persian digits ۰–۹ and the punctuation ، ؛ ؟ « ». HomiKey works out which Persian layout you have installed.
- **Optional language switch.** Can switch the Windows input language to match the text it just produced.
- **Your clipboard stays yours.** HomiKey saves the clipboard, uses it briefly, then restores it. Its temporary text is kept out of Windows clipboard history (Win+V).
- **Per-app exclude list.** Turn HomiKey off in programs where you don't want it. Terminals and password managers are excluded by default, because Ctrl+C means something else there.
- **Persian and English interface.** Fully translated and switchable at runtime. By default it follows your Windows display language. The Persian interface is right-to-left and uses the Vazirmatn font.
- **Tray app.** No window in your way. It can run when you sign in to Windows.
- **Private.** No telemetry, no update checks, no network access at all.
- **Web version.** The same converter runs in the browser at <https://amir2139.github.io/HomiKey/>, for phones, macOS and Linux.

## Install

1. Download `HomiKey-<version>-win-x64.exe` from the [latest release](https://github.com/amir2139/HomiKey/releases/latest).
2. Move it somewhere permanent, for example `C:\Users\<you>\Apps\HomiKey.exe`.
3. Run it. The HomiKey icon appears in the notification area next to the clock.

There is no installer. The exe is self-contained and needs nothing else installed. It runs on 64-bit Windows 10 and 11.

The exe is not code-signed, so Windows SmartScreen may warn you the first time. Choose **More info → Run anyway**. You can check the download against the `.sha256` file next to it in the release.

## Usage

| Do this | To get this |
|---|---|
| Select text, press **Ctrl+Shift+Space** | The selection is converted |
| Press **Ctrl+Shift+Space** again | The original text comes back |
| Press **Ctrl+Shift+Space** with nothing selected | The words before the cursor that were typed in the wrong layout are converted |
| Press **Pause** | The last word you typed is converted |
| Double-click the tray icon | Settings |
| Right-click the tray icon | Turn HomiKey on/off, language, run at startup, exit |

In **Settings** you can change both hotkeys (click the box and press the new combination), choose your Persian layout, turn digit conversion and input-language switching on or off, edit the exclude list, and try conversions live.

Settings are stored in `%APPDATA%\HomiKey\settings.json`.

## Supported layouts

| Layout | Windows name | Notes |
|---|---|---|
| US QWERTY | English (United States), US | The Latin side |
| ISIRI 9147 | Persian (Standard) | پ on M, ZWNJ on Shift+B (and Shift+Space) |
| Legacy | Persian | پ on `\`, ئ on M, ريال on Shift+R |

When converting Persian → Latin, characters that only exist on the other Persian layout are still recognized.

## Build from source

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). The app itself only runs on Windows, but the converter library and its tests build anywhere.

```powershell
git clone https://github.com/amir2139/HomiKey.git
cd HomiKey
dotnet test                                   # Core tests
dotnet run --project src/HomiKey.App          # run the tray app (Windows)

# The same single-file exe the release workflow builds:
dotnet publish src/HomiKey.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

The web version is plain HTML, CSS and JavaScript in [`web/`](web). Open `web/index.html` through any static file server, for example `npx serve web`. Test it with `node web/test.mjs`.

### Project layout

```
src/HomiKey.Core/         conversion library (no Windows dependencies)
src/HomiKey.App/          WinForms tray app
  Resources/Strings*.resx all interface text (English, Persian)
  Assets/                 icons, Vazirmatn font
tests/HomiKey.Core.Tests/ xUnit tests
tests/vectors.json        conversion cases shared by the C# and web tests
web/                      static web version (GitHub Pages)
```

### Releases

Pushing a tag such as `v1.0.0` runs the release workflow: it runs the tests, builds the self-contained single-file `win-x64` exe and attaches it, with its SHA-256, to a new GitHub Release. The web version is deployed to GitHub Pages by running the *Web version (GitHub Pages)* workflow by hand from the Actions tab. Enable Pages with source "GitHub Actions" in the repository settings first; that needs a public repository or a paid plan.

## FAQ

**Nothing happens when I press the hotkey.**
Check that the tray icon is blue (enabled) and that the program isn't in the exclude list. If another program already uses the hotkey, HomiKey says so when it starts; pick another one in Settings. Windows doesn't let normal programs send keys to programs running as administrator, so HomiKey can't convert text in those unless you also run it as administrator.

**The wrong letters come out.**
Open Settings and check *My Persian keyboard layout*. HomiKey guesses it from the installed layouts on first run. You can see yours in Windows Settings → Time & language → Language & region → Persian → Language options.

**Why does HomiKey use the clipboard?**
Copy and paste is the one way to read and replace selected text that works in nearly every Windows program. HomiKey saves what was on the clipboard and restores it right after pasting, and marks its own temporary text so Windows clipboard history skips it. If something else is copied during that moment, HomiKey keeps the new content instead. A few special formats that the source program only produces on request (such as Office "paste link" data) cannot be saved, so after a conversion they are gone from the clipboard; plain text, rich text, images and files come back.

**Why is the terminal excluded?**
In a terminal, Ctrl+C with nothing selected stops the running program. Remove the terminal from the exclude list if you only ever use HomiKey there on a selection.

**In a code editor with nothing selected, a whole line gets duplicated.**
These editors copy the whole line when you press Ctrl+C with nothing selected. HomiKey recognizes this in Visual Studio and VS Code and converts the last word instead. Other editors that do the same (for example JetBrains IDEs) are not recognized; select the text first there, or turn the line copy off in the editor's settings.

**With nothing selected, it converted words I had typed correctly.**
HomiKey tells the two layouts apart by script, not with a dictionary. So when a correct word and a wrong one are in the same script, for example `I said sghl` (meant as "I said سلام"), it can't know where the mistake starts and converts both. Press **Pause** to convert only the last word, or select the text you want converted.

**Converting without a selection removed formatting in Word.**
To fix the words before the cursor, HomiKey selects back to the start of the line and pastes that span back with only the converted words changed. This behaves the same in left-to-right and right-to-left text, but in rich editors the pasted part is plain text. Select the words first to keep formatting.

**My antivirus doesn't like it.**
Programs that send keystrokes to other programs sometimes get flagged. HomiKey only sends Ctrl+C, Ctrl+V and Shift+Home, and only when you press its hotkey. The source is here, and you can build the exe yourself.

**Does it record what I type?**
No. HomiKey has no keyboard hook. It only reads text when you press the hotkey, keeps it in memory just long enough to convert it, and never sends anything anywhere.

**How do I uninstall it?**
Untick *Run at startup*, exit HomiKey from the tray menu, then delete the exe and the `%APPDATA%\HomiKey` folder.

**macOS or Linux?**
Use the [web version](https://amir2139.github.io/HomiKey/).

## License

HomiKey is released under the [MIT license](LICENSE).
The Persian interface uses the [Vazirmatn](https://github.com/rastikerdar/vazirmatn) font, licensed under the SIL Open Font License 1.1. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
