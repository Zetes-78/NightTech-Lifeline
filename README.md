# Night Tech Lifeline

A free scam-rescue button for Windows 10 and 11, from [Night Tech Solutions](https://nighttech.solutions).

Fake "your computer is infected, call Microsoft" screens and tech-support scammers go after seniors and families every
day. Lifeline sits by the clock and gives the person two big buttons and a way to reach someone they trust.

**Download:** [NightTechLifeline-Setup.exe](https://github.com/Zetes-78/NightTech-Lifeline/releases/latest/download/NightTechLifeline-Setup.exe)
· [How to use it](https://nighttech.solutions/security-apps/#lifeline)

## What it does

| Button | What happens |
| --- | --- |
| **Ctrl + Alt + H** | Opens Lifeline on top of everything, even a full-screen fake warning (Ctrl + Alt + L if another program already uses H) |
| **Get me out of this scam screen** | Closes the person's web browsers (Chrome, Edge, Firefox, Brave, Vivaldi, Opera), then moves the browsers' reopen-my-tabs files aside so the scam page doesn't come straight back |
| **Someone is on my computer** | Closes remote-control apps running as the person (AnyDesk, TeamViewer, UltraViewer, ScreenConnect, RustDesk and others) and turns Wi-Fi off; anything installed as a Windows service is reported with "unplug the internet" |
| **Talk to a real person** | Call or text Night Tech, call one family member or friend set up in Settings, or open Night Tech's remote-support page |

It also shows the plain facts (Microsoft and banks never lock your screen) and what to do if the person already called,
let someone on, or paid.

## Safety

- Runs with the person's own rights. It never asks for administrator access and installs just for that person.
- Only closes programs in the person's own Windows session, by a fixed list of names. Night Tech Sentinel and Windows
  itself are never touched.
- Browser tabs are moved into `%LocalAppData%\Night Tech Solutions\Lifeline\Saved tabs`, one folder per rescue, never
  deleted. Links and junctions are never followed.
- Opens only Night Tech's own web pages and phone links, plus the family number (digits only).
- Nothing is sent over the internet. No accounts, ads or tracking. A short activity log stays on the PC for a
  technician helping the person.

Lifeline helps someone get out of a scam screen and cut off a scammer. It can't undo a payment or remove malware; for
that, call your bank and a technician.

## Build

Needs the .NET 8 SDK and Inno Setup 6.3+.

```powershell
.\build.ps1            # tests, publish, installer in dist\
dotnet run --project tests\NightTech.Lifeline.CoreTests   # just the tests
```

`src/NightTech.Lifeline.Core` holds the lists, the session-file finder and the words, with no Windows calls, so the
tests run anywhere. `src/NightTech.Lifeline` is the Windows app (WPF): `Windows/Rescuer.cs` closes browsers and moves tabs,
`Windows/CutOff.cs` closes remote-control apps and turns off Wi-Fi.

## License

MIT. Windows and Microsoft Edge are trademarks of Microsoft; other names are trademarks of their owners. Night Tech
Solutions isn't affiliated with any of them.

---

Need a hand? Call Night Tech Solutions at (864) 381-8510, text (864) 497-2701, or visit
[nighttech.solutions](https://nighttech.solutions).
