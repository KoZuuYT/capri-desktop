# LOGS — Capri Desktop (mod pessoal)

> Leia este arquivo antes de qualquer coisa em uma sessão nova.

## O que é
Código decompilado (C#, .NET 8 WinForms/WPF) do programa que traz a **Capri** (Cyberpunk Edgerunners 2) para o desktop. Os termos do programa permitem modificações para **uso pessoal**, por isso o repositório é **privado** e nunca deve ser público nem redistribuído.

- Versão base do app: **0.7.7** (`src/Properties/AssemblyInfo.cs`)
- Pasta geral: `D:\usuario alternativo\Documents\Capri Desktop\`
  - `capri-decomp\` — fonte + git (`src\`)
  - `capri-test\` — build modificado que roda (`Capri.exe.bak` = executável original)
  - `capri-normal\` — cópia do `Capri.exe`/`Uninstall.exe` originais 0.7.7
- Instalação real e dados (memórias, chat, modelos, ~6 GB): `C:\Users\Usuário\AppData\Local\Capri` — **não mover**: o app procura os dados em `%LOCALAPPDATA%\Capri` (fixo no código, `Program.cs:48`)

## Fluxo de trabalho
1. Editar em `src/CapriDesktop/*.cs`
2. `dotnet build src/Capri.exe.csproj -c Release -o src/out` (o normal são milhares de avisos CA1416 e **0 erros**). **Sempre com `-o src/out`**: sem isso a saída vai para `src/bin/Debug/...` e o capri-test continua com o executável velho
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
- Feito antes do repo: **tradução da Capri ajustada** e **EyePopup adicionado** (`EyePopup.cs` + `src/EyePopup.png`).
- 2026-10-31: projeto movido de `AppData\Local\Temp` para `D:\usuario alternativo\Documents\Capri Desktop`.
- 2026-10-31: **rework do EyePopup** (`EyePopup.cs`, `CheckEyePopup` em `Companion.cs`, cues `EyeOpen`/`EyeClose` em `Cue.cs`/`Sfx.cs`). O olho agora é desenhado por código (o `EyePopup.png` não é mais usado), em 4 humores (semicerrado, sonolento, arregalado, normal) com tamanho, inclinação e paleta aleatórios. Animação de boot e shutdown com glitch, olhar que segue o cursor e piscadas. A janela é transparente pixel a pixel, os cliques passam por ela e ela nunca pega foco. Aparece a cada 25–75 s; em 1 de cada 5 vezes vem um grupo de 2–3 olhos. Testado: o olho aparece corretamente na tela.
- 2026-10-31: olho redesenhado no estilo da arte da Capri (visor verde com 4 formas, olho preto com espinhos, anel verde, íris azul, pupila branca). Animações e sons mantidos.
- 2026-10-31: caixinha irregular e balançando; seção EYES no Mainframe (`Mainframe.cs`, coluna direita y=790; página aumentada de 860 para 960). Preferências `EyePace`, `EyeSize`, `EyeMost` em `Preferences.cs`.
- 2026-10-31: rework do carinho (`PetHearts.cs` virou janela layered com tipos Stroke/React; cues `PetStroke`/`PetHappy`/`PetPurr`/`PetEnd` em `Cue.cs`/`Sfx.cs`; `PurrLines` e `petPurr` em `Companion.cs`, perto de `Stroke`/`Petted`). Ronrona em 35% dos carinhos.
- 2026-10-31: detecção do carinho menos sensível (`petSwing` em `Stroke`: ida >= 30 px, viradas em até 700 ms, 6 viradas).
- 2026-10-31: TalkPill posicionado ao lado do sprite (método que chama `pill.Place` em `Companion.cs`).

## Pedidos pendentes
- (preencher)

## Armadilhas técnicas
- Aviso MSB3245 sobre "System.Core" no build é inofensivo.
- Captura de tela comum (`CopyFromScreen`) não mostra janelas layered como o EyePopup; use BitBlt com `CAPTUREBLT` (0x40000000) para verificar.
- Cuidado: o `Updater` do app pode baixar uma versão oficial por cima do build modificado.
