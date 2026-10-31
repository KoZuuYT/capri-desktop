# LOGS — Capri Desktop (mod pessoal)

> Leia este arquivo antes de qualquer coisa em uma sessão nova.

## O que é
Código decompilado (C#, .NET 8 WinForms/WPF) do programa que traz a **Capri** (Cyberpunk Edgerunners 2) para o desktop. Os termos do programa permitem modificações para **uso pessoal**, por isso o repositório é **privado** e nunca deve ser público nem redistribuído.

- Versão base do app: **0.7.7** (`src/Properties/AssemblyInfo.cs`)
- Fonte: `C:\Users\Usuário\AppData\Local\Temp\capri-decomp\src`
- Build de teste (o que roda): `C:\Users\Usuário\AppData\Local\Temp\capri-test\` (`Capri.exe.bak` = executável original)

## Fluxo de trabalho
1. Editar em `src/CapriDesktop/*.cs`
2. `dotnet build src/Capri.exe.csproj` (saída em `src/out/`; o normal são milhares de avisos CA1416 e **0 erros**)
3. Copiar `src/out/*` para `capri-test\` e testar
4. Atualizar `CHANGELOG.md` e a seção "Onde paramos" abaixo; commit + push

## Mapa dos sistemas (`src/CapriDesktop/`, 156 arquivos)
- **Núcleo/UI:** `Companion.cs` (~15 mil linhas, quase tudo passa por ele), `Program`, `Overlay`, `SpeechBox`, `TalkPill`, `TypeBox`, `TaskPanel`, `Notice`, `PetHearts`
- **IA:** `Brain`, `Agent*`, `ClaudeCode*`, `Claude*`, `Completion*`/`OpenAiRequest`, `Cloud*`, modelos locais (`Model*`), ferramentas (`ToolCall`, `AgentTool*`)
- **Voz/audição:** `Voice*`, `QwenVoice`, `LocalVoice`, `VoiceFx`, `Ears`, `Hearing`, `LocalEars`, `Dictation`, `WakeWord`, `Mic*`, `ISound*`, `SoundTap`
- **Música/dança:** `BeatLock`, `Tempo`, `Onset`, `Groove`, `Biquad`, `Tune*`, `MiniPlayer`
- **Desktop:** `Ledge*`, `Shelf`, `Sight`, `Glances`, `OpenWindow`, `Hands`, `HerCursor`, `HerPointer`, `Takeover`, `Computer`, efeitos (`Shatter`, `Tear`, `Neon`)
- **Outros:** Discord RPC (`Discord*`, `Presence`), `Updater`/`Release*`, `Startup`, `Preferences`, `SavedChat`, `Meeting`, `Note`, `Job*`
- **Recursos:** `src/Capri.Sprite.*.png` (poses idle/walk/dance/perch/laydown...), `src/Capri.Voice.*.wav`

## Onde paramos
- Projeto decompilado e recompilando sem erros (19 builds feitos). Repositório criado em 2026-10-31.
- **TODO:** registrar aqui as modificações e consertos já feitos antes do repo (o histórico das sessões anteriores não foi salvo).

## Pedidos pendentes
- (preencher)

## Armadilhas técnicas
- Aviso MSB3245 sobre "System.Core" no build é inofensivo.
- Cuidado: o `Updater` do app pode baixar uma versão oficial por cima do build modificado.
